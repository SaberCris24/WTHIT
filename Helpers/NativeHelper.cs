using System.Runtime.InteropServices;

namespace Plantilla
{
    internal class NativeHelper
    {
        // Constantes de error
        public const int ERROR_SUCCESS = 0;
        public const int ERROR_INSUFFICIENT_BUFFER = 122;
        public const int APPMODEL_ERROR_NO_PACKAGE = 15700;

        // Importación de la función nativa de Windows
        [DllImport("api-ms-win-appmodel-runtime-l1-1-1", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        internal static extern uint GetCurrentPackageId(ref int pBufferLength, out byte pBuffer);

        // Propiedad para verificar si la aplicación está empaquetada
        public static bool IsAppPackaged
        {
            get
            {
                int bufferSize = 0;
                byte byteBuffer = 0;
                uint lastError = GetCurrentPackageId(ref bufferSize, out byteBuffer);
                
                // Si recibimos APPMODEL_ERROR_NO_PACKAGE, significa que la app no está empaquetada
                return lastError != APPMODEL_ERROR_NO_PACKAGE;
            }
        }
    }
}