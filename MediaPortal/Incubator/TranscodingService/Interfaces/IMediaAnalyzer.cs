using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Plugins.Transcoding.Service.Interfaces
{
  /// <summary>
  /// This Interface should only be used from within MetadataExtractors.
  /// </summary>
  public interface IMediaAnalyzer
  {

    /// <summary>
    /// Sets the timeout after which the Thread gets stopped
    /// </summary>
    int TranscoderTimeout { get; set; }

    /// <summary>
    /// The maximum number of threads to use
    /// </summary>
    int TranscoderMaximumThreads { get; set; }

    /// <summary>
    /// Sets or gets the default encoding for the Subtitle
    /// </summary>
    string SubtitleDefaultEncoding { get; set; }

    /// <summary>
    /// Sets or gets the default language to use for Subtitles
    /// </summary>
    string SubtitleDefaultLanguage { get; set; }
    ILogger Logger { get; set; }
    
    /// <summary>
    /// Pareses a local file using FFProbe and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="lfsra">ILocalFsResourceAccessor to the file</param>
    /// <returns>a Metadata Container with all information about the mediaitem</returns>
    MetadataContainer ParseFile(ILocalFsResourceAccessor lfsra);

    /// <summary>
    /// Pareses a URL using FFProbe and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="streamLink">INetworkResourceAccessor to the file</param>
    /// <returns>a Metadata Container with all information about the URL</returns>
    MetadataContainer ParseStream(INetworkResourceAccessor streamLink);

    /// <summary>
    /// Gets information about the used external subtitle file
    /// </summary>
    /// <param name="info"></param>
    void ParseSubtitleFiles(ref MetadataContainer info);
  }
}
