using System;
using System.Runtime.InteropServices;

namespace Plantilla
{
    internal class NativeHelper
    {
        // Common Win32 error codes used in package detection
        public const int ERROR_SUCCESS = 0;
        public const int ERROR_INSUFFICIENT_BUFFER = 122;
        public const int APPMODEL_ERROR_NO_PACKAGE = 15700;

        // Cached result for whether the app is packaged
        private static bool? _isAppPackagedCache;

        // Lock object for thread-safety
        private static readonly object _lockObject = new();

        // Import of the Win32 API function from kernel32.dll
        // GetCurrentPackageId retrieves the package identity of the current process.
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern int GetCurrentPackageId(
            ref int bufferLength,
            IntPtr buffer);

        /// <summary>
        /// Determines whether the application is running as a packaged (MSIX) app.
        /// Uses caching to avoid repeated Win32 calls.
        /// </summary>
        public static bool IsAppPackaged
        {
            get
            {
                // Return cached value if already evaluated
                if (_isAppPackagedCache.HasValue)
                    return _isAppPackagedCache.Value;

                lock (_lockObject)
                {
                    // Double-check inside lock for thread safety
                    if (_isAppPackagedCache.HasValue)
                        return _isAppPackagedCache.Value;

                    int bufferLength = 0;
                    // Call GetCurrentPackageId with null buffer to check status
                    int result = GetCurrentPackageId(ref bufferLength, IntPtr.Zero);

                    // Interpret results:
                    // - ERROR_INSUFFICIENT_BUFFER → process has a package identity (packaged app)
                    // - APPMODEL_ERROR_NO_PACKAGE → process is not packaged
                    // - Any other code → unexpected error
                    _isAppPackagedCache = result switch
                    {
                        ERROR_INSUFFICIENT_BUFFER => true,
                        APPMODEL_ERROR_NO_PACKAGE => false,
                        _ => throw new InvalidOperationException(
                            $"Unexpected error while checking app package status: {result}")
                    };

                    return _isAppPackagedCache.Value;
                }
            }
        }

        /// <summary>
        /// Clears the cached value so the status will be re-evaluated
        /// on the next call to IsAppPackaged.
        /// </summary>
        internal static void ClearCache()
        {
            lock (_lockObject)
            {
                _isAppPackagedCache = null;
            }
        }
    }
}
