using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private Lazy<Dictionary<string, List<MethodLineExtent>>> _methodExtentsByDocument = 
            new(CalculateMethodExtentsByDocument);

        private static Dictionary<string, List<MethodLineExtent>> CalculateMethodExtentsByDocument()
        {
            Dictionary<string, List<MethodLineExtent>> methodExtentsByDocument = new();
            foreach (var types in _module.Types)
            {
                foreach (MethodDef method in types.Methods)
                {
                    if (!method.HasBody || !method.Body.HasPdbMethod) continue;
                    var methodScope = method.Body.PdbMethod.Scope;
                    var symbolMethod = _reader.GetMethod(method, 1);
                    int minLine = int.MaxValue, maxLine = int.MinValue;

                    string document = null;
                    foreach (var sp in symbolMethod.SequencePoints)
                    {
                        document ??= sp.Document.URL;
                        if (sp.Line < minLine) minLine = sp.Line;
                        if (sp.EndLine > maxLine) maxLine = sp.EndLine;
                    }

                    if (document == null) continue;
                    if (!methodExtentsByDocument.TryGetValue(document, out var methodExtentsInDoc))
                    {
                        methodExtentsByDocument[document] = methodExtentsInDoc = new List<MethodLineExtent>();
                    }
                    
                    methodExtentsInDoc.Add(new MethodLineExtent(method.Rid,1, minLine, maxLine));
                }
            }

            return methodExtentsByDocument;

        }

        public (int methodToken, int ilOffset, List<Variable> locals) GetILOffsetAndLocals_FromDocumentPosition(
            string filePath, int line, int column)
        {
            if (!_methodExtentsByDocument.Value.TryGetValue(filePath, out var methodExtentsInDoc))
            {
                return (default, default, default);
            }

            foreach (var methodLineExtent in methodExtentsInDoc)
            {
                if (line > methodLineExtent.MinLine && line < methodLineExtent.MaxLine)
                {
                    var method = _module.ResolveMethod(methodLineExtent.Method);
                    var symbolMethod = _reader.GetMethod(method, 1);
                    foreach (var sp in symbolMethod.SequencePoints)
                    {
                        if (sp.Line <= line &&
                            sp.EndLine >= line &&
                            sp.Column >= column &&
                            sp.EndColumn >= column)
                        {
                            return (methodToken: (int)method.MDToken.Raw, 
                                    ilOffset:    sp.Offset,
                                    locals:      GetVariablesInScope(symbolMethod, sp.Offset));
                        }
                    }

                    
                }
            }
            return (default, default, default);

            //       SymGetFileLineOffsets64( hProcess,Path.GetFileName(_assemblyFullPath),filePath )
        }

        private List<Variable> GetVariablesInScope(SymbolMethod method, int offset)
        {
            var smallestContainingScope = method.RootScope.Children
                .FirstOrDefault(s => s.StartOffset <= offset && 
                                               s.EndOffset >= offset) ??
                                          method.RootScope;
            
            return new List<Variable>(smallestContainingScope.Locals.Select(v => 
                new Variable(v.Index, v.Name)));
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
            var sequencePoints = methodDef.Body
                .Instructions
                .Where(i => i.SequencePoint is not null)
                .GroupBy(i => i.SequencePoint, new dnlibSpComparer())
                .Select(s => new SequencePoint()
            {
                DocumentUrl = s.Key.Document.Url,
                StartLine = s.Key.StartLine,
                EndLine = s.Key.EndLine,
                StartColumn = s.Key.StartColumn,
                EndColumn = s.Key.EndColumn,
                Offset = (int)s.Min(i => i.Offset)
                
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
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal readonly struct MethodLineExtent
    {
        internal sealed class MethodComparer : IComparer<MethodLineExtent>
        {
            public static readonly MethodComparer Instance = new MethodComparer();
            public int Compare(MethodLineExtent x, MethodLineExtent y) => x.Method.CompareTo(y.Method);
        }

        internal sealed class MinLineComparer : IComparer<MethodLineExtent>
        {
            public static readonly MinLineComparer Instance = new MinLineComparer();
            public int Compare(MethodLineExtent x, MethodLineExtent y) => x.MinLine - y.MinLine;
        }

        public readonly uint Method;
        public readonly int Version;
        public readonly int MinLine;
        public readonly int MaxLine;

        public MethodLineExtent(uint method, int version, int minLine, int maxLine)
        {
            Method = method;
            Version = version;
            MinLine = minLine;
            MaxLine = maxLine;
        }

        public static MethodLineExtent Merge(MethodLineExtent left, MethodLineExtent right)
        {
            Debug.Assert(left.Method == right.Method);
            Debug.Assert(left.Version == right.Version);
            return new MethodLineExtent(left.Method, left.Version, Math.Min(left.MinLine, right.MinLine), Math.Max(left.MaxLine, right.MaxLine));
        }

        public MethodLineExtent ApplyDelta(int delta) =>
            new MethodLineExtent(Method, Version, MinLine + delta, MaxLine + delta);

        private string GetDebuggerDisplay() =>
            $"{Method} v{Version} [{MinLine}-{MaxLine}]";
    }

}