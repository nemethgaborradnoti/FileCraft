using FileCraft.ViewModels.Shared;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FileCraft.Views.Shared
{
    public partial class GenericDialogWindow : Window
    {
        public GenericDialogWindow(GenericDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Owner = Application.Current.MainWindow;

            if (!string.IsNullOrEmpty(viewModel.PrimaryButtonStyle))
            {
                var style = TryFindResource(viewModel.PrimaryButtonStyle) as Style;
                if (style != null)
                {
                    PrimaryButton.Style = style;
                }
            }

            if (!string.IsNullOrEmpty(viewModel.TertiaryButtonStyle))
            {
                var style = TryFindResource(viewModel.TertiaryButtonStyle) as Style;
                if (style != null)
                {
                    TertiaryButton.Style = style;
                }
            }

            viewModel.RequestClose += (result) =>
            {
                DialogResult = result;
                Close();
            };

            if (viewModel.IsInputVisible)
            {
                Loaded += (s, e) =>
                {
                    InputTextBox.Focus();
                    InputTextBox.SelectAll();
                };
            }
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}