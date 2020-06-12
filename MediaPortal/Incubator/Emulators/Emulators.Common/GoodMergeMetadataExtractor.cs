using Emulators.Common.GoodMerge;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emulators.Common
{
  public class GoodMergeMetadataExtractor : IMetadataExtractor
  {
    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    public static Guid METADATAEXTRACTOR_ID = new Guid("D9A88C00-55EB-4CF3-BD86-2C8F226FF6BA");
    public const string GOODMERGE_CATEGORY_NAME = "GoodMerge";
    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected static MediaCategory _goodmergeCategory;
    protected MetadataExtractorMetadata _metadata;

    static GoodMergeMetadataExtractor()
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(GOODMERGE_CATEGORY_NAME, out _goodmergeCategory))
        _goodmergeCategory = mediaAccessor.RegisterMediaCategory(GOODMERGE_CATEGORY_NAME, new List<MediaCategory> { GameMetadataExtractor.GameMediaCategory });
      MEDIA_CATEGORIES.Add(_goodmergeCategory);
      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(GoodMergeAspect.Metadata);
    }

    public GoodMergeMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "GoodMerge metadata extractor", MetadataExtractorPriority.Extended, true,
          MEDIA_CATEGORIES, new[] { GoodMergeAspect.Metadata });
    }

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        IFileSystemResourceAccessor fsra = mediaItemAccessor as IFileSystemResourceAccessor;
        if (fsra == null || !fsra.IsFile)
          return Task.FromResult(false);

        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
          return Task.FromResult(ExtractGoodMergeData(rah.LocalFsResourceAccessor.LocalFileSystemPath, extractedAspectData));
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        Logger.Info("GoodMergeMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return Task.FromResult(false);
    }

    protected static bool ExtractGoodMergeData(string path, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      List<string> items;
      using (IExtractor extractor = ExtractorFactory.Create(path))
      {
        if (!extractor.IsArchive())
          return false;
        items = extractor.GetArchiveFiles();
      }
      if (items != null && items.Count > 0)
      {
        Logger.Debug("GoodMergeMetadataExtractor: Found {0} items in archive '{1}'", items.Count, path);
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, GoodMergeAspect.ATTR_GOODMERGE_ITEMS, items);
        return true;
      }
      else
      {
        Logger.Warn("GoodMergeMetadataExtractor: File '{0}' is empty or not a valid archive", path);
      }
      return false;
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

    public Task<IList<MediaItemSearchResult>> SearchForMatchesAsync(IDictionary<Guid, IList<MediaItemAspect>> searchAspectData, ICollection<string> searchCategories)
    {
      return Task.FromResult<IList<MediaItemSearchResult>>(null);
    }

    public Task<bool> AddMatchedAspectDetailsAsync(IDictionary<Guid, IList<MediaItemAspect>> matchedAspectData)
    {
      return Task.FromResult(false);
    }

    public Task<bool> DownloadMetadataAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      return Task.FromResult(false);
    }
  }
}
