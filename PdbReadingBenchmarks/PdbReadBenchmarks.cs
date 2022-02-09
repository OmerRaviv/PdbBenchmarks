using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VerifyXunit;
using Xunit;

namespace PdbReadingBenchmarks
{
    [UsesVerify]
    public class DebuggerLineProbes : PdbReadBenchmarksBase
    {
        [Theory] [MemberData(nameof(LineProbeSupportedScenarios))]
        [Benchmark] [ArgumentsSource(nameof(LineProbeSupportedScenarios))]
        public Task GetILOffsetAndLocals_FromFileLine(PdbType pdbType, PdbReaderLibrary readerLibrary)
        {
            var reader = CreateDebugInfoProvider(readerLibrary, PdbType.WindowsPdb);
            var sampleData = SamplePdbQuery.GetSampleData(pdbType);
            var result = reader.GetILOffsetAndLocals_FromDocumentPosition(
                sampleData.SampleFilePath,
                sampleData.Line,
                sampleData.Column);

            return VerifyResults(result, pdbType);
        }

        public static IEnumerable<object[]> LineProbeSupportedScenarios => AllScenarios.Where(obj => 
            obj[1].Equals(PdbReaderLibrary.Dnlib) ||
            obj[1].Equals(PdbReaderLibrary.DiaNativeSymReader));
    }
    
    [UsesVerify]
    public class DebuggerMethodProbes : PdbReadBenchmarksBase
    {
        [Theory]    [MemberData(nameof(AllScenarios))]
        [Benchmark] [ArgumentsSource(nameof(AllScenarios))]
        public Task GetFileLineNumber_FromMethodTokenAndBytecodeOffset(PdbType pdbType, PdbReaderLibrary readerLibrary)
        {
            var reader = CreateDebugInfoProvider(readerLibrary, pdbType);
            var sampleData = SamplePdbQuery.GetSampleData(pdbType);
            var methodToken = sampleData.GetSampleMethodToken();
            var result = reader.GetDebugInfo(methodToken);

            return VerifyResults(result, pdbType);
        }
    }
}