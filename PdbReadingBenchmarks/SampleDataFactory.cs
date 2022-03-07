using System;
using System.IO;

namespace PdbReadingBenchmarks
{
    public static class SampleDataFactory
    {
        public static SamplePdbQuery GetSampleData(PdbType pdbType) =>
            pdbType switch
            {
                PdbType.WindowsPdb => GetWindowsPdbSample(),
                PdbType.PortablePdb => GetPortablePdbSample(),
                _ => throw new ArgumentOutOfRangeException(nameof(pdbType))
            };

        private static SamplePdbQuery GetPortablePdbSample()
        {
            var assemblyFullPath = Path.Combine(Environment.CurrentDirectory, "LargePdbSamples", "PortablePdb",
                "Newtonsoft.Json.dll");
            return new SamplePdbQuery()
            {
                AssemblyFullPath = assemblyFullPath,
                PdbFilePath = Path.ChangeExtension(assemblyFullPath, "pdb"),
                SampleFilePath = @"/_/Src/Newtonsoft.Json/JsonReader.cs",
                Line = 540,
                Column = 21,
                ClassName = "Newtonsoft.Json.JsonReader",
                MethodName = "ReadAsBytes",
                MethodToken = 100663545
            };
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
                MethodToken = 100666261
            };
        }
    }
}