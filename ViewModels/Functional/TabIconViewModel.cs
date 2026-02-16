using System.Windows.Media;

namespace FileCraft.ViewModels.Functional
{
    public class TabIconViewModel
    {
        public string Name { get; }
        public string IconGlyph { get; }
        public Brush IconBrush { get; }

        public TabIconViewModel(string name, string iconGlyph, Brush iconBrush)
        {
            Name = name;
            IconGlyph = iconGlyph;
            IconBrush = iconBrush;
        }
    }
}