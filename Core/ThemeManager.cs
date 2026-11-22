using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using System.Diagnostics;

namespace WTHIT.Core
{
    public sealed class ThemeManager
    {
        private static readonly Lazy<ThemeManager> _instance = new(() => new ThemeManager());
        private const string ThemeKey = "AppTheme";
        private ElementTheme _currentTheme = ElementTheme.Default;
        private bool _initialized;

        private ThemeManager() { }

        public static ThemeManager Instance => _instance.Value;
        public ElementTheme CurrentTheme => _currentTheme;

        public void Initialize()
        {
            if (_initialized) return;

            try
            {
                _currentTheme = LoadTheme();
                _initialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize theme: {ex.Message}");
                _currentTheme = ElementTheme.Default;
                _initialized = true;
            }
        }

        public void SetTheme(ElementTheme theme)
        {
            _currentTheme = theme;
            SaveTheme(theme);
            ApplyTheme(theme);
        }

        public void ApplyTheme(ElementTheme theme)
        {
            if (App.MainWindow?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
                UpdateCaptionButtons(theme);
            }
        }

        private ElementTheme LoadTheme()
        {
            try
            {
                var savedTheme = SettingsManager.Instance.GetValue<string>(ThemeKey, nameof(ElementTheme.Default));
                return savedTheme switch
                {
                    nameof(ElementTheme.Dark) => ElementTheme.Dark,
                    nameof(ElementTheme.Light) => ElementTheme.Light,
                    _ => ElementTheme.Default
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load theme: {ex.Message}");
                return ElementTheme.Default;
            }
        }

        private void SaveTheme(ElementTheme theme)
        {
            try
            {
                SettingsManager.Instance.SetValue(ThemeKey, theme.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save theme: {ex.Message}");
            }
        }

        private void UpdateCaptionButtons(ElementTheme theme)
        {
            try
            {
                if (App.MainWindow?.AppWindow?.TitleBar is not { } titleBar) return;

                var effectiveTheme = GetEffectiveTheme(theme);
                var color = effectiveTheme == ElementTheme.Dark ? Colors.White : Colors.Black;

                titleBar.ButtonForegroundColor = color;
                titleBar.ButtonHoverForegroundColor = color;
                titleBar.ButtonPressedForegroundColor = color;
                titleBar.ButtonInactiveForegroundColor = Colors.Gray;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating caption buttons: {ex.Message}");
            }
        }

        public static ElementTheme GetEffectiveTheme(ElementTheme theme)
        {
            if (theme != ElementTheme.Default)
                return theme;

            try
            {
                var uiSettings = new Windows.UI.ViewManagement.UISettings();
                var background = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
                return (background.R + background.G + background.B) > 384
                    ? ElementTheme.Light
                    : ElementTheme.Dark;
            }
            catch
            {
                return ElementTheme.Light;
            }
        }

        /// <summary>
        /// Resolves a themed Fluent brush (e.g. "TextFillColorPrimaryBrush") for a SPECIFIC
        /// theme, instead of relying on Application.Current.Resources[key] directly.
        ///
        /// That plain indexer resolves against the OS/system theme, not against a
        /// ContentDialog's own RequestedTheme override. When the app's theme differs from
        /// the Windows theme (e.g. app forced to Light while Windows is in Dark), every
        /// brush pulled that way comes back as the WRONG theme's color - which is why text
        /// can turn invisible (e.g. white text rendered on a light dialog background).
        ///
        /// This method reaches into the correct ThemeDictionaries["Light"/"Dark"] entry so
        /// the color always matches the theme actually being applied to the dialog.
        /// </summary>
        public static Brush GetBrush(string resourceKey, ElementTheme theme, Brush? fallback = null)
        {
            try
            {
                var effectiveTheme = GetEffectiveTheme(theme);
                var dictionaryName = effectiveTheme == ElementTheme.Dark ? "Dark" : "Light";

                // XamlControlsResources is a merged dictionary. Looking only at the
                // application dictionary misses its Light/Dark dictionaries and then the
                // fallback below resolves according to the Windows theme (not the app theme).
                if (TryGetThemedResource(Application.Current.Resources, dictionaryName, resourceKey, out var themedValue)
                    && themedValue is Brush themedBrush)
                {
                    return themedBrush;
                }

                // Some custom/merged brushes don't live in a theme dictionary at all -
                // fall back to the plain merged lookup for those.
                if (Application.Current.Resources.TryGetValue(resourceKey, out var value) && value is Brush brush)
                    return brush;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to resolve theme brush '{resourceKey}': {ex.Message}");
            }

            return fallback ?? new SolidColorBrush(Colors.Gray);
        }

        private static bool TryGetThemedResource(ResourceDictionary dictionary, string themeName, string resourceKey, out object? value)
        {
            value = null;

            if (dictionary.ThemeDictionaries.TryGetValue(themeName, out var themedDictionary)
                && themedDictionary is ResourceDictionary themeResources
                && themeResources.TryGetValue(resourceKey, out value))
            {
                return true;
            }

            foreach (var mergedDictionary in dictionary.MergedDictionaries)
            {
                if (TryGetThemedResource(mergedDictionary, themeName, resourceKey, out value))
                    return true;
            }

            return false;
        }
    }
}
