namespace FileCraft.Models
{
    public class FileExportResult
    {
        public string OutputFilePath { get; set; } = string.Empty;
        public long ExportedLines { get; set; }
        public long ExportedCharacters { get; set; }
        public int FilesWithIgnoredCommentsCount { get; set; }
        public long IgnoredLines { get; set; }
        public long IgnoredCharacters { get; set; }
    }
}