#region License

/*
 * Version: MPL 1.1
 *
 * The contents of this file are subject to the Mozilla Public License Version
 * 1.1 (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 *
 * Software distributed under the License is distributed on an "AS IS" basis,
 * WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
 * for the specific language governing rights and limitations under the
 * License.
 *
 * The Original Code is MediaPortal - an Home Cinema PC. The Initial Developer
 * of the Original Code is www.Music-Media-Soft.de.
 *
 * Port(s) to the Original Code made by us and Copyright (C) 2026 are licensed
 * under the Mozilla Public License Version 1.1 or later.
 */

#endregion

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  /// <summary>
  /// Metadata extractor that recognises video items by the type of their resource and assigns the
  /// <see cref="VideoAspect"/>, which is the aspect that marks a <see cref="MediaItem"/> as a video.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This extractor exists because the extractor that normally provides that mark,
  /// <c>VideoMetadataExtractor</c>, opens the container with native libraries (MediaInfo, OpenCV) that
  /// are only built for Windows. As long as no extractor assigns the <see cref="VideoAspect"/>, nothing
  /// in a video share is ever recognised as a video - and that keeps every other video extractor away,
  /// too: the NFO extractors deliberately do nothing for a resource that no earlier extractor has
  /// called a video (see <see cref="NfoVideoMetadataExtractor"/>), so on Linux or Android a movie share
  /// was walked from top to bottom and imported nothing at all.
  /// </para>
  /// <para>
  /// This extractor closes that gap by deciding with the name of the resource alone, which needs no
  /// native code: a file whose extension is a known video container (.mkv, .mp4, .avi, ...) is a video,
  /// and a directory that contains such a file is a video as well.
  /// </para>
  /// <para>
  /// For a file it also notes the resource itself as the item's primary resource, which is what the
  /// library hands back to anyone who later wants to know where a title is or play it. Upstream that
  /// is written by <c>VideoMetadataExtractor</c> together with the stream data, so without it a title
  /// is in the database but has no path in the answer.
  /// </para>
  /// <para>
  /// It stops there on purpose. It reports no attribute values that it has not read from the resource,
  /// so it cannot contradict a real stream analyser running next to it: duration, resolution and codec
  /// stay unknown until an analyser that can read the container does its job. Reading those from
  /// container headers with managed code is this extractor's follow-up task.
  /// </para>
  /// </remarks>
  public class VideoContainerMetadataExtractor : IMetadataExtractor
  {
    #region Constants / Static fields

    /// <summary>
    /// GUID of the <see cref="VideoContainerMetadataExtractor"/>.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "C4F1B0D8-6E2A-4F1B-9C3D-8A7E5F0B6D21";
    public static readonly Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    /// <summary>
    /// MediaCategories this MetadataExtractor is applied to.
    /// </summary>
    /// <remarks>
    /// The video subcategories ("Movie", "Series", ...) are children of the "Video" category and a
    /// share that asks for a subcategory also gets the extractors of its parent categories, so naming
    /// the parent covers every kind of video share.
    /// </remarks>
    private readonly static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();

    /// <summary>
    /// A mime type is only useful to us if it says "video/something".
    /// </summary>
    private const string VIDEO_MIME_TYPE_PREFIX = "video/";

    #endregion

    #region Private fields

    /// <summary>
    /// Metadata of this MetadataExtractor.
    /// </summary>
    private readonly MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    /// <summary>
    /// Initializes <see cref="MEDIA_CATEGORIES"/>.
    /// </summary>
    static VideoContainerMetadataExtractor()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Video);
    }

    /// <summary>
    /// Instantiates a new <see cref="VideoContainerMetadataExtractor"/> object.
    /// </summary>
    public VideoContainerMetadataExtractor()
    {
      // The priority is "Core" for two reasons: the extractors that build on the knowledge that a
      // resource is a video (the NFO ones among them) run at "Extended", so this one has to come
      // first; and it has to be on par with the Windows-only VideoMetadataExtractor, so that a server
      // that has both applies them in the same round and merges their results.
      _metadata = new MetadataExtractorMetadata(
        metadataExtractorId: METADATAEXTRACTOR_ID,
        name: "Video container metadata extractor",
        metadataExtractorPriority: MetadataExtractorPriority.Core,
        processesNonFiles: true,
        shareCategories: MEDIA_CATEGORIES,
        extractedAspectTypes: new MediaItemAspectMetadata[]
        {
          VideoAspect.Metadata,
          ProviderResourceAspect.Metadata
        });
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    /// <summary>
    /// Assigns the <see cref="VideoAspect"/> to the given media item if it is a video file or a
    /// directory that contains one.
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata.</param>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s with the extracted metadata.</param>
    /// <param name="forceQuickMode">Unused; this extractor never accesses the network.</param>
    /// <returns><c>true</c> if the resource was recognised as a video.</returns>
    public Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      var fsra = mediaItemAccessor as IFileSystemResourceAccessor;
      if (fsra == null || !fsra.Exists)
        return Task.FromResult(false);

      if (fsra.IsFile)
      {
        string mimeType;
        if (!IsVideoFile(fsra, out mimeType))
          return Task.FromResult(false);

        // A file that is a video is an item of its own, and its resource is itself. Whoever asks the
        // library for a title afterwards reads this path back to play the film, which is why writing it
        // belongs here: the extractor that decided that this resource is a video is the one that knows
        // which resource it is.
        AddPrimaryResource(extractedAspectData, fsra, mimeType);
      }
      else if (!ContainsVideoFile(fsra))
      {
        return Task.FromResult(false);
      }

      // A directory that holds a video is an item of its own from now on, not a listing of other items.
      MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISVIRTUAL, false);
      // This aspect is what makes a MediaItem a video. We add it without any attribute values - see the
      // remarks on this class.
      MediaItemAspect.GetOrCreateAspect(extractedAspectData, VideoAspect.Metadata);

      ServiceRegistration.Get<ILogger>().Debug("VideoContainerMetadataExtractor: Resource '{0}' is a video", mediaItemAccessor);
      return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public bool IsDirectorySingleResource(IResourceAccessor mediaItemAccessor)
    {
      // A folder of videos stays a folder of videos. Saying "single resource" here would hide the files
      // inside it from the importer, which is what the disc image extractors do for a VIDEO_TS folder.
      return false;
    }

    /// <inheritdoc/>
    public bool IsStubResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    /// <inheritdoc/>
    public bool TryExtractStubItems(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
    {
      return false;
    }

    /// <inheritdoc/>
    public Task<IList<MediaItemSearchResult>> SearchForMatchesAsync(IDictionary<Guid, IList<MediaItemAspect>> searchAspectData, ICollection<string> searchCategories)
    {
      return Task.FromResult((IList<MediaItemSearchResult>)new List<MediaItemSearchResult>());
    }

    /// <inheritdoc/>
    public Task<bool> AddMatchedAspectDetailsAsync(IDictionary<Guid, IList<MediaItemAspect>> matchedAspectData)
    {
      return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<bool> DownloadMetadataAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      return Task.FromResult(false);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Checks whether the given directory contains a video file.
    /// </summary>
    private static bool ContainsVideoFile(IFileSystemResourceAccessor fsra)
    {
      // The importer applies the extractors to a folder before it applies them to the files inside it,
      // so a movie folder has to be recognisable as a video on its own: it is one because the film is
      // in it.
      ICollection<IFileSystemResourceAccessor> children;
      try
      {
        children = fsra.GetFiles();
      }
      catch (Exception e)
      {
        // Listing a directory can fail for reasons that are not our business (a share that vanished
        // mid-import, a permission we do not have). Anything we cannot look into is not a video as far
        // as we can tell.
        ServiceRegistration.Get<ILogger>().Debug("VideoContainerMetadataExtractor: Cannot list resource '{0}': {1}", fsra, e.Message);
        return false;
      }

      try
      {
        foreach (var child in children)
        {
          string childMimeType;
          if (child.IsFile && IsVideoFile(child, out childMimeType))
            return true;
        }
      }
      finally
      {
        // Every accessor GetFiles gave us is ours to dispose, also the ones we did not look into.
        foreach (var child in children)
          child.Dispose();
      }

      return false;
    }

    /// <summary>
    /// Checks whether the given file is a video by the extension of its name, and reports the mime type
    /// that belongs to that extension.
    /// </summary>
    /// <remarks>
    /// The extension is all this extractor looks at on purpose. Looking at the content instead is done
    /// by <see cref="MimeTypeDetector.GetMimeType(string,string)"/>, which asks the Windows system
    /// library urlmon.dll what it makes of the first 256 bytes of the file - an answer this extractor
    /// cannot get on the platforms it was written for. It also opens every file it is asked about,
    /// which for a media library means reading the first bytes of every film on every import run.
    /// </remarks>
    private static bool IsVideoFile(IFileSystemResourceAccessor fsra, out string mimeType)
    {
      mimeType = null;
      // The lookup needs a name, and only a resource that is on a filesystem has one.
      var localFsra = fsra as ILocalFsResourceAccessor;
      if (localFsra == null)
        return false;

      string detected = MimeTypeDetector.GetMimeTypeFromExtension(localFsra.LocalFileSystemPath);
      if (string.IsNullOrEmpty(detected) || !detected.StartsWith(VIDEO_MIME_TYPE_PREFIX, StringComparison.OrdinalIgnoreCase))
        return false;

      mimeType = detected;
      return true;
    }

    /// <summary>
    /// Notes that the resource of the item being extracted is the given file.
    /// </summary>
    private static void AddPrimaryResource(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, IFileSystemResourceAccessor fsra, string mimeType)
    {
      MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(extractedAspectData, ProviderResourceAspect.Metadata);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, mimeType);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, fsra.Size);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, fsra.CanonicalLocalResourcePath.Serialize());
    }

    #endregion
  }
}
