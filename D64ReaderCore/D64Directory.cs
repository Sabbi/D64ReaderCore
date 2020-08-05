using System.Collections.Generic;

namespace D64Reader
{
    public class D64Directory
    {
        public string DiskType { get; set; }
        public IEnumerable<DirectoryItem> DirectoryItems { get; set; }
        public string DiskName { get; set; }
        public string DiskId { get; set; }
        public int FreeBlocks { get; set; }
    }
}