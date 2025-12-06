using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Shared;
using FileCraft.Views.Shared;
using Fonts;

namespace FileCraft.Services
{
    public class DialogService : IDialogService
    {
        public string? SelectFolder(string description)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = description
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FolderName;
            }

            return null;
        }

        public void ShowNotification(string title, string message, DialogIconType iconType)
        {
            var (iconGlyph, iconBrush) = GetMaterialIconData(iconType);
            var viewModel = new GenericDialogViewModel
            {
                Title = title,
                Message = message,
                IconGlyph = iconGlyph,
                IconBrush = iconBrush,
                PrimaryButtonText = ResourceHelper.GetString("Common_OkButton"),
                PrimaryButtonStyle = "PrimaryButton"
            };
            var window = new GenericDialogWindow(viewModel);
            window.ShowDialog();
        }

        public bool ShowConfirmation(string title, string message, DialogIconType iconType, int? filesAffected = null)
        {
            var (iconGlyph, iconBrush) = GetMaterialIconData(iconType);
            var viewModel = new GenericDialogViewModel
            {
                Title = title,
                Message = message,
                IconGlyph = iconGlyph,
                IconBrush = iconBrush,
                FilesAffected = filesAffected,
                PrimaryButtonText = ResourceHelper.GetString("Common_YesButton"),
                PrimaryButtonStyle = "PrimaryButton",
                SecondaryButtonText = ResourceHelper.GetString("Common_CancelButton")
            };
            var window = new GenericDialogWindow(viewModel);
            return window.ShowDialog() ?? false;
        }

        public bool ShowCopyTreeConfirmation(string title, DialogIconType iconType, string sourceName, string? sourceIcon, int sourceCount, string destName, string? destIcon, int destCount)
        {
            var (iconGlyph, iconBrush) = GetMaterialIconData(iconType);
            var viewModel = new GenericDialogViewModel
            {
                Title = title,
                IconGlyph = iconGlyph,
                IconBrush = iconBrush,
                IsCopyTreeVisible = true,
                SourceTabName = sourceName,
                SourceTabIcon = sourceIcon,
                SourceFolderCount = sourceCount,
                DestinationTabName = destName,
                DestinationTabIcon = destIcon,
                DestinationFolderCount = destCount,
                PrimaryButtonText = ResourceHelper.GetString("Common_YesButton"),
                PrimaryButtonStyle = "WarningButton",
                SecondaryButtonText = ResourceHelper.GetString("Common_CancelButton")
            };
            var window = new GenericDialogWindow(viewModel);
            return window.ShowDialog() ?? false;
        }

        public ExitConfirmationResult ShowExitConfirmation(string title, string message)
        {
            var (iconGlyph, iconBrush) = GetMaterialIconData(DialogIconType.Warning);
            var viewModel = new GenericDialogViewModel
            {
                Title = title,
                Message = message,
                IconGlyph = iconGlyph,
                IconBrush = iconBrush,
                PrimaryButtonText = ResourceHelper.GetString("Common_SaveButton"),
                PrimaryButtonStyle = "PrimaryButton",
                TertiaryButtonText = ResourceHelper.GetString("ExitConfirmation_DontSaveButton"),
                TertiaryButtonStyle = "DangerButton",
                SecondaryButtonText = ResourceHelper.GetString("Common_CancelButton")
            };

            var window = new GenericDialogWindow(viewModel);
            window.ShowDialog();
            return viewModel.ExitResult;
        }

        public string? ShowRenamePresetDialog(string currentName, int presetNumber)
        {
            var viewModel = new GenericDialogViewModel
            {
                Title = ResourceHelper.GetString("RenamePreset_Title"),
                Message = string.Format(ResourceHelper.GetString("RenamePreset_Prompt"), presetNumber),
                IsInputVisible = true,
                InputText = currentName,
                PrimaryButtonText = ResourceHelper.GetString("Common_OkButton"),
                PrimaryButtonStyle = "PrimaryButton",
                SecondaryButtonText = ResourceHelper.GetString("Common_CancelButton")
            };

            var window = new GenericDialogWindow(viewModel);
            if (window.ShowDialog() == true)
            {
                return viewModel.InputText;
            }
            return null;
        }

        public string? ShowEditIgnoredFoldersDialog(string currentFolders)
        {
            var viewModel = new GenericDialogViewModel
            {
                Title = ResourceHelper.GetString("EditIgnored_Title"),
                Message = ResourceHelper.GetString("EditIgnored_Instruction"),
                IsInputVisible = true,
                InputText = currentFolders,
                PrimaryButtonText = ResourceHelper.GetString("Common_OkButton"),
                PrimaryButtonStyle = "PrimaryButton",
                SecondaryButtonText = ResourceHelper.GetString("Common_CancelButton")
            };

            var window = new GenericDialogWindow(viewModel);
            if (window.ShowDialog() == true)
            {
                return viewModel.InputText;
            }
            return null;
        }

        public bool ShowBulkSearchDialog(IEnumerable<SelectableFile> allFiles)
        {
            var (iconGlyph, iconBrush) = GetMaterialIconData(DialogIconType.Info);
            var bulkSearchWindow = new BulkSearchWindow(allFiles, iconGlyph, iconBrush);
            return bulkSearchWindow.ShowDialog() ?? false;
        }

        public IEnumerable<string>? ShowIgnoredCommentsDialog(IEnumerable<SelectableFile> selectedFiles, IEnumerable<string> previouslyIgnoredFiles)
        {
            var window = new IgnoredCommentsWindow(selectedFiles, previouslyIgnoredFiles);
            if (window.ShowDialog() == true)
            {
                return window.GetIgnoredFilePaths();
            }
            return null;
        }

        private (string Glyph, Brush Brush) GetMaterialIconData(DialogIconType iconType)
        {
            string glyph = iconType switch
            {
                DialogIconType.Info => MaterialIcons.info,
                DialogIconType.Warning => MaterialIcons.warning,
                DialogIconType.Success => MaterialIcons.check_circle,
                DialogIconType.Error => MaterialIcons.cancel,
                _ => MaterialIcons.help_outline
            };

            string colorKey = iconType switch
            {
                DialogIconType.Info => "PrimaryBrush",
                DialogIconType.Warning => "DangerBrush",
                DialogIconType.Success => "SuccessBrush",
                DialogIconType.Error => "DangerBrush",
                _ => "TextBrush"
            };

            var brush = Application.Current.FindResource(colorKey) as Brush ?? Brushes.Black;
            return (glyph, brush);
        }
    }
}