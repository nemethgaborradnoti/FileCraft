using FileCraft.ViewModels.Functional;
using System.Windows;

namespace FileCraft.Views.Shared
{
    public partial class PathPresetsWindow : Window
    {
        public PathPresetsWindow(PathPresetsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Owner = Application.Current.MainWindow;

            viewModel.RequestClose += OnRequestClose;
        }

        private void OnRequestClose()
        {
            DialogResult = true;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is PathPresetsViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}