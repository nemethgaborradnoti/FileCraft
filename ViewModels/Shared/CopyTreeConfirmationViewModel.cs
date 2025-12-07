namespace FileCraft.ViewModels.Shared
{
    public class CopyTreeConfirmationViewModel : BaseViewModel
    {
        public string SourceTabName { get; set; } = string.Empty;
        public string? SourceTabIcon { get; set; }
        public Brush? SourceTabIconBrush { get; set; }
        public int SourceFolderCount { get; set; }

        public string DestinationTabName { get; set; } = string.Empty;
        public string? DestinationTabIcon { get; set; }
        public Brush? DestinationTabIconBrush { get; set; }
        public int DestinationFolderCount { get; set; }
    }
}