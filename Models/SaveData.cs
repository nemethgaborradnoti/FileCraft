namespace FileCraft.Models
{
    public class SaveData
    {
        public string PresetName { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public int SelectedTabIndex { get; set; } = 0;
        public FileContentExportSettings FileContentExport { get; set; } = new();
        public FolderContentExportSettings FolderContentExport { get; set; } = new();
        public TreeGeneratorSettings TreeGenerator { get; set; } = new();
        public SettingsPageSettings SettingsPage { get; set; } = new();
    }

    public abstract class ExportSettingsBase
    {
        public string OutputFileName { get; set; } = "ExportedFile";
        public bool AppendTimestamp { get; set; } = false;
    }

    public class FileContentExportSettings : ExportSettingsBase
    {
        public List<string> SelectedExtensions { get; set; } = new();
        public List<string> SelectedFilePaths { get; set; } = new();
        public List<FolderState> FolderTreeState { get; set; } = new();

        public FileContentExportSettings()
        {
            OutputFileName = "FileContents";
        }
    }

    public class FolderContentExportSettings : ExportSettingsBase
    {
        public List<string> SelectedColumns { get; set; } = new();
        public List<FolderState> FolderTreeState { get; set; } = new();
        public FolderContentExportSettings()
        {
            OutputFileName = "FolderContents";
        }
    }

    public class TreeGeneratorSettings : ExportSettingsBase
    {
        public List<FolderState> FolderTreeState { get; set; } = new();
        public TreeGeneratorSettings()
        {
            OutputFileName = "TreeStructure";
        }
    }

    public class SettingsPageSettings
    {
    }
}
