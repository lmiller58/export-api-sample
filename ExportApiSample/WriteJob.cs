namespace ExportApiSample
{
    /// <summary>
    /// Contains the necessary information for a 
    /// thread to stream a document's text from the Export API
    /// </summary>
    public class WriteJob
    {
        /// <summary>
        /// Path for file to be written
        /// </summary>
        public string Path { get; set; }


        /// <summary>
        /// Artifact ID of the workspace
        /// </summary>
        public int WorkspaceId { get; set; }

        /// <summary>
        /// Artifact ID of the document
        /// </summary>
        public int DocumentId { get; set; }


        /// <summary>
        /// The extracted text's field Artifact ID
        /// </summary>
        public int LongTextFieldId { get; set; }
    }
}
