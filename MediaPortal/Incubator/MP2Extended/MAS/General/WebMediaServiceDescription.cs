using System.Collections.Generic;

namespace MediaPortal.Plugins.MP2Extended.MAS.General
{
  public class WebMediaServiceDescription
  {
    public int ApiVersion { get; set; }
    public string ServiceVersion { get; set; }

    public List<WebBackendProvider> AvailableMovieLibraries { get; set; }
    public List<WebBackendProvider> AvailableMusicLibraries { get; set; }
    public List<WebBackendProvider> AvailablePictureLibraries { get; set; }
    public List<WebBackendProvider> AvailableTvShowLibraries { get; set; }
    public List<WebBackendProvider> AvailableFileSystemLibraries { get; set; }

    public int DefaultMovieLibrary { get; set; }
    public int DefaultMusicLibrary { get; set; }
    public int DefaultPictureLibrary { get; set; }
    public int DefaultTvShowLibrary { get; set; }
    public int DefaultFileSystemLibrary { get; set; }
  }
}