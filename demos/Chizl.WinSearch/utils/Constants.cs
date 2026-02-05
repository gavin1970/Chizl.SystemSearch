using System.Text;

namespace Chizl.WinSearch.utils
{
    internal static class Constants
    {
        private static StringBuilder _searchHelp = new StringBuilder();

        static Constants()
        {
            _searchHelp.AppendLine("Spaces within a search query is literal and text is not case sensitive.  'Google Gemini' means full path and/or filename must contain the space between the query.");
            _searchHelp.AppendLine("Wildcards '*' can be used between values to represent any text or space between query. e.g. 'Google*Gemini'");
            _searchHelp.AppendLine("The use of search tokens are available and start with '[' followed by token command, followed by ':' then search values.");
            _searchHelp.AppendLine(" - Each token value is separated by '|' if more than one and is represented as 'OR' with no limitation on how many values.");
            _searchHelp.AppendLine("   Finally, end the search token with ']'.\n");
            _searchHelp.AppendLine("Search tokens available:");
            _searchHelp.AppendLine(" * 'ext'      - Must have one of the file extensions.  Dot is not required before each value.");
            _searchHelp.AppendLine(" * 'includes' - Must contain within the full path, one of the values set.");
            _searchHelp.AppendLine(" * 'excludes' - Must not contain with the full path, one of the values set.\n");
            _searchHelp.AppendLine("Example search string: Google*Gemini [ext:doc|docx|pdf] [includes: \\users\\|\\windows] [excludes:d:|chrome]");
        }

        public static string SearchHelp { get { return _searchHelp.ToString(); } }
    }
}
