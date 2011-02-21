using System.IO;
using System.Reflection;

namespace HttpServer.Rendering
{
	/// <summary>
	/// Container to bind resource names to assemblies
	/// </summary>
	internal class ResourceInfo
	{
		private readonly string _resourceExtension;
		private readonly Assembly _assembly;
		private readonly string _resourceName;

		/// <summary>
		/// Instantiates an instance of <see cref="ResourceInfo"/>
		/// </summary>
		/// <param name="fullname">The full name/path of the resource</param>
		/// <param name="assembly">The assembly the resource exists in</param>
		public ResourceInfo(string fullname, Assembly assembly)
		{
			_resourceName = fullname;
			_assembly = assembly;

			int dotIndex = fullname.LastIndexOf('.');
			if (dotIndex != -1)
				_resourceExtension = _resourceName.Substring(dotIndex + 1);
			else
				_resourceExtension = string.Empty;
		}

		/// <summary>
		/// Retrieves the assembly the resource resides in
		/// </summary>
		public Assembly Assembly
		{
			get { return _assembly; }
		}

		/// <summary>
		/// Retrieves the full name/path of the assembly
		/// </summary>
		public string Name
		{
			get { return _resourceName; }
		}

		/// <summary>
		/// Retrieves the extension of the resource
		/// </summary>
		public string Extension
		{
			get { return _resourceExtension; }
		}

		/// <summary>
		/// Retrieves a stream to the resouce
		/// </summary>
		/// <returns>Null if the resource couldn't be located somehow</returns>
		public Stream GetStream()
		{
			return _assembly.GetManifestResourceStream(Name);
		}
	}
}
