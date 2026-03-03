namespace FileCraft.Models
{
    public class PathPreset
    {
        public string Name { get; set; } = string.Empty;
        public List<string> FilePaths { get; set; } = new();
        public DateTime LastModified { get; set; }
    }
}