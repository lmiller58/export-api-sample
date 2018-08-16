using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Relativity.Kepler.Transport;
using Relativity.Services;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;
using FieldType = Relativity.Services.Objects.DataContracts.FieldType;

namespace ExportApiSample
{
    /// <summary>
    /// Contains public common methods and elements. Couldn't think of a better name.
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// The token returned if extracted text size exceeds cutoff value
        /// </summary>
        private const string _TOKEN = "#KCURA99DF2F0FEB88420388879F1282A55760#";

        /// <summary>
        /// Reference to object manager
        /// </summary>
        public static IObjectManager ObjMgr = null;

        /// <summary>
        /// Aliases for the proper field names as shown in Relativity
        /// </summary>
        public static class FieldTypes
        {
            public const string CURRENCY = "Currency";
            public const string DATE = "Date";
            public const string DECIMAL = "Decimal";
            public const string FILE = "File";
            public const string FIXED_LENGTH_TXT = "Fixed-Length Text";
            public const string LONG_TXT = "Long Text";
            public const string MULTI_CHOICE = "Multiple Choice";
            public const string MULTI_OBJECT = "Multiple Object";
            public const string SINGLE_CHOICE = "Single Choice";
            public const string SINGLE_OBJECT = "Single Object";
            public const string USER = "User";
            public const string WHOLE_NUMBER = "Whole Number";
            public const string YES_NO = "Yes/No";
        }

        /// <summary>
        /// The object type ID for fields
        /// </summary>
        public const int FIELD_OBJ_TYPE_ID = 14;

        private static readonly BlockingCollection<WriteJob> _writeJobs = 
            new BlockingCollection<WriteJob>(10000);


        /// <summary>
        /// THIS is the method that needs to be what each
        /// thread executes--streams the text to a file. Before
        /// execution, it requires that Common.ObjMgr point to
        /// a valid IObjectManager service.
        /// </summary>
        public static void StreamToFile()
        {
            // check if Object Manager has been initialized
            if (ObjMgr == null)
            {
                throw new ApplicationException(
                    "Object manager is null");
            }

            const int BUFFER_SIZE = 5000;
            while (!_writeJobs.IsCompleted)
            {
                WriteJob op;
                bool success = _writeJobs.TryTake(out op);
                if (!success)
                {
                    Thread.Sleep(100);
                    continue;
                }

                // stream

                // create ref to object
                var relativityObj = new RelativityObjectRef
                {
                    ArtifactID = op.DocumentId
                };

                // create ref to field
                var longTextFieldRef = new FieldRef
                {
                    ArtifactID = op.LongTextFieldId
                };

                using (IKeplerStream ks = ObjMgr.StreamLongTextAsync(
                    op.WorkspaceId, relativityObj, longTextFieldRef).Result)
                using (Stream s = ks.GetStreamAsync().Result)
                using (var reader = new StreamReader(s))
                using (var writer = new StreamWriter(op.Path, append: false))
                {
                    
                    char[] buffer = new char[BUFFER_SIZE];

                    int copied;
                    do
                    {
                        copied = reader.Read(buffer, 0, BUFFER_SIZE);
                        writer.Write(
                            copied == BUFFER_SIZE
                            ? buffer
                            : buffer.Take(copied).ToArray());
                        writer.Flush();
                    } while (copied > 0);

                }
            }
        }


        /// <summary>
        /// Returns all of the fields for a given object type.
        /// Does not include system fields.
        /// </summary>
        /// <param name="objMgr"></param>
        /// <param name="workspaceId"></param>
        /// <param name="objectTypeId"></param>
        /// <returns></returns>
        public static async Task<List<Field>> GetAllFieldsForObject(
            IObjectManager objMgr, 
            int workspaceId, 
            int objectTypeId)
        {
            var objectTypeCondition = new WholeNumberCondition(
                "Object Type Artifact Type ID", 
                NumericConditionEnum.EqualTo, 
                objectTypeId);

            // we want to exclude system types
            var textCondition = new TextCondition("Name", TextConditionEnum.Like, "System");
            NotCondition excludeSystemCondition = textCondition.Negate();

            var condition = new CompositeCondition(
                objectTypeCondition, 
                CompositeConditionEnum.And, 
                excludeSystemCondition);

            var queryRequest = new QueryRequest
            {
                ObjectType = new ObjectTypeRef { ArtifactTypeID = FIELD_OBJ_TYPE_ID },
                Condition = condition.ToQueryString(),
                Fields = new List<FieldRef>
                {
                    //new FieldRef() { Name = "Name" },
                    new FieldRef { Name = "Field Type" }
                },
                IncludeIDWindow = false ,
                IncludeNameInQueryResult = true
            };

            int start = 0;

            // a document shouldn't have more than 1000 fields, I would hope
            const int LENGTH = 1000;

            var retVal = new List<Field>();

            QueryResult result = await objMgr.QueryAsync(workspaceId, queryRequest, start, LENGTH);

            foreach (RelativityObject field in result.Objects)
            {

                if (!field.FieldValuePairExists("Field Type"))
                {
                    continue;  // skip
                }
                // determine the field type
                string fieldTypeName = field.FieldValues.First().Value.ToString();
                FieldType type;
                switch (fieldTypeName)
                {
                    case FieldTypes.CURRENCY:
                        type = FieldType.Currency;
                        break;
                    case FieldTypes.DATE:
                        type = FieldType.Date;
                        break;
                    case FieldTypes.DECIMAL:
                        type = FieldType.Decimal;
                        break;
                    case FieldTypes.FILE:
                        type = FieldType.File;
                        break;
                    case FieldTypes.FIXED_LENGTH_TXT:
                        type = FieldType.FixedLengthText;
                        break;
                    case FieldTypes.LONG_TXT:
                        type = FieldType.LongText;
                        break;
                    case FieldTypes.MULTI_CHOICE:
                        type = FieldType.MultipleChoice;
                        break;
                    case FieldTypes.MULTI_OBJECT:
                        type = FieldType.MultipleObject;
                        break;
                    case FieldTypes.SINGLE_CHOICE:
                        type = FieldType.SingleChoice;
                        break;
                    case FieldTypes.SINGLE_OBJECT:
                        type = FieldType.SingleObject;
                        break;
                    case FieldTypes.USER:
                        type = FieldType.User;
                        break;
                    case FieldTypes.WHOLE_NUMBER:
                        type = FieldType.WholeNumber;
                        break;
                    case FieldTypes.YES_NO:
                        type = FieldType.YesNo;
                        break;
                    default:
                        type = FieldType.Empty;
                        break;
                }

                var fieldToAdd = new Field
                {
                    ArtifactID = field.ArtifactID,
                    FieldType = type,
                    Name = field.Name
                };
                retVal.Add(fieldToAdd);
            }
            
            return retVal;
        }

        /// <summary>
        /// Lets this class know that we've retrieved all batches
        /// </summary>
        public static void CompleteAddingBatches()
        {
            _writeJobs.CompleteAdding();
        }


        /// <summary>
        /// Appends the metadata to a load file and writes the extracted
        /// text to a separate file
        /// </summary>
        /// <param name="workspaceId">Artifact ID of the workspace</param>
        /// <param name="objectId">Artifact ID of the object</param>
        /// <param name="fieldNames">List of field names associated with the objects</param>
        /// <param name="fieldValues">List of field values asscoiated with the objects</param>
        /// <param name="loadFilePath">Path to the load file</param>
        public static void AppendToLoadFileAsync(
            int workspaceId,
            int objectId,
            List<Field> fieldNames, 
            List<object> fieldValues, 
            string loadFilePath)
        {
            // this list of fields should be in the same order as that of our requests.
            if (fieldValues.Count != fieldNames.Count)
            {
                string err = "Lengths of queried and returned fields do not match:\n" +
                             $"Queried: {fieldNames.Count}\n" +
                             $"Returned: {fieldValues.Count}";
                throw new ApplicationException(err);
            }

            string[] rowData = new string[fieldNames.Count];

            const string TEXT_FOLDER_NAME = "TEXT";
            const string fileExt = ".txt";
            // get parent folder for the load file
            string parentDir = Directory.GetParent(loadFilePath).FullName;
            string outputFileFolder = parentDir + @"\" + TEXT_FOLDER_NAME;
            // check if directory exists
            if (!Directory.Exists(outputFileFolder))
            {
                Directory.CreateDirectory(outputFileFolder);
            }

            for (int i = 0; i < fieldValues.Count; i++)
            {
                Field fieldName = fieldNames[i];
                object fieldValue = fieldValues[i];
                string fieldValAsStr = fieldValue?.ToString() ?? String.Empty;

                if (String.IsNullOrEmpty(fieldValAsStr))
                {
                    rowData[i] = String.Empty;
                    continue;
                }

                switch (fieldName.FieldType)
                {
                    // types that can be directly 
                    // converted to a string
                    case FieldType.Currency:
                    case FieldType.Date:
                    case FieldType.Decimal:
                    case FieldType.Empty:                   
                    case FieldType.WholeNumber:
                        rowData[i] = fieldValAsStr;
                        break;
                    case FieldType.FixedLengthText:
                        string cleaned = Regex.Replace(fieldValAsStr, @"\t|\n|\r", "");
                        rowData[i] = "\"" + cleaned + "\"";                     
                        break;
                    case FieldType.LongText:                       
                        // generate a GUID for the file name
                        string textFileName = Guid.NewGuid().ToString() + fileExt;
                        string outputFile = outputFileFolder + @"\" + textFileName;
                        if (fieldValAsStr.Equals(_TOKEN))
                        {
                            // this means that we've exceeded our specified cutoff value,
                            // so we need to stream the long text
                            AddToQueueAsync(workspaceId, objectId, fieldName.ArtifactID, outputFile);
                        }
                        else
                        {
                            // this means that we're getting back the text in the JSON,
                            // so we'll just write it to the destination
                            File.WriteAllText(outputFile, fieldValAsStr);
                        }
                        
                        string relativePath = @".\" + TEXT_FOLDER_NAME + @"\" + textFileName;
                        rowData[i] = relativePath;
                        break;

                    case FieldType.MultipleChoice:
                        JArray multiChoiceValues = JArray.Parse(Convert.ToString(fieldValue));
                        List<string> multichoiceValuesNames =
                            multiChoiceValues.Select(jToken => jToken["Name"].ToObject<string>()).ToList();
                        rowData[i] = String.Join(";", multichoiceValuesNames);
                        break;                 
                    
                    case FieldType.SingleChoice:
                        JObject fieldRef = JObject.Parse(fieldValAsStr);
                        rowData[i] = fieldRef["Name"].ToObject<string>();
                        break;

                    case FieldType.MultipleObject:
                    case FieldType.SingleObject:
                        // TODO: actual implementaion
                        rowData[i] = "Not included";
                        break;

                    case FieldType.File:
                        //throw new NotImplementedException(
                        //    "Export API does not support native file streaming yet.");
                        rowData[i] = "Not included";
                        break;

                    case FieldType.User:
                        JArray userFieldArr = JArray.Parse(fieldValAsStr);
                        List<string> userFieldVals = userFieldArr.Select(x => x["Name"].ToObject<string>()).ToList();
                        rowData[i] = string.Join("; ", userFieldVals);
                        break;

                    case FieldType.YesNo:
                        // handle case where we get a true/false instead of JSON
                        var boolMap = new Dictionary<string, string>
                        {
                            { "True", "Yes" },
                            { "False", "No" },
                            { "", "" }
                        };

                        if (boolMap.ContainsKey(fieldValAsStr))
                        {
                            rowData[i] = boolMap[fieldValAsStr];
                            break;
                        }

                        JArray yesNoArray = JArray.Parse(fieldValAsStr);
                        List<string> yesNoValues = yesNoArray.Select(jToken => jToken["Name"].ToObject<string>()).ToList();
                        rowData[i] = String.Join("; ", yesNoValues);
                        break;

                    default:
                        rowData[i] = String.Empty;
                        break;
                }
            }

            // write row to csv
            File.AppendAllText(loadFilePath, Environment.NewLine + String.Join(",", rowData));
        }

        private static bool IsValidJsonObject(string input, out JObject result)
        {
            try
            {
                result = JObject.Parse(input);
                return true;
            }
            catch (JsonReaderException)
            {
                result = null;
                return false;
            }
        }


        /// <summary>
        /// Adds a write job to the queue
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <param name="objectId"></param>
        /// <param name="longTextField"></param>
        /// <param name="outputFile"></param>
        /// <returns></returns>
        private static void AddToQueueAsync(
            int workspaceId,
            int objectId,
            int longTextFieldId, 
            string outputFile)
        {
            var writeJob = new WriteJob
            {
                WorkspaceId = workspaceId,
                DocumentId = objectId,
                LongTextFieldId = longTextFieldId,
                Path = outputFile
            };

            _writeJobs.Add(writeJob);           
        }



        /// <summary>
        /// Returns the number of digits in an integer (base 10). 
        /// If negative, the negative sign does not count.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static int CountBase10Digits(long num)
        {
            if (num == 0)
            {
                return 1;
            }
            num = Math.Abs(num);

            return (int)Math.Log10(num);
        }

    }
}
