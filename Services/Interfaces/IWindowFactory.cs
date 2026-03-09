using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface IWindowFactory
    {
        void ShowPathPresetsManagerDialog();
        void ShowPresetLoadSummaryDialog(PathPresetLoadResult result);
    }
}