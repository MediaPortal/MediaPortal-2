using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;

namespace Emulators.Common.Games
{
  public class GameInfo
  {
    public Guid MatcherId { get; set; }
    public string OnlineId { get; set; }
    public int GamesDbId { get; set; }
    public string GameName { get; set; }
    public string PlatformId { get; set; }
    public string Platform { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string Description { get; set; }
    public string Certification { get; set; }
    public string Players { get; set; }
    public bool Coop { get; set; }
    public string Publisher { get; set; }
    public string Developer { get; set; }
    public double Rating { get; set; }
    public List<string> Genres { get; internal set; }

    public GameInfo()
    {
      Genres = new List<string>();
    }

    /// <summary>
    /// Copies the contained movie information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData, ILocalFsResourceAccessor lfsra)
    {
      MediaItemAspect.GetOrCreateAspect(aspectData, GameAspect.Metadata);
      MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(aspectData, ProviderResourceAspect.Metadata);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, GameCategory.CategoryNameToMimeType(Platform));
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, lfsra.Size);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, lfsra.CanonicalLocalResourcePath.Serialize());
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_ISVIRTUAL, false);

      if (!string.IsNullOrEmpty(GameName))
      {
        MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, GameName);
        MediaItemAspect.SetAttribute(aspectData, GameAspect.ATTR_GAME_NAME, GameName);
        MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, BaseInfo.GetSortTitle(GameName));
      }
      if (ReleaseDate.HasValue)
      {
        MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, ReleaseDate);
        MediaItemAspect.SetAttribute(aspectData, GameAspect.ATTR_YEAR, ReleaseDate.Value.Year);
      }

      if (!string.IsNullOrEmpty(Platform)) MediaItemAspect.SetAttribute(aspectData, GameAspect.ATTR_PLATFORM, Platform);
      if (!string.IsNullOrEmpty(PlatformId)) MediaItemAspect.SetAttribute(aspectData, GameAspect.ATTR_PLATFORM_ID, PlatformId);
      if (MatcherId != Guid.Empty) MediaItemAspect.SetAttribute(aspectData, GameAspect.ATTR_MATCHER_ID, MatcherId);
      if (!string.IsNullOrEmpty(OnlineId)) MediaItemAspect.SetAttribute(aspectData, GameAspect.ATTR_ONLINE_ID, OnlineId);
      if (GamesDbId > 0) MediaItemAspect.SetAttribute(aspectData, GameAspect.ATTR_TGDB_ID, GamesDbId);
      if (!string.IsNullOrEmpty(Description)) MediaItemAspect.SetAttribute(aspectData, GameAspect.ATTR_DESCRIPTION, Description);
      if (!string.IsNullOrEmpty(Certification)) MediaItemAspect.SetAttribute(aspectData, GameAspect.ATTR_CERTIFICATION, Certification);
      if (!string.IsNullOrEmpty(Developer)) MediaItemAspect.SetAttribute(aspectData, GameAspect.ATTR_DEVELOPER, Developer);
      if (Rating > 0d) MediaItemAspect.SetAttribute(aspectData, GameAspect.ATTR_RATING, Rating);
      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, GameAspect.ATTR_GENRES, Genres);
      return true;
    }
  }
}
