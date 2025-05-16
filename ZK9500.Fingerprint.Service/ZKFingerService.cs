using System;
using System.IO;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using libzkfpcsharp;
using ZK9500.Fingerprint.Service.Services;


namespace ZK9500.Fingerprint.Service
{
    public class ZKFingerService : ServiceBase
    {
        private Thread httpThread;
        private bool running;
        private FingerprintService fingerprintService;

        protected override void OnStart(string[] args)
        {
            running = true;
            httpThread = new Thread(HttpServer);
            httpThread.Start();
            //fingerprintService = new FingerprintService();
        }

        protected override void OnStop()
        {
            running = false;
            //fingerprintService.Dispose();
            httpThread?.Join();
        }

        private void HttpServer()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();

            while (running)
            {
                HttpListenerContext context = null;
                try
                {
                    context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => ProcessRequest(context));
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 995)
                {
                    // Error por detención controlada
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error aceptando conexión: {ex.Message}");
                    context?.Response.Close();
                }
            }
            listener.Stop();
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            try
            {
                using (var fingerService = new FingerprintService()) // Se crea por cada petición
                {
                    var request = context.Request;
                    var response = context.Response;

                    if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/capturar")
                    {
                        var result = fingerService.CapturarHuella();
                        SendResponse(response, 200, result ?? "{\"error\":\"No se pudo capturar huella\"}");
                        //fingerService.Dispose();
                    }
                    else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/validar")
                    {
                        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                        {
                            string base64Template = reader.ReadToEnd();
                            bool match = fingerService.ValidarHuella(base64Template.Trim());
                            SendResponse(response, 200, $"{{\"match\":{match.ToString().ToLower()}}}");
                            //fingerService.Dispose();
                        }
                    }
                    else
                    {
                        SendResponse(response, 404, "{\"error\":\"Endpoint no encontrado\"}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error procesando petición: {ex.Message}");
                
                try
                {
                    
                    //SendResponse(context.Response, 500, "{\"error\":\"Error interno del servidor\"}");
                    SendResponse(context.Response, 500, ex.Message);
                }
                catch { /* Ignorar errores al enviar respuesta de error */ }
            }
            finally
            {
                //fingerprintService = new FingerprintService();
                //fingerprintService.Dispose();
                context?.Response.Close();
            }
        }

        //private void ProcessRequest(HttpListenerContext context)
        //{
        //    try
        //    {
        //        var request = context.Request;
        //        var response = context.Response;

        //        if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/capturar")
        //        {
        //            fingerprintService = new FingerprintService();
        //            var result = fingerprintService.CapturarHuella();
        //            SendResponse(response, 200, result ?? "{\"error\":\"No se pudo capturar huella\"}");
        //        }
        //        else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/validar")
        //        {
        //            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
        //            {
        //                string base64Template = reader.ReadToEnd();
        //                bool match = fingerprintService.ValidarHuella(base64Template.Trim());
        //                SendResponse(response, 200, $"{{\"match\":{match.ToString().ToLower()}}}");
        //            }
        //        }
        //        else
        //        {
        //            SendResponse(response, 404, "{\"error\":\"Endpoint no encontrado\"}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error en petición: {ex.Message}");
        //        try
        //        {
        //            SendResponse(context.Response, 500, "{\"error\":\"Error procesando solicitud\"}");
        //        }
        //        catch { /* Ignorar si ya no se puede enviar respuesta */ }
        //    }
        //    finally
        //    {
        //        context?.Response.Close();
        //    }
        //}

        private void SendResponse(HttpListenerResponse response, int statusCode, string content)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(content);
                response.StatusCode = statusCode;
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                //fingerprintService.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando respuesta: {ex.Message}");
                throw;
            }
        }

        //private void ProcessRequest(HttpListenerContext context)
        //{
        //    var request = context.Request;
        //    var response = context.Response;

        //    try
        //    {
        //        if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/capturar")
        //        {
        //            var result = fingerprintService.CapturarHuella();
        //            byte[] buffer = Encoding.UTF8.GetBytes(result ?? "error, no se pudo capturar huella");
        //            response.ContentType = "application/json";
        //            response.ContentLength64 = buffer.Length;
        //            response.OutputStream.Write(buffer, 0, buffer.Length);

        //            response.OutputStream.Close();
        //        }
        //        else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/validar")
        //        {
        //            string base64Template;

        //            using (var reader = new StreamReader(request.InputStream))
        //            {
        //                base64Template = reader.ReadToEnd();
        //            }
        //                bool match = fingerprintService.ValidarHuella(base64Template.Trim());
        //                byte[] buffer = Encoding.UTF8.GetBytes($"{{\"match\": {match.ToString().ToLower()} }}");

        //                response.ContentType = "application/json";
        //                response.ContentLength64 = buffer.Length;
        //                response.OutputStream.Write(buffer, 0, buffer.Length);

        //                response.OutputStream.Close();
        //        }
        //        else
        //        {
        //            byte[] buffer = Encoding.UTF8.GetBytes("{\"error\":\"Endpoint no encontrado\"}");
        //            response.StatusCode = 404;
        //            response.ContentLength64 = buffer.Length;
        //            response.OutputStream.Write(buffer, 0, buffer.Length);

        //            response.OutputStream.Close();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log del error
        //        Console.WriteLine($"Error al procesar solicitud: {ex}");

        //        try
        //        {
        //            byte[] buffer = Encoding.UTF8.GetBytes("{\"error\":\"Error interno del servidor\"}");
        //            response.StatusCode = 500;
        //            response.ContentLength64 = buffer.Length;
        //            response.OutputStream.Write(buffer, 0, buffer.Length);
        //            response.OutputStream.Close();
        //        }
        //        catch { /* Ignorar errores secundarios */ }
        //    }
        //}


        public void StartForConsole()
        {
            OnStart(null); // Llama al método OnStart del servicio
            Console.WriteLine("Servicio iniciado. Escuchando en http://localhost:8080/");
            System.Diagnostics.Debug.WriteLine("Servicio iniciado. Escuchando en http://localhost:8080/");
        }

        public void StopForConsole()
        {
            OnStop(); // Llama al método OnStop del servicio
            Console.WriteLine("Servicio detenido");
        }

    }
}