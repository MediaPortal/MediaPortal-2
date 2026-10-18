using System;

namespace MediaPortal.Frontend.Player
{
  /// <summary>
  /// What to play, expressed as something this assembly can hand to an engine without knowing what the engine
  /// will do with it.
  /// </summary>
  /// <remarks>
  /// It is a value type with two ways to exist - <see cref="FromFile"/> and <see cref="FromUrl"/> - because the
  /// difference matters on every platform and nowhere more than on Android, where a path the process is not
  /// allowed to open is the normal case rather than the exception. An engine that is given a URL does not get to
  /// decide to treat it as a file, and an engine that is given a path does not get to hand it to a network
  /// stack.
  /// <para>
  /// Nothing here touches the file system. Whether the file exists is the engine's question to answer (and the
  /// engine's error to report), because by the time a player opens it the answer may have changed, and because
  /// a check here would be a second, different definition of "playable" to keep in sync.
  /// </para>
  /// </remarks>
  public readonly struct MediaLocation : IEquatable<MediaLocation>
  {
    private readonly string _value;

    private MediaLocation(string value, bool isFile)
    {
      _value = value;
      IsFile = isFile;
    }

    /// <summary>True when this is a local file, false when it is a URL. Never "maybe".</summary>
    public bool IsFile { get; }

    /// <summary>
    /// A path on the machine this process runs on. Relative paths are refused: a player started by a service
    /// has a working directory nobody meant it to inherit, and "relative to whatever" is not what a media
    /// database stores.
    /// </summary>
    public static MediaLocation FromFile(string path)
    {
      if (string.IsNullOrWhiteSpace(path))
        throw new ArgumentException("An empty path is not a media file.", nameof(path));
      if (!System.IO.Path.IsPathFullyQualified(path))
        throw new ArgumentException($"The media path must be absolute, got '{path}'.", nameof(path));
      return new MediaLocation(path, true);
    }

    /// <summary>
    /// Something to stream. Only the schemes a player can actually play are accepted, so that a typo arrives as
    /// an <see cref="ArgumentException"/> here rather than as an unexplained failure three layers down.
    /// </summary>
    public static MediaLocation FromUrl(string url)
    {
      if (string.IsNullOrWhiteSpace(url))
        throw new ArgumentException("An empty URL is not a media stream.", nameof(url));
      if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? parsed))
        throw new ArgumentException($"'{url}' is not an absolute URL.", nameof(url));
      return FromUrl(parsed);
    }

    /// <summary>As <see cref="FromUrl(string)"/>, from an already-parsed URI.</summary>
    public static MediaLocation FromUrl(Uri uri)
    {
      if (uri == null)
        throw new ArgumentNullException(nameof(uri));
      switch (uri.Scheme)
      {
        case "http":
        case "https":
        case "rtsp":
        case "rtp":
        case "udp":
        case "smb":
        case "sftp":
        case "ftp":
          return new MediaLocation(uri.AbsoluteUri, false);
        case "file":
          // Accepted, but turned back into a path: two representations of "a file on this machine" would mean
          // two things a screen has to compare.
          return new MediaLocation(uri.LocalPath, true);
        default:
          throw new ArgumentException($"Cannot play a '{uri.Scheme}:' address.", nameof(uri));
      }
    }

    /// <summary>
    /// The string an engine is given: a path for files, the full URL for streams.
    /// </summary>
    public string EngineString => _value ?? string.Empty;

    /// <summary>
    /// Safe to write in a log or show on a screen: a URL's credentials are removed. The token endpoint taught
    /// the same lesson on the server side - what a request looks like in a log is a security property.
    /// </summary>
    public string Describe()
    {
      if (IsFile)
        return _value ?? string.Empty;
      try
      {
        Uri uri = new Uri(_value!);
        if (string.IsNullOrEmpty(uri.UserInfo))
          return uri.AbsoluteUri;
        UriBuilder builder = new UriBuilder(uri) { UserName = string.Empty, Password = string.Empty };
        return builder.Uri.AbsoluteUri + " (credentials hidden)";
      }
      catch (UriFormatException)
      {
        return "(unreadable address)";
      }
    }

    public bool Equals(MediaLocation other)
    {
      return IsFile == other.IsFile && string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
      return obj is MediaLocation other && Equals(other);
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(_value, IsFile);
    }

    /// <summary>Same as <see cref="Describe"/> - there is no version of this that is safe to print raw.</summary>
    public override string ToString()
    {
      return Describe();
    }
  }
}
