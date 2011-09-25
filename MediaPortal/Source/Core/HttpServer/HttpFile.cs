using System;
using System.IO;

namespace HttpServer
{
  /// <summary>
  /// Container class for posted files
  /// </summary>
  public class HttpFile : IDisposable
  {
    private string _name;
    private string _filename;
    private string _uploadFilename;
    private string _contentType;
    private bool _disposed;

    /// <summary>
    /// Creates a container for a posted file
    /// </summary>
    /// <param name="name">The identifier of the post field</param>
    /// <param name="filename">The file path</param>
    /// <param name="contentType">The content type of the file</param>
    /// <param name="uploadFilename">The name of the file uploaded</param>
    /// <exception cref="ArgumentNullException">If any parameter is null or empty</exception>
    public HttpFile(string name, string filename, string contentType, string uploadFilename)
    {
      if (string.IsNullOrEmpty(name))
        throw new ArgumentNullException("name");
      if (string.IsNullOrEmpty(filename))
        throw new ArgumentNullException("filename");
      if (string.IsNullOrEmpty(contentType))
        throw new ArgumentNullException("contentType");
      if (string.IsNullOrEmpty(uploadFilename))
        throw new ArgumentNullException("uploadFilename");

      _name = name;
      _filename = filename;
      _contentType = contentType;
      _uploadFilename = uploadFilename;
    }

    /// <summary>
    /// Creates a container for a posted file <see cref="HttpFile(string, string, string, string)"/>
    /// </summary>
    /// <exception cref="ArgumentNullException">If any parameter is null or empty</exception>
    public HttpFile(string name, string filename, string contentType) : this(name, filename, contentType, "Undefined")
    {
    }

    /// <summary>Destructor disposing the file</summary>
    ~HttpFile()
    {
      Dispose(false);
    }

    /// <summary>
    /// The name/id of the file
    /// </summary>
    public string Name
    {
      get
      {
        AssertDisposed();
        return _name;
      }
      set { _name = value; }
    }

    /// <summary>
    /// The full file path
    /// </summary>
    public string Filename
    {
      get
      {
        AssertDisposed();
        return _filename;
      }
      set { _filename = value; }
    }

    /// <summary>
    /// The name of the uploaded file
    /// </summary>
    public string UploadFilename
    {
      get { return _uploadFilename; }
      set { _uploadFilename = value; }
    }

    /// <summary>
    /// The type of file
    /// </summary>
    public string ContentType
    {
      get
      {
        AssertDisposed();
        return _contentType;
      }
      set { _contentType = value; }
    }

    private void AssertDisposed()
    {
      if (_disposed)
        throw new ObjectDisposedException("Object has been disposed");
    }

    /// <summary>
    /// Deletes the temporary file
    /// </summary>
    /// <param name="disposing">True if manual dispose</param>
    protected void Dispose(bool disposing)
    {
      if (File.Exists(_filename))
      {
        try
        {
          File.Delete(_filename);
        }
        catch (Exception err)
        {
          // todo, write logging code
          throw new Exception("Error deleting temporary file!", err);
        }
      }

      _disposed = true;
    }

    /// <summary>
    /// Disposing interface, cleans up managed resources (the temporary file) and suppresses finalization
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
  }
}