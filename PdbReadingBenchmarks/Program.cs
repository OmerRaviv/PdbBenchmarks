using BenchmarkDotNet.Running;

namespace PdbReadingBenchmarks
{
    public class Program
    {
        public static void Main()
        {
            // Operations we will need to benchmark:
            // Get IL offset and locals in scope from (file, line number)               (for debugger line probe)
            // Get names of locals from a method token                                  (for debugger method probe)
            // Get file/line number from (assembly name, method token, bytecode offset) (for continuous profiler & backend callstack parser)
            // Get all sequence points from method token                                (for CI Visibility - code coverage)
            // Get file/line number from (class name, method name)                      (for CI Visibility - get test definition source location)
           
            //BenchmarkRunner.Run(typeof(DebuggerLineProbes));
            //BenchmarkRunner.Run(typeof(DebuggerMethodProbes));
            BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}