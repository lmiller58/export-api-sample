using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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


        public const int FIELD_OBJ_TYPE_ID = 14;

        public static async Task<List<Field>> GetAllFieldsForObject(
            IObjectManager objMgr, 
            int workspaceId, 
            int objectTypeId)
        {
            var condition = new WholeNumberCondition(
                "Object Type Artifact Type ID", 
                NumericConditionEnum.EqualTo, 
                objectTypeId);

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

            // a document/RDO shouldn't have more than 1000 fields, I would hope
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
        /// Appends the metadata to a file
        /// </summary>
        /// <param name="objMgr">Object manager service</param>
        /// <param name="workspaceId">Artifact ID of the workspace</param>
        /// <param name="objectId">Artifact ID of the object</param>
        /// <param name="fieldNames">List of field names associated with the objects</param>
        /// <param name="fieldValues">List of field values asscoiated with the objects</param>
        /// <param name="loadFilePath">Path to the load file</param>
        public static async Task AppendToLoadFileAsync(
            IObjectManager objMgr,
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
                    case FieldType.FixedLengthText:
                    case FieldType.WholeNumber:
                        // for some reason, we will get JSON
                        // for some fixed-length fields, so we
                        // need to clean them--this is whack
                        JObject jsonObj;
                        bool isValidJson = IsValidJsonObject(fieldValAsStr, out jsonObj);
                        if (isValidJson)
                        {
                            // add quotes
                            rowData[i] = "\"" + jsonObj["Name"].ToObject<string>() + "\"";
                        }
                        else
                        {
                            string cleaned = Regex.Replace(fieldValAsStr, @"\t|\n|\r", "");
                            rowData[i] = cleaned;
                        }
                        break;
                    case FieldType.LongText:
                        // get parent folder for the load file
                        string parentDir = Directory.GetParent(loadFilePath).FullName;
                        string outputFileFolder = parentDir + @"\" + TEXT_FOLDER_NAME;
                        // check if directory exists
                        if (!Directory.Exists(outputFileFolder))
                        {
                            Directory.CreateDirectory(outputFileFolder);
                        }
                        const string fileExt = ".txt";

                        // randomly generate a GUID for the file name
                        string textFileName = Guid.NewGuid().ToString() + fileExt;
                        string outputFile = outputFileFolder + @"\" + textFileName;
                        await StreamToFileAsync(objMgr, workspaceId, objectId, fieldName, outputFile);
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
        /// Streams the text data to a file
        /// </summary>
        /// <param name="objMgr"></param>
        /// <param name="workspaceId"></param>
        /// <param name="objectId"></param>
        /// <param name="longTextField"></param>
        /// <param name="outputFile"></param>
        /// <returns></returns>
        private static async Task StreamToFileAsync(
            IObjectManager objMgr,
            int workspaceId,
            int objectId,
            Field longTextField, 
            string outputFile)
        {
            // create ref to object
            var relativityObj = new RelativityObjectRef
            {
                ArtifactID = objectId
            };

            // create ref to field
            var longTextFieldRef = new FieldRef
            {
                ArtifactID = longTextField.ArtifactID
            };

            using (IKeplerStream ks = await objMgr.StreamLongTextAsync(
                workspaceId, relativityObj, longTextFieldRef))
            using (Stream s = await ks.GetStreamAsync())
            using (var reader = new StreamReader(s))
            using (var writer = new StreamWriter(outputFile, append: false))
            {
                const int BUFFER_SIZE = 5000;
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
