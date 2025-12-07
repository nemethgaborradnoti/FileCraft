using Fonts;

namespace FileCraft.Shared.Helpers
{
    public class IconDefinition
    {
        public string Glyph { get; }
        public Brush Brush { get; }

        public IconDefinition(string glyph, string colorResourceKey)
        {
            Glyph = glyph;
            Brush = Application.Current.TryFindResource(colorResourceKey) as Brush ?? Brushes.Black;
        }

        public IconDefinition(string glyph, Brush brush)
        {
            Glyph = glyph;
            Brush = brush;
        }
    }

    public static class AppIcons
    {
        public static IconDefinition FileContentExport => new(MaterialIcons.topic, "FolderIconBrush");
        public static IconDefinition TreeGenerator => new(MaterialIcons.park, "TreeIconBrush");
        public static IconDefinition FolderContentExport => new(MaterialIcons.dns, "PrimaryBrush");
        public static IconDefinition Options => new(MaterialIcons.settings, "GrayTextBrush");

        public static IconDefinition Undo => new(MaterialIcons.keyboard_arrow_left, "TextBrush");
        public static IconDefinition Redo => new(MaterialIcons.keyboard_arrow_right, "TextBrush");
        public static IconDefinition Save => new(MaterialIcons.save, "TextBrush");
        public static IconDefinition Delete => new(MaterialIcons.delete, "DangerBrush");
        public static IconDefinition Fullscreen => new(MaterialIcons.fullscreen, "TextBrush");
        public static IconDefinition FullscreenExit => new(MaterialIcons.fullscreen_exit, "TextBrush");

        public static IconDefinition Warning => new(MaterialIcons.warning, "DangerBrush");
        public static IconDefinition Error => new(MaterialIcons.error, "DangerBrush");
        public static IconDefinition Success => new(MaterialIcons.check_circle, "SuccessBrush");
        public static IconDefinition Info => new(MaterialIcons.info, "PrimaryBrush");
    }
}