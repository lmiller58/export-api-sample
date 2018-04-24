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
        /// Exports all metadata and long text of documents from a workspace
        /// </summary>
        /// <param name="objMgr"></param>
        /// <param name="workspaceId"></param>
        /// <param name="outDirectory">Directory to which we are exporting the files</param>
        public static async Task ExportData(IObjectManager objMgr, int workspaceId, string outDirectory)
        {
            // check if directory exists and create
            // it if it doesn't
            if (!Directory.Exists(outDirectory))
            {
                Directory.CreateDirectory(outDirectory);
            }

            // specify a load file name
            string loadFile = $"{outDirectory}\\load.csv";

            // specify a batch size
            const int BATCH_SIZE = 200;

            // get all of the fields on the Document object
            List<Field> fields = 
                await Common.GetAllFieldsForObject(objMgr, workspaceId, DOC_TYPE_ID);

            // write all of the names of the fields to the load file
            // so they become column headings
            IEnumerable<string> fieldNames = fields.Select(x => x.Name);
            File.AppendAllText(loadFile, String.Join(",", fieldNames));

            // convert to FieldRefs for the query
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

                    // this list of fields should be in the same order as that of our requests.
                    if (fieldValues.Count != fields.Count)
                    {
                        string err = "Lengths of queried and returned fields do not match:\n" +
                                    $"Queried: {fields.Count}\n" + 
                                    $"Returned: {fieldValues.Count}";
                        throw new ApplicationException(err);
                    }

                    for (int i = 0; i < fieldValues.Count; i++)
                    {
                        Field field = fields[i];
                        object fieldValue = fieldValues[i];
                        await Common.AppendToLoadFileAsync(
                            objMgr, 
                            workspaceId, 
                            obj.ArtifactID, 
                            fields, 
                            fieldValues,
                            loadFile);
                    }
                }
                currBatchCount++;
            }
        }

    }
}
