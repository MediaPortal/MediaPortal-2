using System.IO;

namespace HttpServer.Rendering
{
    /// <summary>
    /// Interface used to load templates from different sources.
    /// </summary>
    public interface ITemplateLoader
    {
        /// <summary>
        /// Load a template into a <see cref="TextReader"/> and return it.
        /// </summary>
        /// <param name="path">Relative path (and filename) to template.</param>
        /// <returns>a <see cref="TextReader"/> if file was found; otherwise null.</returns>
        TextReader LoadTemplate(string path);

        /// <summary>
        /// Fetch all files from the resource that matches the specified arguments.
        /// </summary>
        /// <param name="path">Where the file should reside.</param>
        /// <param name="filename">Files to check</param>
        /// <returns>a list of files if found; or an empty array if no files are found.</returns>
        string[] GetFiles(string path, string filename);

        /// <summary>
        /// Check's whether a template should be reloaded or not.
        /// </summary>
        /// <param name="info">template information</param>
        /// <returns>true if template is OK; false if it do not exist or are old.</returns>
        bool CheckTemplate(ITemplateInfo info);

		/// <summary>
		/// Returns whether or not the loader has an instance of the file requested
		/// </summary>
		/// <param name="filename">The name of the template/file</param>
		/// <returns>True if the loader can provide the file</returns>
    	bool HasTemplate(string filename);
    }
}
