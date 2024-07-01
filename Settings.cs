using System.Collections.Generic;

namespace CodeAggregatorGtk
{
    public class Settings
    {
        public string SourceFolder { get; set; } = string.Empty;
        public string OutputFile { get; set; } = string.Empty;
        public List<string> SelectedNodes { get; set; } = new List<string>();
        public Dictionary<string, string> OutputPaths { get; set; } = new Dictionary<string, string>();
    }
}