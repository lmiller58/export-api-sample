using System;
using System.Collections;
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
        public const int DOC_TYPE_ID = 10;


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

            // specify a load file name
            string loadFile = $"{outDirectory}\\load.csv";

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

            // get all of the fields on the Document object
            IEnumerable<Field> fields = 
                await Common.GetAllFieldsForObject(objMgr, workspaceId, DOC_TYPE_ID);

            IEnumerable<FieldRef> fieldRefs = fields.Select(x => new FieldRef {ArtifactID = x.ArtifactID});

            // query the workspace for all documents
            var query = new QueryRequest
            {
                ObjectType = new ObjectTypeRef { ArtifactTypeID = DOC_TYPE_ID },
                // don't need to return too many characters
                // for export initialization
                MaxCharactersForLongTextValues = 25,
                Fields = fieldRefs
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

            while (true)
            {
                RelativityObjectSlim[] docBatch =
                    await objMgr.RetrieveNextResultsBlockFromExportAsync(workspaceId, initResults.RunID, BATCH_SIZE);

                if (docBatch == null || !docBatch.Any())
                {
                    break;
                }
                await Console.Out.WriteLineAsync($"Exporting batch {currBatchCount} of {batchCountDefinitely} (size {docBatch.Length}).");
                foreach (RelativityObjectSlim obj in docBatch)
                {
                    List<object> fieldValues = obj.Values;
                    
                }
                currBatchCount++;
            }
        }

    }
}
