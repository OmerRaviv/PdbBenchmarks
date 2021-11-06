using System;
using BenchmarkDotNet.Running;
using DbgHelpPdbReader;
using PdbReadingBenchmarks.DiaNativeSymReader;

namespace PdbReadingBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            for (int i = 0; i < 1000; i++)
            {
             
                new PdbReadBenchmarks().ReadWithDbgHelpReader();   
            }
            //return;
//            new PdbReadBenchmarks().ReadWithDbgHelpReader();
//            new PdbReadBenchmarks().ReadWithDiaNativeSymReader();
     //       var summary = BenchmarkRunner.Run<PdbReadBenchmarks>(); return;
            //
            // for (int i = 0; i < 100; i++)
            // {
            //     new PdbReadBenchmarks().ReadWithMjsabbyReader();    
            // }            


        }
    }
}