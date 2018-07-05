using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExportApiHelper;
using Relativity.Services.Objects.DataContracts;

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
                Fields = new FieldAndType[]
                {
                    new FieldAndType
                    {
                        Field = new Relativity.Services.Objects.DataContracts.FieldRef
                        {
                            Name = "Control Number",
                        },
                        Type = FieldTypes.FixedLengthText
                    },

                    new FieldAndType
                    {
                        Field = new FieldRef
                        {
                            Name = "Extracted Text"
                        },
                        Type = FieldTypes.LongText
                    }
                },

                WorkspaceId = workspaceId,
                MaximumInlineTextSize = 1024 * 100,
                RelativityUrl = new Uri(url),
                UserName = user,
                UserPassword = password
            };

            ExportApiHelper.ExportApiHelper exportHelper = config.Create();

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
