using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Validation;
using System.IO;
using System.Text;

namespace FileCraft.Services
{
    public class FileOperationService : IFileOperationService
    {
        private readonly ISharedStateService _sharedStateService;

        public FileOperationService(ISharedStateService sharedStateService)
        {
            _sharedStateService = sharedStateService;
        }

        public async Task<string> ExportFolderContentsAsync(string destinationPath, IEnumerable<string> includedFolderPaths, string outputFileName, IEnumerable<string> selectedColumns)
        {
            Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
            Guard.AgainstNullOrEmpty(includedFolderPaths, nameof(includedFolderPaths), "No folders were selected to export from.");
            Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));
            Guard.AgainstNullOrEmpty(selectedColumns, nameof(selectedColumns), "No columns were selected for export.");

            var allFiles = new List<string>();
            foreach (var folderPath in includedFolderPaths)
            {
                if (Directory.Exists(folderPath))
                {
                    allFiles.AddRange(Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly));
                }
            }

            Guard.AgainstNullOrEmpty(allFiles, nameof(allFiles), "The selected folders contain no files to export.");

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine(string.Join(";", selectedColumns));

            var columnExtractors = new Dictionary<string, Func<FileInfo, string>>
            {
                { "Name", fi => fi.Name },
                { "Size (byte)", fi => fi.Length.ToString() },
                { "CreationTime", fi => $"{fi.CreationTime:yyyy-MM-dd HH:mm:ss}" },
                { "LastWriteTime", fi => $"{fi.LastWriteTime:yyyy-MM-dd HH:mm:ss}" },
                { "LastAccessTime", fi => $"{fi.LastAccessTime:yyyy-MM-dd HH:mm:ss}" },
                { "IsReadOnly", fi => fi.IsReadOnly.ToString() },
                { "Attributes", fi => fi.Attributes.ToString() },
                { "FullPath", fi => fi.FullName },
                { "Parent", fi => fi.Directory?.Name ?? string.Empty },
                { "Format", fi => fi.Extension }
            };

            foreach (var filePath in allFiles.OrderBy(f => f))
            {
                var fileInfo = new FileInfo(filePath);
                var lineParts = new List<string>();
                foreach (var column in selectedColumns)
                {
                    if (columnExtractors.TryGetValue(column, out var extractor))
                    {
                        lineParts.Add(extractor(fileInfo));
                    }
                }
                csvBuilder.AppendLine(string.Join(";", lineParts));
            }

            string outputFilePath = Path.Combine(destinationPath, $"{outputFileName}.txt");
            await File.WriteAllTextAsync(outputFilePath, csvBuilder.ToString());

            return outputFilePath;
        }

        public async Task<string> GenerateTreeStructureAsync(string sourcePath, string destinationPath, ISet<string> excludedFolderPaths, string outputFileName)
        {
            Guard.AgainstNullOrWhiteSpace(sourcePath, nameof(sourcePath));
            Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
            Guard.AgainstNonExistentDirectory(sourcePath, "The selected source folder does not exist.");
            Guard.AgainstNull(excludedFolderPaths, nameof(excludedFolderPaths));
            Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));

            StringBuilder treeBuilder = new StringBuilder();
            treeBuilder.AppendLine(new DirectoryInfo(sourcePath).Name);

            BuildTree(sourcePath, "", treeBuilder, excludedFolderPaths, true);

            string outputFilePath = Path.Combine(destinationPath, $"{outputFileName}.txt");
            await File.WriteAllTextAsync(outputFilePath, treeBuilder.ToString());

            return outputFilePath;
        }

        private void BuildTree(string directoryPath, string indent, StringBuilder builder, ISet<string> excludedFolderPaths, bool isLastParent)
        {
            try
            {
                var ignoredFolderNames = new HashSet<string>(_sharedStateService.IgnoredFolders, StringComparer.OrdinalIgnoreCase);

                var subDirectories = Directory.GetDirectories(directoryPath)
                    .Where(d => !excludedFolderPaths.Contains(d))
                    .Where(d => !ignoredFolderNames.Contains(new DirectoryInfo(d).Name))
                    .OrderBy(d => d)
                    .ToArray();

                var files = Directory.GetFiles(directoryPath)
                    .OrderBy(f => f)
                    .ToArray();

                for (int i = 0; i < subDirectories.Length; i++)
                {
                    var subDirInfo = new DirectoryInfo(subDirectories[i]);
                    bool isLast = (i == subDirectories.Length - 1) && (files.Length == 0);
                    builder.AppendLine($"{indent}{(isLast ? "└── " : "├── ")}{subDirInfo.Name}");
                    BuildTree(subDirInfo.FullName, indent + (isLast ? "    " : "│   "), builder, excludedFolderPaths, isLast);
                }

                for (int i = 0; i < files.Length; i++)
                {
                    var fileInfo = new FileInfo(files[i]);
                    bool isLast = i == files.Length - 1;
                    builder.AppendLine($"{indent}{(isLast ? "└── " : "├── ")}{fileInfo.Name}");
                }
            }
            catch (UnauthorizedAccessException)
            {
                builder.AppendLine($"{indent}└── [Access Denied]");
            }
        }

        private int FindActualCommentIndex(string line)
        {
            bool inDoubleQuotes = false;
            bool inSingleQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"' && (i == 0 || line[i - 1] != '\\'))
                {
                    inDoubleQuotes = !inDoubleQuotes;
                }
                else if (c == '\'' && (i == 0 || line[i - 1] != '\\'))
                {
                    inSingleQuotes = !inSingleQuotes;
                }

                if (!inDoubleQuotes && !inSingleQuotes)
                {
                    if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '/')
                    {
                        if (i > 0 && line[i - 1] == ':')
                        {
                            continue;
                        }
                        return i;
                    }
                }
            }
            return -1;
        }


        public async Task<(string FilePath, int NormalCommentLines, int NormalCommentChars, int XmlCommentLines, int XmlCommentChars)> ExportSelectedFileContentsAsync(string destinationPath, IEnumerable<SelectableFile> selectedFiles, string outputFileName, bool ignoreNormalComments, bool ignoreXmlComments)
        {
            Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
            Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));
            Guard.AgainstNullOrEmpty(selectedFiles, nameof(selectedFiles), "No files were selected for export.");

            var contentBuilder = new StringBuilder();
            var selectedFilesList = selectedFiles.ToList();
            int totalNormalCommentLines = 0;
            int totalNormalCommentChars = 0;
            int totalXmlCommentLines = 0;
            int totalXmlCommentChars = 0;

            for (int i = 0; i < selectedFilesList.Count; i++)
            {
                var file = selectedFilesList[i];
                try
                {
                    contentBuilder.AppendLine($"=== {file.RelativePath} file contains:");
                    contentBuilder.AppendLine();

                    var lines = await File.ReadAllLinesAsync(file.FullPath);

                    foreach (var line in lines)
                    {
                        string finalLine = line;
                        bool commentRemoved = false;

                        int commentIndex = FindActualCommentIndex(line);

                        if (commentIndex != -1)
                        {
                            bool isXmlComment = commentIndex + 2 < line.Length && line[commentIndex + 2] == '/';

                            if (ignoreXmlComments && isXmlComment)
                            {
                                string codePart = line.Substring(0, commentIndex);
                                string commentPart = line.Substring(commentIndex);
                                totalXmlCommentChars += commentPart.Length;
                                totalXmlCommentLines++;
                                finalLine = codePart;
                                commentRemoved = true;
                            }
                            else if (ignoreNormalComments && !isXmlComment)
                            {
                                string codePart = line.Substring(0, commentIndex);
                                string commentPart = line.Substring(commentIndex);
                                totalNormalCommentChars += commentPart.Length;
                                totalNormalCommentLines++;
                                finalLine = codePart;
                                commentRemoved = true;
                            }
                        }

                        if (commentRemoved && string.IsNullOrWhiteSpace(finalLine))
                        {
                            continue;
                        }
                        else
                        {
                            contentBuilder.AppendLine(finalLine.TrimEnd());
                        }
                    }

                    if (i < selectedFilesList.Count - 1)
                    {
                        contentBuilder.AppendLine();
                        contentBuilder.AppendLine("===========");
                    }
                }
                catch (Exception ex)
                {
                    contentBuilder.AppendLine($"--- Could not read file: {file.FileName}. Error: {ex.Message} ---");
                    contentBuilder.AppendLine();
                }
            }

            string outputFilePath = Path.Combine(destinationPath, $"{outputFileName}.txt");
            await File.WriteAllTextAsync(outputFilePath, contentBuilder.ToString());
            return (outputFilePath, totalNormalCommentLines, totalNormalCommentChars, totalXmlCommentLines, totalXmlCommentChars);
        }
    }
}