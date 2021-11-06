using System.Diagnostics;

namespace PdbReadingBenchmarks.Contracts
{
    /// <summary>
    /// A sequence point read from a PDB file or produced by the decompiler.
    /// </summary>
    [DebuggerDisplay("SequencePoint IL_{Offset,h}-IL_{EndOffset,h}, {StartLine}:{StartColumn}-{EndLine}:{EndColumn}, IsHidden={IsHidden}")]
    public class SequencePoint
    {
        /// <summary>
        /// IL start offset.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// IL end offset.
        /// </summary>
        /// <remarks>
        /// This does not get stored in debug information;
        /// it is used internally to create hidden sequence points
        /// for the IL fragments not covered by any sequence point.
        /// </remarks>
        public int EndOffset { get; set; }

        public int StartLine { get; set; }
        public int StartColumn { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }

        public bool IsHidden {
            get { return StartLine == 0xfeefee && StartLine == EndLine; }
        }

        public string DocumentUrl { get; set; }

        internal void SetHidden()
        {
            StartLine = EndLine = 0xfeefee;
        }
    }
}