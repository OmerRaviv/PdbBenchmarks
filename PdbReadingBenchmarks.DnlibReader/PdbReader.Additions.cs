using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Managed
{
    public partial class PdbReader
    {
        internal SymbolMethod GetContainingMethod(string documentUrl, int line, int column, out int? bytecodeOffset)
        {
            var candidateSequencePoints = new List<(SymbolMethod method, SymbolSequencePoint sp)>();
            foreach (var function in functions.Values)
            {
                var methodIsInDocument = function.SequencePoints.Any(s => s.Document.URL == documentUrl);

                if (methodIsInDocument)
                {
                    var method = this.GetMethod(((ModuleDefMD)module).ResolveMethod(MDToken.ToRID(function.token)), 1);
                    foreach (var sp in method.SequencePoints)
                    {
                        if (sp.Line <= line && sp.EndLine >= line &&
                            sp.Column >= column && sp.EndColumn >= column)
                        {
                            candidateSequencePoints.Add((method, sp));
                        }
                    }
                }
            }

            var matchingSequencePoint = candidateSequencePoints.FirstOrDefault();
            bytecodeOffset = matchingSequencePoint.sp.Offset;
            return matchingSequencePoint.method; // TODO - find shortest sequence point
        }
    }
}