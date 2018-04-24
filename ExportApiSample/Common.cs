using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relativity.Services;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;

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

        public static async Task<IEnumerable<FieldRef>> GetAllFieldsForObject(
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
                IncludeIDWindow = true  // just want the Artifact IDs
            };

            int start = 0;
            const int LENGTH = 100;

            QueryResultSlim result = await objMgr.QuerySlimAsync(workspaceId, queryRequest, start, LENGTH);
            
            // I think the IDWindow should include all of the docs...
            return result
                .IDWindow
                .Select(id => new FieldRef {ArtifactID = id});
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
