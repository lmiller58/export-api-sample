using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public static async Task<IEnumerable<Field>> GetAllFieldsForObject(
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
                    new FieldRef { Name = "Field Type" }
                },
                IncludeIDWindow = false 
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
                    FieldType = type
                };
                retVal.Add(fieldToAdd);
            }
            
            return retVal;
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
