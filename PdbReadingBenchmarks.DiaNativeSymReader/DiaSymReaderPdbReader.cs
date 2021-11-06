using System;
using System.Collections.Generic;
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
            var pdbStream = File.OpenRead(_pdbFullPath);
            pdbStream.Position = 0;

            _symReader = CreateNativeSymReader(pdbStream);
            var symUnmanagedMethod = GetMethod(methodMetadataToken);
            return (GetSequencePoints(symUnmanagedMethod), GetLocalVariables(symUnmanagedMethod));
        }

        private IList<Variable> GetLocalVariables(ISymUnmanagedMethod method)
        {
            var rootScope = method.GetRootScope();
            var childScopes = rootScope.GetChildren();
            int childScopesCount = childScopes.Length;
            var result = GetVariablesFromScope(childScopes.Single());

            return result;

        }

        private static Variable[] GetVariablesFromScope(ISymUnmanagedScope rootScope)
        {
            var locals = rootScope.GetLocals();
            var result = new Variable[locals.Length];
            for (var index = 0; index < locals.Length; index++)
            {
                var localVariable = locals[index];
                string name = localVariable.GetName();
                result[index] = new Variable(index, name);
            }

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
                    //DocumentUrl = sp.Document.GetUrl()
                };
                
                if (!enumerator.MoveNext())
                {
                    throw new InvalidDataException(
                        $"Expected method to have {count} sequence points but it only had {i}");
                }
            }

            return result;

        }

        private ISymUnmanagedMethod GetMethod(int methodToken)
        {
            ISymUnmanagedMethod method;
            _symReader.GetMethod(methodToken, out method);
            return method;
        }

        private static ISymUnmanagedReader3 CreateNativeSymReader(Stream pdbStream)
        {
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