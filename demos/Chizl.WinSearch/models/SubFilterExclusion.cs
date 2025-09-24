namespace Chizl.SearchSystemUI
{
    public class SubFilterExclusion
    {
        public SubFilterExclusion(string filter, FilterType type)
        {
            this.Filter = filter.ToLower();
            this.FilterRaw = filter;
            this.Type = type != FilterType.Unknown ? type : FindType(filter);
        }

        public string Filter { get; }
        public string FilterRaw { get; }
        public FilterType Type { get; }

        private FilterType FindType(string filter)
        {
            var trimmed = filter.Trim();

            if (trimmed.StartsWith("."))
                return FilterType.Extension;
            else if (trimmed.Length <= 3 && trimmed[1].Equals(':'))
                return FilterType.Drive;
            else if (string.IsNullOrWhiteSpace(trimmed))
                return FilterType.NoExtension;
            else 
                return FilterType.Contains;
        }
    }
}
