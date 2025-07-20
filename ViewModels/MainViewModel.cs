using FileCraft.Services;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Validation;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace FileCraft.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _sourcePath;
        private string _destinationPath;
        private readonly IDialogService _dialogService;

        public event PropertyChangedEventHandler PropertyChanged;

        public string SourcePath
        {
            get => _sourcePath;
            set { _sourcePath = value; OnPropertyChanged(); }
        }

        public string DestinationPath
        {
            get => _destinationPath;
            set { _destinationPath = value; OnPropertyChanged(); }
        }

        public ICommand ExportFolderContentsCommand { get; }
        public ICommand GenerateTreeStructureCommand { get; }


        public MainViewModel()
        {
            _dialogService = new DialogService();

            ExportFolderContentsCommand = new RelayCommand(ExportFolderContents, CanExecuteOperation);
            GenerateTreeStructureCommand = new RelayCommand(GenerateTreeStructure, CanExecuteOperation);
        }

        private bool CanExecuteOperation(object parameter)
        {
            return !string.IsNullOrWhiteSpace(SourcePath) &&
                   !string.IsNullOrWhiteSpace(DestinationPath);
        }

        private void ExportFolderContents(object parameter)
        {
            try
            {
                Guard.AgainstNullOrWhiteSpace(SourcePath, nameof(SourcePath), "Please select a source folder.");
                Guard.AgainstNullOrWhiteSpace(DestinationPath, nameof(DestinationPath), "Please select a destination folder.");
                Guard.AgainstNonExistentDirectory(SourcePath, "The selected source folder does not exist.");

                var options = parameter as Dictionary<string, object>;
                Guard.AgainstNull(options, nameof(options), "Internal application error: Command parameter is missing.");

                bool includeSubfolders = (bool)options["IncludeSubfolders"];

                var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(SourcePath, "*.*", searchOption);

                if (files.Length == 0)
                {
                    _dialogService.ShowNotification("Information", "The selected folder contains no files to export.");
                    return;
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
                string outputFilePath = Path.Combine(DestinationPath, outputFileName);
                File.WriteAllText(outputFilePath, csvBuilder.ToString());

                _dialogService.ShowNotification("Success", $"Folder contents exported successfully!\n\n{files.Length} files were processed.\nSaved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"An unexpected error occurred:\n\n{ex.Message}");
            }
        }

        private void GenerateTreeStructure(object parameter)
        {
            try
            {
                Guard.AgainstNullOrWhiteSpace(SourcePath, nameof(SourcePath), "Please select a source folder.");
                Guard.AgainstNullOrWhiteSpace(DestinationPath, nameof(DestinationPath), "Please select a destination folder.");
                Guard.AgainstNonExistentDirectory(SourcePath, "The selected source folder does not exist.");

                var options = parameter as Dictionary<string, object>;
                Guard.AgainstNull(options, nameof(options), "Internal application error: Command parameter is missing.");

                string excludeFoldersText = (string)options["ExcludeFoldersText"];

                var excludedFolders = new HashSet<string>(
                    excludeFoldersText.Split(';').Select(f => f.Trim()).Where(f => !string.IsNullOrWhiteSpace(f)),
                    StringComparer.OrdinalIgnoreCase);

                StringBuilder treeBuilder = new StringBuilder();
                treeBuilder.AppendLine(SourcePath);

                BuildTree(SourcePath, "", treeBuilder, excludedFolders);

                string outputFileName = $"treestructure_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string outputFilePath = Path.Combine(DestinationPath, outputFileName);
                File.WriteAllText(outputFilePath, treeBuilder.ToString());

                _dialogService.ShowNotification("Success", $"Tree structure file was created successfully!\n\nSaved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"An unexpected error occurred:\n\n{ex.Message}");
            }
        }

        private void BuildTree(string directoryPath, string indent, StringBuilder builder, HashSet<string> excludedFolders)
        {
            string[] subDirectories;
            string[] files;

            try
            {
                subDirectories = Directory.GetDirectories(directoryPath)
                    .Where(d => !excludedFolders.Contains(Path.GetFileName(d)))
                    .ToArray();
                files = Directory.GetFiles(directoryPath);
            }
            catch (UnauthorizedAccessException)
            {
                builder.AppendLine($"{indent}└── [Access Denied]");
                return;
            }

            for (int i = 0; i < subDirectories.Length; i++)
            {
                var subDir = subDirectories[i];
                bool isLast = (i == subDirectories.Length - 1) && (files.Length == 0);
                builder.AppendLine($"{indent}{(isLast ? "└── " : "├── ")}{Path.GetFileName(subDir)}");
                BuildTree(subDir, indent + (isLast ? "    " : "│   "), builder, excludedFolders);
            }

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                bool isLast = i == files.Length - 1;
                builder.AppendLine($"{indent}{(isLast ? "└── " : "├── ")}{Path.GetFileName(file)}");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
