using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace megalink
{
    class Program
    {

        static Edio edio;

        static void Main(string[] args)
        {

            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("megalink v" + Assembly.GetEntryAssembly().GetName().Version);

            try
            {
                megalink(args);
            }
            catch (Exception x)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Logger.nl();
                Logger.err(x.Message);
                Console.ResetColor();
            }

        }

        static void megalink(string[] args)
        {
            edio = getEdio(args);

            bool force_app_mode = true;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-appmode")) force_app_mode = false;
                if (args[i].Equals("-sermode")) force_app_mode = false;
            }
            if (force_app_mode)
            {
                edio.exitServiceMode();
            }

            CmdProcessor.start(args, edio);

            //edio.getConfig().print();
        }

        private static Edio getEdio(string[] args)
        {
            Edio result;
            try
            {
                if (args.Length > 0 && args[0].Equals("-port"))
                {
                    Logger.inf($"opening port {args[1]}");
                    result = new Edio(args[1]);
                }
                else
                {
                    result = new Edio();
                }
            }
            catch (Exception)
            {
                System.Threading.Thread.Sleep(500);
                result = new Edio();
            }
            
            Logger.inf("EverDrive found at " + result.PortName);
            Logger.inf("EDIO status: " + result.getStatus().ToString("X4"));
            Console.WriteLine("");
            
            return result;
        }
    }
}
