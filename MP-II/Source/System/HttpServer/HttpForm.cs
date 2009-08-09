using System;
using System.Collections.Generic;

namespace HttpServer
{
  /// <summary>Container for posted form data</summary>
  public class HttpForm : HttpInput
  {
    /// <summary>Instance to help mark a non-initialized form</summary>
    public static readonly HttpForm EmptyForm = new HttpForm("EmptyForm", true);

    private readonly Dictionary<string, HttpFile> _files = new Dictionary<string, HttpFile>();

    /// <summary>Initializes a form container with the specified name</summary>
    public HttpForm() : base("form")
    {
    }

    /// <summary>
    /// Makes a deep copy of the input
    /// </summary>
    /// <param name="input">The input to copy</param>
    public HttpForm(HttpInput input) : base(input)
    {
    }

    private HttpForm(string name, bool ignoreChanges) : base(name, ignoreChanges)
    {
    }

    /// <summary>
    /// Adds a file to the collection of posted files
    /// </summary>
    /// <param name="file">The file to add</param>
    /// <exception cref="ArgumentException">If the file is already added</exception>
    /// <exception cref="ArgumentNullException">If file is null</exception>
    /// <exception cref="InvalidOperationException">If the instance is HttpForm.EmptyForm which cannot be modified</exception>
    public void AddFile(HttpFile file)
    {
      if (_ignoreChanges)
        throw new InvalidOperationException("Cannot add files to instance HttpForm.EmptyForm.");
      if (file == null)
        throw new ArgumentNullException("file");

      if (_files.ContainsKey(file.Name))
        throw new ArgumentException("File named '" + file.Name + "' already exists!");

      _files.Add(file.Name, file);
    }

    /// <summary>
    /// Checks if the form contains a specified file
    /// </summary>
    /// <param name="name">Field name of the file parameter</param>
    /// <returns>True if the file exists</returns>
    /// <exception cref="InvalidOperationException">If the instance is HttpForm.EmptyForm which cannot be modified</exception>
    public bool ContainsFile(string name)
    {
      if (_ignoreChanges)
        throw new InvalidOperationException("Cannot request information from instance HttpForm.EmptyForm.");
      if (string.IsNullOrEmpty(name))
        throw new ArgumentNullException("name");

      return _files.ContainsKey(name);
    }

    /// <summary>
    /// Retrieves a file held by by the form
    /// </summary>
    /// <param name="name">The identifier of the file</param>
    /// <returns>The requested file or null if the file was not found</returns>
    /// <exception cref="ArgumentNullException">If name is null or empty</exception>
    /// <exception cref="InvalidOperationException">If the instance is HttpForm.EmptyForm which cannot be modified</exception>
    public HttpFile GetFile(string name)
    {
      if (_ignoreChanges)
        throw new InvalidOperationException("Cannot retrieve files from instance HttpForm.EmptyForm.");
      if (string.IsNullOrEmpty(name))
        throw new ArgumentNullException("name");

      if (!_files.ContainsKey(name))
        return null;

      return _files[name];
    }

    /// <summary>
    /// Retrieves the number of files added to the <see cref="HttpForm"/>
    /// </summary>
    /// <returns>0 if no files are added</returns>
    public IList<HttpFile> Files
    {
      get
      {
        if (_files.Count == 0)
          return EmptyFileCollection;

        List<HttpFile> files = new List<HttpFile>();
        foreach (HttpFile file in _files.Values)
          files.Add(file);
        return files.AsReadOnly();
      }
    }

    private static readonly IList<HttpFile> EmptyFileCollection = new List<HttpFile>().AsReadOnly();

    /// <summary>Disposes all held HttpFile's and resets values</summary>
    public override void Clear()
    {
      base.Clear();

      foreach (KeyValuePair<string, HttpFile> pair in _files)
        pair.Value.Dispose();

      _files.Clear();
    }
  }
}