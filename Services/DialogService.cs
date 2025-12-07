using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Shared;
using FileCraft.Views.Shared;

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
            var iconDef = GetAppIcon(iconType);
            var viewModel = new GenericDialogViewModel
            {
                Title = title,
                Message = message,
                IconGlyph = iconDef.Glyph,
                IconBrush = iconDef.Brush,
                PrimaryButtonText = ResourceHelper.GetString("Common_OkButton"),
                PrimaryButtonStyle = ResourceKeys.PrimaryButton
            };
            var window = new GenericDialogWindow(viewModel);
            window.ShowDialog();
        }

        public bool ShowConfirmation(string title, string message, DialogIconType iconType, int? filesAffected = null)
        {
            var iconDef = GetAppIcon(iconType);
            var viewModel = new GenericDialogViewModel
            {
                Title = title,
                Message = message,
                IconGlyph = iconDef.Glyph,
                IconBrush = iconDef.Brush,
                FilesAffected = filesAffected,
                PrimaryButtonText = ResourceHelper.GetString("Common_YesButton"),
                PrimaryButtonStyle = ResourceKeys.PrimaryButton,
                SecondaryButtonText = ResourceHelper.GetString("Common_CancelButton")
            };
            var window = new GenericDialogWindow(viewModel);
            return window.ShowDialog() ?? false;
        }

        public bool ShowCopyTreeConfirmation(string title, DialogIconType iconType, CopyTreeConfirmationViewModel contentViewModel)
        {
            var iconDef = GetAppIcon(iconType);

            var viewModel = new GenericDialogViewModel
            {
                Title = title,
                IconGlyph = iconDef.Glyph,
                IconBrush = iconDef.Brush,
                CustomContent = contentViewModel,
                PrimaryButtonText = ResourceHelper.GetString("Common_YesButton"),
                PrimaryButtonStyle = ResourceKeys.WarningButton,
                SecondaryButtonText = ResourceHelper.GetString("Common_CancelButton")
            };

            var window = new GenericDialogWindow(viewModel);
            return window.ShowDialog() ?? false;
        }

        public ExitConfirmationResult ShowExitConfirmation(string title, string message)
        {
            var iconDef = AppIcons.Warning;
            var viewModel = new GenericDialogViewModel
            {
                Title = title,
                Message = message,
                IconGlyph = iconDef.Glyph,
                IconBrush = iconDef.Brush,
                PrimaryButtonText = ResourceHelper.GetString("Common_SaveButton"),
                PrimaryButtonStyle = ResourceKeys.PrimaryButton,
                TertiaryButtonText = ResourceHelper.GetString("ExitConfirmation_DontSaveButton"),
                TertiaryButtonStyle = ResourceKeys.DangerButton,
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
                PrimaryButtonStyle = ResourceKeys.PrimaryButton,
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
                PrimaryButtonStyle = ResourceKeys.PrimaryButton,
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
            var iconDef = AppIcons.Info;
            var bulkSearchWindow = new BulkSearchWindow(allFiles, iconDef.Glyph, iconDef.Brush);
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

        private IconDefinition GetAppIcon(DialogIconType iconType)
        {
            return iconType switch
            {
                DialogIconType.Info => AppIcons.Info,
                DialogIconType.Warning => AppIcons.Warning,
                DialogIconType.Success => AppIcons.Success,
                DialogIconType.Error => AppIcons.Error,
                _ => AppIcons.Info
            };
        }
    }
}