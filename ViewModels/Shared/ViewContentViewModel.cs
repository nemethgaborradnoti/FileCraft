using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using System.Windows;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;

namespace FileCraft.ViewModels.Shared
{
    public class ViewContentViewModel : BaseViewModel
    {
        public string Title { get; }
        public string Content { get; }

        public ICommand CloseCommand { get; }
        public ICommand CopyCommand { get; }

        public event Action? RequestClose;

        public ViewContentViewModel(string title, string content)
        {
            Title = title;
            Content = content;

            CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());
            CopyCommand = new RelayCommand(_ => CopyToClipboard());
        }

        private void CopyToClipboard()
        {
            try
            {
                Clipboard.SetText(Content);
            }
            catch
            {
                // Ignore clipboard errors
            }
        }
    }
}