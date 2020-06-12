using MediaPortal.Common.MediaManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.Games
{
  public static class GameAspect
  {
    public static readonly Guid ASPECT_ID = new Guid("71D500E8-F2C3-4DAF-8CE6-A89DFE8FD96E");

    /// <summary>
    /// Contains the localized name of the game.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_GAME_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("GameName", 100, Cardinality.Inline, false);

    /// <summary>
    /// Contains the TGDB ID of the game.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_TGDB_ID =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("TGDBID", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains the id of the matcher that matched the game.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_MATCHER_ID =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("MatcherId", typeof(Guid), Cardinality.Inline, false);

    /// <summary>
    /// Contains the online id of the game.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ONLINE_ID =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("OnlineId", 100, Cardinality.Inline, false);

    /// <summary>
    /// Contains the online id of the platform.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_PLATFORM_ID =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("PlatformId", 100, Cardinality.Inline, false);

    /// <summary>
    /// Contains the platform of the game.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_PLATFORM =
      MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Platform", 100, Cardinality.Inline, true);
    
    /// <summary>
    /// Contains the release year of the game.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_YEAR =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Year", typeof(int), Cardinality.Inline, true);

    /// <summary>
    /// Contains the description of the game.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DESCRIPTION =
      MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Description", 10000, Cardinality.Inline, false);

    /// <summary>
    /// Contains the certification.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_CERTIFICATION =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Certification", 100, Cardinality.Inline, false);

    /// <summary>
    /// Contains the developer of the game.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DEVELOPER =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Developer", 100, Cardinality.Inline, false);

    /// <summary>
    /// Contains the overall rating of the movie. Value ranges from 0 (very bad) to 10 (very good).
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_RATING =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("Rating", typeof(double), Cardinality.Inline, true);

    /// <summary>
    /// Genre string.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_GENRES =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Genres", 100, Cardinality.ManyToMany, true);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
      // TODO: Localize name
      ASPECT_ID, "GameItem", new[] {
            ATTR_GAME_NAME,
            ATTR_TGDB_ID,
            ATTR_MATCHER_ID,
            ATTR_ONLINE_ID,
            ATTR_PLATFORM_ID,
            ATTR_PLATFORM,
            ATTR_YEAR,
            ATTR_DESCRIPTION,
            ATTR_CERTIFICATION,
            ATTR_DEVELOPER,
            ATTR_RATING,
            ATTR_GENRES
        });
  }
}
