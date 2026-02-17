using FileCraft.Models;
using FileCraft.Services.Interfaces;
using System.Windows;

namespace FileCraft.Services
{
    public class ThemeService : IThemeService
    {
        private AppTheme _currentTheme;

        public void SwitchTheme(AppTheme theme)
        {
            var app = Application.Current;
            if (app == null) return;

            var oldDictionary = FindThemeDictionary(app);
            if (oldDictionary != null)
            {
                app.Resources.MergedDictionaries.Remove(oldDictionary);
            }

            var newDictionary = new ResourceDictionary
            {
                Source = new Uri(GetThemeSource(theme), UriKind.RelativeOrAbsolute)
            };

            app.Resources.MergedDictionaries.Add(newDictionary);
            _currentTheme = theme;
        }

        public AppTheme GetCurrentTheme()
        {
            return _currentTheme;
        }

        private ResourceDictionary? FindThemeDictionary(Application app)
        {
            foreach (var dict in app.Resources.MergedDictionaries)
            {
                if (dict.Source != null && dict.Source.ToString().Contains("Colors."))
                {
                    return dict;
                }
            }
            return null;
        }

        private string GetThemeSource(AppTheme theme)
        {
            return theme == AppTheme.Light
                ? "/Themes/Styles/Colors.Light.xaml"
                : "/Themes/Styles/Colors.Dark.xaml";
        }
    }
}