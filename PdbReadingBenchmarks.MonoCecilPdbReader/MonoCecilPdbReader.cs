using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using PdbReadingBenchmarks.Contracts;
using SequencePoint = PdbReadingBenchmarks.Contracts.SequencePoint;
using SRM = System.Reflection.Metadata;

namespace PdbReadingBenchmarks
{
    public class MonoCecilPdbReader : IDebugInfoProvider
    {
        private readonly string _assemblyFullPath;
        private readonly string _pdbFullPath;


        public MonoCecilPdbReader(string assemblyFullPath, string pdbFullPath)
        {
            _assemblyFullPath = assemblyFullPath;
            _pdbFullPath = pdbFullPath;
        }

        public unsafe MethodDebugInfo GetDebugInfo(
            int methodMetadataToken)
        {
            using Stream fileStream = File.OpenRead(_assemblyFullPath);
            using PEReader reader = new PEReader(fileStream);

            var image = reader.GetEntireImage();
                        
            using (UnmanagedMemoryStream stream = new UnmanagedMemoryStream(image.Pointer, image.Length))
            using (var moduleDef = ModuleDefinition.ReadModule(stream))
            {
                moduleDef.ReadSymbols(new PdbReaderProvider().GetSymbolReader(moduleDef, _pdbFullPath));
                
                var cecilMethod = moduleDef.LookupToken(methodMetadataToken) as MethodDefinition;
                var debugInformation = cecilMethod?.DebugInformation;
                if (debugInformation == null)
                    throw new InvalidOperationException("Obtaining debug information failed");
                var sequencePoints = GetSequencePoints(debugInformation);
                var variables = GetVariables(debugInformation);
                return new MethodDebugInfo(sequencePoints, variables);
            }
        }

        public LineDebugInfo GetILOffsetAndLocals_FromDocumentPosition(
            string filePath, int line, int column)
        {
            throw new NotImplementedException();
        }

        private static List<Variable> GetVariables(MethodDebugInformation debugInfo)
        {
            var variables = new List<Variable>();
            foreach (var scope in debugInfo.GetScopes())
            {
                if (!scope.HasVariables)
                    continue;
                foreach (var v in scope.Variables)
                {
                    variables.Add(new Variable(v.Index, v.Name));
                }
            }

            return variables;
        }

        private static IList<SequencePoint> GetSequencePoints(MethodDebugInformation debugInfo)
        {
            IList<SequencePoint> sequencePoints = Array.Empty<SequencePoint>();
            if (debugInfo.HasSequencePoints)
            {
                sequencePoints = new List<SequencePoint>(debugInfo.SequencePoints.Count);
                foreach (var point in debugInfo.SequencePoints)
                {
                    sequencePoints.Add(new SequencePoint
                    {
                        Offset = point.Offset,
                        StartLine = point.StartLine,
                        StartColumn = point.StartColumn,
                        EndLine = point.EndLine,
                        EndColumn = point.EndColumn,
                        DocumentUrl = point.Document.Url
                    });
                }
            }

            return sequencePoints;
        }
    }
    
}