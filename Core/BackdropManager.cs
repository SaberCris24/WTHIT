using System;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace WTHIT.Core
{
    /// <summary>
    /// Manages window backdrop effects
    /// </summary>
    public sealed class BackdropManager
    {
        private static readonly Lazy<BackdropManager> _instance = new(() => new BackdropManager());
        private const string BackdropKey = "AppBackdrop";
        private bool _initialized;

        private BackdropManager() { }

        public static BackdropManager Instance => _instance.Value;
        public BackdropType CurrentBackdrop { get; private set; } = BackdropType.Mica;

        public void Initialize()
        {
            if (_initialized) return;

            try
            {
                CurrentBackdrop = LoadBackdrop();
                _initialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize backdrop: {ex.Message}");
                CurrentBackdrop = BackdropType.Mica;
                _initialized = true;
            }
        }

        public void SetBackdrop(Window window, BackdropType type)
        {
            if (window == null) return;

            CurrentBackdrop = type;
            SaveBackdrop(type);

            if (!MicaController.IsSupported() && !DesktopAcrylicController.IsSupported())
                return;

            window.SystemBackdrop = type switch
            {
                BackdropType.Mica when MicaController.IsSupported() => new MicaBackdrop { Kind = MicaKind.Base },
                BackdropType.MicaAlt when MicaController.IsSupported() => new MicaBackdrop { Kind = MicaKind.BaseAlt },
                BackdropType.Acrylic when DesktopAcrylicController.IsSupported() => new DesktopAcrylicBackdrop(),
                _ when MicaController.IsSupported() => new MicaBackdrop { Kind = MicaKind.Base },
                _ => null
            };
        }

        public BackdropType GetBestAvailableBackdrop()
        {
            if (MicaController.IsSupported()) return BackdropType.Mica;
            if (DesktopAcrylicController.IsSupported()) return BackdropType.Acrylic;
            return BackdropType.Mica;
        }

        private BackdropType LoadBackdrop()
        {
            try
            {
                var saved = SettingsManager.Instance.GetValue<string>(BackdropKey, nameof(BackdropType.Mica));
                return saved switch
                {
                    nameof(BackdropType.MicaAlt) => BackdropType.MicaAlt,
                    nameof(BackdropType.Acrylic) => BackdropType.Acrylic,
                    _ => BackdropType.Mica
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load backdrop: {ex.Message}");
                return BackdropType.Mica;
            }
        }

        private void SaveBackdrop(BackdropType type)
        {
            try
            {
                SettingsManager.Instance.SetValue(BackdropKey, type.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save backdrop: {ex.Message}");
            }
        }
    }

    public enum BackdropType
    {
        Mica,
        MicaAlt,
        Acrylic
    }
}
