using System.Collections.Generic;

namespace PdbReadingBenchmarks.Contracts
{
    public interface IDebugInfoProvider
    {
        unsafe (IList<SequencePoint> sequencePoints, IList<Variable> variables) GetDebugInfo(int methodMetadataToken);
    }
}