using System.Collections.Generic;
using System.Reflection;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Portable
{
    sealed partial class PortablePdbReader
    {
        internal IEnumerable<uint> GetMethodsContainedInDocument(string documentUrl)
        {
            int requestedDocumentRid = GetDocumentRid(documentUrl);
            
            for (uint methodRid = 0; methodRid < pdbMetadata.TablesStream.MethodDebugInformationTable.Rows; methodRid++)
            {
                if (!pdbMetadata.TablesStream.TryReadMethodDebugInformationRow(methodRid, out var row))
                    continue;

                if (row.SequencePoints == 0)
                    continue;


                if (row.Document == requestedDocumentRid)
                {
                    yield return methodRid;
                }
            }
        }
        private int GetDocumentRid(string documentUrl)
        {
            var docTbl = pdbMetadata.TablesStream.DocumentTable;
            var docs = new SymbolDocument[docTbl.Rows];
            var nameReader = new DocumentNameReader(pdbMetadata.BlobStream);
            for (int i = 0; i < docs.Length; i++)
            {
                if (!pdbMetadata.TablesStream.TryReadDocumentRow((uint)i + 1, out var row)) continue;
                
                var url = nameReader.ReadDocumentName(row.Name);

             
                if (url == documentUrl) return i + 1;
            }

            return -1;
        }

    }
}