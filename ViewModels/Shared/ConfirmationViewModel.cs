namespace FileCraft.ViewModels.Shared
{
    public class ConfirmationViewModel : BaseViewModel
    {
        public string ActionName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? FilesAffected { get; set; }
        public string IconPath { get; set; } = string.Empty;
    }
}