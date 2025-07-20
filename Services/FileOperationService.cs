using FileCraft.Services.Interfaces;
using FileCraft.Shared.Validation;
using System.IO;
using System.Text;

namespace FileCraft.Services
{
    public class FileOperationService : IFileOperationService
    {
        public Task<string> ExportFolderContentsAsync(string sourcePath, string destinationPath, bool includeSubfolders)
        {
            return Task.Run(() =>
            {
                Guard.AgainstNullOrWhiteSpace(sourcePath, nameof(sourcePath));
                Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
                Guard.AgainstNonExistentDirectory(sourcePath, "The selected source folder does not exist.");

                var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(sourcePath, "*.*", searchOption);

                if (files.Length == 0)
                {
                    throw new InvalidOperationException("The selected folder contains no files to export.");
                }

                var csvBuilder = new StringBuilder();
                csvBuilder.AppendLine("Name;Size (KB);Modification Date;Creation Date;Last Access Date;Format");

                foreach (var filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);
                    csvBuilder.AppendLine(
                        $"{fileInfo.Name};" +
                        $"{(fileInfo.Length / 1024.0):F2};" +
                        $"{fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss};" +
                        $"{fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss};" +
                        $"{fileInfo.LastAccessTime:yyyy-MM-dd HH:mm:ss};" +
                        $"{fileInfo.Extension}");
                }

                string outputFileName = $"ExportedContents_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string outputFilePath = Path.Combine(destinationPath, outputFileName);
                File.WriteAllText(outputFilePath, csvBuilder.ToString());

                return outputFilePath;
            });
        }

        public Task<string> GenerateTreeStructureAsync(string sourcePath, string destinationPath, ISet<string> excludedFolders)
        {
            return Task.Run(() =>
            {
                Guard.AgainstNullOrWhiteSpace(sourcePath, nameof(sourcePath));
                Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
                Guard.AgainstNonExistentDirectory(sourcePath, "The selected source folder does not exist.");

                StringBuilder treeBuilder = new StringBuilder();
                treeBuilder.AppendLine(new DirectoryInfo(sourcePath).Name);

                BuildTree(sourcePath, "", treeBuilder, excludedFolders, true);

                string outputFileName = $"treestructure_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string outputFilePath = Path.Combine(destinationPath, outputFileName);
                File.WriteAllText(outputFilePath, treeBuilder.ToString());

                return outputFilePath;
            });
        }

        private void BuildTree(string directoryPath, string indent, StringBuilder builder, ISet<string> excludedFolders, bool isLastParent)
        {
            try
            {
                var subDirectories = Directory.GetDirectories(directoryPath)
                    .Where(d => !excludedFolders.Contains(Path.GetFileName(d)))
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
                    BuildTree(subDirInfo.FullName, indent + (isLast ? "    " : "│   "), builder, excludedFolders, isLast);
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
    }
}
