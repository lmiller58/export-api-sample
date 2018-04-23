using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Relativity.Kepler.Transport;
using Relativity.Services.DataContracts.ImportExport;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;

namespace ExportApiSample
{
    public static class Documents
    {
        public const int TYPE_ID = 10;


        /// <summary>
        /// Returns the number of digits in an integer (base 10). 
        /// If negative, the negative sign does not count.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        private static int NumDigits(long num)
        {
            if (num == 0)
            {
                return 1;
            }
            num = Math.Abs(num);

            return (int) Math.Log10(num);
        }


        /// <summary>
        /// Exports all native files from a workspace
        /// </summary>
        /// <param name="objMgr"></param>
        /// <param name="workspaceId"></param>
        /// <param name="outDirectory">Directory to which we are exporting the files</param>
        public static async void ExportNatives(IObjectManager objMgr, int workspaceId, string outDirectory)
        {
            // check if directory exists and create
            // it if it doesn't
            if (!Directory.Exists(outDirectory))
            {
                Directory.CreateDirectory(outDirectory);
            }

            // specify folder names we want
            const string NATIVE_DIR = "NATIVES";
            const string EXTR_TXT_DIR = "TEXT";

            // if we want to batch out our export
            // into folders
            const string BATCH_PREFIX = "VOL_";

            // folder structure will look like this:
            /*   VOL_0001 
             *      NATIVES
             *      TEXT
             *   VOL_0002
             *      NATIVES
             *      TEXT
             *   ...     
             */    
            // specify a batch size
            const int BATCH_SIZE = 200;

            // specify the fields we want returned
            var fields = new List<FieldRef>
            {
                new FieldRef{Name = "Control Number"},
                new FieldRef{Name = "Extracted Text"},
                new FieldRef{Name = "Custodian - Single Choice"}
            };

            // query the workspace for all documents
            var query = new QueryRequest
            {
                ObjectType = new ObjectTypeRef { ArtifactTypeID = TYPE_ID },
                MaxCharactersForLongTextValues = 25,
                Fields = fields
            };

            const int startPage = 0; // index of starting page

            // initialize export so we know how many documents total we 
            // are exporting
            ExportInitializationResults initResults =
                await objMgr.InitializeExportAsync(workspaceId, query, startPage);

            long totalCount = initResults.RecordCount;

            // if total count is evenly divisble by the 
            // batch size, then we don't have to create any batches
            // for the leftovers
            long batchCountMaybe = totalCount / BATCH_SIZE;
            long batchCountDefinitely = 
                totalCount % BATCH_SIZE == 0 
                ? batchCountMaybe 
                : batchCountMaybe + 1;

            long currBatchCount = 1;

            bool done = false;
            while (!done)
            {
                RelativityObjectSlim[] currentBatch =
                    await objMgr.RetrieveNextResultsBlockFromExportAsync(workspaceId, initResults.RunID, BATCH_SIZE);

                if (currentBatch != null && currentBatch.Any())
                {
                    await Console.Out.WriteLineAsync($"Exporting batch {currBatchCount} of {batchCountDefinitely}.");
                    foreach (RelativityObjectSlim obj in currentBatch)
                    {
                        
                    }
                }
                else
                {
                    done = true;
                }
            }
        }
    }
}
