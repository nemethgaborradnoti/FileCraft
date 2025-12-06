using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Helpers;
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
            Owner = Application.Current.MainWindow;
            PromptText.Text = string.Format(ResourceHelper.GetString("RenamePreset_Prompt"), presetNumber);
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
                    ResourceHelper.GetString("RenamePreset_InvalidInputTitle"),
                    ResourceHelper.GetString("RenamePreset_InvalidInputMessage"),
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