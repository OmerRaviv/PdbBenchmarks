using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
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
        public static SamplePdbQuery GetSampleData(PdbType pdbType)
        {
            return pdbType switch
            {
                PdbType.WindowsPdb => GetWindowsPdbSample(),
                PdbType.PortablePdb => GetPortablePdbSample(),
                _ => throw new ArgumentOutOfRangeException(nameof(pdbType))
            };
        }

        private static SamplePdbQuery GetPortablePdbSample()
        {
            throw new NotImplementedException();
        }

        private static SamplePdbQuery GetWindowsPdbSample()
        {
            var assemblyFullPath = Path.Combine(Environment.CurrentDirectory, "LargePdbSamples", "WindowsPdb",
                "nunit.framework.dll");
            return new SamplePdbQuery()
            {
                AssemblyFullPath = assemblyFullPath,
                PdbFilePath = Path.ChangeExtension(assemblyFullPath, "pdb"),
                SampleFilePath = @"C:\src\nunit\nunit\src\NUnitFramework\framework\Api\FrameworkController.cs",
                Line = 233,
                Column = 17,
                ClassName = "NUnit.Framework.Api.FrameworkController",
                MethodName = "RunTests",
            };
        }

        public string ClassName { get; init; }

        public string MethodName { get; init; }
        
        public string PdbFilePath { get; init; }
        public string AssemblyFullPath { get; init; }
        public string SampleFilePath { get; init; }
        public int Line { get; init; }
        public int Column { get; init; }

        public int GetSampleMethodToken() 
        {
            unsafe
            {
                using Stream fileStream = File.OpenRead(AssemblyFullPath);
                using var reader = new PEReader(fileStream);

                var image = reader.GetEntireImage();

                using (var stream = new UnmanagedMemoryStream(image.Pointer, image.Length))
                using (var moduleDef = ModuleDefinition.ReadModule(stream))
                {
                    var method = moduleDef.GetType(ClassName).GetMethods().First(
                        m => m.Name == MethodName);
                    return method.MetadataToken.ToInt32();
                }
            }
        }
    }
}