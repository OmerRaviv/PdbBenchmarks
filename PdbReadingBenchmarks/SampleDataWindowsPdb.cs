using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}

namespace PdbReadingBenchmarks
{
    public class SamplePdbQuery
    {
        public string ClassName { get; init; }

        public string MethodName { get; init; }
        
        public string PdbFilePath { get; init; }
        public string AssemblyFullPath { get; init; }
        public string SampleFilePath { get; init; }
        public int Line { get; init; }
        public int Column { get; init; }
    
        public int MethodToken { get; init; }

        public int GetSampleMethodToken() => MethodToken;
        // public int GetSampleMethodToken() 
        // {
        //     unsafe
        //     {
        //         using Stream fileStream = File.OpenRead(AssemblyFullPath);
        //         using var reader = new PEReader(fileStream);
        //
        //         var image = reader.GetEntireImage();
        //
        //         using (var stream = new UnmanagedMemoryStream(image.Pointer, image.Length))
        //         using (var moduleDef = ModuleDefinition.ReadModule(stream))
        //         {
        //             var method = moduleDef.GetType(ClassName).GetMethods().First(
        //                 m => m.Name == MethodName);
        //             return method.MetadataToken.ToInt32();
        //         }
        //     }
        // }
    }
}