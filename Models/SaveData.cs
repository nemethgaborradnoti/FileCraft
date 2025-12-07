using FileCraft.Shared.Helpers;

namespace FileCraft.Models
{
    public class SaveData
    {
        public string PresetName { get; set; } = string.Empty;
        public DateTime? LastModified { get; set; }
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
        public string OutputFileName { get; set; } = ResourceHelper.GetString("Model_DefaultExportedFileName");
        public bool AppendTimestamp { get; set; } = false;
    }

    public class FileContentExportSettings : ExportSettingsBase
    {
        public List<string> SelectedExtensions { get; set; } = new();
        public List<string> SelectedFilePaths { get; set; } = new();
        public List<string> IgnoredCommentFilePaths { get; set; } = new();
        public List<FolderState> FolderTreeState { get; set; } = new();

        public FileContentExportSettings()
        {
            OutputFileName = ResourceHelper.GetString("Model_DefaultFileContentsName");
        }
    }

    public class FolderContentExportSettings : ExportSettingsBase
    {
        public List<string> SelectedColumns { get; set; } = new();
        public List<FolderState> FolderTreeState { get; set; } = new();
        public FolderContentExportSettings()
        {
            OutputFileName = ResourceHelper.GetString("Model_DefaultFolderContentsName");
        }
    }

    public class TreeGeneratorSettings : ExportSettingsBase
    {
        public List<FolderState> FolderTreeState { get; set; } = new();
        public TreeGeneratorSettings()
        {
            OutputFileName = ResourceHelper.GetString("Model_DefaultTreeStructureName");
        }
    }

    public class SettingsPageSettings
    {
        public List<string> IgnoredFolders { get; set; } = new() { "bin", "obj" };
    }
}