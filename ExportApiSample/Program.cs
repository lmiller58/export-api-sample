using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using Relativity.Services.Objects;
using Relativity.Services.ServiceProxy;

namespace ExportApiSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // read credentials from app.config
            var configReader = new AppSettingsReader();
            string url = configReader.GetValue("RelativityBaseURI", typeof(string)).ToString();
            string user = configReader.GetValue("RelativityUserName", typeof(string)).ToString();
            string password = configReader.GetValue("RelativityPassword", typeof(string)).ToString();

            ServiceFactory factory = GetServiceFactory(user, password, url);
            if (factory == null)
            {
                Console.WriteLine("Failed to get service factory.");
                Pause();
                return;
            }

            Console.WriteLine("Successfully created service factory.");

            // create multiple threads
            int numThreads = 2;  // default 2
            string numThreadsAsStr = configReader.GetValue("ThreadCount", typeof(string)).ToString();
            if (!String.IsNullOrEmpty(numThreadsAsStr))
            {
                numThreads = Int32.Parse(numThreadsAsStr);
            }

            List<Thread> threads = new List<Thread>(numThreads);
            for (int i = 0; i < numThreads; i++)
            {
                threads.Add(new Thread(Common.WriteToFile));
            }


            ServicePointManager.DefaultConnectionLimit = 32;

            int largeDocsSearch = 1239403;
            int smallDocsSearch = 1239404;

            
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // start threads
            foreach (Thread t in threads)
            {
                t.Start();
            }

            using (IObjectManager objMgr = factory.CreateProxy<IObjectManager>())
            {
                string outPutDir = configReader.GetValue("ExportFolder", typeof(string)).ToString();
                try
                {
                    //Documents.ExportFromSavedSearchAsync(
                    //    objMgr,
                    //    workspaceId: 1017273,
                    //    savedSearchId: smallDocsSearch,
                    //    outDirectory: outPutDir).Wait();
                    Documents.ExportAllDocsAndFieldsAsync(
                        objMgr, 
                        workspaceId: 1017273, 
                        outDirectory: outPutDir).Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine(e.InnerException);
                    }
                }

            }
            foreach (Thread t in threads)
            {
                t.Join();
            }
            stopwatch.Stop();
            Console.WriteLine($"Elapsed: {stopwatch.Elapsed.TotalSeconds}");
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
