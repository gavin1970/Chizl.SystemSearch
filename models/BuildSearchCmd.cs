﻿using System;
using System.Collections.Generic;

namespace Chizl.SystemSearch
{
    internal enum CommandType
    {
        path,
        ext,
        filter,
        exclude
    }

    /// <summary>
    /// Set this up to easily switch commands bounderies.
    /// [ext:txt,doc,docx]
    /// </summary>
    internal readonly struct Seps
    {
        //public static string sStart { get; } = "[";
        //public static string sEnd { get; } = "]";
        //public static string sOr { get; } = "|";
        //public static string sWild { get; } = "*";
        //public static string sMulti { get; } = ",";
        public static char cStart { get; } = '[';
        public static char cEnd { get; } = ']';
        public static char cOr { get; } = '|';
        public static char cWild { get; } = '*';
        public static char cMulti { get; } = '·';       // Alt-250 = ·
        public static char cCmdEnd { get; } = ':';
        public static char cFilterPos { get; } = '■';   // Alt-254 = ■
        public static char cExtPos { get; } = '☻';      // Alt-258 = ☻
        public static char cPathPos { get; } = '♥';     // Alt-259 = ♥
        public static string GetCommandString(CommandType cmdType) => $"{cmdType}{cCmdEnd}";
        public static string GetCommandToken(CommandType cmdType) => cmdType == CommandType.ext ? $"{cExtPos}" : (cmdType == CommandType.filter || cmdType == CommandType.exclude) ? $"{cFilterPos}" : $"{cPathPos}";
    }

    internal static class SepsExt
    {
        public static string Str(this Seps @seps) => $"{@seps}";
    }

    internal class BuildSearchCmd
    {
        private string[] _searchCriteria = new string[0];
        private readonly string[] _spaceRemovals = new string[4] { ":", "|", "[", "]" };

        private BuildSearchCmd() { IsEmpty = true; }
        public BuildSearchCmd(ref string searchCriteria) => _searchCriteria = FindCommands(ref searchCriteria);

        public static BuildSearchCmd Empty { get { return new BuildSearchCmd(); } }
        public bool IsEmpty { get; }

        public List<SearchCommand> Commands { get; } = new List<SearchCommand>();
        public string[] SearchCriteria => _searchCriteria;
        private string DupSearchReplace(string text, string[] searches, string replaceWith, bool trim = true)
        {
            bool hadChange = true;
            while (hadChange)
            {
                hadChange = false;
                foreach (var search in searches)
                {
                    while (text.Contains(search))
                    {
                        hadChange = true;
                        text = text.Replace(search, replaceWith);
                        if (trim)
                            text = text.Trim();
                    }
                }
            }

            return text;
        }
        private string DupSearchReplace(string text, string search, string replaceWith, bool trim = true) => DupSearchReplace(text, new string[] { search }, replaceWith, trim);
        private string TrimOff(string search, char chr)
        {
            search = search.Trim();
            while (search.StartsWith(chr.ToString()))
                search = search.TrimStart(chr).Trim();

            while (search.EndsWith(chr.ToString()))
                search = search.TrimEnd(chr).Trim();
            return search;
        }
        private string[] FindCommands(ref string searchCriteria)
        {
            var hasPathSearch = false;
            var hasExtSearch = false;
            var hasFilter = false;

            // confort chars people like to use to seperate things.
            searchCriteria = searchCriteria.Replace($",", $"")
                                           .Replace($"+", $"")
                                           .Replace($";", $"").Trim();

            // replace all spaces in front of or after these chars.
            foreach (var ch in _spaceRemovals)
            {
                // this will auto correct the following type of query:
                //      "landon + ,  [path:code|gavin] ;  [ext: .txt | pdf |. doc | docx | .mp4]"
                // to look like this:
                //      "landon[path:code|gavin][ext:.txt|pdf|. doc|docx|.mp4]"
                searchCriteria = DupSearchReplace(searchCriteria, new string[] { $"{ch} ", $" {ch}" }, $"{ch}");
            }

            var retVal = searchCriteria;

            if (!searchCriteria.Contains($"{Seps.cStart}") || !searchCriteria.Contains($"{Seps.cEnd}"))
                return retVal.SplitOn(Seps.cWild);

            searchCriteria = searchCriteria.Replace($"{Seps.cStart}", $"{Seps.cMulti}{Seps.cStart}")
                                           .Replace($"{Seps.cEnd}", $"{Seps.cEnd}{Seps.cMulti}").Trim();

            // trim out any spaces around the multi-search extensions
            searchCriteria = DupSearchReplace(searchCriteria, new string[] { $"{Seps.cMulti} ", $" {Seps.cMulti}" }, $"{Seps.cMulti}");

            // This is used for the search criteria
            var cmdPatterns = searchCriteria.SplitOn(Seps.cMulti);

            // This is the search string being reformatted and will be what is sent
            // as a SearchMessageType.SearchQueryUsed event.
            // It will be up to the UI if it wants to update it's own search bar or not.
            searchCriteria = searchCriteria.Replace($"{Seps.cMulti}", "");
            searchCriteria = searchCriteria.Replace($"{Seps.cStart}", $" + {Seps.cStart}");
            searchCriteria = searchCriteria.Replace($"{Seps.cEnd}", $"{Seps.cEnd} + ").Trim();

            searchCriteria = TrimOff(searchCriteria, '+');
            searchCriteria = DupSearchReplace(searchCriteria, "  ", " ");
            searchCriteria = DupSearchReplace(searchCriteria, " + + ", " + ");

            foreach (var cmd in cmdPatterns)
            {
                var iS = cmd.IndexOf(Seps.cStart);
                if (iS == -1)
                    continue;

                var nS = iS + 1;
                var iE = cmd.IndexOf(Seps.cEnd, nS);
                if (iE == -1)
                    continue;

                // get command param, add a byte to end, for removeal
                var search = cmd.Substring(nS, iE++ - nS);
                // remove command and param from search string.
                var remove = cmd.Substring(iS, iE - iS);

                // if the search extension doesn't have the ext type seperated from the values, we can't tell what is needed, lets skip extension.
                var srchSep = search.IndexOf(Seps.cCmdEnd);
                if (srchSep == -1)
                    continue;

                var cmdType = CommandType.ext;
                // get command type
                switch (search.Substring(0, srchSep).ToLowerInvariant())
                {
                    case "ext":
                        cmdType = CommandType.ext;
                        break;
                    case "path":
                        cmdType = CommandType.path;
                        break;
                    case "filter":
                        cmdType = CommandType.filter;
                        break;
                    case "exclude":
                        cmdType = CommandType.exclude;
                        break;
                    default:
                        // no idea what was passed, but it wasn't anything expected.
                        continue;
                }

                // this will resolve "ext:.txt|pdf|. doc|docx|.mp4", to look like: "ext:.txt|pdf|.doc|docx|.mp4"
                // path and filter could have spaces within folder / file names, so we will not replace them.
                if (cmdType.Equals(CommandType.ext))
                {
                    searchCriteria = searchCriteria.Replace(search, search.Replace(" ", ""));
                    search = search.Replace(" ", "");
                }

                // use for token later.
                hasPathSearch = hasPathSearch || cmdType == CommandType.path;
                // use for token later.
                hasExtSearch = hasExtSearch || cmdType == CommandType.ext;
                // use for token later.
                hasFilter = hasFilter || cmdType == CommandType.filter || cmdType == CommandType.exclude;

                // replace the command with a token for search order
                retVal = retVal.Replace(remove, "");

                if (hasPathSearch && search.IndexOf(Seps.cOr) == -1)
                {
                    hasPathSearch = false;
                    var part = cmdType.ToString();
                    search = search.StartsWith(part, StringComparison.CurrentCultureIgnoreCase) ? search.Substring(part.Length + 1) : search;
                    retVal = $"{search}{Seps.cWild}{retVal}";
                }
                else
                    // Process one or more of existing type
                    MultiBuildCommands(cmdType, search);
            }

            // Add search tokens at the end if tokens exists.
            // Can have one or more tokens. Loading filters/exclude first, this removes
            // the larger count of files before running the other extensions.
            retVal = retVal.Trim();
            retVal += hasPathSearch ? $"{Seps.cWild}{Seps.GetCommandToken(CommandType.path)}{Seps.cWild}" : "";
            retVal += hasFilter ? $"{Seps.cWild}{Seps.GetCommandToken(CommandType.filter)}{Seps.cWild}" : "";
            retVal += hasExtSearch ? $"{Seps.cWild}{Seps.GetCommandToken(CommandType.ext)}{Seps.cWild}" : "";

            // SplitOn will auto strip ** by ignoring blank entries if exists.
            return retVal.Replace(",", "").SplitOn(Seps.cWild);
        }
        private void MultiBuildCommands(CommandType cmdType, string search)
        {
            var part = cmdType.ToString();
            search = search.StartsWith(part, StringComparison.CurrentCultureIgnoreCase) ? search.Substring(part.Length + 1) : search;
            var multiPart = search.SplitOn(Seps.cOr);
            foreach (var cmd in multiPart)
                Commands.Add(new SearchCommand(cmdType, cmd));
        }
    }

    internal class SearchCommand
    {
        public SearchCommand(CommandType searchPart, string search)
        {
            CommandType = searchPart;
            Search = (searchPart.Equals(CommandType.ext)
                ? SetExt(search)                            // strips all spaces and adds '.' at the start, if not there.
                : search)
                .Replace(Seps.cWild.ToString(), "");        // wild cards are not supported in search extensions
        }
        public CommandType CommandType { get; }
        public string Search { get; }

        private string SetExt(string search)
        {
            search = search.Replace(" ", "").Trim();
            if (search.StartsWith("."))     // File could have multiple '.', only checking the first char exists.
                return search.Trim();
            else
                return $".{search.Trim()}";
        }
    }
}
