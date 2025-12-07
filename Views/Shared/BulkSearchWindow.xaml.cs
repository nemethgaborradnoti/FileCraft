using FileCraft.ViewModels.Shared;
using System.Windows;
using System.Windows.Controls;

namespace FileCraft.Views.Shared
{
    public partial class BulkSearchWindow : Window
    {
        public BulkSearchWindow(BulkSearchViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Owner = Application.Current.MainWindow;

            viewModel.RequestClose += (result) =>
            {
                DialogResult = result;
                Close();
            };
        }

        private void InputTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0)
            {
                CountScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            }
            if (e.HorizontalChange != 0)
            {
                CountScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }
    }
}