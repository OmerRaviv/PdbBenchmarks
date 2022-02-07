using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Xunit;

namespace PdbReadingBenchmarks
{
    public class DebuggerLineProbes : PdbReadBenchmarksBase
    {
        [Theory] [MemberData(nameof(AllScenarios))]
        [Benchmark] [ArgumentsSource(nameof(AllScenarios))]
        public Task GetILOffsetAndLocals_FromFileLine(PdbType pdbType, PdbReaderLibrary readerLibrary)
        {
            var reader = CreateDebugInfoProvider(readerLibrary, PdbType.WindowsPdb);
            var sampleData = SamplePdbQuery.GetSampleData(pdbType);
            var result = reader.GetILOffsetAndLocalsFromDocumentPosition(
                sampleData.SampleFilePath,
                sampleData.Line,
                sampleData.Column);

            return VerifyResults(result);
        }
    }
    
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

            return VerifyResults(result);
        }
    }
}