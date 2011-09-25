using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Fadd;
using Fadd.Logging;
using Xunit;

namespace HttpServer.Rendering
{
	/// <summary>
	/// Class to handle loading of resource files
	/// </summary>
	public class ResourceManager
	{
		/// <summary><![CDATA[
		/// Maps uri's to resources, Dictionary<uri, resource>
		/// ]]></summary>
		private readonly Dictionary<string, List<ResourceInfo>> _loadedResources = new Dictionary<string, List<ResourceInfo>>();

		private readonly ILogWriter _logWriter;

		/// <summary>
		/// Initializes the <see cref="ResourceManager"/>
		/// </summary>
		/// <param name="logWriter">The log writer to use for outputting loaded resource</param>
		public ResourceManager(ILogWriter logWriter)
		{
			_logWriter = logWriter;
		}

		/// <summary>
		/// Parses a filename and sets it to the extensionless name in lowercase. The extension is cut out without the dot.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="extension"></param>
		/// <usage>
		/// string ext;
		/// string filename = "/uSeR/teSt.haMl";
		/// ParseName(ref filename, out ext);
		/// Console.WriteLine("File: " + filename); 
		/// Console.WriteLine("Ext: " + ext);
		/// -> user/test
		/// -> haml
		/// </usage>
		private static void ParseName(ref string filename, out string extension)
		{
			Check.NotEmpty(filename, "filename");

			filename = filename.ToLower();
			int indexOfExtension = filename.LastIndexOf('.');
			if (indexOfExtension == -1)
			{
				extension = string.Empty;
				filename = filename.TrimStart('/');
			}
			else
			{
				extension = filename.Substring(indexOfExtension + 1);
				filename = filename.Substring(0, indexOfExtension).TrimStart('/');
			}
		}

		#region Test for ParseName

		[Fact]
		private static void TestParseName()
		{
			string extension;
			string filename = "/uSEr/test/hej.*";
			ParseName(ref filename, out extension);
			Assert.Equal("*", extension);
			Assert.Equal("user/test/hej", filename);

			filename = "test/teSt.xMl";
			ParseName(ref filename, out extension);
			Assert.Equal("xml", extension);
			Assert.Equal("test/test", filename);

			filename = "test/TeSt";
			ParseName(ref filename, out extension);
			Assert.Equal(string.Empty, extension);
			Assert.Equal("test/test", filename);
		}

		#endregion

		/// <summary>
		/// Add a resource to a specified uri without extension, ie user/test
		/// </summary>
		/// <param name="uri">The uri to add the resource to</param>
		/// <param name="info">The <see cref="ResourceInfo"/> instance describing the resource</param>
		private void AddResource(string uri, ResourceInfo info)
		{
			List<ResourceInfo> resources;
			if (!_loadedResources.TryGetValue(uri, out resources))
			{
				resources = new List<ResourceInfo>();
				_loadedResources.Add(uri, resources);
			}

			if (resources.Find(delegate(ResourceInfo resource) { return resource.Extension == info.Extension; }) != null)
				throw new InvalidOperationException(string.Format("A resource with the name '{0}.{1}' has already been added.", uri, info.Extension));

			resources.Add(info);
		}

		/// <summary>
		/// Loads resources from a namespace in the given assembly to an uri
		/// </summary>
		/// <param name="toUri">The uri to map the resources to</param>
		/// <param name="fromAssembly">The assembly in which the resources reside</param>
		/// <param name="fromNamespace">The namespace from which to load the resources</param>
		/// <usage>
		/// resourceLoader.LoadResources("/user/", typeof(User).Assembly, "MyLib.Models.User.Views");
		/// 
		/// will make ie the resource MyLib.Models.User.Views.list.Haml accessible via /user/list.haml or /user/list/
		/// </usage>
		public void LoadResources(string toUri, Assembly fromAssembly, string fromNamespace)
		{
			toUri = toUri.ToLower().TrimEnd('/');
			fromNamespace = fromNamespace.ToLower();
			if (!fromNamespace.EndsWith("."))
				fromNamespace += ".";

			foreach (string resourceName in fromAssembly.GetManifestResourceNames())
			{
				if (resourceName.ToLower().StartsWith(fromNamespace))
				{
					ResourceInfo info = new ResourceInfo(resourceName, fromAssembly);
					string uri = toUri + "/" + resourceName.Substring(fromNamespace.Length).ToLower().Replace('.', '/');
					uri = uri.TrimStart('/');
					if (!string.IsNullOrEmpty(info.Extension))
						uri = uri.Substring(0, uri.Length - info.Extension.Length - 1);

					AddResource(uri, info);
					_logWriter.Write(this, LogPrio.Info, "Resource '" + info.Name + "' loaded to uri: " + uri);					
				}
			}
		}

		#region Test for LoadResources

		[Fact]
		private static void TestLoadTemplates()
		{
			LogManager.SetProvider(new NullLogProvider());

			ResourceManager resourceManager = new ResourceManager(new NullLogWriter());
			resourceManager.LoadResources("/test/", resourceManager.GetType().Assembly, resourceManager.GetType().Namespace);
			Assert.NotNull(resourceManager._loadedResources["test/resourcetest"]);
			Assert.Equal("haml", resourceManager._loadedResources["test/resourcetest"][0].Extension);
			Assert.Equal(resourceManager.GetType().Namespace + ".resourcetest.haml", resourceManager._loadedResources["test/resourcetest"][0].Name);

			resourceManager._loadedResources.Clear();
			resourceManager.LoadResources("/user", resourceManager.GetType().Assembly, resourceManager.GetType().Namespace);
			Assert.Equal(resourceManager.GetType().Namespace + ".resourcetest.haml", resourceManager._loadedResources["user/resourcetest"][0].Name);

			resourceManager._loadedResources.Clear();
			resourceManager.LoadResources("/user/test/", resourceManager.GetType().Assembly, resourceManager.GetType().Namespace);
			Assert.Equal(resourceManager.GetType().Namespace + ".resourcetest.haml", resourceManager._loadedResources["user/test/resourcetest"][0].Name);

			resourceManager._loadedResources.Clear();
			resourceManager.LoadResources("/", resourceManager.GetType().Assembly, resourceManager.GetType().Namespace);
			Assert.Equal(resourceManager.GetType().Namespace + ".resourcetest.haml", resourceManager._loadedResources["resourcetest"][0].Name);
		}

		#endregion

		/// <summary>
		/// Retrieves a stream for the specified resource path if loaded otherwise null
		/// </summary>
		/// <param name="path">Path to the resource to retrieve a stream for</param>
		/// <returns>A stream or null if the resource couldn't be found</returns>
		public Stream GetResourceStream(string path)
		{
			path = path.Replace('\\', '/');
			if(!ContainsResource(path))
				return null;

			string ext;
			ParseName(ref path, out ext);

			List<ResourceInfo> resources = _loadedResources[path];
			ResourceInfo info = resources.Find(delegate(ResourceInfo resInfo) { return resInfo.Extension == ext; });
		
			return info != null ? info.GetStream() : null;
		}

		#region Test for GetResourceStream

		[Fact]
		private static void TestGetResourceStream()
		{
			ResourceManager resources = new ResourceManager(new NullLogWriter());
			resources.LoadResources("/", resources.GetType().Assembly, "HttpServer.Rendering");
			Assert.NotNull(resources.GetResourceStream("resourcetest.haml"));
			Assert.NotNull(resources.GetResourceStream("\\resourcetest.haml"));
		}

		#endregion

		/// <summary>
		/// Fetch all files from the resource that matches the specified arguments.
		/// </summary>
		/// <param name="path">The path to the resource to extract</param>
		/// <returns>
		/// a list of files if found; or an empty array if no files are found.
		/// </returns>
		public string[] GetFiles(string path)
		{
			Check.NotEmpty(path, "path");
			path = path.Replace('\\', '/');

			List<string> files = new List<string>();

			string ext;
			ParseName(ref path, out ext);

			List<ResourceInfo> resources;
			if (!_loadedResources.TryGetValue(path, out resources))
				return new string[] { };

			foreach (ResourceInfo resource in resources)
				if (resource.Extension == ext || ext == "*")
					files.Add(path + "." + resource.Extension);

			return files.ToArray();
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
			Check.NotEmpty(path, "path");
			Check.NotEmpty(filename, "filename");

			path = path.EndsWith("/") ? path : path + "/";
			return GetFiles(path + filename);
		}

		#region Test GetFiles

		[Fact]
		private static void TestGetFiles()
		{
			ResourceManager resourceManager = new ResourceManager(new NullLogWriter());
			resourceManager.LoadResources("/test/", resourceManager.GetType().Assembly, resourceManager.GetType().Namespace);
			string[] files = resourceManager.GetFiles("/test/", "resourcetest.xml");
			Assert.Equal(1, files.Length);
			Assert.Equal("test/resourcetest.xml", files[0]);

			files = resourceManager.GetFiles("/test/", "resourcetest.*");
			Assert.Equal(2, files.Length);

			files = resourceManager.GetFiles("/test/haml/", "resourcetest2.haml");
			Assert.Equal(1, files.Length);

			files = resourceManager.GetFiles("/test/haml/resourcetest2.haml");
			Assert.Equal(1, files.Length);

			files = resourceManager.GetFiles("/test/resourcetest.*");
			Assert.Equal(2, files.Length);
		}

		#endregion

		/// <summary>
		/// Returns whether or not the loader has an instance of the file requested
		/// </summary>
		/// <param name="filename">The name of the template/file</param>
		/// <returns>True if the loader can provide the file</returns>
		public bool ContainsResource(string filename)
		{
			filename = filename.Replace('\\', '/');

			string ext;
			ParseName(ref filename, out ext);

			List<ResourceInfo> resources;
			if (!_loadedResources.TryGetValue(filename, out resources))
				return false;

			foreach (ResourceInfo resource in resources)
				if (resource.Extension == ext || ext == "*")
					return true;

			return false;
		}

		#region Test ContainsResource

		[Fact]
		private static void TestContainsResource()
		{
			ResourceManager resourceManager = new ResourceManager(new NullLogWriter());
			resourceManager.LoadResources("/test/", resourceManager.GetType().Assembly, resourceManager.GetType().Namespace);
			Assert.True(resourceManager.ContainsResource("/test/resourcetest.xml"));
			Assert.True(resourceManager.ContainsResource("/test/resourcetest.haml"));
			Assert.True(resourceManager.ContainsResource("/test/resourcetest.*"));
			Assert.True(resourceManager.ContainsResource("/test/haml/resourcetest2.*"));
			Assert.True(resourceManager.ContainsResource("/test/haml/resourcetest2.haml"));

			Assert.False(resourceManager.ContainsResource("/test/resourcetest"));
			Assert.False(resourceManager.ContainsResource("/test/rwerourcetest.xml"));
			Assert.False(resourceManager.ContainsResource("/test/resourcetest.qaml"));
			Assert.False(resourceManager.ContainsResource("/wrong/rwerourcetest.xml"));
			Assert.False(resourceManager.ContainsResource("/test/haml/resourcetest2.xml"));

			resourceManager._loadedResources.Clear();
			resourceManager.LoadResources("/", resourceManager.GetType().Assembly, resourceManager.GetType().Namespace);
			Assert.True(resourceManager.ContainsResource("/resourcetest.*"));
			Assert.True(resourceManager.ContainsResource("resourcetest.haml"));
		}

		#endregion
	}
}
