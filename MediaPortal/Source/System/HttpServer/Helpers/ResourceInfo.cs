using System.IO;
using System.Reflection;

namespace HttpServer.Helpers
{
  /// <summary>
  /// Container to bind resource names to assemblies
  /// </summary>
  internal class ResourceInfo
  {
    /// <summary>
    /// Instantiates an instance of <see cref="ResourceInfo"/>
    /// </summary>
    /// <param name="uri">The dot seperated uri the resource maps to</param>
    /// <param name="resourceName">The full resource name</param>
    /// <param name="assembly">The assembly the resource exists in</param>
    public ResourceInfo(string resourceName, string uri, Assembly assembly)
    {
      Check.NotEmpty(resourceName, "resourceName");
      Check.NotEmpty(uri, "uri");
      Check.Require(assembly, "assembly");

      ResourceName = resourceName;
      Assembly = assembly;
      Uri = uri;

      int dotIndex = Uri.LastIndexOf('.');
      if (dotIndex != -1)
      {
        Extension = Uri.Substring(dotIndex + 1);
        ExtensionLessUri = Uri.Substring(0, Uri.Length - Extension.Length - 1);
      }
      else
        Extension = string.Empty;
    }

    /// <summary>
    /// Retrieves the assembly the resource resides in
    /// </summary>
    public Assembly Assembly { get; private set; }

    /// <summary>
    /// Retrieves the full name/path of the assembly
    /// </summary>
    public string Uri { get; private set; }

    /// <summary>
    /// Retrieves the extension of the resource
    /// </summary>
    public string Extension { get; private set; }

    /// <summary>Returns the Uri without extension</summary>
    public string ExtensionLessUri { get; private set; }

    /// <summary>Retrieves the full path name to the resource file</summary>
    public string ResourceName { get; private set; }

    /// <summary>
    /// Retrieves a stream to the resource
    /// </summary>
    /// <returns>Null if the resource couldn't be located somehow</returns>
    public Stream GetStream()
    {
      return Assembly.GetManifestResourceStream(ResourceName);
    }
  }
}