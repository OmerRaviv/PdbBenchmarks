using System;
using BenchmarkDotNet.Running;
using PdbReadingBenchmarks.DbgHelpPdbReader;
using PdbReadingBenchmarks.DiaNativeSymReader;

namespace PdbReadingBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Operations we will need to benchmark:
            // Get IL offset and locals from (file, line number)                        (for debugger line probe)
            // Get names of locals from a method token                                  (for debugger method probe)
            // Get file/line number from (assembly name, method token, bytecode offset) (for profiler & backend callstack parser)
            // Get all sequence points from method token                                (for CI Visibility - code coverage)
            // Get file/line number from (class name, method name)                      (for CI Visibility - get test definition source location)

            BenchmarkRunner.Run(typeof(DebuggerLineProbes));
            //BenchmarkRunner.Run(typeof(Program).Assembly);
            //    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}