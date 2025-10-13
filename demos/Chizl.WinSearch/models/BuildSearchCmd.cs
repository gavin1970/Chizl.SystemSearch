namespace Chizl.SearchSystemUI
{
    internal class BuildSearchCmd
    {
        private BuildSearchCmd() { IsEmpty = true; }
        public BuildSearchCmd(string searchCriteria) { }

        public static BuildSearchCmd Empty { get { return new BuildSearchCmd(); } }
        public bool IsEmpty { get; }


    }
}
