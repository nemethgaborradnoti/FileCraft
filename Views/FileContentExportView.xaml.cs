namespace FileCraft.Views
{
    public partial class FileContentExportView : System.Windows.Controls.UserControl
    {
        public FileContentExportView()
        {
            InitializeComponent();
            excludeFoldersTextBox.Text = "obj;bin;.git;.vs;node_modules";
        }
    }
}
