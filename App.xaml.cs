using FileCraft.Core.DependencyInjection;
using FileCraft.Services;
using FileCraft.Services.Interfaces;
using FileCraft.ViewModels;
using FileCraft.ViewModels.Functional;
using FileCraft.ViewModels.Shared;
using System.Windows;

namespace FileCraft
{
    public partial class App : System.Windows.Application
    {
        public static ServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ServiceProvider = new ServiceProvider();
            ConfigureServices(ServiceProvider);

            var mainWindow = ServiceProvider.GetService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(ServiceProvider services)
        {
            services.RegisterSingleton<IDialogService, DialogService>();
            services.RegisterSingleton<IFileOperationService, FileOperationService>();
            services.RegisterSingleton<IFolderTreeService, FolderTreeService>();
            services.RegisterSingleton<ISettingsService, SettingsService>();
            services.RegisterSingleton<ISharedStateService, SharedStateService>();

            services.RegisterSingleton<IFileQueryService, FileQueryService>();
            services.RegisterSingleton<FolderTreeManager, FolderTreeManager>();

            services.RegisterSingleton<FileContentExportViewModel, FileContentExportViewModel>();
            services.RegisterSingleton<TreeGeneratorViewModel, TreeGeneratorViewModel>();
            services.RegisterSingleton<FolderContentExportViewModel, FolderContentExportViewModel>();
            services.RegisterSingleton<FileRenamerViewModel, FileRenamerViewModel>();
            services.RegisterSingleton<SettingsViewModel, SettingsViewModel>();

            services.RegisterSingleton<MainViewModel, MainViewModel>();

            services.RegisterTransient<MainWindow, MainWindow>();
        }
    }
}
