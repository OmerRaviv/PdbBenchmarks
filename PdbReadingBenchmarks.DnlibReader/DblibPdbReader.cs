using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Pdb.Symbols;
using PdbReadingBenchmarks.Contracts;
using SequencePoint = PdbReadingBenchmarks.Contracts.SequencePoint;

namespace PdbReadingBenchmarks.DnlibReader
{

    public class DnlibPdbReader : PdbReadingBenchmarks.Contracts.IDebugInfoProvider
    {
        private readonly string _assemblyFullPath;
        private readonly string _pdbFullPath;
        private static ModuleDefMD _module;
        private static SymbolReader _reader;


        public DnlibPdbReader(string assemblyFullPath, string pdbFullPath)
        {
            _assemblyFullPath = assemblyFullPath;
            _pdbFullPath = pdbFullPath;
            
            
            _module ??= ModuleDefMD.Load(_assemblyFullPath,
                new ModuleCreationOptions { PdbOptions = PdbReaderOptions.MicrosoftComReader});
            
            _reader ??= (SymbolReader) _module.GetType()
                .GetMethod("CreateSymbolReader", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(_module, new object[]
                {
                    new ModuleCreationOptions { PdbOptions = PdbReaderOptions.MicrosoftComReader }
                });

        }

        public (int methodToken, int ilOffset, List<Variable> locals) GetILOffsetAndLocalsFromDocumentPosition(
            string filePath, int line, int column)
        {
            for (var i = 0; i < _reader.Documents.Count; i++)
            {
                if (_reader.Documents[i].URL == filePath)
                {
                    
                }
            }

            //       SymGetFileLineOffsets64( hProcess,Path.GetFileName(_assemblyFullPath),filePath )
            return (default, default,default);
        }

        
        public (IList<SequencePoint> sequencePoints, IList<Variable> variables) GetDebugInfo(int methodMetadataToken)
        {
            MethodDef? methodDef = _module.ResolveMethod(MDToken.ToRID(methodMetadataToken));
            // SymbolMethod method = _reader.GetMethod(methodDef, 1);
            //
            // var variables = method.RootScope.Children.First().Locals.Select(v => new Variable(v.Index, v.Name)).ToList();
            // var sequencePoints = method.SequencePoints.Select(s => new SequencePoint()
            // {
            //     DocumentUrl = s.Document.URL,
            //     StartLine = s.Line,
            //     EndLine = s.EndLine,
            //     StartColumn = s.Column,
            //     EndColumn = s.EndColumn,
            //     Offset = s.Offset
            // }).ToList();
            var variables = methodDef.Body.PdbMethod.Scope.Scopes.First().Variables.Select(v => new Variable(v.Index, v.Name)).ToList();
            ;
            var sequencePoints = methodDef.Body.Instructions.Select(i => i.SequencePoint).Where(s => s is not null).Distinct<dnlib.DotNet.Pdb.SequencePoint>(new dnlibSpComparer()).Select(s => new SequencePoint()
            {
                DocumentUrl = s.Document.Url,
                StartLine = s.StartLine,
                EndLine = s.EndLine,
                StartColumn = s.StartColumn,
                EndColumn = s.EndColumn,
                
            }).ToList();
            return (sequencePoints, variables);
            //return (sequencePoints, variables);
        }
    }

    public class dnlibSpComparer : IEqualityComparer<dnlib.DotNet.Pdb.SequencePoint>
    {
        public bool Equals(dnlib.DotNet.Pdb.SequencePoint x, dnlib.DotNet.Pdb.SequencePoint y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return Equals(x.Document, y.Document) && x.StartLine == y.StartLine && x.StartColumn == y.StartColumn && x.EndLine == y.EndLine && x.EndColumn == y.EndColumn;
        }

        public int GetHashCode(dnlib.DotNet.Pdb.SequencePoint obj)
        {
            unchecked
            {
                var hashCode = (obj.Document != null ? obj.Document.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ obj.StartLine;
                hashCode = (hashCode * 397) ^ obj.StartColumn;
                hashCode = (hashCode * 397) ^ obj.EndLine;
                hashCode = (hashCode * 397) ^ obj.EndColumn;
                return hashCode;
            }
        }
    }

    public class SPEqualityCompare : IEqualityComparer<SequencePoint>
    {
        public bool Equals(SequencePoint x, SequencePoint y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Offset == y.Offset && x.EndOffset == y.EndOffset && x.StartLine == y.StartLine && x.StartColumn == y.StartColumn && x.EndLine == y.EndLine && x.EndColumn == y.EndColumn && x.DocumentUrl == y.DocumentUrl;
        }

        public int GetHashCode(SequencePoint obj)
        {
            unchecked
            {
                var hashCode = obj.Offset;
                hashCode = (hashCode * 397) ^ obj.EndOffset;
                hashCode = (hashCode * 397) ^ obj.StartLine;
                hashCode = (hashCode * 397) ^ obj.StartColumn;
                hashCode = (hashCode * 397) ^ obj.EndLine;
                hashCode = (hashCode * 397) ^ obj.EndColumn;
                hashCode = (hashCode * 397) ^ obj.DocumentUrl.GetHashCode();
                return hashCode;
            }
        }
    }
}