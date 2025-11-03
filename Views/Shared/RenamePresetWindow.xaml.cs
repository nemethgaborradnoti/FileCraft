using FileCraft.Models;
using FileCraft.Services.Interfaces;
using System.Windows;

namespace FileCraft.Views.Shared
{
    public partial class RenamePresetWindow : Window
    {
        public string NewPresetName { get; private set; } = string.Empty;
        private readonly IDialogService _dialogService;

        public RenamePresetWindow(string currentName, int presetNumber, IDialogService dialogService)
        {
            InitializeComponent();
            Owner = System.Windows.Application.Current.MainWindow;
            PromptText.Text = $"Preset {presetNumber} name:";
            NameTextBox.Text = currentName;
            _dialogService = dialogService;
            NameTextBox.Focus();
            NameTextBox.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                NewPresetName = NameTextBox.Text.Trim();
                DialogResult = true;
                Close();
            }
            else
            {
                _dialogService.ShowNotification(
                    "Invalid Input",
                    "Preset name cannot be empty.",
                    DialogIconType.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
