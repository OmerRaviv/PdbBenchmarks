using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;
using PdbReadingBenchmarks.Contracts;
using SequencePoint = PdbReadingBenchmarks.Contracts.SequencePoint;

namespace PdbReadingBenchmarks.DnlibReader
{

    public class DnlibPdbReader : IDebugInfoProvider
    {
        private readonly string _assemblyFullPath;
        private readonly string _pdbFullPath;
        private readonly ModuleDefMD _module;
        private readonly SymbolReader _reader;


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
            _methodExtentsByDocument = new(CalculateMethodExtentsByDocument);
            _reader.Initialize(_module);
        }

        private Lazy<Dictionary<string, List<MethodLineExtent>>> _methodExtentsByDocument;

        private Dictionary<string, List<MethodLineExtent>> CalculateMethodExtentsByDocument()
        {
            Dictionary<string, List<MethodLineExtent>> methodExtentsByDocument = new();
            foreach (var types in _module.Types)
            {
                foreach (MethodDef method in types.Methods)
                {
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

        private int GetDocumentRid(Metadata pdbMetadata, string documentUrl)
        {
            
            var docTbl = pdbMetadata.TablesStream.DocumentTable;
            var docs = new SymbolDocument[docTbl.Rows];
            var nameReader = new DocumentNameReader(pdbMetadata.BlobStream);
            for (int i = 0; i < docs.Length; i++)
            {
                if (!pdbMetadata.TablesStream.TryReadDocumentRow((uint)i + 1, out var row)) continue;
                
                var url = nameReader.ReadDocumentName(row.Name);

             
                if (url == documentUrl) return i + 1;
            }

            return -1;
        }

        private IEnumerable<uint> GetMethodsContainedInDocument(string documentUrl)
        {
            Metadata pdbMetadata = (Metadata)_reader.GetType().GetField("pdbMetadata", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_reader);
            int requestedDocumentRid = GetDocumentRid(pdbMetadata, documentUrl);
            
            for (uint methodRid = 0; methodRid < pdbMetadata.TablesStream.MethodDebugInformationTable.Rows; methodRid++)
            {
                if (!pdbMetadata.TablesStream.TryReadMethodDebugInformationRow(methodRid, out var row))
                    continue;

                if (row.SequencePoints == 0)
                    continue;


                if (row.Document == requestedDocumentRid)
                {
                    yield return methodRid;
                }
            }
        }

        public class FakeMethodDef : MethodDef
        {
            public FakeMethodDef(uint rid)
            {
                this.rid = rid;
            }
        }
        public LineDebugInfo GetILOffsetAndLocals_FromDocumentPosition(
            string filePath, int line, int column)
        {
            foreach (var methodRid in GetMethodsContainedInDocument(filePath))
            {
                //var method = _module.ResolveMethod((uint)methodRid);
                var method = new FakeMethodDef(methodRid);
                var symbolMethod = _reader.GetMethod(method, 1);
                foreach (var sp in symbolMethod.SequencePoints)
                {
                    if (sp.Line <= line &&
                        sp.EndLine >= line &&
                        sp.Column >= column &&
                        sp.EndColumn >= column)
                    {
                        return new LineDebugInfo(
                            MethodToken: (int)method.MDToken.Raw,
                            ILOffset: sp.Offset,
                            Locals: GetVariablesInScope(symbolMethod, sp.Offset));
                    }
                }
            }
            return new LineDebugInfo(default, default, default);

            //       SymGetFileLineOffsets64( hProcess,Path.GetFileName(_assemblyFullPath),filePath )
        }
        // public LineDebugInfo GetILOffsetAndLocals_FromDocumentPosition(
        //     string filePath, int line, int column)
        // {
        //     
        //     if (!_methodExtentsByDocument.Value.TryGetValue(filePath, out var methodExtentsInDoc))
        //     {
        //         return new LineDebugInfo(default, default, default);
        //     }
        //
        //     foreach (var methodLineExtent in methodExtentsInDoc)
        //     {
        //         if (line > methodLineExtent.MinLine && line < methodLineExtent.MaxLine)
        //         {
        //             var method = _module.ResolveMethod(methodLineExtent.Method);
        //             var symbolMethod = _reader.GetMethod(method, 1);
        //             foreach (var sp in symbolMethod.SequencePoints)
        //             {
        //                 if (sp.Line <= line &&
        //                     sp.EndLine >= line &&
        //                     sp.Column >= column &&
        //                     sp.EndColumn >= column)
        //                 {
        //                     return new LineDebugInfo(
        //                         MethodToken: (int)method.MDToken.Raw, 
        //                         ILOffset: sp.Offset,
        //                         Locals: GetVariablesInScope(symbolMethod, sp.Offset));
        //                 }
        //             }
        //
        //             
        //         }
        //     }
        //     return new LineDebugInfo(default, default, default);
        //
        //     //       SymGetFileLineOffsets64( hProcess,Path.GetFileName(_assemblyFullPath),filePath )
        // }

        private List<Variable> GetVariablesInScope(SymbolMethod method, int offset)
        {
            return
                GetAllScopes(method)
                    .Where(s => s.StartOffset <= offset && s.EndOffset >= offset)
                    .SelectMany(s => s.Locals.Select(v => new Variable(v.Index, v.Name)))
                    .ToList();
        }

        private static IList<SymbolScope> GetAllScopes(SymbolMethod method)
        {
            var result = new List<SymbolScope>();
            RetrieveAllNestedScopes(method.RootScope, result);
            return result;
        }

        private static void RetrieveAllNestedScopes(SymbolScope scope, List<SymbolScope> result)
        {
            // Recursively extract all nested scopes in method
            if (scope == null) return;
            result.Add(scope);
            foreach (var innerScope in scope.Children)
            {
                RetrieveAllNestedScopes(innerScope,result);
            }
        }


        public MethodDebugInfo GetDebugInfo(int methodMetadataToken)
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
            return new MethodDebugInfo(sequencePoints, variables);
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

    	struct DocumentNameReader {
		const int MAX_NAME_LENGTH = 64 * 1024;
		readonly Dictionary<uint, string> docNamePartDict;
		readonly BlobStream blobStream;
		readonly StringBuilder sb;

		char[] prevSepChars;
		int prevSepCharsLength;
		byte[] prevSepCharBytes;
		int prevSepCharBytesCount;

		public DocumentNameReader(BlobStream blobStream) {
			docNamePartDict = new Dictionary<uint, string>();
			this.blobStream = blobStream;
			sb = new StringBuilder();

			prevSepChars = new char[2];
			prevSepCharsLength = 0;
			prevSepCharBytes = new byte[3];
			prevSepCharBytesCount = 0;
		}

		public string ReadDocumentName(uint offset) {
			sb.Length = 0;
			if (!blobStream.TryCreateReader(offset, out var reader))
				return string.Empty;
			var sepChars = ReadSeparatorChar(ref reader, out int sepCharsLength);
			bool needSep = false;
			while (reader.Position < reader.Length) {
				if (needSep)
					sb.Append(sepChars, 0, sepCharsLength);
				needSep = !(sepCharsLength == 1 && sepChars[0] == '\0');
				var part = ReadDocumentNamePart(reader.ReadCompressedUInt32());
				sb.Append(part);
				if (sb.Length > MAX_NAME_LENGTH) {
					sb.Length = MAX_NAME_LENGTH;
					break;
				}
			}
			return sb.ToString();
		}

		string ReadDocumentNamePart(uint offset) {
			if (docNamePartDict.TryGetValue(offset, out var name))
				return name;
			if (!blobStream.TryCreateReader(offset, out var reader))
				return string.Empty;
			name = reader.ReadUtf8String((int)reader.BytesLeft);
			docNamePartDict.Add(offset, name);
			return name;
		}

		char[] ReadSeparatorChar(ref DataReader reader, out int charLength) {
			if (prevSepCharBytesCount != 0 && prevSepCharBytesCount <= reader.Length) {
				var pos = reader.Position;
				bool ok = true;
				for (int i = 0; i < prevSepCharBytesCount; i++) {
					if (i >= prevSepCharBytes.Length || reader.ReadByte() != prevSepCharBytes[i]) {
						ok = false;
						break;
					}
				}
				if (ok) {
					charLength = prevSepCharsLength;
					return prevSepChars;
				}
				reader.Position = pos;
			}

			var decoder = Encoding.UTF8.GetDecoder();
			var bytes = new byte[1];
			prevSepCharBytesCount = 0;
			for (int i = 0; ; i++) {
				byte b = reader.ReadByte();
				prevSepCharBytesCount++;
				if (i == 0 && b == 0)
					break;
				if (i < prevSepCharBytes.Length)
					prevSepCharBytes[i] = b;
				bytes[0] = b;
				bool isLastByte = reader.Position + 1 == reader.Length;
				decoder.Convert(bytes, 0, 1, prevSepChars, 0, prevSepChars.Length, isLastByte, out int bytesUsed, out prevSepCharsLength, out bool completed);
				if (prevSepCharsLength > 0)
					break;
			}
			charLength = prevSepCharsLength;
			return prevSepChars;
		}
	}

}