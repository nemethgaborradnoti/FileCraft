using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface IFileOperationService
    {
        Task<string> GenerateTreeStructureAsync(string sourcePath, string destinationPath, ISet<string> excludedFolders, string outputFileName);

        Task<string> ExportFolderContentsAsync(string destinationPath, IEnumerable<string> includedFolderPaths, string outputFileName, IEnumerable<string> selectedColumns);

        Task<(string FilePath, int NormalCommentLines, int NormalCommentChars, int XmlCommentLines, int XmlCommentChars)> ExportSelectedFileContentsAsync(string destinationPath, IEnumerable<SelectableFile> selectedFiles, string outputFileName, bool ignoreNormalComments, bool ignoreXmlComments);
        Task<string> GetIgnoredCommentsPreviewAsync(IEnumerable<SelectableFile> selectedFiles, bool ignoreNormalComments, bool ignoreXmlComments);
    }
}
