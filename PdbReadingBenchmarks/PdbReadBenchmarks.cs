using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using DbgHelpPdbReader;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using PdbReadingBenchmarks.DiaNativeSymReader;

namespace PdbReadingBenchmarks
{
    [RankColumn,MemoryDiagnoser,NativeMemoryProfiler]
    [SimpleJob(RunStrategy.Monitoring, 10, 0, 100)]
    public class PdbReadBenchmarks
    {
        private static readonly int _methodToken;
        private static readonly string _pdbFilePath;
        private static readonly string _assemblyFullPath;

        static PdbReadBenchmarks()
        {
            _assemblyFullPath = Path.Combine(Environment.CurrentDirectory, "LargePdbSamples", "WindowsPdb", "nunit.framework.dll");
            _pdbFilePath = Path.ChangeExtension(_assemblyFullPath, "pdb");
            _methodToken =
                GetMethodToken(); 
        }

        [GlobalSetup]
        public void Setup()
        {
            
            Assembly.LoadFrom(_assemblyFullPath);
            ProveAllBenchmarkYieldTheSameResults();
        }

        private void ProveAllBenchmarkYieldTheSameResults()
        {
            // Assert.Equals...  (the same way dddog do it)
        }

        private static int GetMethodToken()
        {
            unsafe
            {
                using Stream fileStream = File.OpenRead(_assemblyFullPath);
                using var reader = new PEReader(fileStream);

                var image = reader.GetEntireImage();

                using (var stream = new UnmanagedMemoryStream(image.Pointer, image.Length))
                using (var moduleDef = ModuleDefinition.ReadModule(stream))
                {
                    var method = moduleDef.GetType("NUnit.Framework.Api.FrameworkController").GetMethods().First(
                        m => m.Name == "RunTests");
                    return method.MetadataToken.ToInt32();
                }
            }
        }

        [Benchmark]
        public void ReadWithMonoCecil()
        {
            var reader = new MonoCecilPdbReader(_assemblyFullPath, _pdbFilePath);
            var (sequencePoints, variables) = reader.GetDebugInfo(_methodToken);
        }

        [Benchmark]
        public void ReadWithDiaNativeSymReader()
        {
            var reader = new DiaSymReaderPdbReader(_pdbFilePath);
            var (sequencePoints, variables) = reader.GetDebugInfo(_methodToken);
        }


        [Benchmark]
        public void ReadWithDbgHelpReader()
        {
            var reader = new DebugHelpPdbReader(_assemblyFullPath, _pdbFilePath);
            var (sequencePoints, variables) = reader.GetDebugInfo(_methodToken);
        }

        // [Benchmark]
        // public void ReadWithMjsabbyReader()
        // {
        //     var reader = new MjsabbyPdbReader(_assemblyFullPath, _pdbFilePath);
        //     var (sequencePoints, variables) = reader.GetDebugInfo(_methodToken);
        // }
    }
}