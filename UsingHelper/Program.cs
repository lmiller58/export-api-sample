using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Relativity.ObjectManager.ExportApiHelper;
using Relativity.Services.Objects.DataContracts;
using Relativity.Services.ServiceProxy;

namespace UsingHelper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // read credentials and settings from app.config file
            var configReader = new AppSettingsReader();
            
            // the base URL should be stricly the instance name
            // --no "/Relativity" appended at the end
            string url = configReader.GetValue("RelativityBaseURI", typeof(string)).ToString();
            string user = configReader.GetValue("RelativityUserName", typeof(string)).ToString();
            string password = configReader.GetValue("RelativityPassword", typeof(string)).ToString();

            int workspaceId = 0;  
            string workspaceIdAsStr = configReader.GetValue("WorkspaceId", typeof(string)).ToString();
            if (!String.IsNullOrEmpty(workspaceIdAsStr))
            {
                workspaceId = Int32.Parse(workspaceIdAsStr);
            }

            if (workspaceId == 0)
            {
                Console.WriteLine("Invalid workspace ID.");
                return;
            }

            var config = new ExportApiHelperConfig
            {
                BlockSize = 1000,
                QueryRequest = new QueryRequest
                {
                    Fields = new FieldRef[]
                    {
                        new FieldRef { Name = "Control Number" },
                        new FieldRef { Name = "Extracted Text" }
                    },

                    // this is the cutoff value--anything greater 
                    // than this many bytes will be streamed
                    MaxCharactersForLongTextValues = 1000 * 1024
                },

                WorkspaceId = workspaceId,
                RelativityUrl = new Uri(url),
                Credentials = new UsernamePasswordCredentials(user, password),
                ScaleFactor = 8
            };

            ExportApiHelper exportHelper = config.Create();

            var cts = new System.Threading.CancellationTokenSource();

            // Extracted Text is the second field in the config.Fields collection
            int extractedTextIndex = 1;
            exportHelper.Run(new MyExportHandler(extractedTextIndex), cts.Token);

            Pause();
        }


        private static void Pause()
        {
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
    }
}
