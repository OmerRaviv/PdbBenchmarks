

using System;
using System.Collections.Generic;
using System.Diagnostics;
using PdbReadingBenchmarks.Contracts;
using Vanara.PInvoke;
using Zodiacon.DebugHelp;
using static Vanara.PInvoke.DbgHelp;

namespace PdbReadingBenchmarks.DbgHelpPdbReader
{
    
    public class DebugHelpPdbReader : IDebugInfoProvider
    {
        
        private readonly string _assemblyFullPath;
        private static SymbolHandler? _symbolHandler;
        private static ulong? _address;


        public DebugHelpPdbReader(string assemblyFullPath)
        {
            _assemblyFullPath = assemblyFullPath;
        }
        public unsafe MethodDebugInfo GetDebugInfo(int methodMetadataToken)
        {
            
            using var currentProcess = Process.GetCurrentProcess();
            _symbolHandler ??= SymbolHandler.CreateFromProcess(currentProcess.Id, SymbolOptions.Debug);
            
            _address ??= _symbolHandler.LoadSymbolsForModule(_assemblyFullPath);

            var symbol = GetSymbolForToken(_symbolHandler, _address.Value, methodMetadataToken);

            // We need to set up a stack frame that will be used by SymEnumSymbolsForAddr
            // Of course we dont have a real stack frame as we only want to query information
            // but its enough to set the instruction offset for SymEnumSymbols to work.
            IMAGEHLP_STACK_FRAME frame  = new IMAGEHLP_STACK_FRAME { InstructionOffset = symbol.Address };
            
            // With SymSetContext we can set the current context
            // in which symbols should be enumerated
            // and evaluated. The last parameter is reserved!
            // If the currently set context is the same
            // as we set the function will return false but
            // setting ERROR_SUCCESS because it actually didnt
            // fail but just didnt do anything.
            var hProcess = new HPROCESS(new IntPtr(currentProcess.Id));
            if (!SymSetContext(hProcess, frame))
            {
                Win32Utils.ThrowOnWin32Error();
            }

            
            // // The last parameter will be passed to every call to EnumParamsCallback.
            IList<SymbolInfo> symbols = _symbolHandler.EnumSymbols(0, null);
            var variables = new List<Variable>(symbols.Count);
            foreach (var symbolInfo in symbols)
            {
                variables.Add(new Variable(symbolInfo.Index, symbolInfo.Name));
            }

            
            var sequencePoints = new List<SequencePoint>();
            return new MethodDebugInfo(sequencePoints,variables);
        }

        private bool EnumParamsCallback(in SYMBOL_INFO psyminfo, uint symbolsize, IntPtr usercontext)
        {
            return true;
        }

        private static SymbolInfo GetSymbolForToken(SymbolHandler? symbolHandler, ulong address,
            int methodMetadataToken)
        {
            SymbolInfo info = default;
            info.Init();
            if (!symbolHandler.GetSymbolFromToken((uint)methodMetadataToken, address,ref info))
            {
                Win32Utils.ThrowOnWin32Error();
            }
            return info;
        }

        public LineDebugInfo GetILOffsetAndLocals_FromDocumentPosition(
            string filePath, int line, int column)
        {
            
     //       SymGetFileLineOffsets64( hProcess,Path.GetFileName(_assemblyFullPath),filePath )
            return new LineDebugInfo(default, default,default);
        }
    }
}