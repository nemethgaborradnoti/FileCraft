namespace FileCraft.Models
{
    public class SaveData
    {
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public int SelectedTabIndex { get; set; } = 0;
        public List<FolderState> FolderTreeState { get; set; } = new();
        public FileContentExportSettings FileContentExport { get; set; } = new();
        public FolderContentExportSettings FolderContentExport { get; set; } = new();
        public TreeGeneratorSettings TreeGenerator { get; set; } = new();
        public FileRenamerSettings FileRenamer { get; set; } = new();
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

        public FileContentExportSettings()
        {
            OutputFileName = "FileContentsExport";
        }
    }

    public class FolderContentExportSettings : ExportSettingsBase
    {
        public List<string> SelectedColumns { get; set; } = new();
        public FolderContentExportSettings()
        {
            OutputFileName = "FolderContentsExport";
        }
    }

    public class TreeGeneratorSettings : ExportSettingsBase
    {
        public TreeGeneratorSettings()
        {
            OutputFileName = "TreeStructure";
        }
    }

    public class FileRenamerSettings : ExportSettingsBase
    {
        public bool IncludeFolders { get; set; } = false;
        public FileRenamerSettings()
        {
            OutputFileName = "RenameResult";
        }
    }

    public class SettingsPageSettings
    {
        // Future options
    }
}
