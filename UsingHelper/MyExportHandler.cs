using System;
using ExportApiHelper;
using Relativity.Services.Objects.DataContracts;

namespace UsingHelper
{
    public class MyExportHandler : IExportApiHandler
    {
        /// <summary>
        /// Print a progress message every _PRINT_FREQ 
        /// items
        /// </summary>
        private const int _PRINT_FREQ = 1000;

        /// <summary>
        /// Keep track of total items exported
        /// </summary>
        private int _itemCount = 0;


        /// <summary>
        /// In the field collection, which index points 
        /// to the Extracted Text field
        /// </summary>
        public int ExtractedTextIndex { get; }

        /// <summary>
        /// Creates a new export handler. The extracted text
        /// index is the 0-based index that points to
        /// the extracted text field in the returned field
        /// collection. This field collection corresponds to
        /// the field names collection in 
        /// ExportApiHelperConfig.Fields
        /// </summary>
        /// <param name="extractedTextIndex"></param>
        public MyExportHandler(int extractedTextIndex)
        {
            this.ExtractedTextIndex = extractedTextIndex;
        }

        public bool TheadSafe => true;

        public void After(bool complete)
        {
            Console.WriteLine($"Completed export: {complete}");
        }

        public void Before(long itemCount)
        {
            // check to make sure use has specified a non-negative index
            // for the extracted field
            if (ExtractedTextIndex < 0)
            {
                throw new InvalidOperationException(
                    $"The specified extracted text index {this.ExtractedTextIndex} is not valid. " +
                    "Please specify the index of the extracted text field in the returned collection");
            }
            Console.WriteLine($"Current itemCount: {itemCount}");
        }

        public void Error(string message, Exception exception)
        {
            Console.WriteLine("Error: " + message);
            Console.WriteLine(exception);
        }

        /// <summary>
        /// Handles the import of an item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Item(RelativityObjectSlim item)
        {
            // the handler has to know ahead of the time at which index the 
            // extracted text will be, since RelativityObjectSlim only returns
            // the values
            int textIndex = this.ExtractedTextIndex;

            if (item.Values[textIndex] is string)
            {
                // handle exporting string
            }
            else if (item.Values[textIndex] is System.IO.Stream)
            {
                // handle stream
            }

            // thread-safe increment
            System.Threading.Interlocked.Increment(ref _itemCount);

            if (_itemCount % _PRINT_FREQ == 0)
            {
                Console.WriteLine($"Exported {_itemCount} items");
            }
            return true;
        }
    }
}
