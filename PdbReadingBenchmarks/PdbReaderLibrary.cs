namespace PdbReadingBenchmarks
{
    public enum PdbReaderLibrary
    {
        /// <summary>
        /// DbgHelp is a Win32 API that supports reading Windows PDBs.
        /// </summary>
        DbgHelp,
        
        /// <summary>
        /// Use the DiaSymReader COM interfaces (ISymUnmanagedReader, ISymUnmanagedBinder, etc.)
        /// For Windows PDBs, this utilizes Microsoft.DiaSymReader.Native.
        /// For portable PDBs, this utilizes Microsoft.DiaSymReader.PortablePdb
        /// (which utilizes System.Reflection.Metadata internally).
        /// </summary>
        DiaNativeSymReader,
        
        /// <summary>
        /// Use Mono.Cecil ( https://github.com/jbevain/cecil )
        /// </summary>
        MonoCecil,
        
        /// <summary>
        /// Use dnlib. dnlib supports several approaches for reading windows PDBs (see https://github.com/0xd4d/dnlib#windows-pdbs )
        /// In this option, dnlib will internally use a managed reader for both portable and Windows PDBs.
        /// </summary>
        Dnlib_Managed,
        
        /// <summary>
        /// Use dnlib. dnlib supports several approaches for reading windows PDBs (see https://github.com/0xd4d/dnlib#windows-pdbs )
        /// In this option, dnlib will internally use a managed reader for portable PDBs, and use Microsoft.DiaSymReader.Native for Windows PDBs.
        /// </summary>
        Dnlib_DiaSymReader,
    }
}