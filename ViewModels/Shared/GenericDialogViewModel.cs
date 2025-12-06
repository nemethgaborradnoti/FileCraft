using FileCraft.Models;
using FileCraft.Shared.Commands;
using System.Windows.Input;
using System.Windows.Media;

namespace FileCraft.ViewModels.Shared
{
    public class GenericDialogViewModel : BaseViewModel
    {
        private string _title = string.Empty;
        private string _message = string.Empty;
        private string _iconGlyph = string.Empty;
        private Brush _iconBrush = Brushes.Black;
        private string _inputText = string.Empty;
        private bool _isInputVisible;
        private bool _isCopyTreeVisible;
        private string _primaryButtonText = "OK";
        private string _secondaryButtonText = string.Empty;
        private string _tertiaryButtonText = string.Empty;
        private string _primaryButtonStyle = "PrimaryButton";
        private string _tertiaryButtonStyle = "DangerButton";

        public event Action<bool?>? RequestClose;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(); }
        }

        public string IconGlyph
        {
            get => _iconGlyph;
            set { _iconGlyph = value; OnPropertyChanged(); }
        }

        public Brush IconBrush
        {
            get => _iconBrush;
            set { _iconBrush = value; OnPropertyChanged(); }
        }

        public bool IsInputVisible
        {
            get => _isInputVisible;
            set { _isInputVisible = value; OnPropertyChanged(); }
        }

        public string InputText
        {
            get => _inputText;
            set
            {
                _inputText = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsCopyTreeVisible
        {
            get => _isCopyTreeVisible;
            set { _isCopyTreeVisible = value; OnPropertyChanged(); }
        }

        public string PrimaryButtonText
        {
            get => _primaryButtonText;
            set { _primaryButtonText = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsPrimaryVisible)); }
        }

        public string SecondaryButtonText
        {
            get => _secondaryButtonText;
            set { _secondaryButtonText = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsSecondaryVisible)); }
        }

        public string TertiaryButtonText
        {
            get => _tertiaryButtonText;
            set { _tertiaryButtonText = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsTertiaryVisible)); }
        }

        public string PrimaryButtonStyle
        {
            get => _primaryButtonStyle;
            set { _primaryButtonStyle = value; OnPropertyChanged(); }
        }

        public string TertiaryButtonStyle
        {
            get => _tertiaryButtonStyle;
            set { _tertiaryButtonStyle = value; OnPropertyChanged(); }
        }

        public bool IsPrimaryVisible => !string.IsNullOrEmpty(PrimaryButtonText);
        public bool IsSecondaryVisible => !string.IsNullOrEmpty(SecondaryButtonText);
        public bool IsTertiaryVisible => !string.IsNullOrEmpty(TertiaryButtonText);

        public string? SourceTabName { get; set; }
        public string? SourceTabIcon { get; set; }
        public int SourceFolderCount { get; set; }
        public string? DestinationTabName { get; set; }
        public string? DestinationTabIcon { get; set; }
        public int DestinationFolderCount { get; set; }

        public int? FilesAffected { get; set; }

        public ExitConfirmationResult ExitResult { get; private set; } = ExitConfirmationResult.Cancel;

        public ICommand PrimaryCommand { get; }
        public ICommand SecondaryCommand { get; }
        public ICommand TertiaryCommand { get; }

        public GenericDialogViewModel()
        {
            PrimaryCommand = new RelayCommand(OnPrimary, CanExecutePrimary);
            SecondaryCommand = new RelayCommand(OnSecondary);
            TertiaryCommand = new RelayCommand(OnTertiary);
        }

        private bool CanExecutePrimary(object? parameter)
        {
            if (IsInputVisible)
            {
                return !string.IsNullOrWhiteSpace(InputText);
            }
            return true;
        }

        private void OnPrimary(object? parameter)
        {
            ExitResult = ExitConfirmationResult.Save;
            RequestClose?.Invoke(true);
        }

        private void OnSecondary(object? parameter)
        {
            ExitResult = ExitConfirmationResult.Cancel;
            RequestClose?.Invoke(false);
        }

        private void OnTertiary(object? parameter)
        {
            ExitResult = ExitConfirmationResult.DontSave;
            RequestClose?.Invoke(true);
        }
    }
}