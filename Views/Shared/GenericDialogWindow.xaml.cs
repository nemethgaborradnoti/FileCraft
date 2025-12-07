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

            ApplyButtonStyle(PrimaryButton, viewModel.PrimaryButtonStyle);
            ApplyButtonStyle(SecondaryButton, viewModel.SecondaryButtonStyle);
            ApplyButtonStyle(TertiaryButton, viewModel.TertiaryButtonStyle);

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

        private void ApplyButtonStyle(Button button, string styleKey)
        {
            if (!string.IsNullOrEmpty(styleKey))
            {
                var style = TryFindResource(styleKey) as Style;
                if (style != null)
                {
                    button.Style = style;
                }
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