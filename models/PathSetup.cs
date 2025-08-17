namespace Chizl.SystemSearch
{
    internal class PathSetup
    {
        public PathSetup(string name, bool allowed)
        {
            Name = name;
            Allowed = allowed;
        }
        public string Name { get; }
        public bool Allowed { get; }
    }
}
