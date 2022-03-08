using System;
using System.Collections.Generic;

namespace PdbReadingBenchmarks.Contracts
{
    
    public record MethodDebugInfo(IList<SequencePoint> SequencePoints, IList<Variable> Variables);

    public record LineDebugInfo(int MethodToken, int ILOffset, List<Variable> Locals); 
    
    public interface IDebugInfoProvider : IDisposable
    {
       MethodDebugInfo GetDebugInfo(int methodMetadataToken);
       LineDebugInfo GetILOffsetAndLocals_FromDocumentPosition(string filePath, int line, int column);
    }
}