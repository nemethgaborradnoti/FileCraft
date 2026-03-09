using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.ViewModels.Functional;
using FileCraft.ViewModels.Shared;
using FileCraft.Views.Shared;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FileCraft.Services
{
    public class WindowFactory : IWindowFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public WindowFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void ShowPathPresetsManagerDialog()
        {
            var viewModel = _serviceProvider.GetRequiredService<PathPresetsViewModel>();
            var window = new PathPresetsWindow(viewModel);
            window.ShowDialog();
        }

        public void ShowPresetLoadSummaryDialog(PathPresetLoadResult result)
        {
            var sharedStateService = _serviceProvider.GetRequiredService<ISharedStateService>();
            var viewModel = new PresetLoadSummaryViewModel(result, sharedStateService);
            var window = new PresetLoadSummaryWindow(viewModel);
            window.ShowDialog();
        }
    }
}