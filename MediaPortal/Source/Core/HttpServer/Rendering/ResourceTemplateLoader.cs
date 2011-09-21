using System.IO;
using System.Reflection;
using Xunit;

namespace HttpServer.Rendering
{
    /// <summary>
    /// Loads templates from embedded resources.
    /// </summary>
    public class ResourceTemplateLoader : ITemplateLoader
    {
    	private readonly ResourceManager _resourceManager;

		/// <summary>
		/// Initializes the <see cref="ResourceTemplateLoader"/>
		/// </summary>
		public ResourceTemplateLoader() : this(new NullLogWriter())
		{}

		/// <summary>
		/// Initializes the <see cref="ResourceTemplateLoader"/>
		/// </summary>
		/// <param name="logWriter">Log writer to use when logging events and loaded templates</param>
		public ResourceTemplateLoader(ILogWriter logWriter)
		{
			_resourceManager = new ResourceManager(logWriter);
		}

		/// <summary>
		/// Loads templates from a namespace in the given assembly to an uri
		/// </summary>
		/// <param name="toUri">The uri to map the resources to</param>
		/// <param name="fromAssembly">The assembly in which the resources reside</param>
		/// <param name="fromNamespace">The namespace from which to load the resources</param>
		/// <usage>
		/// resourceLoader.LoadResources("/user/", typeof(User).Assembly, "MyLib.Models.User.Views");
		/// 
		/// will make ie the resource MyLib.Models.User.Views.list.Haml accessible via /user/list.haml or /user/list/
		/// </usage>
		public void LoadTemplates(string toUri, Assembly fromAssembly, string fromNamespace)
		{
			_resourceManager.LoadResources(toUri, fromAssembly, fromNamespace);
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
			if(path.EndsWith("*"))
			{
				string[] files = _resourceManager.GetFiles(path);
				if(files.Length == 0)
					return null;

				path = files[0];
			}

			Stream stream = _resourceManager.GetResourceStream(path);

			if (stream != null)
				return new StreamReader(stream);

			return null;
		}

		#region Test LoadTemplate

		[Fact]
		private static void TestLoadTemplate()
		{
			ResourceTemplateLoader loader = new ResourceTemplateLoader();
			loader.LoadTemplates("/test/", loader.GetType().Assembly, loader.GetType().Namespace);
			TextReader reader = loader.LoadTemplate("test\\resourcetest.haml");
			Assert.NotNull(reader);
			reader.Dispose();
		}

    	#endregion

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
			return _resourceManager.GetFiles(path, filename);
		}

		/// <summary>
		/// Always returns true since a resource won't be updated during execution
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
    	public bool CheckTemplate(ITemplateInfo info)
    	{
    		return true;
    	}

		/// <summary>
		/// Returns whether or not the loader has an instance of the file requested
		/// </summary>
		/// <param name="filename">The name of the template/file</param>
		/// <returns>True if the loader can provide the file</returns>
    	public bool HasTemplate(string filename)
    	{
			return _resourceManager.ContainsResource(filename);
		}
	}
}
