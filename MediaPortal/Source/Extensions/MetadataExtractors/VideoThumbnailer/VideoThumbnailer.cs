#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using OpenCvSharp;

namespace MediaPortal.Extensions.MetadataExtractors.VideoThumbnailer
{
  /// <summary>
  /// MediaPortal 2 metadata extractor to exctract thumbnails from videos.
  /// </summary>
  public class VideoThumbnailer : IMetadataExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the VideoThumbnailer metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "FB0AA0ED-97B2-4721-BE74-AC67E77A17B2";

    /// <summary>
    /// Video metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    #region Protected fields and classes

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static VideoThumbnailer()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Video);
    }

    public VideoThumbnailer()
    {
      // Creating thumbs with this MetadataExtractor takes much longer than downloading them from the internet.
      // This MetadataExtractor only creates thumbs if the ThumbnailLargeAspect has not been filled before.
      // ToDo: Correct this once we have a better priority system
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Video thumbnail extractor", MetadataExtractorPriority.FallBack, true,
          MEDIA_CATEGORIES, new[]
              {
                ThumbnailLargeAspect.Metadata
              });
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public async Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        if (forceQuickMode)
          return false;

        byte[] thumb;
        // We only want to create missing thumbnails here, so check for existing ones first
        if (MediaItemAspect.TryGetAttribute(extractedAspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out thumb) && thumb != null)
          return false;

        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
          return false;
        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
          return await ExtractThumbnailAsync(rah.LocalFsResourceAccessor, extractedAspectData).ConfigureAwait(false);
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Error("VideoThumbnailer: Exception reading resource '{0}' (Text: '{1}')", e, mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    private Task<bool> ExtractThumbnailAsync(ILocalFsResourceAccessor lfsra, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      // We can only work on files and make sure this file was detected by a lower MDE before (title is set then).
      // VideoAspect must be present to be sure it is actually a video resource.
      if (!lfsra.IsFile || !extractedAspectData.ContainsKey(VideoStreamAspect.ASPECT_ID))
        return Task.FromResult(false);

      //ServiceRegistration.Get<ILogger>().Info("VideoThumbnailer: Evaluate {0}", lfsra.ResourceName);

      bool isPrimaryResource = false;
      IList<MultipleMediaItemAspect> resourceAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspectData, ProviderResourceAspect.Metadata, out resourceAspects))
      {
        foreach (MultipleMediaItemAspect pra in resourceAspects)
        {
          string accessorPath = (string)pra.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
          ResourcePath resourcePath = ResourcePath.Deserialize(accessorPath);
          if (resourcePath.Equals(lfsra.CanonicalLocalResourcePath))
          {
            if (pra.GetAttributeValue<int?>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_PRIMARY)
            {
              isPrimaryResource = true;
              break;
            }
          }
        }
      }

      if (!isPrimaryResource) //Ignore subtitles
        return Task.FromResult(false);

      // Check for a reasonable time offset
      int defaultVideoOffset = 720;
      long videoDuration;
      double downscale = 2; // Reduces the video frame size to a half of original
      IList<MultipleMediaItemAspect> videoAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspectData, VideoStreamAspect.Metadata, out videoAspects))
      {
        if ((videoDuration = videoAspects[0].GetAttributeValue<long>(VideoStreamAspect.ATTR_DURATION)) > 0)
        {
          if (defaultVideoOffset > videoDuration * 1 / 3)
            defaultVideoOffset = Convert.ToInt32(videoDuration * 1 / 3);
        }

        double width = videoAspects[0].GetAttributeValue<int>(VideoStreamAspect.ATTR_WIDTH);
        double height = videoAspects[0].GetAttributeValue<int>(VideoStreamAspect.ATTR_HEIGHT);
        downscale = width / 256.0; //256 is max size of large thumbnail aspect
      }

      using (lfsra.EnsureLocalFileSystemAccess())
      {
        VideoCapture capture = new VideoCapture();
        capture.Open(lfsra.LocalFileSystemPath);
        capture.PosMsec = defaultVideoOffset * 1000;
        var mat = capture.RetrieveMat();
        if (mat.Height > 0 && mat.Width > 0)
        {
          double width = mat.Width;
          double height = mat.Height;
          mat = mat.Resize(new OpenCvSharp.Size(width / downscale, height / downscale));
          var binary = mat.ToBytes();
          MediaItemAspect.SetAttribute(extractedAspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, binary);
          ServiceRegistration.Get<ILogger>().Info("VideoThumbnailer: Successfully created thumbnail for resource '{0}'", lfsra.LocalFileSystemPath);
        }
        else
        {
          // Calling EnsureLocalFileSystemAccess not necessary; only string operation
          ServiceRegistration.Get<ILogger>().Warn("VideoThumbnailer: Failed to create thumbnail for resource '{0}'", lfsra.LocalFileSystemPath);
        }
      }
      return Task.FromResult(true);
    }

    public bool IsDirectorySingleResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool IsStubResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool TryExtractStubItems(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
    {
      return false;
    }

    #endregion
  }
}
