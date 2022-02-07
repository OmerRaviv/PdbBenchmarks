using System.Collections.Generic;

namespace PdbReadingBenchmarks.Contracts
{
    public interface IDebugInfoProvider
    {
        unsafe (IList<SequencePoint> sequencePoints, IList<Variable> variables) GetDebugInfo(int methodMetadataToken);
        (int methodToken, int ilOffset, List<Variable> locals) GetILOffsetAndLocalsFromDocumentPosition(string filePath,
            int line, int column);
    }
}