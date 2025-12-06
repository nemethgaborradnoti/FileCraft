namespace FileCraft.ViewModels.Shared
{
    public class ConfirmationViewModel : BaseViewModel
    {
        public string ActionName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? FilesAffected { get; set; }
        public string IconGlyph { get; set; } = string.Empty;
        public Brush IconBrush { get; set; } = Brushes.Black;

        public bool IsCopyTreeMessage { get; set; }
        public string? SourceTabName { get; set; }
        public string? SourceTabIcon { get; set; }
        public int SourceFolderCount { get; set; }
        public string? DestinationTabName { get; set; }
        public string? DestinationTabIcon { get; set; }
        public int DestinationFolderCount { get; set; }
    }
}