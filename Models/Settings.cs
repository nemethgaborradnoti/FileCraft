namespace FileCraft.Models
{
    public class Settings
    {
        public string SourcePath { get; set; } = string.Empty;

        public string DestinationPath { get; set; } = string.Empty;

        public List<FolderState> FolderTreeState { get; set; } = new();
    }
}
