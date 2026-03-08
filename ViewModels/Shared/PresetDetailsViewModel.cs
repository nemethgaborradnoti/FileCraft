using FileCraft.Models;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using Fonts;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FileCraft.ViewModels.Shared
{
    public class DetailItem
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class PresetDetailsViewModel : BaseViewModel
    {
        public string Title { get; }
        public string PresetName { get; }
        public DateTime LastModified { get; }
        public string Description { get; }

        public ObservableCollection<DetailItem> Statistics { get; } = new();
        public ObservableCollection<string> ContentList { get; } = new();
        public string ContentHeader { get; private set; } = "Content";

        public ICommand CloseCommand { get; }

        public event Action? RequestClose;

        public PresetDetailsViewModel(PresetItemViewModel presetItem)
        {
            Title = $"Details: {presetItem.Name}";
            PresetName = presetItem.Name;
            LastModified = presetItem.LastModified;
            Description = presetItem.Description;

            CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());

            LoadData(presetItem.RawData);
        }

        private void LoadData(object rawData)
        {
            if (rawData is PathPreset pathPreset)
            {
                LoadPathPreset(pathPreset);
            }
            else if (rawData is PresetEntity savePreset)
            {
                LoadSavePreset(savePreset);
            }
        }

        private void LoadPathPreset(PathPreset preset)
        {
            Statistics.Add(new DetailItem { Label = "Total Files", Value = preset.FilePaths.Count.ToString(), Icon = MaterialIcons.description });

            ContentHeader = "Included Files";
            foreach (var path in preset.FilePaths.OrderBy(x => x))
            {
                ContentList.Add(path);
            }
        }

        private void LoadSavePreset(PresetEntity preset)
        {
            var data = preset.Data;
            var stats = preset.Statistics;

            string displayVersion = preset.AppVersion;
            if (System.Version.TryParse(displayVersion, out var parsedVersion))
            {
                displayVersion = $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}";
            }

            Statistics.Add(new DetailItem { Label = "App Version", Value = displayVersion, Icon = MaterialIcons.info });
            Statistics.Add(new DetailItem { Label = "Folders Configured", Value = stats.FolderCount.ToString(), Icon = MaterialIcons.folder });
            Statistics.Add(new DetailItem { Label = "Explicit Files", Value = stats.FileCount.ToString(), Icon = MaterialIcons.description });

            ContentHeader = "Configuration Summary";

            if (data.FileContentExport.SelectedFilePaths.Any() || data.FileContentExport.FolderTreeState.Any(x => x.IsSelected == true))
            {
                ContentList.Add("--- File Content Export ---");
                ContentList.Add($"Output: {data.FileContentExport.OutputFileName}");
                if (data.FileContentExport.SelectedExtensions.Any())
                {
                    ContentList.Add($"Extensions: {string.Join(", ", data.FileContentExport.SelectedExtensions)}");
                }
                ContentList.Add($"Selected Files: {data.FileContentExport.SelectedFilePaths.Count}");
                ContentList.Add("");
            }

            if (data.TreeGenerator.FolderTreeState.Any(x => x.IsSelected == true))
            {
                ContentList.Add("--- Tree Generator ---");
                ContentList.Add($"Output: {data.TreeGenerator.OutputFileName}");
                ContentList.Add($"Mode: {data.TreeGenerator.GenerationMode}");
                ContentList.Add("");
            }

            if (data.FolderContentExport.FolderTreeState.Any(x => x.IsSelected == true))
            {
                ContentList.Add("--- Folder Content Export ---");
                ContentList.Add($"Output: {data.FolderContentExport.OutputFileName}");
                if (data.FolderContentExport.SelectedColumns.Any())
                {
                    ContentList.Add($"Columns: {string.Join(", ", data.FolderContentExport.SelectedColumns)}");
                }
            }
        }
    }
}