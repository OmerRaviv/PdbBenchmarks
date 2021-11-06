

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using PdbReadingBenchmarks.Contracts;
using Vanara.PInvoke;
using Zodiacon.DebugHelp;
using static Vanara.PInvoke.DbgHelp;

namespace DbgHelpPdbReader
{
    public class DebugHelpPdbReader : IDebugInfoProvider
    {
        
        private readonly string _assemblyFullPath;
        private readonly string _pdbFullPath;
        private static SymbolHandler? _symbolHandler;
        private static ulong? _address;


        public DebugHelpPdbReader(string assemblyFullPath, string pdbFullPath)
        {
            _assemblyFullPath = assemblyFullPath;
            _pdbFullPath = pdbFullPath;
        }
        public unsafe (IList<SequencePoint> sequencePoints, IList<Variable> variables) GetDebugInfo(int methodMetadataToken)
        {
            
            using var currentProcess = Process.GetCurrentProcess();
            _symbolHandler ??= SymbolHandler.CreateFromProcess(currentProcess.Id, SymbolOptions.Debug);
            //var symbolHandler = new SymbolHandler(currentProcess.SafeHandle.DangerousGetHandle(), true);
            // foreach (var enumModule in symbolHandler.EnumModules())
            // {
            //     Console.WriteLine($"->  {enumModule.Name} : {enumModule.Base}");
            // }

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

            //
            // // The last parameter will be passed to every call to EnumParamsCallback.
            foreach (var enumSymbol in _symbolHandler.EnumSymbols(0, null))
            {
               // Console.WriteLine($" IN ENUM ->>>> {enumSymbol.Name} {enumSymbol.Index}");

            }
            
            // if(!SymEnumSymbols(hProcess, BaseOfDll: 0, Mask: null, EnumParamsCallback))
             // {
             // getting just 1 character...
             //     Win32Utils.ThrowOnWin32Error();
             // }
            return default;
        }

        private bool EnumParamsCallback(in SYMBOL_INFO psyminfo, uint symbolsize, IntPtr usercontext)
        {
            
            Console.WriteLine($" IN ENUM ->>>> {psyminfo.Name} {psyminfo.Index}");
            return true;
        }

        private static SymbolInfo GetSymbolForToken(SymbolHandler? symbolHandler, ulong address,
            int methodMetadataToken)
        {
            foreach (var enumSymbol in symbolHandler.EnumSymbols(address))
            {
                long methodToken = enumSymbol.Value;
                if (methodMetadataToken == methodToken) return enumSymbol;
            }
            
            SymbolInfo infoFromName = default;
            symbolHandler.GetSymbolFromName("RunTests", ref infoFromName);
            
            SymbolInfo info = default;
            info.Init();
            if (!symbolHandler.GetSymbolFromToken((uint)methodMetadataToken, ref info))
            {
                Win32Utils.ThrowOnWin32Error();
            }
            
            return info;
            return default;
        }
    }
}