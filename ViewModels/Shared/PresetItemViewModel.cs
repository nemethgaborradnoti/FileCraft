using FileCraft.Shared.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FileCraft.ViewModels.Shared
{
    public class PresetItemViewModel : INotifyPropertyChanged
    {
        public object Id { get; }
        public string Name { get; }
        public DateTime LastModified { get; }
        public string Description { get; }
        public object RawData { get; }

        public string DisplayDateString
        {
            get
            {
                var timeSpan = DateTime.Now - LastModified;
                string relativeTime;

                if (timeSpan.TotalMinutes < 1)
                {
                    relativeTime = "Just now";
                }
                else if (timeSpan.TotalHours < 1)
                {
                    int minutes = (int)timeSpan.TotalMinutes;
                    relativeTime = $"{minutes} minute{(minutes != 1 ? "s" : "")} ago";
                }
                else if (timeSpan.TotalDays < 1)
                {
                    int hours = (int)timeSpan.TotalHours;
                    relativeTime = $"{hours} hour{(hours != 1 ? "s" : "")} ago";
                }
                else
                {
                    int days = (int)timeSpan.TotalDays;
                    relativeTime = $"{days} day{(days != 1 ? "s" : "")} ago";
                }

                return $"{LastModified:yyyy.MM.dd HH:mm} • {relativeTime}";
            }
        }

        public ICommand LoadCommand { get; }
        public ICommand OverwriteCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand ViewDetailsCommand { get; }

        public event Action<PresetItemViewModel>? LoadRequested;
        public event Action<PresetItemViewModel>? OverwriteRequested;
        public event Action<PresetItemViewModel>? DeleteRequested;
        public event Action<PresetItemViewModel>? RenameRequested;
        public event Action<PresetItemViewModel>? ViewDetailsRequested;

        public PresetItemViewModel(object id, string name, DateTime lastModified, string description, object rawData)
        {
            Id = id;
            Name = name;
            LastModified = lastModified;
            Description = description;
            RawData = rawData;

            LoadCommand = new RelayCommand(_ => LoadRequested?.Invoke(this));
            OverwriteCommand = new RelayCommand(_ => OverwriteRequested?.Invoke(this));
            DeleteCommand = new RelayCommand(_ => DeleteRequested?.Invoke(this));
            RenameCommand = new RelayCommand(_ => RenameRequested?.Invoke(this));
            ViewDetailsCommand = new RelayCommand(_ => ViewDetailsRequested?.Invoke(this));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}