namespace FileCraft.Models
{
    public class PresetStatistics
    {
        public int FolderCount { get; set; }
        public int FileCount { get; set; }
        public long TotalSize { get; set; }
        public long TotalLineCount { get; set; }
        public long TotalCharacterCount { get; set; }
    }
}