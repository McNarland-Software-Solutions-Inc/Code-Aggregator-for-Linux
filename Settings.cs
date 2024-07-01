using System.Collections.Generic;

namespace CodeAggregatorGtk
{
    public class Settings
    {
        public string SourceFolder { get; set; } = string.Empty;
        public List<string> Include { get; set; } = new List<string>();
        public List<string> Exclude { get; set; } = new List<string>();
        public string OutputFile { get; set; } = string.Empty;
    }
}
