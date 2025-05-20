using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZK9500.Fingerprint.Service
{
    static class Program
    {
        private static ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        static void Main()
        {
            //solo comenta o descomenta la seccion que necesites utilizar (produccion o degug por consola)

            #region codigo para usar como servicio de windows
            var servicesToRun = new ServiceBase[]
             {
                new ZKFingerService()
             };

            if (Environment.UserInteractive)
            {
                // Modo consola
                Console.WriteLine("Modo consola - Iniciando servicio...");
                var service = new ZKFingerService();
                service.StartForConsole();
                Console.WriteLine("Servicio en ejecución. Presiona ENTER para detener.");
                Console.ReadLine();
                service.StopForConsole();
            }
            else
            {
                // Modo servicio Windows
                ServiceBase.Run(servicesToRun);
            }
            #endregion


            #region codigo para probar y hacer debug  

            if (Environment.UserInteractive)
            {
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    _shutdownEvent.Set();
                };

                Console.WriteLine("Ejecutando en modo consola...");
                var service = new ZKFingerService();
                service.StartForConsole();
                Console.WriteLine("Servicio simulado en ejecución. Presiona CTRL+C para detener...");

                // Espera hasta que se presione CTRL+C
                _shutdownEvent.WaitOne();

                service.StopForConsole();
                Console.WriteLine("Servicio detenido correctamente");
            }
            else
            {
                ServiceBase[] ServicesToRun = new ServiceBase[]
                {
                    new ZKFingerService()
                };
                ServiceBase.Run(ServicesToRun);
            }

            #endregion

            #region codigo anterior
            //ServiceBase[] ServicesToRun;
            //ServicesToRun = new ServiceBase[]
            //{
            //    new ZKFingerService()
            //};
            //ServiceBase.Run(ServicesToRun);

            //if (Environment.UserInteractive)
            //{
            //    // Modo consola
            //    Console.WriteLine("Ejecutando en modo consola...");
            //    var service = new ZKFingerService();
            //    service.StartForConsole();
            //    Console.WriteLine("Servicio simulado en ejecución. Presiona ENTER para detener...");
            //    Console.ReadLine();
            //    service.StopForConsole();
            //}
            //else
            //{
            //    // Modo servicio
            //    ServiceBase[] ServicesToRun = new ServiceBase[]
            //    {
            //        new ZKFingerService()
            //    };
            //    ServiceBase.Run(ServicesToRun);
            //}
            #endregion
        }
    }
}