using FileCraft.ViewModels.Shared;
using System.Windows;

namespace FileCraft.Views.Shared
{
    public partial class PresetLoadSummaryWindow : Window
    {
        public PresetLoadSummaryWindow(PresetLoadSummaryViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Owner = Application.Current.MainWindow;

            viewModel.RequestClose += () =>
            {
                Close();
            };
        }
    }
}