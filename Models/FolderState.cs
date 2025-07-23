namespace FileCraft.Models
{
    public class FolderState
    {
        public string FullPath { get; set; } = string.Empty;

        public bool? IsSelected { get; set; }

        public bool IsExpanded { get; set; }
    }
}
