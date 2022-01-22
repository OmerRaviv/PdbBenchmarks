using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using PdbReadingBenchmarks.DbgHelpPdbReader;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using PdbReadingBenchmarks.DiaNativeSymReader;
using Xunit;

namespace PdbReadingBenchmarks
{
    
    // Operations we will need:
    // Get IL offset and locals from (file, line number)                        (for debugger line probe)
    // Get names of locals from a method token                                  (for debugger method probe)
    // Get file/line number from (assembly name, method token, bytecode offset) (for profiler & backend callstack parser)
    // Get all sequence points from method token                                (for CI Visibility)
    // Get file/line number from (class name, method name)                      (for CI Visibility)
    
    
    [RankColumn,MemoryDiagnoser,NativeMemoryProfiler]
    [SimpleJob(RunStrategy.Monitoring, 10, 0, 100)]
    public class GetILOffsetAndLocalsFromFileLineBenchmarks
    {
        private static readonly int _methodToken;
        private static readonly string _pdbFilePath;
        private static readonly string _assemblyFullPath;

        static GetILOffsetAndLocalsFromFileLineBenchmarks()
        {
            _assemblyFullPath = Path.Combine(Environment.CurrentDirectory, "LargePdbSamples", "WindowsPdb", "nunit.framework.dll");
            _pdbFilePath = Path.ChangeExtension(_assemblyFullPath, "pdb");
            _methodToken = GetMethodToken(); 
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
        
        [Fact]
        [Benchmark]
        public void ReadWithDnlib()
        {
            var reader = new PdbReadingBenchmarks.DnlibReader.DnlibPdbReader(_assemblyFullPath, _pdbFilePath);
            var (sequencePoints, variables) = reader.GetDebugInfo(_methodToken);
        }

        
        [Benchmark]
        public void ReadWithMonoCecil()
        {
            var reader = new MonoCecilPdbReader(_assemblyFullPath, _pdbFilePath);
            var (sequencePoints, variables) = reader.GetDebugInfo(_methodToken);
        }

        [Fact]
        [Benchmark]
        public void ReadWithDiaNativeSymReader()
        {
            var reader = new DiaSymReaderPdbReader(_pdbFilePath);
            var (sequencePoints, variables) = reader.GetDebugInfo(_methodToken);
        }

        [Fact]
        [Benchmark]
        public void ReadWithDbgHelpReader()
        {
            var reader = new DebugHelpPdbReader(_assemblyFullPath);
            
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