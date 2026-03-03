namespace FileCraft.Models
{
    public class PathPresetLoadResult
    {
        public int TotalPaths { get; set; }
        public int SuccessfullyLoaded { get; set; }
        public int Changed { get; set; }
        public int Unchanged { get; set; }
        public List<string> NotFoundPaths { get; set; } = new();
    }
}