using System;
using System.Runtime.InteropServices;
using dnlib.DotNet.Pdb.Symbols;
using PdbReadingBenchmarks.Contracts;

namespace dnlib.DotNet.Pdb.Dss 
{
    using static SymbolReaderImpl.InteropUtilities;
    sealed partial class SymbolReaderImpl
    {
        internal SymbolMethod GetContainingMethod(string documentUrl, int line, int column, out int? bytecodeOffset)
        {
            ISymUnmanagedDocument document = reader.GetDocument(documentUrl);
            // TODO: Error handling
            reader.GetMethodFromDocumentPosition(document, (uint)line, (uint)column, out var method);
            method.GetOffset(document, (uint)line, (uint)column, out uint offset);
            method.GetToken(out uint token);

            bytecodeOffset = (int?)offset;
            return new SymbolMethodImpl(this, method);
        }


        internal class InteropUtilities
        {
            private static readonly IntPtr s_ignoreIErrorInfo = new IntPtr(-1);
            
            internal static void ThrowExceptionForHR(int hr)
            {
                // E_FAIL indicates "no info".
                // E_NOTIMPL indicates a lack of ISymUnmanagedReader support (in a particular implementation).
                if (hr < 0 && hr != HResult.E_FAIL && hr != HResult.E_NOTIMPL)
                {
                    Marshal.ThrowExceptionForHR(hr, s_ignoreIErrorInfo);
                }
            }
        }
    }
    internal static class HResult
    {
        internal const int S_OK = 0;
        internal const int S_FALSE = 1;
        internal const int E_NOTIMPL = unchecked((int)0x80004001);
        internal const int E_FAIL = unchecked((int)0x80004005);
        internal const int E_INVALIDARG = unchecked((int)0x80070057);
        internal const int E_UNEXPECTED = unchecked((int)0x8000FFFF);
    }

    static class SymUnmanagedReaderExtensions
    {
        public static ISymUnmanagedDocument GetDocument(this ISymUnmanagedReader reader, string name)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            ISymUnmanagedDocument document;
            reader.GetDocument(name, language: default, languageVendor: default, documentType: default, out document);
            return document;
        }
    }
   
    
}