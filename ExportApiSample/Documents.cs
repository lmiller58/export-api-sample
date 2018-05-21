using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Relativity.Services.DataContracts.ImportExport;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;

namespace ExportApiSample
{
    public static class Documents
    {
        public const int DOC_TYPE_ID = 10;

        /// <summary>
        /// Returns the Control Number and Extracted Text fields
        /// </summary>
        /// <returns></returns>
        private static List<Field> GetMinimumFields()
        {
            List<Field> docFields = new List<Field>
            {
                new Field()
                {
                    ArtifactID = 1003667,
                    Name = "Control Number",
                    FieldType = FieldType.FixedLengthText
                },
                new Field()
                {
                    ArtifactID = 1003668,
                    Name = "Extracted Text",
                    FieldType = FieldType.LongText
                }
            };
            return docFields;
        }


        /// <summary>
        /// Generic export method
        /// </summary>
        /// <param name="objMgr"></param>
        /// <param name="workspaceId"></param>
        /// <param name="batchSize"></param>
        /// <param name="fields"></param>
        /// <param name="queryRequest"></param>
        /// <param name="outDirectory"></param>
        /// <returns></returns>
        private static void Export(
            IObjectManager objMgr,
            int workspaceId,
            int batchSize,
            List<Field> fields, 
            QueryRequest queryRequest,
            string outDirectory)
        {
            // check if directory exists and create
            // it if it doesn't
            if (!Directory.Exists(outDirectory))
            {
                Directory.CreateDirectory(outDirectory);
            }

            // specify a load file name
            string loadFile = $"{outDirectory}\\load.csv";

            // write all of the names of the fields to the load file
            // so they become column headings
            IEnumerable<string> fieldNames = fields.Select(x => x.Name);
            File.AppendAllText(loadFile, String.Join(",", fieldNames));

            // convert to FieldRefs for the query
            IEnumerable<FieldRef> fieldRefs = fields.Select(x => new FieldRef { ArtifactID = x.ArtifactID });
            queryRequest.Fields = fieldRefs;

            const int startPage = 0; // index of starting page

            // initialize export so we know how many documents total we 
            // are exporting
            ExportInitializationResults initResults =
                objMgr.InitializeExportAsync(workspaceId, queryRequest, startPage).Result;

            long totalCount = initResults.RecordCount;

            // if total count is evenly divisble by the 
            // batch size, then we don't have to create any batches
            // for the leftovers
            long batchCountMaybe = totalCount / batchSize;
            long batchCountDefinitely =
                totalCount % batchSize == 0
                    ? batchCountMaybe
                    : batchCountMaybe + 1;

            long currBatchCount = 1;

            while (true)
            {
                RelativityObjectSlim[] docBatch =
                    objMgr.RetrieveNextResultsBlockFromExportAsync(workspaceId, initResults.RunID, batchSize).Result;

                if (docBatch == null || !docBatch.Any())
                {
                    break;
                }
                Console.WriteLine($"Exporting batch {currBatchCount} of {batchCountDefinitely} (size {docBatch.Length}).");
                foreach (RelativityObjectSlim obj in docBatch)
                {
                    List<object> fieldValues = obj.Values;

                    Common.AppendToLoadFileAsync(
                        objMgr,
                        workspaceId,
                        obj.ArtifactID,
                        fields,
                        fieldValues,
                        loadFile);
                }
                currBatchCount++;
            }

            // finish up the queue
            Common.CompleteAddingBatches();

            Directory.Delete(outDirectory, true);
        }



        /// <summary>
        /// Exports all metadata and long text of documents from a workspace
        /// </summary>
        /// <param name="objMgr"></param>
        /// <param name="workspaceId"></param>
        /// <param name="outDirectory">Directory to which we are exporting the files</param>
        public static async Task ExportAllDocsAndFieldsAsync(IObjectManager objMgr, int workspaceId, string outDirectory)
        {
            // specify a batch size
            const int BATCH_SIZE = 1000;

            // get all of the fields on the Document object
            //List<Field> fields =
            //    await Common.GetAllFieldsForObject(objMgr, workspaceId, DOC_TYPE_ID);

            List<Field> fields = GetMinimumFields();

            // query the workspace for all documents
            var query = new QueryRequest
            {
                ObjectType = new ObjectTypeRef { ArtifactTypeID = DOC_TYPE_ID },
                MaxCharactersForLongTextValues = 25
            };

            Export(objMgr, workspaceId, BATCH_SIZE, fields, query, outDirectory);

        }


        /// <summary>
        /// Export documents only from a saved search
        /// </summary>
        /// <param name="objMgr"></param>
        /// <param name="workspaceId"></param>
        /// <param name="savedSearchId"></param>
        /// <param name="outDirectory"></param>
        /// <returns></returns>
        public static async Task ExportFromSavedSearchAsync(
            IObjectManager objMgr, 
            int workspaceId, 
            int savedSearchId, 
            string outDirectory)
        {
            // specify a batch size
            const int BATCH_SIZE = 1000;

            // get all of the fields on the Document object
            List<Field> fields =
                await Common.GetAllFieldsForObject(objMgr, workspaceId, DOC_TYPE_ID);

            var query = new QueryRequest
            {
                ObjectType = new ObjectTypeRef { ArtifactTypeID = DOC_TYPE_ID },
                // don't need to return too many characters
                // for export initialization
                MaxCharactersForLongTextValues = 25,
                //ExecutingSavedSearchID = savedSearchId
                Condition = $"(('Artifact ID' IN SAVEDSEARCH {savedSearchId}))"
            };

            Export(objMgr, workspaceId, BATCH_SIZE, fields, query, outDirectory);
        }

    }
}
