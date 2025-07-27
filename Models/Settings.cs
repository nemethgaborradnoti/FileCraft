namespace FileCraft.Models
{
    public class Settings
    {
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public List<FolderState> FolderTreeState { get; set; } = new();
        public FileContentExportSettings FileContentExport { get; set; } = new();
        public FolderContentExportSettings FolderContentExport { get; set; } = new();
        public TreeGeneratorSettings TreeGenerator { get; set; } = new();
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
}
