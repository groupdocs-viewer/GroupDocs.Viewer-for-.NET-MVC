using System.Collections.Generic;

namespace Viewer.Wrapper
{
    public class PostedDataWrapper
    {
        public string path { get; set; }
        public string guid { get; set; }
        public bool htmlMode { get; set; }
        public string password { get; set; }
        public int page { get; set; }
        public int angle { get; set; }
        public List<int> pages { get; set; }
        public string url { get; set; }
        public bool rewrite { get; set; }
    }
}