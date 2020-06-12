using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emulators.Common.Games
{
  public class GameInfo
  {
    public static readonly string SOURCE_MOBY = "MOBY";
    public static readonly string SOURCE_GAMESDB = "GDB";
    public static readonly string SOURCE_RAWG = "RAWG";

    public static readonly string TYPE_GAME = "GAME";

    public static readonly string PLATFORM_PC = "PC";
    public static readonly string PLATFORM_NINTENDO_GAMECUBE = "Nintendo GameCube";
    public static readonly string PLATFORM_NINTENDO_64 = "Nintendo 64";
    public static readonly string PLATFORM_NINTENDO_GAME_BOY = "Nintendo Game Boy";
    public static readonly string PLATFORM_NINTENDO_GAME_BOY_ADVANCE = "Nintendo Game Boy Advance";
    public static readonly string PLATFORM_NINTENDO_SNES = "Super Nintendo (SNES)";
    public static readonly string PLATFORM_NINTENDO_NES = "Nintendo Entertainment System (NES)";
    public static readonly string PLATFORM_NINTENDO_DS = "Nintendo DS";
    public static readonly string PLATFORM_NINTENDO_WII = "Nintendo Wii";
    public static readonly string PLATFORM_SONY_PLAYSTATION = "Sony Playstation";
    public static readonly string PLATFORM_SONY_PLAYSTATION_2 = "Sony Playstation 2";
    public static readonly string PLATFORM_SONY_PLAYSTATION_3 = "Sony Playstation 3";
    public static readonly string PLATFORM_SONY_PLAYSTATION_PORTABLE = "Sony Playstation Portable";
    public static readonly string PLATFORM_MICROSOFT_XBOX = "Microsoft Xbox";
    public static readonly string PLATFORM_MICROSOFT_XBOX_360 = "Microsoft Xbox 360";
    public static readonly string PLATFORM_SEGA_DREAMCAST = "Sega Dreamcast";
    public static readonly string PLATFORM_SEGA_SATURN = "Sega Saturn";
    public static readonly string PLATFORM_SEGA_GENESIS = "Sega Genesis";
    public static readonly string PLATFORM_SEGA_GAME_GEAR = "Sega Game Gear";
    public static readonly string PLATFORM_SEGA_CD = "Sega CD";
    public static readonly string PLATFORM_ATARI_2600 = "Atari 2600";
    public static readonly string PLATFORM_ARCADE = "Arcade";
    public static readonly string PLATFORM_NEO_GEO = "Neo Geo";
    public static readonly string PLATFORM_3DO = "3DO";
    public static readonly string PLATFORM_ATARI_5200 = "Atari 5200";
    public static readonly string PLATFORM_ATARI_7800 = "Atari 7800";
    public static readonly string PLATFORM_ATARI_JAGUAR = "Atari Jaguar";
    public static readonly string PLATFORM_ATARI_JAGUAR_CD = "Atari Jaguar CD";
    public static readonly string PLATFORM_ATARI_XE = "Atari XE";
    public static readonly string PLATFORM_COLECOVISION = "Colecovision";
    public static readonly string PLATFORM_INTELLIVISION = "Intellivision";
    public static readonly string PLATFORM_SEGA_32X = "Sega 32X";
    public static readonly string PLATFORM_TURBOGRAFX_16 = "TurboGrafx 16";
    public static readonly string PLATFORM_SEGA_MASTER_SYSTEM = "Sega Master System";
    public static readonly string PLATFORM_SEGA_MEGA_DRIVE = "Sega Mega Drive";
    public static readonly string PLATFORM_MAC_OS = "Mac OS";
    public static readonly string PLATFORM_NINTENDO_WII_U = "Nintendo Wii U";
    public static readonly string PLATFORM_SONY_PLAYSTATION_VITA = "Sony Playstation Vita";
    public static readonly string PLATFORM_COMMODORE_64 = "Commodore 64";
    public static readonly string PLATFORM_NINTENDO_GAME_BOY_COLOR = "Nintendo Game Boy Color";
    public static readonly string PLATFORM_AMIGA = "Amiga";
    public static readonly string PLATFORM_NINTENDO_3DS = "Nintendo 3DS";
    public static readonly string PLATFORM_SINCLAIR_ZX_SPECTRUM = "Sinclair ZX Spectrum";
    public static readonly string PLATFORM_AMSTRAD_CPC = "Amstrad CPC";
    public static readonly string PLATFORM_IOS = "iOS";
    public static readonly string PLATFORM_ANDROID = "Android";
    public static readonly string PLATFORM_PHILIPS_CDI = "Philips CD-i";
    public static readonly string PLATFORM_NINTENDO_VIRTUAL_BOY = "Nintendo Virtual Boy";
    public static readonly string PLATFORM_SONY_PLAYSTATION_4 = "Sony Playstation 4";
    public static readonly string PLATFORM_MICROSOFT_XBOX_ONE = "Microsoft Xbox One";
    public static readonly string PLATFORM_OUYA = "Ouya";
    public static readonly string PLATFORM_NEO_GEO_POCKET = "Neo Geo Pocket";
    public static readonly string PLATFORM_NEO_GEO_POCKET_COLOR = "Neo Geo Pocket Color";
    public static readonly string PLATFORM_ATARI_LYNX = "Atari Lynx";
    public static readonly string PLATFORM_WONDERSWAN = "WonderSwan";
    public static readonly string PLATFORM_WONDERSWAN_COLOR = "WonderSwan Color";
    public static readonly string PLATFORM_MAGNAVOX_ODYSSEY_2 = "Magnavox Odyssey 2";
    public static readonly string PLATFORM_FAIRCHILD_CHANNEL_F = "Fairchild Channel F";
    public static readonly string PLATFORM_MSX = "MSX";
    public static readonly string PLATFORM_PC_FX = "PC-FX";
    public static readonly string PLATFORM_SHARP_X68000 = "Sharp X68000";
    public static readonly string PLATFORM_FM_TOWNS_MARTY = "FM Towns Marty";
    public static readonly string PLATFORM_PC_88 = "PC-88";
    public static readonly string PLATFORM_PC_98 = "PC-98";
    public static readonly string PLATFORM_NUON = "Nuon";
    public static readonly string PLATFORM_FAMICOM_DISK_SYSTEM = "Famicom Disk System";
    public static readonly string PLATFORM_ATARI_ST = "Atari ST";
    public static readonly string PLATFORM_N_GAGE = "N-Gage";
    public static readonly string PLATFORM_VECTREX = "Vectrex";
    public static readonly string PLATFORM_GAME_COM = "Game.com";
    public static readonly string PLATFORM_TRS_80_COLOR_COMPUTER = "TRS-80 Color Computer";
    public static readonly string PLATFORM_APPLE_II = "Apple II";
    public static readonly string PLATFORM_ATARI_800 = "Atari 800";
    public static readonly string PLATFORM_ACORN_ARCHIMEDES = "Acorn Archimedes";
    public static readonly string PLATFORM_COMMODORE_VIC_20 = "Commodore VIC-20";
    public static readonly string PLATFORM_COMMODORE_128 = "Commodore 128";
    public static readonly string PLATFORM_AMIGA_CD32 = "Amiga CD32";
    public static readonly string PLATFORM_MEGA_DUCK = "Mega Duck";
    public static readonly string PLATFORM_SEGA_SG_1000 = "SEGA SG-1000";
    public static readonly string PLATFORM_GAME_WATCH = "Game & Watch";
    public static readonly string PLATFORM_HANDHELD_ELECTRONIC_GAMES_LCD = "Handheld Electronic Games (LCD)";
    public static readonly string PLATFORM_DRAGON_32_64 = "Dragon 32/64";
    public static readonly string PLATFORM_TEXAS_INSTRUMENTS_TI_99_4A = "Texas Instruments TI-99/4A";
    public static readonly string PLATFORM_ACORN_ELECTRON = "Acorn Electron";
    public static readonly string PLATFORM_TURBOGRAFX_CD = "TurboGrafx CD";
    public static readonly string PLATFORM_NEO_GEO_CD = "Neo Geo CD";
    public static readonly string PLATFORM_NINTENDO_POKEMON_MINI = "Nintendo Pokémon Mini";
    public static readonly string PLATFORM_SEGA_PICO = "Sega Pico";
    public static readonly string PLATFORM_WATARA_SUPERVISION = "Watara Supervision";
    public static readonly string PLATFORM_TOMY_TUTOR = "Tomy Tutor";
    public static readonly string PLATFORM_MAGNAVOX_ODYSSEY_1 = "Magnavox Odyssey 1";
    public static readonly string PLATFORM_GAKKEN_COMPACT_VISION = "Gakken Compact Vision";
    public static readonly string PLATFORM_EMERSON_ARCADIA_2001 = "Emerson Arcadia 2001";
    public static readonly string PLATFORM_CASIO_PV_1000 = "Casio PV-1000";
    public static readonly string PLATFORM_EPOCH_CASSETTE_VISION = "Epoch Cassette Vision";
    public static readonly string PLATFORM_EPOCH_SUPER_CASSETTE_VISION = "Epoch Super Cassette Vision";
    public static readonly string PLATFORM_RCA_STUDIO_II = "RCA Studio II";
    public static readonly string PLATFORM_BALLY_ASTROCADE = "Bally Astrocade";
    public static readonly string PLATFORM_APF_MP_1000 = "APF MP-1000";
    public static readonly string PLATFORM_COLECO_TELSTAR_ARCADE = "Coleco Telstar Arcade";
    public static readonly string PLATFORM_NINTENDO_SWITCH = "Nintendo Switch";
    public static readonly string PLATFORM_MILTON_BRADLEY_MICROVISION = "Milton Bradley Microvision";
    public static readonly string PLATFORM_ENTEX_SELECT_A_GAME = "Entex Select-a-Game";
    public static readonly string PLATFORM_ENTEX_ADVENTURE_VISION = "Entex Adventure Vision";
    public static readonly string PLATFORM_PIONEER_LASERACTIVE = "Pioneer LaserActive";
    public static readonly string PLATFORM_ACTION_MAX = "Action Max";
    public static readonly string PLATFORM_SHARP_X1 = "Sharp X1";
    public static readonly string PLATFORM_FUJITSU_FM_7 = "Fujitsu FM-7";
    public static readonly string PLATFORM_SAM_COUPE = "SAM Coupé";




    public string MobyId;
    public int GamesDbId;
    public int RAWGVgDbId;
    public Dictionary<string, string> CustomIds { get; } = new Dictionary<string, string>();
    public string SearchName;
    public string GameName;
    public string Platform;
    public DateTime? ReleaseDate;
    public string Description;
    public string Certification;
    public string Players;
    public bool Coop;
    public string Publisher;
    public string Developer;
    public SimpleRating Rating;
    public List<string> Genres { get; } = new List<string>();

    /// <summary>
    /// Copies the contained game information into MediaItemAspect.
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
      if (!string.IsNullOrEmpty(Description)) MediaItemAspect.SetAttribute(aspectData, GameAspect.ATTR_DESCRIPTION, Description);
      if (!string.IsNullOrEmpty(Certification)) MediaItemAspect.SetAttribute(aspectData, GameAspect.ATTR_CERTIFICATION, Certification);
      if (!string.IsNullOrEmpty(Developer)) MediaItemAspect.SetAttribute(aspectData, GameAspect.ATTR_DEVELOPER, Developer);
      if (!Rating.IsEmpty) MediaItemAspect.SetAttribute(aspectData, GameAspect.ATTR_RATING, Rating.RatingValue);
      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, GameAspect.ATTR_GENRES, Genres);

      if (!string.IsNullOrEmpty(MobyId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, SOURCE_MOBY, TYPE_GAME, MobyId);
      if (GamesDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, SOURCE_GAMESDB, TYPE_GAME, GamesDbId.ToString());
      if (RAWGVgDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, SOURCE_RAWG, TYPE_GAME, RAWGVgDbId.ToString());
      foreach (var customId in CustomIds)
      {
        MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, customId.Key, TYPE_GAME, customId.Value);
      }

      return true;
    }

    public void SetIdsAndName(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (MediaItemAspect.TryGetAttribute(aspectData, GameAspect.ATTR_GAME_NAME, out string gameName))
        GameName = gameName;

      IList<MultipleMediaItemAspect> externalIdAspects;
      if (MediaItemAspect.TryGetAspects(aspectData, ExternalIdentifierAspect.Metadata, out externalIdAspects))
      {
        foreach (MultipleMediaItemAspect externalId in externalIdAspects)
        {
          string source = externalId.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string id = externalId.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          string type = externalId.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          if (type == TYPE_GAME)
          {
            if (source == SOURCE_GAMESDB)
            {
              GamesDbId = Convert.ToInt32(id);
            }
            else if (source == SOURCE_MOBY)
            {
              MobyId = id;
            }
            else if (source == SOURCE_RAWG)
            {
              RAWGVgDbId = Convert.ToInt32(id);
            }
            else
            {
              CustomIds.Add(source, id);
            }
          }
        }
      }
    }

    public void Merge(GameInfo game)
    {
      MetadataUpdater.SetOrUpdateId(ref GamesDbId, game.GamesDbId);
      MetadataUpdater.SetOrUpdateId(ref RAWGVgDbId, game.RAWGVgDbId);
      MetadataUpdater.SetOrUpdateId(ref MobyId, game.MobyId);

      MetadataUpdater.SetOrUpdateString(ref GameName, game.GameName, false);
      MetadataUpdater.SetOrUpdateString(ref Description, game.Description);

      MetadataUpdater.SetOrUpdateString(ref Developer, game.Developer, false);

      MetadataUpdater.SetOrUpdateRatings(ref Rating, game.Rating);

      if (!ReleaseDate.HasValue)
        ReleaseDate = game.ReleaseDate;

      if (!Genres.Any() && game.Genres != null)
      {
        foreach (var genre in game.Genres)
          Genres.Add(genre);
      }
    }
  }
}
