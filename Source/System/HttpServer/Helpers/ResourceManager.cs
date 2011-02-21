using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace HttpServer.Helpers
{
  /// <summary>Class to handle loading of resource files</summary>
  public class ResourceManager
  {
    private readonly ILogWriter _logger;

    private readonly Dictionary<string, ResourceInfo> _resources = new Dictionary<string, ResourceInfo>();

    private readonly Dictionary<string, List<ResourceInfo>> _mappedResources =
        new Dictionary<string, List<ResourceInfo>>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceManager"/> class.
    /// </summary>
    public ResourceManager()
    {
      _logger = NullLogWriter.Instance;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceManager"/> class.
    /// </summary>
    /// <param name="writer">logger.</param>
    public ResourceManager(ILogWriter writer)
    {
      _logger = writer ?? NullLogWriter.Instance;
    }

    /// <summary>
    /// Loads resources from a namespace in the given assembly to an URI
    /// </summary>
    /// <param name="toUri">The URI to map the resources to</param>
    /// <param name="fromAssembly">The assembly in which the resources reside</param>
    /// <param name="fromNamespace">The namespace from which to load the resources</param>
    /// <usage>
    /// <code>
    /// resourceLoader.LoadResources("/user/", typeof(User).Assembly, "MyLib.Models.User.Views");
    /// </code>
    /// Will make the resource MyLib.Models.User.Views.list.Haml accessible via /user/list.haml or /user/list/
    /// </usage>
    /// <returns>The amount of loaded files, giving you the possibility of making sure the resources needed gets loaded</returns>
    /// <exception cref="InvalidOperationException">If a resource has already been mapped to an uri</exception>
    public int LoadResources(string toUri, Assembly fromAssembly, string fromNamespace)
    {
      // We'll save all uri's point seperated to be able to retrieve files with dots in 'em (like /user/oblata.exe which would be mapped to user.oblata.exe)
      toUri = toUri.ToLower().Replace('/', '.').Trim('.');
      if (!string.IsNullOrEmpty(toUri))
        toUri += ".";

      fromNamespace = fromNamespace.ToLower();
      if (!fromNamespace.EndsWith("."))
        fromNamespace += ".";

      int added = 0;
      foreach (string resourceName in fromAssembly.GetManifestResourceNames())
      {
        string name = resourceName.ToLower();
        if (!name.StartsWith(fromNamespace))
          continue;

        // Create a search name for the resource file, aka just strip the from namespace from resource path and add uri
        string uri = toUri + name.Substring(fromNamespace.Length);
        ResourceInfo info = new ResourceInfo(resourceName, uri, fromAssembly);

        // If a resource has previously been added to the exact url we need to throw an exception
        if (_resources.ContainsKey(info.Uri))
          throw new InvalidOperationException("A resource has already been mapped to the uri: " + info.Uri);

        List<ResourceInfo> mapped;
        if (!_mappedResources.TryGetValue(info.ExtensionLessUri + ".*", out mapped))
          _mappedResources.Add(info.ExtensionLessUri + ".*", new List<ResourceInfo> {info});
        else
          mapped.Add(info);

        _resources.Add(info.Uri, info);
        added++;

        _logger.Write(this, LogPrio.Info, "Resource '" + info.ResourceName + "' loaded to uri: " + uri);
      }

      return added;
    }

    private string FormatPath(string path)
    {
      return path.ToLower().Replace('/', '.').Replace('\\', '.').Trim('.');
    }

    /// <summary>
    /// Retrieves a stream for the specified resource path if loaded otherwise null
    /// </summary>
    /// <param name="path">Path to the resource to retrieve a stream for</param>
    /// <returns>A stream or null if the resource couldn't be found</returns>
    public Stream GetResourceStream(string path)
    {
      ResourceInfo info;
      if (!_resources.TryGetValue(FormatPath(path), out info))
        return null;

      return info.GetStream();
    }

    /// <summary>
    /// Fetch all files from the resource that matches the specified arguments.
    /// </summary>
    /// <param name="path">The path to the resource to extract</param>
    /// <returns>
    /// a list of files if found; or an empty array if no files are found.
    /// </returns>
    /// <exception cref="ArgumentException">Search path must end with an asterisk for finding arbitrary files</exception>
    public string[] GetFiles(string path)
    {
      Check.NotEmpty(path, "path");
      if (!path.EndsWith(".*"))
      {
        ResourceInfo info;
        return _resources.TryGetValue(FormatPath(path), out info) ? new[] {path} : new string[] {};
      }

      List<ResourceInfo> resources;
      if (!_mappedResources.TryGetValue(FormatPath(path), out resources))
        return new string[] {};

      path = path.TrimEnd('*');
      List<string> files = new List<string>();
      foreach (ResourceInfo info in resources)
        files.Add(path + info.Extension);

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

      path = path.EndsWith(".") ? path : (path + ".");
      return GetFiles(path + filename);
    }

    /// <summary>
    /// Returns whether or not the loader has an instance of the file requested
    /// </summary>
    /// <param name="filename">The name of the template/file</param>
    /// <returns>True if the loader can provide the file</returns>
    public bool ContainsResource(string filename)
    {
      filename = FormatPath(filename);
      if (filename.EndsWith("*"))
        return _mappedResources.ContainsKey(filename);

      return _resources.ContainsKey(filename);
    }
  }
}