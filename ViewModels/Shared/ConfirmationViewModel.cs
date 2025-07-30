using FileCraft.ViewModels;

namespace FileCraft.ViewModels.Shared
{
    public class ConfirmationViewModel : BaseViewModel
    {
        private string _actionName = string.Empty;
        public string ActionName
        {
            get => _actionName;
            set
            {
                _actionName = value;
                OnPropertyChanged();
            }
        }

        private string _destinationPath = string.Empty;
        public string DestinationPath
        {
            get => _destinationPath;
            set
            {
                _destinationPath = value;
                OnPropertyChanged();
            }
        }

        private int _filesAffected;
        public int FilesAffected
        {
            get => _filesAffected;
            set
            {
                _filesAffected = value;
                OnPropertyChanged();
            }
        }
    }
}
