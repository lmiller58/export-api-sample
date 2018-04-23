using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Relativity.Kepler.Transport;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;

namespace ExportApiSample
{
    public static class Documents
    {
        public const int TYPE_ID = 10;


        /// <summary>
        /// Returns the number of digits in an integer. 
        /// If negative, the negative sign does not count.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        private static int NumDigits(int num)
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
        public static void ExportNatives(IObjectManager objMgr, int workspaceId, string outDirectory)
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

        }
    }
}
