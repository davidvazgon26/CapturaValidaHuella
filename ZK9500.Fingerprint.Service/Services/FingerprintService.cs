using System;
using System.IO;
using System.Text;
using System.Threading;
using libzkfpcsharp;
using ZK9500.Fingerprint.Service.Helpers;

namespace ZK9500.Fingerprint.Service.Services
{
    public class FingerprintService : IDisposable
    {
        private IntPtr devHandle = IntPtr.Zero;
        private IntPtr dbHandle = IntPtr.Zero;
        private int width = 0, height = 0;

        private static readonly object _lock = new object();
        private static bool _dispositivoReconectado = false;

        public FingerprintService()
        {

            zkfp2.Terminate();
            Thread.Sleep(1000);



            Console.WriteLine("Inicializando dispositivo de huellas...");
            System.Diagnostics.Debug.WriteLine("Inicializando dispositivo de huellas...");

            if (zkfp2.Init() != 0)
                throw new Exception("No se pudo inicializar el dispositivo, tal vez esta desconectado o dejo de funcionar, intenta conectar en otro puerto USB");

            int deviceCount = zkfp2.GetDeviceCount();
            Console.WriteLine($"Dispositivos detectados: {deviceCount}");
            System.Diagnostics.Debug.WriteLine($"Dispositivos detectados: {deviceCount}");

            if (zkfp2.GetDeviceCount() == 0)
                throw new Exception("No hay dispositivos conectados");

            Console.WriteLine("Abriendo dispositivo...");
            System.Diagnostics.Debug.WriteLine("Abriendo dispositivo...");
            devHandle = zkfp2.OpenDevice(0);

            if (devHandle == IntPtr.Zero)
                throw new Exception("No se pudo abrir el dispositivo");

            Console.WriteLine("Inicializando base de datos de huellas...");
            System.Diagnostics.Debug.WriteLine("Inicializando base de datos de huellas...");
            dbHandle = zkfp2.DBInit();

            byte[] paramValue = new byte[4];
            int size = 4;
            zkfp2.GetParameters(devHandle, 1, paramValue, ref size);
            zkfp2.ByteArray2Int(paramValue, ref width);

            size = 4;
            zkfp2.GetParameters(devHandle, 2, paramValue, ref size);
            zkfp2.ByteArray2Int(paramValue, ref height);

            Console.WriteLine($"Dispositivo listo. Resolución: {width}x{height}");
            System.Diagnostics.Debug.WriteLine($"Dispositivo listo. Resolución: {width}x{height}");
        }

        public string CapturarHuella()
        {
            if (zkfp2.GetDeviceCount() == 0)
                throw new Exception("No hay dispositivos conectados");

            Console.WriteLine("Por favor, coloque su dedo en el lector...");
            System.Diagnostics.Debug.WriteLine("Por favor, coloque su dedo en el lector...");

            byte[] img = new byte[width * height];
            byte[] tmpl = new byte[2048];
            int size = 2048;
            int intentos = 0;
            const int maxIntentos = 40; // Aumentamos los intentos para dar más tiempo
            const int delayMs = 500; // Medio segundo entre intentos

            byte[] paramValue1 = new byte[4];
            zkfp.Int2ByteArray(1, paramValue1);

            while (intentos < maxIntentos)
            {
                //Encender led del lector
                zkfp2.SetParameters(devHandle, 102, paramValue1, 4);

                // Intenta capturar la huella
                int result = zkfp2.AcquireFingerprint(devHandle, img, tmpl, ref size);

                if (result == 0) // Éxito
                {
                    MemoryStream ms = new MemoryStream();
                    BitmapHelper.GetBitmap(img, width, height, ref ms);
                    string imgBase64 = Convert.ToBase64String(ms.ToArray());
                    string tmplBase64 = Convert.ToBase64String(tmpl, 0, size);

                    Console.WriteLine("Huella capturada exitosamente");
                    System.Diagnostics.Debug.WriteLine("Huella capturada exitosamente");

                    //apagar led
                    zkfp.Int2ByteArray(0, paramValue1);
                    zkfp2.SetParameters(devHandle, 102, paramValue1, 4);

                    return $"{{\"image_base64\":\"{imgBase64}\",\"template_base64\":\"{tmplBase64}\"}}";
                }
                else if (result == -2) // Error común: dedo no detectado
                {
                    Console.WriteLine("Esperando huella... (" + (intentos + 1) + "/" + maxIntentos + ")");
                    System.Diagnostics.Debug.WriteLine("Esperando huella..." + (intentos + 1) + " / " + maxIntentos + ")");
                }
                else // Otro error
                {
                    Console.WriteLine($"Error en captura (Código: {result}). Reintentando...");
                    System.Diagnostics.Debug.WriteLine($"Error en captura (Código: {result}). Reintentando...");
                }

                Thread.Sleep(delayMs);
                intentos++;

                // Opcional: Aviso cada 5 segundos
                if (intentos % 10 == 0)
                {
                    Console.WriteLine("Por favor, coloque su dedo en el sensor...");
                    System.Diagnostics.Debug.WriteLine("Por favor, coloque su dedo en el sensor...");
                }
            }

            Console.WriteLine("Tiempo de espera agotado. No se detectó huella.");
            System.Diagnostics.Debug.WriteLine("Por favor, coloque su dedo en el sensor...");
            return null;
        }

        public bool ValidarHuella(string base64Template)
        {
            if (string.IsNullOrWhiteSpace(base64Template))
            {
                throw new ArgumentException("El template no puede estar vacío");
            }

            // Limpieza del string
            base64Template = base64Template
                .Trim()
                .Replace(" ", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace("\t", "");

            if (zkfp2.GetDeviceCount() == 0)
                throw new Exception("No hay dispositivos conectados");

            Console.WriteLine("Por favor, coloque su dedo en el lector para validar...");
            System.Diagnostics.Debug.WriteLine("Por favor, coloque su dedo en el lector para validar...");

            try
            {
                byte[] stored = Convert.FromBase64String(base64Template);
                byte[] current = new byte[2048];
                int size = 2048;
                byte[] img = new byte[width * height];
                int intentos = 0;
                const int maxIntentos = 40; // Aumentamos los intentos para dar más tiempo
                const int delayMs = 500; // Medio segundo entre intentos

                byte[] paramValue1 = new byte[4];
                zkfp.Int2ByteArray(1, paramValue1);

                while (intentos < maxIntentos)
                {
                    Console.WriteLine($"Intento {intentos + 1} de {maxIntentos}");
                    System.Diagnostics.Debug.WriteLine($"Intento {intentos + 1} de {maxIntentos}");

                    //Encender led del lector
                    zkfp2.SetParameters(devHandle, 102, paramValue1, 4);

                    if (zkfp2.AcquireFingerprint(devHandle, img, current, ref size) == 0)
                    {
                        bool match = zkfp2.DBMatch(dbHandle, stored, current) > 0;
                        Console.WriteLine($"Validación {(match ? "exitosa" : "fallida")}");
                        System.Diagnostics.Debug.WriteLine($"Validación {(match ? "exitosa" : "fallida")}");
                        return match;
                    }

                    Console.WriteLine("No se detectó huella. Por favor, intente nuevamente.");
                    System.Diagnostics.Debug.WriteLine("No se detectó huella. Por favor, intente nuevamente.");

                    Thread.Sleep(delayMs);
                    intentos++;
                }

                Console.WriteLine("No se pudo validar la huella después de varios intentos");
                System.Diagnostics.Debug.WriteLine("No se pudo validar la huella después de varios intentos");
                return false;
            }
            catch (FormatException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en formato Base64: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Template recibido: {base64Template}");
                throw new ArgumentException("Formato Base64 inválido", ex);
            }
        }

        public void Dispose()
        {
            //zkfp2.CloseDevice(devHandle);
            //zkfp2.Terminate();

            if (dbHandle != IntPtr.Zero)
                zkfp2.DBFree(dbHandle);

            if (devHandle != IntPtr.Zero)
                zkfp2.CloseDevice(devHandle);

            zkfp2.Terminate();
        }
    }
}