using System;
using System.IO;
using Fadd;

namespace HttpServer.Rendering
{

    /// <summary>
    /// This template loader loads all templates from a folder on the hard drive.
    /// </summary>
    public class FileTemplateLoader : ITemplateLoader
    {
        private string _pathPrefix = "views\\";

        /// <summary>
        /// Initializes a new instance of the <see cref="FileTemplateLoader"/> class.
        /// </summary>
        /// <param name="pathPrefix">A prefix that is prepended to all requested files.</param>
        /// <seealso cref="PathPrefix"/>
        public FileTemplateLoader(string pathPrefix)
        {
            Check.Require(pathPrefix, "pathPrefix");
            _pathPrefix = pathPrefix;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileTemplateLoader"/> class.
        /// </summary>
        public FileTemplateLoader()
        {
            _pathPrefix = Directory.Exists("..\\..\\views\\") ? "..\\..\\views\\" : "views\\";
        }

        /// <summary>
        /// A prefix that is prepended to all requested files.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// // will look after template in Environment.CurrentDirectory + "views\\<ControllerName>\\templateName.*"
        /// mgr.PathPrefix = "views\\";
        /// ]]>
        /// </code>
        /// </example>
        /// <remarks>PathPrefix may not be null, only string.Empty
        /// </remarks>
        public string PathPrefix
        {
            get { return _pathPrefix; }
            set
            {

                if (value == null)
                    throw new ArgumentNullException("value");

                if (!string.IsNullOrEmpty(_pathPrefix) && _pathPrefix[_pathPrefix.Length - 1] != '\\')
                    _pathPrefix = value + "\\";
                else
                    _pathPrefix = value;
            }
        }

        /// <summary>
        /// Load a template into a <see cref="TextReader"/> and return it.
        /// </summary>
        /// <param name="path">Relative path (and filename) to template.</param>
        /// <returns>
        /// a <see cref="TextReader"/> if file was found; otherwise null.
        /// </returns>
        public TextReader LoadTemplate(string path)
        {
			if(!File.Exists(_pathPrefix + path))
				return null;

            try
            {
                Stream stream = File.OpenRead(_pathPrefix + path);
                TextReader reader = new StreamReader(stream);
                return reader;
            }
            catch (DirectoryNotFoundException err)
            {
                throw new FileNotFoundException("Directory not found for: " + path, err);
            }
            catch (PathTooLongException err)
            {
                throw new FileNotFoundException("Path too long: " + path, err);
            }
            catch (UnauthorizedAccessException err)
            {
                throw new UnauthorizedAccessException("Failed to access: " + path, err);
            }
        }

        /// <summary>
        /// Fetch all files from the resource that matches the specified arguments.
        /// </summary>
        /// <param name="path">Where the file should reside.</param>
        /// <param name="filename">Files to check</param>
        /// <returns>
        /// a list of files if found; or an empty array if no files are found.
        /// </returns>
        public string[] GetFiles(string path, string filename)
        {
			if(Directory.Exists(_pathPrefix + path))
                return Directory.GetFiles(_pathPrefix + path, filename);

        	return new string[]{};
        }

		/// <summary>
		/// Returns whether or not the loader has an instance of the file requested
		/// </summary>
		/// <param name="filename">The name of the template/file</param>
		/// <returns>True if the loader can provide the file</returns>
		public bool HasTemplate(string filename)
		{
			return File.Exists(_pathPrefix + filename);
		}

        /// <summary>
        /// Check's whether a template should be reloaded or not.
        /// </summary>
        /// <param name="info">template information</param>
        /// <returns>
        /// true if template is OK; false if it do not exist or are old.
        /// </returns>
        public bool CheckTemplate(ITemplateInfo info)
        {
			if(HasTemplate(info.Filename))
                return File.GetLastWriteTime(_pathPrefix + info.Filename) <= info.CompiledWhen;

            return false;
        }
    }
}
