using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using VerifyTests;

namespace PdbReadingBenchmarks
{
    


    public enum PdbType
    {
        WindowsPdb,
        PortablePdb
    }
}