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
    /// Contains public common methods. Couldn't think of a better name.
    /// </summary>
    public static class Common
    {
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
                Fields = new List<FieldRef>(),
                IncludeIDWindow = true  // just want the Artifact IDs
            };

            int start = 0;
            const int LENGTH = 1;

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
