using FileCraft.Services.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileCraft.Services
{
    public class SharedStateService : ISharedStateService
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _sourcePath = string.Empty;
        public string SourcePath
        {
            get => _sourcePath;
            set
            {
                if (_sourcePath != value)
                {
                    _sourcePath = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _destinationPath = string.Empty;
        public string DestinationPath
        {
            get => _destinationPath;
            set
            {
                if (_destinationPath != value)
                {
                    _destinationPath = value;
                    OnPropertyChanged();
                }
            }
        }

        private List<string> _ignoredFolders = new();
        public List<string> IgnoredFolders
        {
            get => _ignoredFolders;
            set
            {
                if (_ignoredFolders != value)
                {
                    _ignoredFolders = value;
                    OnPropertyChanged();
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
