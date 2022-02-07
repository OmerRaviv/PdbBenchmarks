using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DiaSymReader;
using PdbReadingBenchmarks.Contracts;

namespace PdbReadingBenchmarks.DiaNativeSymReader
{
    public class DiaSymReaderPdbReader : IDebugInfoProvider
    {
        private readonly string _pdbFullPath;


        public DiaSymReaderPdbReader(string pdbFullPath)
        {
            _pdbFullPath = pdbFullPath;
        }

        public (IList<SequencePoint> sequencePoints, IList<Variable> variables) GetDebugInfo(int methodMetadataToken)
        {
            _symReader ??= CreateNativeSymReader();
            var symUnmanagedMethod = GetMethod(methodMetadataToken);
     ////       var documents = symUnmanagedMethod.GetDocumentsForMethod();
     ////       var urlBytes = new char[2000];
     ////        documents.First().GetUrl(2000, out int count, urlBytes);
      //       string url = new string(urlBytes, 0, count -1);
            return (GetSequencePoints(symUnmanagedMethod), GetLocalVariables(symUnmanagedMethod));
        }
        public static class HResults
        {
            public static readonly int S_OK = 0;
        }

        public void CheckForError(int hresult, string nameOfOperation)
        {
            if (hresult != HResults.S_OK)
            {
                throw new ApplicationException($"{nameOfOperation} failed with HResult {hresult:X}");
            }
        }
        public (int methodToken, int ilOffset, List<Variable> locals) GetILOffsetAndLocalsFromDocumentPosition(string filePath, int line, int column)
        {
            _symReader ??= CreateNativeSymReader();
            ISymUnmanagedDocument? document = _symReader.GetDocument(filePath);


            CheckForError(_symReader.GetMethodFromDocumentPosition(document, line, column, out var method), "GetMethodFromDocumentPosition");
            CheckForError(method.GetOffset(document, line, column, out int bytecodeOffset), "GetOffset");
            CheckForError(method.GetToken(out int token), "GetToken");

            var localVariablesInScope = GetLocalVariables(method,bytecodeOffset);
            
            return (token, bytecodeOffset,localVariablesInScope);

        }
        
        private List<Variable> GetLocalVariables(ISymUnmanagedMethod method, int? bytecodeOffset = null)
        {
            var rootScope = method.GetRootScope();
            var childScopes = rootScope.GetChildren();
            return GetVariablesFromScope(childScopes.Single(), bytecodeOffset);

        }

        private static List<Variable> GetVariablesFromScope(ISymUnmanagedScope rootScope, int? bytecodeOffset)
        {
            var locals = rootScope.GetLocals();
            var result = new List<Variable>(locals.Length);
            for (var index = 0; index < locals.Length; index++)
            {
                var localVariable = locals[index];
                string name = localVariable.GetName();
                
                if (bytecodeOffset.HasValue &&
                    (bytecodeOffset.Value > rootScope.GetEndOffset() ||
                     bytecodeOffset.Value < rootScope.GetStartOffset()))
                {
                    continue; // Variable is not in scope
                }
                result.Add(new Variable(index, name));
                
            }

            return result;
        }

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("Microsoft.DiaSymReader.Native.x86.dll", EntryPoint = "CreateSymReader")]
        private extern static void CreateSymReader32(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("Microsoft.DiaSymReader.Native.amd64.dll", EntryPoint = "CreateSymReader")]
        private extern static void CreateSymReader64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symReader);

        private static ISymUnmanagedReader3 _symReader;


        private IList<SequencePoint> GetSequencePoints(ISymUnmanagedMethod method)
        {
            IEnumerable<SymUnmanagedSequencePoint> sequencePoints = 
                SymUnmanagedExtensions.GetSequencePoints(method);
            
            Marshal.ThrowExceptionForHR(method.GetSequencePointCount(out int count));
            
            var result = new SequencePoint[count];
            var enumerator = sequencePoints.GetEnumerator();
            
            for (var i = 0; i < count; i++)
            {
                
                var sp = enumerator.Current;
                
                result[i] = new Contracts.SequencePoint()
                {
                    
                    Offset = sp.Offset,
                    StartColumn = sp.StartColumn,
                    EndColumn = sp.EndColumn,
                    StartLine = sp.StartLine,
                    EndLine = sp.EndLine,
                    DocumentUrl = GetUrl(sp.Document) 
                };
                
                if (!enumerator.MoveNext())
                {
                    throw new InvalidDataException(
                        $"Expected method to have {count} sequence points but it only had {i}");
                }
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

        private  ISymUnmanagedReader3 CreateNativeSymReader()
        {
            var pdbStream = File.OpenRead(_pdbFullPath);
            pdbStream.Position = 0;

            object symReader = null;
            var guid = default(Guid);
            if (IntPtr.Size == 4)
            {
                CreateSymReader32(ref guid, out symReader);
            }
            else
            {
                CreateSymReader64(ref guid, out symReader);
            }
            var reader = (ISymUnmanagedReader3)symReader;
            var hr = reader.Initialize(new DummyMetadataImport(), null, null, new ComStreamWrapper(pdbStream));
            Marshal.ThrowExceptionForHR(hr);
            return reader;
        }

        public void Dispose()
        {
            ((ISymUnmanagedDispose) _symReader).Destroy();
        }
    }
}