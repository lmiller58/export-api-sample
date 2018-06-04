using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExportApiHelper;
using Relativity.Services.Objects.DataContracts;

namespace UsingHelper
{
    public class MyExportHandler : IExportApiHandler
    {
        public bool TheadSafe => throw new NotImplementedException();

        public void After(bool complete)
        {
            throw new NotImplementedException();
        }

        public void Before(long itemCount)
        {
            throw new NotImplementedException();
        }

        public void Error(string message, Exception exception)
        {
            throw new NotImplementedException();
        }

        public bool Item(RelativityObjectSlim item)
        {
            throw new NotImplementedException();
        }
    }
}
