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
using dnlib.DotNet.Pdb.Dss;
using dnlib.DotNet.Pdb.Managed;
using dnlib.DotNet.Pdb.Portable;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;
using PdbReadingBenchmarks.Contracts;
using SequencePoint = PdbReadingBenchmarks.Contracts.SequencePoint;
using SymbolReaderFactory = dnlib.DotNet.Pdb.SymbolReaderFactory;

namespace PdbReadingBenchmarks.DnlibReader
{

    public class DnlibPdbReader : IDebugInfoProvider
    {
        private readonly string _assemblyFullPath;
        private readonly string _pdbFullPath;
        private readonly ModuleDefMD _module;
        private readonly SymbolReader _reader;


        public DnlibPdbReader(string assemblyFullPath, string pdbFullPath, bool useDiaSymReader)
        {
	        _assemblyFullPath = assemblyFullPath;
            _pdbFullPath = pdbFullPath;
            _module = useDiaSymReader ? null : ModuleDefMD.Load(File.OpenRead(_assemblyFullPath));
            _reader = CreateSymbolReader(new ModuleCreationOptions(CLRRuntimeReaderKind.CLR)
            {
	            PdbFileOrData = _pdbFullPath,
	            PdbOptions = useDiaSymReader ? PdbReaderOptions.MicrosoftComReader : PdbReaderOptions.None
            });
            _reader.Initialize(_module);
        }
        
        SymbolReader CreateSymbolReader(ModuleCreationOptions options)
        {
	        var metadata = MetadataFactory.Load(_assemblyFullPath, CLRRuntimeReaderKind.CLR);
	        if (options.PdbFileOrData is not null) {
		        var pdbFileName = options.PdbFileOrData as string;
		        if (!string.IsNullOrEmpty(pdbFileName))
		        {
			        var pdbStream = DataReaderFactoryFactory.Create(pdbFileName, false);
			        var symReader = SymbolReaderFactory.Create(options.PdbOptions, metadata, pdbStream);
			        if (symReader is not null)
				        return symReader;
		        }

	        }

	        if (options.TryToLoadPdbFromDisk)
		        return SymbolReaderFactory.CreateFromAssemblyFile(options.PdbOptions, metadata, _assemblyFullPath);

	        return null;
        }

        private class FakeMethodDef : MethodDef
        {
            public FakeMethodDef(uint rid)
            {
                this.rid = rid;
            }
        }
        public LineDebugInfo GetILOffsetAndLocals_FromDocumentPosition(
            string filePath, int line, int column)
        {
            var method = GetContainingMethodAndOffset(filePath,line, column, out int? offset);
            if (method != null)
            {
	            return new LineDebugInfo(
		            MethodToken: method.Token,
		            ILOffset: offset.Value,
		            Locals: GetVariablesInScope(method, offset.Value));
            }

            return new LineDebugInfo(default, default, default);
        }

        private SymbolMethod GetContainingMethodAndOffset(string filePath, int line, int column, out int? bytecodeOffset)
        {
	        return _reader switch
	        {
		        PortablePdbReader portablePdbReader => portablePdbReader.GetContainingMethod(filePath,line, column, out bytecodeOffset),
		        PdbReader managedPdbReader => managedPdbReader.GetContainingMethod(filePath, line, column, out bytecodeOffset),
		        SymbolReaderImpl symUnmanagedReader => symUnmanagedReader.GetContainingMethod(filePath, line, column, out bytecodeOffset),
		        _ => throw new ArgumentOutOfRangeException(nameof(filePath), $"Reader type {_reader.GetType().FullName} is not supported")
	        };
        }

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
	        var rid = MDToken.ToRID(methodMetadataToken); 
	        var mdMethod = new FakeMethodDef(rid);
	        //var mdMethod = _module.ResolveMethod(rid);
	        var method = _reader.GetMethod(mdMethod, 1);
	        var variables = 
		        GetAllScopes(method)
					.SelectMany(m => m.Locals)
					.Select(l => new Variable(l.Index, l.Name))
					.ToList();
	            	        
            var sequencePoints = method.SequencePoints
                .Select(s => new SequencePoint()
            {
                DocumentUrl = s.Document.URL,
                StartLine = s.Line,
                EndLine = s.EndLine,
                StartColumn = s.Column,
                EndColumn = s.EndColumn,
                Offset = s.Offset
                
            }).ToList();
            return new MethodDebugInfo(sequencePoints, variables);

        }

        public void Dispose()
        {
	        _module?.Dispose();
	        _reader?.Dispose();
        }
    }
}