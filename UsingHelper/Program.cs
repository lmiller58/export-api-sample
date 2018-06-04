using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExportApiHelper;
using Relativity.Services.Objects.DataContracts;

namespace UsingHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ExportApiHelperConfig
            {
                BlockSize = 1000,
                Fields = new FieldAndType[]
                {
                    new FieldAndType
                    {
                        Field = new Relativity.Services.Objects.DataContracts.FieldRef
                        {
                            Name = "Control Number",
                        },
                        Type = FieldTypes.FixedLengthText
                    },

                    new FieldAndType
                    {
                        Field = new FieldRef
                        {
                            Name = "Extracted Text"
                        },
                        Type = FieldTypes.LongText
                    }
                },

                WorkspaceId = 1017273,
                MaximumInlineTextSize = 1024 * 100,
                RelativityUrl = new Uri("https://p-dv-vm-ij1ps8v.kcura.corp"),
                UserName = "relativity.admin@kcura.com",
                UserPassword = "Test1234!"
            };
        }
    }
}
