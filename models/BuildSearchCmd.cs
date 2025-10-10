using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace Chizl.SystemSearch
{
    internal enum CommandType
    {
        Path,
        Ext
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
        public static char cIgnore { get; } = '■';      // Alt-254 = ■
        public static char cExtPos { get; } = '☻';      // Alt-258 = ☻
        public static char cPathPos { get; } = '♥';     // Alt-259 = ♥
        public static string GetCommandString(CommandType cmdType) => $"{cmdType}{cCmdEnd}";
        public static string GetCommandToken(CommandType cmdType) => cmdType == CommandType.Ext ? $"{cExtPos}" : $"{cPathPos}";
    }

    internal class BuildSearchCmd
    {
        private string[] _searchCriteria = new string[0];
        private BuildSearchCmd() { IsEmpty = true; }
        public BuildSearchCmd(string searchCriteria) => _searchCriteria = FindCommands(searchCriteria);

        public static BuildSearchCmd Empty { get { return new BuildSearchCmd(); } }
        public bool IsEmpty { get; }

        public List<SearchCommand> Commands { get; } = new List<SearchCommand>();
        public string[] SearchCriteria => _searchCriteria;
        /*
        private string[] GetAllCommands(string searchCriteria)
        {
            var retVal = new List<string>();
            var defVal = searchCriteria.SplitOn(Seps.cIgnore);

            if (!searchCriteria.Contains(Seps.cStart) || !searchCriteria.Contains(Seps.cEnd))
                return defVal;

            if (retVal.Count.Equals(0))
                return defVal;
            else
                return retVal.ToArray();
        }
        /**/
        private string[] FindCommands(string searchCriteria)
        {
            string[] spaceRemovals = new string[] { ":", "|", "[", "]" };

            // confort chars people like to use to seperate things.
            searchCriteria = searchCriteria.Replace($",", $"")
                                           .Replace($"+", $"")
                                           .Replace($";", $"").Trim();

            foreach (var ch in spaceRemovals)
            {
                // this will auto correct the following type of query:
                //      "landon + ,  [path:code|gavin] ;  [ext: .txt | pdf |. doc | docx | .mp4]"
                // to look like this:
                //      "landon[path:code|gavin][ext:.txt|pdf|. doc|docx|.mp4]"
                while (searchCriteria.Contains($"{ch} ") || searchCriteria.Contains($" {ch}"))
                {
                    searchCriteria = searchCriteria.Replace($"{ch} ", $"{ch}").Trim();
                    searchCriteria = searchCriteria.Replace($" {ch}", $"{ch}").Trim();
                }
            }

            var retVal = searchCriteria;

            if (!searchCriteria.Contains(Seps.cStart) || !searchCriteria.Contains(Seps.cEnd))
                return retVal.SplitOn(Seps.cWild);

            searchCriteria = searchCriteria.Replace($"{Seps.cStart}", $"{Seps.cMulti}{Seps.cStart}")
                                           .Replace($"{Seps.cEnd}", $"{Seps.cEnd}{Seps.cMulti}").Trim();

            while (searchCriteria.Contains($"{Seps.cMulti} ")
                || searchCriteria.Contains($" {Seps.cMulti}"))
            {
                searchCriteria = searchCriteria.Replace($"{Seps.cMulti} ", $"{Seps.cMulti}").Trim();
                searchCriteria = searchCriteria.Replace($" {Seps.cMulti}", $"{Seps.cMulti}").Trim();
            }

            var cmdPatterns = searchCriteria.SplitOn(Seps.cMulti);
            var hasExtSearch = false;
            var hasPathSearch = false;
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
                // get command type
                var cmdType = search.StartsWith(Seps.GetCommandString(CommandType.Ext), StringComparison.CurrentCultureIgnoreCase) ? CommandType.Ext : CommandType.Path;

                // this will resolve "ext:.txt|pdf|. doc|docx|.mp4", to look like: "ext:.txt|pdf|.doc|docx|.mp4"
                if (cmdType.Equals(CommandType.Ext))
                    search = search.Replace(" ", "");

                // use for token later.
                hasPathSearch = hasPathSearch || cmdType == CommandType.Path;
                // use for token later.
                hasExtSearch = hasExtSearch || cmdType == CommandType.Ext;

                // replace the command with a token for search order
                retVal = retVal.Replace(remove, "");
                // Process one or more of existing type
                MultiBuildCommands(cmdType, search);
            }

            // add search token at the start if exists.  Can have 1 or both tokens, but both up front and Path before Ext.
            retVal = hasExtSearch ? $"*{Seps.GetCommandToken(CommandType.Ext)}*{retVal.Trim()}" : retVal.Trim();
            retVal = hasPathSearch ? $"*{Seps.GetCommandToken(CommandType.Path)}*{retVal.Trim()}" : retVal.Trim();
            // SplitOn will auto strip ** by ignoring blank entries if exists.
            return retVal.Replace(",", "").SplitOn(Seps.cWild);
        }
        private void MultiBuildCommands(CommandType cmdType, string search)
        {
            var part = cmdType.ToString().ToLower();
            search = search.StartsWith(part, StringComparison.CurrentCultureIgnoreCase) ? search.Substring(part.Length+1) : search;
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
            Search = (searchPart.Equals(CommandType.Ext) ? ($".{search.Replace(".", "")}".ToLower()) : search).Replace(Seps.cWild.ToString(), "");
        }
        public CommandType CommandType { get; }
        public string Search { get; }
    }
}
