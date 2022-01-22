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
            var summary = BenchmarkRunner.Run<GetILOffsetAndLocalsFromFileLineBenchmarks>();
        }
    }
}