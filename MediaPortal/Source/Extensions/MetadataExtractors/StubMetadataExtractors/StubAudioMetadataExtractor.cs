#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MetadataExtractors.StubMetadataExtractors.Settings;
using MediaPortal.Extensions.MetadataExtractors.StubMetadataExtractors.StubReaders;
using MediaPortal.Extensions.MetadataExtractors.StubMetadataExtractors.Stubs;
using MediaPortal.Common.MediaManagement.Helpers;

namespace MediaPortal.Extensions.MetadataExtractors.StubMetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor for CD reading from local stub-files.
  /// </summary>
  public class StubAudioMetadataExtractor : IMetadataExtractor, IDisposable
  {
    #region Constants / Static fields

    /// <summary>
    /// GUID of the StubMetadataExtractors plugin
    /// </summary>
    public const string PLUGIN_ID_STR = "A33319F7-D311-44A9-BE7E-2F0E88AC4EEF";
    public static readonly Guid PLUGIN_ID = new Guid(PLUGIN_ID_STR);

    /// <summary>
    /// GUID for the StubAudioMetadataExtractor
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "687CE3E9-73C4-4464-9B1A-BC0905D18E1B";
    public static readonly Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    /// <summary>
    /// MediaCategories this MetadataExtractor is applied to
    /// </summary>
    private const string MEDIA_CATEGORY_NAME_AUDIO = "Audio";
    private readonly static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();

    #endregion

    #region Private fields

    /// <summary>
    /// Metadata of this MetadataExtractor
    /// </summary>
    private readonly MetadataExtractorMetadata _metadata;

    /// <summary>
    /// Settings of the <see cref="StubAudioMetadataExtractor"/>
    /// </summary>
    private readonly StubAudioMetadataExtractorSettings _settings;
    
    /// <summary>
    /// Debug logger
    /// </summary>
    /// <remarks>
    /// NoLogger if _settings.EnableDebugLogging == <c>false</c>"/>
    /// FileLogger if _settings.EnableDebugLogging == <c>true</c>"/>
    /// </remarks>
    private readonly ILogger _debugLogger;

    /// <summary>
    /// Unique number of the last MediaItem for which this MetadataExtractor was called
    /// </summary>
    private long _lastMediaItemNumber = 1;

    #endregion

    #region Ctor

    /// <summary>
    /// Initializes <see cref="MEDIA_CATEGORIES"/> and, if necessary, registers the "Movie" <see cref="MediaCategory"/>
    /// </summary>
    static StubAudioMetadataExtractor()
    {
      MediaCategory audioCategory;
      var mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_AUDIO, out audioCategory))
        audioCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_AUDIO, new List<MediaCategory> { DefaultMediaCategories.Audio });
      MEDIA_CATEGORIES.Add(audioCategory);
    }

    /// <summary>
    /// Instantiates a new <see cref="NfoMovieMetadataExtractor"/> object
    /// </summary>
    public StubAudioMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(
        metadataExtractorId: METADATAEXTRACTOR_ID,
        name: "Stub audio metadata extractor",
        metadataExtractorPriority: MetadataExtractorPriority.Stub,
        processesNonFiles: true,
        shareCategories: MEDIA_CATEGORIES,
        extractedAspectTypes: new MediaItemAspectMetadata[]
        {
          MediaAspect.Metadata,
          AudioAspect.Metadata,
          StubAspect.Metadata
        });

      _settings = ServiceRegistration.Get<ISettingsManager>().Load<StubAudioMetadataExtractorSettings>();

      if (_settings.EnableDebugLogging)
      {
        _debugLogger = FileLogger.CreateFileLogger(ServiceRegistration.Get<IPathManager>().GetPath(@"<LOG>\StubAudioMetadataExtractorDebug.log"), LogLevel.Debug, false, true);
        LogSettings();
      }
      else
        _debugLogger = new NoLogger();
    }

    #endregion

    #region Logging helpers

    /// <summary>
    /// Logs version and setting information into <see cref="_debugLogger"/>
    /// </summary>
    private void LogSettings()
    {
      _debugLogger.Info("-------------------------------------------------------------");
      _debugLogger.Info("StubAudioMetadataExtractor v{0} instantiated", ServiceRegistration.Get<IPluginManager>().AvailablePlugins[PLUGIN_ID].Metadata.PluginVersion);
      _debugLogger.Info("Setttings:");
      _debugLogger.Info("   EnableDebugLogging: {0}", _settings.EnableDebugLogging);
      _debugLogger.Info("   WriteRawNfoFileIntoDebugLog: {0}", _settings.WriteRawStubFileIntoDebugLog);
      _debugLogger.Info("   WriteStubObjectIntoDebugLog: {0}", _settings.WriteStubObjectIntoDebugLog);
      _debugLogger.Info("   AudioCdStubFileExtensions: {0}", String.Join(" ", _settings.AudioCdStubFileExtensions));
      _debugLogger.Info("   SeparatorCharacters: {0}", String.Join(" ", _settings.SeparatorCharacters));
      _debugLogger.Info("   IgnoreStrings: {0}", String.Join(";", _settings.IgnoreStrings));
      _debugLogger.Info("-------------------------------------------------------------");
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      return Task.FromResult(false);
    }

    public bool IsDirectorySingleResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool IsStubResource(IResourceAccessor mediaItemAccessor)
    {
      if (_settings.AudioCdStubFileExtensions.Where(e => mediaItemAccessor.Path.ToString().EndsWith("." + e, StringComparison.InvariantCultureIgnoreCase)).Any())
      {
        return true;
      }
      return false;
    }

    public bool TryExtractStubItems(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
    {
      // The following is bad practice as it wastes one ThreadPool thread.
      // ToDo: Once the IMetadataExtractor interface is updated to support async operations, call TryExtractMetadataAsync directly
      return TryExtractStubItemsAsync(mediaItemAccessor, extractedStubAspectData).Result;
    }

    private async Task<bool> TryExtractStubItemsAsync(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
    {
      // Get a unique number for this call to TryExtractMetadataAsync. We use this to make reading the debug log easier.
      // This MetadataExtractor is called in parallel for multiple MediaItems so that the respective debug log entries
      // for one call are not contained one after another in debug log. We therefore prepend this number before every log entry.
      var miNumber = Interlocked.Increment(ref _lastMediaItemNumber);
      try
      {
        _debugLogger.Info("[#{0}]: Start extracting stubs for resource '{1}'", miNumber, mediaItemAccessor);

        if (!IsStubResource(mediaItemAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract stubs; file does not have a supported extension", miNumber);
          return false;
        }

        // This MetadataExtractor only works for MediaItems accessible by an IFileSystemResourceAccessor.
        // Otherwise it is not possible to find a stub-file in the MediaItem's directory.
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract stubs; mediaItemAccessor is not an IFileSystemResourceAccessor", miNumber);
          return false;
        }

        var fsra = mediaItemAccessor as IFileSystemResourceAccessor;
        var albumStubReader = new StubAlbumReader(_debugLogger, miNumber, true, _settings);
        if (fsra != null && await albumStubReader.TryReadMetadataAsync(fsra).ConfigureAwait(false))
        {
          AlbumStub album = albumStubReader.GetAlbumStubs().FirstOrDefault();
          if (album != null && album.Tracks != null && album.Tracks > 0)
          {
            for (int trackNo = 1; trackNo <= album.Tracks.Value; trackNo++)
            {
              Dictionary<Guid, IList<MediaItemAspect>> extractedAspectData = new Dictionary<Guid, IList<MediaItemAspect>>();
              string title = string.Format("{0}: {1}", album.Title, "Track " + trackNo);

              MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(extractedAspectData, ProviderResourceAspect.Metadata);
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_STUB);
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, fsra.CanonicalLocalResourcePath.Serialize());
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "audio/L16");

              SingleMediaItemAspect audioAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, AudioAspect.Metadata);
              audioAspect.SetAttribute(AudioAspect.ATTR_ISCD, true);
              audioAspect.SetAttribute(AudioAspect.ATTR_TRACK, trackNo);
              audioAspect.SetAttribute(AudioAspect.ATTR_TRACKNAME, title);
              audioAspect.SetAttribute(AudioAspect.ATTR_ENCODING, "PCM");
              if (album.Cd.HasValue)
                audioAspect.SetAttribute(AudioAspect.ATTR_DISCID, album.Cd.Value);
              audioAspect.SetAttribute(AudioAspect.ATTR_BITRATE, 1411); // 44.1 kHz * 16 bit * 2 channel
              audioAspect.SetAttribute(AudioAspect.ATTR_CHANNELS, 2);
              audioAspect.SetAttribute(AudioAspect.ATTR_NUMTRACKS, album.Tracks.Value);
              audioAspect.SetAttribute(AudioAspect.ATTR_ALBUM, album.Title);
              if (album.Artists.Count > 0)
                audioAspect.SetCollectionAttribute(AudioAspect.ATTR_ALBUMARTISTS, album.Artists);

              SingleMediaItemAspect stubAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, StubAspect.Metadata);
              stubAspect.SetAttribute(StubAspect.ATTR_DISC_NAME, album.DiscName);
              stubAspect.SetAttribute(StubAspect.ATTR_MESSAGE, album.Message);

              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, title);
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_SORT_TITLE, BaseInfo.GetSortTitle(title));
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISVIRTUAL, false);
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISSTUB, true);
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, fsra.LastChanged);

              extractedStubAspectData.Add(extractedAspectData);
            }
          }
        }
        else
          _debugLogger.Warn("[#{0}]: No valid metadata found in album stub file", miNumber);


        _debugLogger.Info("[#{0}]: Successfully finished extracting stubs", miNumber);
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("StubAudioMetadataExtractor: Exception while extracting stubs for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
        _debugLogger.Error("[#{0}]: Exception while extracting stubs", e, miNumber);
        return false;
      }
    }

    public Task<IList<MediaItemSearchResult>> SearchForMatchesAsync(IDictionary<Guid, IList<MediaItemAspect>> searchAspectData, ICollection<string> searchCategories)
    {
      return Task.FromResult<IList<MediaItemSearchResult>>(null);
    }

    public Task<bool> AddMatchedAspectDetailsAsync(IDictionary<Guid, IList<MediaItemAspect>> matchedAspectData)
    {
      return Task.FromResult(false);
    }

    #endregion
  }
}
