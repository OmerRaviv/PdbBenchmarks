using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DiaSymReader;
using PdbReadingBenchmarks.Contracts;

namespace PdbReadingBenchmarks.DiaNativeSymReader
{
    public class DiaSymReaderPdbReader : IDebugInfoProvider
    {
        private readonly string _pdbFullPath;
        private readonly string _assemblyFullPath;


        public DiaSymReaderPdbReader(string pdbFullPath, string assemblyFullPath)
        {
            _pdbFullPath = pdbFullPath;
            _assemblyFullPath = assemblyFullPath;
        }

        public MethodDebugInfo GetDebugInfo(int methodMetadataToken)
        {
            _symReader ??= CreateNativeSymReader();
            var symUnmanagedMethod = GetMethod(methodMetadataToken);
            return new MethodDebugInfo(GetSequencePoints(symUnmanagedMethod), GetLocalVariables(symUnmanagedMethod));
        }

        private static class HResults
        {
            public static readonly int S_OK = 0;
        }

        private void CheckForError(int hresult, string nameOfOperation)
        {
            if (hresult != HResults.S_OK)
            {
                throw new ApplicationException($"{nameOfOperation} failed with HResult {hresult:X}");
            }
        }
        public LineDebugInfo GetILOffsetAndLocals_FromDocumentPosition(string filePath, int line, int column)
        {
            _symReader ??= CreateNativeSymReader();
            ISymUnmanagedDocument? document = _symReader.GetDocument(filePath);
            
            CheckForError(_symReader.GetMethodFromDocumentPosition(document, line, column, out var method), "GetMethodFromDocumentPosition");
            CheckForError(method.GetOffset(document, line, column, out int bytecodeOffset), "GetOffset");
            CheckForError(method.GetToken(out int token), "GetToken");

            var localVariablesInScope = GetLocalVariables(method,bytecodeOffset);
            
            return new LineDebugInfo(token, bytecodeOffset,localVariablesInScope);

        }
        
        private List<Variable> GetLocalVariables(ISymUnmanagedMethod method, int? bytecodeOffset = null)
        {
            return GetScopes(method, bytecodeOffset).SelectMany(s => s.GetLocals()
                .Select(l => new Variable(0, l.GetName()))).ToList();
        }
        
        void RetrieveScopes(ISymUnmanagedScope scope, int? bytecodeOffset, List<ISymUnmanagedScope> result)
        {
            if (scope == null) return;
            if (bytecodeOffset.HasValue &&
                (bytecodeOffset.Value > scope.GetEndOffset() ||
                 bytecodeOffset.Value < scope.GetStartOffset()))
            {
                return;
            }
            
            result.Add(scope);

            foreach (var nestedScope in scope.GetChildren())
            {
                RetrieveScopes(nestedScope, bytecodeOffset, result);
            }
        }


        List<ISymUnmanagedScope> GetScopes(ISymUnmanagedMethod method, int? bytescodeOffset)
        {
        
            var result = new List<ISymUnmanagedScope>();
            RetrieveScopes(method.GetRootScope(), bytescodeOffset, result);
            return result;
        }



        
        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("Microsoft.DiaSymReader.Native.x86.dll", EntryPoint = "CreateSymReader")]
        private extern static void CreateSymReader32(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("Microsoft.DiaSymReader.Native.amd64.dll", EntryPoint = "CreateSymReader")]
        private extern static void CreateSymReader64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symReader);

        private ISymUnmanagedReader3 _symReader;


        private IList<SequencePoint> GetSequencePoints(ISymUnmanagedMethod method)
        {
            var sequencePoints = SymUnmanagedExtensions.GetSequencePoints(method);
            
            Marshal.ThrowExceptionForHR(method.GetSequencePointCount(out int count));
            
            var result = new List<SequencePoint>(count);
            using var enumerator = sequencePoints.GetEnumerator();
            
            while (enumerator.MoveNext())
            {
                SymUnmanagedSequencePoint sp = enumerator.Current;
                if (sp.IsHidden || sp.IsEmpty()) continue;

                result.Add(new SequencePoint()
                    {
                        Offset = sp.Offset,
                        StartColumn = sp.StartColumn,
                        EndColumn = sp.EndColumn,
                        StartLine = sp.StartLine,
                        EndLine = sp.EndLine,
                        DocumentUrl = GetUrl(sp.Document)
                    });
            }

            return result;

        }

        private string GetUrl(ISymUnmanagedDocument doc)
        {
            if (doc is null) return null;
            
            CheckForError(doc.GetUrl(0, out int urlLength, null), "GetUrl");

            // urlLength includes terminating '\0'
            char[] urlBuffer = new char[urlLength];
            CheckForError(doc.GetUrl(urlLength, out urlLength, urlBuffer), "GetUrl");

            return new string(urlBuffer, 0, urlLength - 1);
        }
        private ISymUnmanagedMethod GetMethod(int methodToken)
        {
            ISymUnmanagedMethod method;
            _symReader.GetMethod(methodToken, out method);
            return method;
        }


        private ISymUnmanagedReader5 CreateNativeSymReader()
        {
            var pdbStream = File.OpenRead(_pdbFullPath);
            var peStream = File.OpenRead(_assemblyFullPath);
            pdbStream.Position = 0;
            bool isPortable = pdbStream.ReadByte() == 'B' && pdbStream.ReadByte() == 'S' && pdbStream.ReadByte() == 'J' && pdbStream.ReadByte() == 'B';
            pdbStream.Position = 0;

            var metadataProvider = new SymMetadataProvider(peStream);

            if (isPortable)
            {
                return (ISymUnmanagedReader5)new  Microsoft.DiaSymReader.PortablePdb.SymBinder().GetReaderFromStream(
                    pdbStream, 
                    SymUnmanagedReaderFactory.CreateSymReaderMetadataImport(metadataProvider));
            }
            else
            {
                return SymUnmanagedReaderFactory.CreateReader<ISymUnmanagedReader5>(pdbStream, metadataProvider);
            }
        }
        // private  ISymUnmanagedReader3 CreateNativeSymReader()
        // {
        //     var pdbStream = File.OpenRead(_pdbFullPath);
        //     pdbStream.Position = 0;
        //
        //     object symReader = null;
        //     var guid = default(Guid);
        //     if (IntPtr.Size == 4)
        //     {
        //         CreateSymReader32(ref guid, out symReader);
        //     }
        //     else
        //     {
        //         CreateSymReader64(ref guid, out symReader);
        //     }
        //     var reader = (ISymUnmanagedReader3)symReader;
        //     var hr = reader.Initialize(new DummyMetadataImport(), null, null, new ComStreamWrapper(pdbStream));
        //     Marshal.ThrowExceptionForHR(hr);
        //     return reader;
        // }

        public void Dispose()
        {
            ((ISymUnmanagedDispose) _symReader).Destroy();
        }
    }
    static class SymUnmanagedSequencePointExtensions
    {
        public static bool IsEmpty(this SymUnmanagedSequencePoint sp) =>
            sp.EndColumn == 0 && sp.EndLine == 0 && sp.StartLine == 0 && sp.StartColumn == 0;

    }
}