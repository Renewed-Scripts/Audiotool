using System.ComponentModel;
using System.IO;
using System.Windows;

namespace Audiotool.Services
{
    public enum ThemeType
    {
        Light,
        Dark
    }

    public class ThemeManager : INotifyPropertyChanged
    {
        private static ThemeManager _instance;
        private static readonly object _lock = new object();
        private ThemeType _currentTheme;
        private const string ThemeSettingsFile = "theme_settings.txt";

        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ThemeManager();
                    }
                }
                return _instance;
            }
        }

        private ThemeManager()
        {
            LoadThemeFromSettings();
            ApplyTheme(CurrentTheme);
        }

        public ThemeType CurrentTheme
        {
            get => _currentTheme;
            private set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    OnPropertyChanged(nameof(CurrentTheme));
                    OnPropertyChanged(nameof(IsDarkMode));
                }
            }
        }

        public bool IsDarkMode => CurrentTheme == ThemeType.Dark;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        public void SetTheme(ThemeType theme)
        {
            if (CurrentTheme == theme) return;

            var previousTheme = CurrentTheme;
            CurrentTheme = theme;

            ApplyTheme(theme);
            SaveThemeToSettings();

            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(previousTheme, theme));
        }

        private void ApplyTheme(ThemeType theme)
        {
            var app = Application.Current;
            if (app == null) return;

            RemoveExistingThemeResources(app);

            string themeUri = theme == ThemeType.Dark
                ? "Themes/DarkTheme.xaml"
                : "Themes/LightTheme.xaml";

            try
            {
                var themeResourceDict = new ResourceDictionary
                {
                    Source = new Uri(themeUri, UriKind.Relative)
                };

                app.Resources.MergedDictionaries.Add(themeResourceDict);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        private void RemoveExistingThemeResources(Application app)
        {
            for (int i = app.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                var dict = app.Resources.MergedDictionaries[i];
                if (dict.Source != null &&
                    (dict.Source.ToString().Contains("LightTheme.xaml") ||
                     dict.Source.ToString().Contains("DarkTheme.xaml")))
                {
                    app.Resources.MergedDictionaries.RemoveAt(i);
                }
            }
        }

        private void LoadThemeFromSettings()
        {
            try
            {
                if (File.Exists(ThemeSettingsFile))
                {
                    var themeString = File.ReadAllText(ThemeSettingsFile).Trim();
                    if (Enum.TryParse<ThemeType>(themeString, out var savedTheme))
                    {
                        _currentTheme = savedTheme;
                        return;
                    }
                }
            }
            catch
            {
            }

            _currentTheme = ThemeType.Dark;
        }

        private void SaveThemeToSettings()
        {
            try
            {
                File.WriteAllText(ThemeSettingsFile, CurrentTheme.ToString());
            }
            catch
            {
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public ThemeType PreviousTheme { get; }
        public ThemeType NewTheme { get; }

        public ThemeChangedEventArgs(ThemeType previousTheme, ThemeType newTheme)
        {
            PreviousTheme = previousTheme;
            NewTheme = newTheme;
        }
    }
}
