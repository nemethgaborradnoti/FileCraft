using FileCraft.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TextBox = System.Windows.Controls.TextBox;

namespace FileCraft
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            Closing += MainWindow_Closing;
            this.PreviewMouseDown += MainWindow_PreviewMouseDown;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.Save();
        }

        private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var clickedElement = e.OriginalSource as DependencyObject;

                while (clickedElement != null)
                {
                    if (clickedElement is TextBox || clickedElement is PasswordBox)
                    {
                        return;
                    }
                    clickedElement = VisualTreeHelper.GetParent(clickedElement);
                }

                var focusedElement = Keyboard.FocusedElement as UIElement;
                if (focusedElement is TextBox || focusedElement is PasswordBox)
                {
                    Keyboard.ClearFocus();
                    FocusManager.SetFocusedElement(this, this);
                }
            }
        }
    }
}
