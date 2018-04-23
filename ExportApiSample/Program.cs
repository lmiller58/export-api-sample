using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Relativity.Services.Objects;
using Relativity.Services.ServiceProxy;

namespace ExportApiSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // read credentials from file
            const string credsFile = @"C:\Creds\export-api.txt";
            string[] urlUserPassword = File.ReadAllLines(credsFile);
            string url = urlUserPassword[0];
            string user = urlUserPassword[1];
            string password = urlUserPassword[3];

            ServiceFactory factory = GetServiceFactory(user, password, url);
            if (factory == null)
            {
                Console.WriteLine("Failed to get service factory.");
                Pause();
                return;
            }

            using (IObjectManager objMgr = factory.CreateProxy<IObjectManager>())
            {
                
            }

            Pause();
        }


        /// <summary>
        /// Get the "factory" which will help "create" a proxy connection
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="servicesUrl"></param>
        /// <returns></returns>
        private static ServiceFactory GetServiceFactory(
            string username, 
            string password, 
            string servicesUrl)
        {
            const string ending = "/Relativity.REST/api";
            if (servicesUrl.Length <= ending.Length || !servicesUrl.EndsWith(ending))
            {
                Console.WriteLine($"Invalid services URL. Make sure to append {ending} at the end.");
                return null;
            }

            var servicesUri = new Uri(servicesUrl);
            var credentials = new UsernamePasswordCredentials(username, password);
            return new ServiceFactory(
                new ServiceFactorySettings(servicesUri, servicesUri, credentials)
                );
        }

        private static void Pause(string message = "Press any key to continue...")
        {
            Console.WriteLine(message);
            Console.ReadKey(true);
        }
    }
}
