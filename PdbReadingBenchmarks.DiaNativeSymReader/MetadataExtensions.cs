using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace PdbReadingBenchmarks.DiaNativeSymReader
{
    internal static class MetadataExtensions
    {
        internal static int GetMethodToken(this MethodInfo methodInfo)
        {
#if NETSTANDARD1_3
            var methodToken = methodInfo.GetMetadataToken();
#else
            var methodToken = methodInfo.MetadataToken;
#endif

            return methodToken;
        }

        internal static MethodDebugInformationHandle GetMethodDebugInformationHandle(this MethodInfo methodInfo)
        {
            var methodToken = methodInfo.GetMethodToken();
            var handle = ((MethodDefinitionHandle)MetadataTokens.Handle(methodToken)).ToDebugInformationHandle();
            return handle;
        }
    }
}