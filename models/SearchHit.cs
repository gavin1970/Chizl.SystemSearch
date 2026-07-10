namespace Chizl.SystemSearch
{
    internal enum IsFileBinary
    {
        NotVerified = 0,
        Yes = 1,
        No = 2
    }

    //public readonly record struct SearchHit(long LineNumber, int CharPosition, string LineText);
    internal sealed class SearchHit
    {
        public string Searched { get; set; }
        public long LineNumber { get; set; }
        public int CharPosition { get; set; }
        public string LineText { get; set; }
        public string Snippet { get; set; }
        public IsFileBinary IsBinary { get; set; }

        public SearchHit(string searched, long lineNumber, int charPosition, string lineText, string snippet, IsFileBinary isBinary)
        {
            Searched = searched;
            LineNumber = lineNumber;
            CharPosition = charPosition;
            LineText = lineText;
            Snippet = snippet;
            IsBinary = isBinary;
        }
    }
}
