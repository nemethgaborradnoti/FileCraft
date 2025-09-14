using FileCraft.Services;
using FileCraft.Services.Interfaces;
using FileCraft.ViewModels;
using FileCraft.ViewModels.Functional;
using FileCraft.ViewModels.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace FileCraft
{
    public partial class App : System.Windows.Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IFileOperationService, FileOperationService>();
            services.AddSingleton<IFolderTreeService, FolderTreeService>();
            services.AddSingleton<ISaveService, SaveService>();
            services.AddSingleton<ISharedStateService, SharedStateService>();
            services.AddSingleton<IFileQueryService, FileQueryService>();
            services.AddSingleton<IUndoService, UndoService>();

            services.AddTransient<FolderTreeManager>();

            services.AddSingleton<FileContentExportViewModel>();
            services.AddSingleton<TreeGeneratorViewModel>();
            services.AddSingleton<FolderContentExportViewModel>();
            services.AddSingleton<OptionsViewModel>();

            services.AddSingleton<MainViewModel>();

            services.AddTransient<MainWindow>();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }

            base.OnExit(e);
        }
    }
}
