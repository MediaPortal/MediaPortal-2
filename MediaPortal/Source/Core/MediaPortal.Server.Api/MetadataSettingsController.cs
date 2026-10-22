/*
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MediaPortal.Common;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Utilities.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// What the server asks the internet - and itself - when it builds the library: the metadata extractors'
  /// online searches and fanart download, whose details of the people behind a title it fetches and how many of
  /// them it keeps, per kind of media, and the online libraries' languages.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The settings behind this endpoint belong to the extractor plugins, not to the server core: each plugin
  /// keeps its own settings class and reads it through the settings manager when it works. This controller is
  /// deliberately on the other side of that wall - it references none of those assemblies at compile time and
  /// instead looks each settings class up among the assemblies the running server has actually loaded. That
  /// has one visible consequence, and the answer carries it on purpose: <b>a block whose extractor is not
  /// installed is absent from the answer, not defaulted</b>. A screen that shows a switch for an extractor
  /// this server does not have would offer a change nothing reads; a screen that hides the block has nothing
  /// to lie about.
  /// </para>
  /// <para>
  /// The fields mirror the extractor settings property for property, with two honesty rules. Only properties
  /// a living room plausibly changes are carried - whether to search online at all, whether to fetch artwork,
  /// whether cached artwork copies are kept, whose details of the people behind a title to fetch, how many
  /// actors and characters to keep, and in which language the online libraries answer - not the extractors'
  /// tuning like year-matching patterns, which the reference also kept out of its settings window. And what
  /// takes effect when is said plainly: these are read when the extractors work, so a change speaks to the
  /// next import pass, not to the library as it stands - the flag
  /// <see cref="MetadataSettingsResult.NextImportPass"/> says so once instead of every screen inventing it.
  /// </para>
  /// <para>
  /// Writing is guarded by <see cref="MediaPortalAuthorizeAttribute"/> like every other settings endpoint:
  /// when the owner turned web authorization on, a token is required; when they did not, the LAN is trusted
  /// with its own server.
  /// </para>
  /// </remarks>
  [ApiController]
  [Route("api/media")]
  [MediaPortalAuthorize]
  public class MetadataSettingsController : ControllerBase
  {
    #region The map between the answer and the extractor settings

    /// <summary>The three shapes a settings property this endpoint speaks can have.</summary>
    private enum FieldKind
    {
      /// <summary>A switch: the wish is a yes or a no, and so is the answer.</summary>
      Toggle,

      /// <summary>A language name. An empty wish means leave the stored name alone.</summary>
      Text,

      /// <summary>
      /// How many people of one kind the extractor keeps. A wish has to be a whole number inside
      /// <see cref="MINIMUM_KEEP_COUNT"/> and <see cref="MAXIMUM_KEEP_COUNT"/>, which is more than the
      /// settings class itself asks for and is said in the refusal.
      /// </summary>
      Count
    }

    /// <summary>
    /// One field of the answer, and where it lives: which settings class holds it and which property of that
    /// class spells it. Resolved by reflection at request time, so this assembly never names an extractor.
    /// </summary>
    private readonly struct FieldMap
    {
      internal FieldMap(string block, string typeName, string property, string field,
          FieldKind kind = FieldKind.Toggle)
      {
        Block = block;
        TypeName = typeName;
        Property = property;
        Field = field;
        Kind = kind;
      }

      internal string Block { get; }
      internal string TypeName { get; }
      internal string Property { get; }
      internal string Field { get; }

      /// <summary>How the property is asked for and answered, so that a language or a number is not read as a switch.</summary>
      internal FieldKind Kind { get; }
    }

    /// <summary>
    /// The fewest actors or characters a caller may ask an extractor to keep. The reference hands the stored
    /// number straight to LINQ's <c>Take</c> when it trims a cast list, and <c>Take</c> throws on a negative
    /// count, so zero - keep none of them - is the floor a running extractor can actually live with.
    /// </summary>
    private const int MINIMUM_KEEP_COUNT = 0;

    /// <summary>
    /// The most actors or characters a caller may ask an extractor to keep. The reference sets no ceiling of
    /// its own and defaults to ten per movie and twenty per series, so this number is the endpoint's own,
    /// stated rather than implied: generous beside those defaults, and low enough that one import pass cannot
    /// have the internet describe a cast list nobody watches. The metadata screen says the same range on its
    /// rows, because a wish refused after it was typed is a wish the viewer typed twice.
    /// </summary>
    private const int MAXIMUM_KEEP_COUNT = 100;

    private const string MOVIE_TYPE = "MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor.Settings.MovieMetadataExtractorSettings";
    private const string SERIES_TYPE = "MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.Settings.SeriesMetadataExtractorSettings";
    private const string AUDIO_TYPE = "MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor.Settings.AudioMetadataExtractorSettings";
    private const string VIDEO_TYPE = "MediaPortal.Extensions.MetadataExtractors.VideoMetadataExtractor.Settings.VideoMetadataExtractorSettings";
    private const string SUBTITLE_TYPE = "MediaPortal.Extensions.MetadataExtractors.SubtitleMetadataExtractor.Settings.SubtitleMetadataExtractorSettings";
    private const string ONLINE_TYPE = "MediaPortal.Extensions.OnlineLibraries.OnlineLibrarySettings";

    /// <summary>
    /// Every field this endpoint speaks, in the order a screen would walk them. A field is only ever present
    /// where it is also true: the series extractor has no video-movie cache flag, and neither would a switch
    /// for one.
    /// </summary>
    private static readonly FieldMap[] FIELDS =
    {
      new FieldMap("movie", MOVIE_TYPE, nameof(MovieFlags.SkipOnlineSearches), nameof(MovieFlags.SkipOnlineSearches)),
      new FieldMap("movie", MOVIE_TYPE, nameof(MovieFlags.SkipFanArtDownload), nameof(MovieFlags.SkipFanArtDownload)),
      new FieldMap("movie", MOVIE_TYPE, nameof(MovieFlags.OnlyLocalMedia), nameof(MovieFlags.OnlyLocalMedia)),
      new FieldMap("movie", MOVIE_TYPE, nameof(MovieFlags.CacheOfflineFanArt), nameof(MovieFlags.CacheOfflineFanArt)),
      new FieldMap("movie", MOVIE_TYPE, nameof(MovieFlags.CacheLocalFanArt), nameof(MovieFlags.CacheLocalFanArt)),
      new FieldMap("movie", MOVIE_TYPE, nameof(MovieFlags.IncludeActorDetails), nameof(MovieFlags.IncludeActorDetails)),
      new FieldMap("movie", MOVIE_TYPE, nameof(MovieFlags.IncludeCharacterDetails), nameof(MovieFlags.IncludeCharacterDetails)),
      new FieldMap("movie", MOVIE_TYPE, nameof(MovieFlags.IncludeDirectorDetails), nameof(MovieFlags.IncludeDirectorDetails)),
      new FieldMap("movie", MOVIE_TYPE, nameof(MovieFlags.IncludeWriterDetails), nameof(MovieFlags.IncludeWriterDetails)),
      new FieldMap("movie", MOVIE_TYPE, nameof(MovieFlags.IncludeProductionCompanyDetails), nameof(MovieFlags.IncludeProductionCompanyDetails)),
      new FieldMap("movie", MOVIE_TYPE, nameof(MovieFlags.MaximumActorCount), nameof(MovieFlags.MaximumActorCount), FieldKind.Count),
      new FieldMap("movie", MOVIE_TYPE, nameof(MovieFlags.MaximumCharacterCount), nameof(MovieFlags.MaximumCharacterCount), FieldKind.Count),

      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.SkipOnlineSearches), nameof(MovieFlags.SkipOnlineSearches)),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.SkipFanArtDownload), nameof(MovieFlags.SkipFanArtDownload)),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.OnlyLocalMedia), nameof(MovieFlags.OnlyLocalMedia)),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.CacheOfflineFanArt), nameof(MovieFlags.CacheOfflineFanArt)),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.CacheLocalFanArt), nameof(MovieFlags.CacheLocalFanArt)),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.IncludeActorDetails), nameof(MovieFlags.IncludeActorDetails)),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.IncludeCharacterDetails), nameof(MovieFlags.IncludeCharacterDetails)),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.IncludeDirectorDetails), nameof(MovieFlags.IncludeDirectorDetails)),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.IncludeWriterDetails), nameof(MovieFlags.IncludeWriterDetails)),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.IncludeTVNetworkDetails), nameof(MovieFlags.IncludeTVNetworkDetails)),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.IncludeProductionCompanyDetails), nameof(MovieFlags.IncludeProductionCompanyDetails)),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.MaximumActorCount), nameof(MovieFlags.MaximumActorCount), FieldKind.Count),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.MaximumCharacterCount), nameof(MovieFlags.MaximumCharacterCount), FieldKind.Count),

      new FieldMap("audio", AUDIO_TYPE, nameof(MovieFlags.SkipOnlineSearches), nameof(MovieFlags.SkipOnlineSearches)),
      new FieldMap("audio", AUDIO_TYPE, nameof(MovieFlags.SkipFanArtDownload), nameof(MovieFlags.SkipFanArtDownload)),
      new FieldMap("audio", AUDIO_TYPE, nameof(MovieFlags.CacheOfflineFanArt), nameof(MovieFlags.CacheOfflineFanArt)),
      new FieldMap("audio", AUDIO_TYPE, nameof(MovieFlags.CacheLocalFanArt), nameof(MovieFlags.CacheLocalFanArt)),
      new FieldMap("audio", AUDIO_TYPE, nameof(MovieFlags.IncludeArtistDetails), nameof(MovieFlags.IncludeArtistDetails)),
      new FieldMap("audio", AUDIO_TYPE, nameof(MovieFlags.IncludeComposerDetails), nameof(MovieFlags.IncludeComposerDetails)),
      new FieldMap("audio", AUDIO_TYPE, nameof(MovieFlags.IncludeMusicLabelDetails), nameof(MovieFlags.IncludeMusicLabelDetails)),
      new FieldMap("audio", AUDIO_TYPE, nameof(MovieFlags.IncludeConductorDetails), nameof(MovieFlags.IncludeConductorDetails)),

      new FieldMap("video", VIDEO_TYPE, nameof(MovieFlags.CacheOfflineFanArt), nameof(MovieFlags.CacheOfflineFanArt)),
      new FieldMap("video", VIDEO_TYPE, nameof(MovieFlags.CacheLocalFanArt), nameof(MovieFlags.CacheLocalFanArt)),
      new FieldMap("video", VIDEO_TYPE, nameof(MovieFlags.CacheOfflineMovieFanArt), nameof(MovieFlags.CacheOfflineMovieFanArt)),
      new FieldMap("video", VIDEO_TYPE, nameof(MovieFlags.CacheLocalMovieFanArt), nameof(MovieFlags.CacheLocalMovieFanArt)),
      new FieldMap("video", VIDEO_TYPE, nameof(MovieFlags.CacheOfflineSeriesFanArt), nameof(MovieFlags.CacheOfflineSeriesFanArt)),
      new FieldMap("video", VIDEO_TYPE, nameof(MovieFlags.CacheLocalSeriesFanArt), nameof(MovieFlags.CacheLocalSeriesFanArt)),

      new FieldMap("subtitle", SUBTITLE_TYPE, nameof(MovieFlags.SkipOnlineSearches), nameof(MovieFlags.SkipOnlineSearches)),

      new FieldMap("online", ONLINE_TYPE, nameof(OnlineFlags.OnlyBasicFanArt), nameof(OnlineFlags.OnlyBasicFanArt)),
      new FieldMap("online", ONLINE_TYPE, nameof(OnlineFlags.UseSecureWebCommunication), nameof(OnlineFlags.UseSecureWebCommunication)),
      new FieldMap("online", ONLINE_TYPE, nameof(OnlineFlags.UseMovieAudioLanguageIfUnmatched), nameof(OnlineFlags.UseMovieAudioLanguageIfUnmatched)),
      new FieldMap("online", ONLINE_TYPE, nameof(OnlineFlags.UseSeriesAudioLanguageIfUnmatched), nameof(OnlineFlags.UseSeriesAudioLanguageIfUnmatched)),
      new FieldMap("online", ONLINE_TYPE, nameof(OnlineFlags.UseMusicAudioLanguageIfUnmatched), nameof(OnlineFlags.UseMusicAudioLanguageIfUnmatched)),
      new FieldMap("online", ONLINE_TYPE, nameof(OnlineFlags.MovieLanguageCulture), nameof(OnlineFlags.MovieLanguageCulture), FieldKind.Text),
      new FieldMap("online", ONLINE_TYPE, nameof(OnlineFlags.SeriesLanguageCulture), nameof(OnlineFlags.SeriesLanguageCulture), FieldKind.Text),
      new FieldMap("online", ONLINE_TYPE, nameof(OnlineFlags.MusicLanguageCulture), nameof(OnlineFlags.MusicLanguageCulture), FieldKind.Text),
      new FieldMap("online", ONLINE_TYPE, nameof(OnlineFlags.SubtitleLanguageCulture), nameof(OnlineFlags.SubtitleLanguageCulture), FieldKind.Text)
    };

    #endregion

    #region The shapes of the answer and the wish

    /// <summary>
    /// The switches and the two counts of one extractor. Every field is nullable in both directions: null in an
    /// answer means the server has no such field to speak of (the extractor is not installed, or the block does
    /// not carry that switch), and null in a request means "leave this one as it is". A count arrives as a whole
    /// number between <see cref="MINIMUM_KEEP_COUNT"/> and <see cref="MAXIMUM_KEEP_COUNT"/>; outside that range
    /// the request is refused before anything is stored.
    /// </summary>
    public class MovieFlags
    {
      public bool? SkipOnlineSearches { get; set; }
      public bool? SkipFanArtDownload { get; set; }
      public bool? OnlyLocalMedia { get; set; }
      public bool? CacheOfflineFanArt { get; set; }
      public bool? CacheLocalFanArt { get; set; }
      public bool? CacheOfflineMovieFanArt { get; set; }
      public bool? CacheLocalMovieFanArt { get; set; }
      public bool? CacheOfflineSeriesFanArt { get; set; }
      public bool? CacheLocalSeriesFanArt { get; set; }

      // Whose details the extractor is asked for, and how many people of a kind it keeps. Null everywhere the
      // extractor's own settings class has no such property, which is why the music block has no counts and the
      // movie block no television network.
      public bool? IncludeActorDetails { get; set; }
      public bool? IncludeCharacterDetails { get; set; }
      public bool? IncludeDirectorDetails { get; set; }
      public bool? IncludeWriterDetails { get; set; }
      public bool? IncludeTVNetworkDetails { get; set; }
      public bool? IncludeProductionCompanyDetails { get; set; }
      public bool? IncludeArtistDetails { get; set; }
      public bool? IncludeComposerDetails { get; set; }
      public bool? IncludeMusicLabelDetails { get; set; }
      public bool? IncludeConductorDetails { get; set; }
      public int? MaximumActorCount { get; set; }
      public int? MaximumCharacterCount { get; set; }
    }

    /// <summary>
    /// The online libraries' own answers: which artwork is fetched at all, whether https is insisted on,
    /// which language each kind of search should be answered in, and whether the media file's own audio
    /// language stands in when nothing matches the preferred one.
    /// </summary>
    public class OnlineFlags
    {
      public bool? OnlyBasicFanArt { get; set; }
      public bool? UseSecureWebCommunication { get; set; }
      public bool? UseMovieAudioLanguageIfUnmatched { get; set; }
      public bool? UseSeriesAudioLanguageIfUnmatched { get; set; }
      public bool? UseMusicAudioLanguageIfUnmatched { get; set; }
      public string MovieLanguageCulture { get; set; }
      public string SeriesLanguageCulture { get; set; }
      public string MusicLanguageCulture { get; set; }
      public string SubtitleLanguageCulture { get; set; }
    }

    /// <summary>
    /// What <c>GET api/media/settings/metadata</c> answers. A block is null when its extractor is not
    /// installed on this server.
    /// </summary>
    public class MetadataSettingsResult
    {
      public MovieFlags Movie { get; set; }
      public MovieFlags Series { get; set; }
      public MovieFlags Audio { get; set; }
      public MovieFlags Video { get; set; }
      public MovieFlags Subtitle { get; set; }
      public OnlineFlags OnlineLibraries { get; set; }

      /// <summary>
      /// Always true: these settings are read while the extractors work, so what changes here speaks to the
      /// next import pass rather than to the library as it stands. Named here so that no screen has to guess
      /// whether a change is live.
      /// </summary>
      public bool NextImportPass { get; set; } = true;
    }

    /// <summary>
    /// What a caller asks to change. Same shape as the answer; every field left out stays as it is.
    /// </summary>
    public class MetadataSettingsRequest
    {
      public MovieFlags Movie { get; set; }
      public MovieFlags Series { get; set; }
      public MovieFlags Audio { get; set; }
      public MovieFlags Video { get; set; }
      public MovieFlags Subtitle { get; set; }
      public OnlineFlags OnlineLibraries { get; set; }
    }

    /// <summary>The body of an answer that refused: the reason, in words a screen can show.</summary>
    public class ErrorResponse
    {
      public string Error { get; set; }
    }

    #endregion

    /// <summary>Reads what the extractors are configured to do.</summary>
    [HttpGet("settings/metadata")]
    public ActionResult<MetadataSettingsResult> GetMetadataSettings()
    {
      try
      {
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>(false);
        if (settingsManager == null)
          return StatusCode(StatusCodes.Status503ServiceUnavailable);

        return Ok(ReadAll(settingsManager, null));
      }
      catch (IllegalCallException ex)
      {
        return new ObjectResult(new ErrorResponse { Error = ex.Message })
        {
          StatusCode = StatusCodes.Status503ServiceUnavailable
        };
      }
    }

    /// <summary>
    /// Changes what the extractors are configured to do. What arrives is applied field by field - only the
    /// fields that are present - and the answer is the whole state after the change. A count outside the range
    /// this endpoint states is refused with a sentence, and the refusal applies nothing at all, not even the
    /// fields that could have been stored.
    /// </summary>
    [HttpPut("settings/metadata")]
    public ActionResult<MetadataSettingsResult> PutMetadataSettings([FromBody] MetadataSettingsRequest request)
    {
      if (request == null)
        return BadRequest(new ErrorResponse { Error = "no settings were sent" });

      string refusal = CountRefusal(request);
      if (refusal != null)
        return BadRequest(new ErrorResponse { Error = refusal });

      try
      {
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>(false);
        if (settingsManager == null)
          return StatusCode(StatusCodes.Status503ServiceUnavailable);

        return Ok(ReadAll(settingsManager, request));
      }
      catch (IllegalCallException ex)
      {
        return new ObjectResult(new ErrorResponse { Error = ex.Message })
        {
          StatusCode = StatusCodes.Status503ServiceUnavailable
        };
      }
    }

    #region Reflection over the loaded extractor settings

    /// <summary>
    /// Reads every block, applying the request's fields to the settings objects on the way and saving the
    /// objects whose values actually moved. A settings class the server has not loaded is not created from
    /// thin air - its block stays absent, and its wish in a request is answered with nothing rather than with
    /// a file for an extractor that does not exist.
    /// </summary>
    private static MetadataSettingsResult ReadAll(ISettingsManager settingsManager,
        MetadataSettingsRequest request)
    {
      Dictionary<string, MovieFlags> aspects = new Dictionary<string, MovieFlags>(StringComparer.Ordinal)
      {
        ["movie"] = null, ["series"] = null, ["audio"] = null, ["video"] = null, ["subtitle"] = null
      };
      Dictionary<string, OnlineFlags> online = new Dictionary<string, OnlineFlags>(StringComparer.Ordinal)
      {
        ["online"] = null
      };

      foreach (IGrouping<string, FieldMap> block in FIELDS.GroupBy(field => field.Block))
      {
        Type settingsType = FindLoadedType(block.First().TypeName);
        if (settingsType == null)
          continue;

        object settings = settingsManager.Load(settingsType);
        if (settings == null)
          // A settings manager that will not answer for this class is in no position to be edited through
          // it; the block reads as absent, which is the same truthful answer as an extractor that is not
          // installed.
          continue;

        bool changed = false;

        foreach (FieldMap map in block)
        {
          PropertyInfo property = settingsType.GetProperty(map.Property,
              BindingFlags.Public | BindingFlags.Instance);
          if (property == null || !property.CanRead)
            continue;

          object stored = property.GetValue(settings);
          string wishText = null;
          bool? wishFlag = null;
          bool wished = false;
          ReadWish(request, map, out wished, out wishFlag, out wishText);
          int? wishCount = CountWish(request, map);

          if (wishCount.HasValue)
          {
            // The range was settled when the request arrived, so a count that reached this point is one the
            // extractor can be handed.
            property.SetValue(settings, wishCount.Value);
            stored = wishCount.Value;
            changed = true;
          }
          else if (wished)
          {
            bool text = map.Kind == FieldKind.Text;
            object wish = text ? (object)(wishText ?? "").Trim() : wishFlag.Value;
            if (text)
            {
              // A language is stored only as a name that could be a name; an empty wish means leave it.
              if (string.IsNullOrEmpty((string)wish) || Equals(stored, wish))
                wished = false;
            }

            if (wished)
            {
              property.SetValue(settings, wish);
              stored = wish;
              changed = true;
            }
          }

          // A block lives in one of the two dictionaries, never in both: the online libraries have a shape of
          // their own, so asking the aspect dictionary for "online" would ask for a key it has never held.
          if (map.Kind == FieldKind.Text)
          {
            online["online"] ??= new OnlineFlags();
            Apply(map, online["online"], null, stored);
          }
          else
          {
            aspects[map.Block] ??= new MovieFlags();
            Apply(map, null, aspects[map.Block], stored);
          }
        }

        if (changed)
          settingsManager.Save(settings);
      }

      return new MetadataSettingsResult
      {
        Movie = aspects["movie"],
        Series = aspects["series"],
        Audio = aspects["audio"],
        Video = aspects["video"],
        Subtitle = aspects["subtitle"],
        OnlineLibraries = online["online"]
      };
    }

    /// <summary>What the request asks of one switch or one language, if anything. Counts are asked for by <see cref="CountWish"/>.</summary>
    private static void ReadWish(MetadataSettingsRequest request, FieldMap map,
        out bool wished, out bool? wishFlag, out string wishText)
    {
      wished = false;
      wishFlag = null;
      wishText = null;

      if (request == null)
        // A read asks nothing of anybody, so there is no wish to read out of it.
        return;

      MovieFlags aspect = AspectOf(request, map.Block);
      OnlineFlags libraries = map.Block == "online" ? request.OnlineLibraries : null;

      if (libraries != null)
      {
        switch (map.Field)
        {
          case nameof(OnlineFlags.OnlyBasicFanArt):
            wished = libraries.OnlyBasicFanArt.HasValue;
            wishFlag = libraries.OnlyBasicFanArt;
            break;
          case nameof(OnlineFlags.UseSecureWebCommunication):
            wished = libraries.UseSecureWebCommunication.HasValue;
            wishFlag = libraries.UseSecureWebCommunication;
            break;
          case nameof(OnlineFlags.UseMovieAudioLanguageIfUnmatched):
            wished = libraries.UseMovieAudioLanguageIfUnmatched.HasValue;
            wishFlag = libraries.UseMovieAudioLanguageIfUnmatched;
            break;
          case nameof(OnlineFlags.UseSeriesAudioLanguageIfUnmatched):
            wished = libraries.UseSeriesAudioLanguageIfUnmatched.HasValue;
            wishFlag = libraries.UseSeriesAudioLanguageIfUnmatched;
            break;
          case nameof(OnlineFlags.UseMusicAudioLanguageIfUnmatched):
            wished = libraries.UseMusicAudioLanguageIfUnmatched.HasValue;
            wishFlag = libraries.UseMusicAudioLanguageIfUnmatched;
            break;
          case nameof(OnlineFlags.MovieLanguageCulture):
            wished = libraries.MovieLanguageCulture != null;
            wishText = libraries.MovieLanguageCulture;
            break;
          case nameof(OnlineFlags.SeriesLanguageCulture):
            wished = libraries.SeriesLanguageCulture != null;
            wishText = libraries.SeriesLanguageCulture;
            break;
          case nameof(OnlineFlags.MusicLanguageCulture):
            wished = libraries.MusicLanguageCulture != null;
            wishText = libraries.MusicLanguageCulture;
            break;
          case nameof(OnlineFlags.SubtitleLanguageCulture):
            wished = libraries.SubtitleLanguageCulture != null;
            wishText = libraries.SubtitleLanguageCulture;
            break;
        }

        return;
      }

      if (aspect == null)
        return;

      switch (map.Field)
      {
        case nameof(MovieFlags.SkipOnlineSearches):
          wished = aspect.SkipOnlineSearches.HasValue;
          wishFlag = aspect.SkipOnlineSearches;
          break;
        case nameof(MovieFlags.SkipFanArtDownload):
          wished = aspect.SkipFanArtDownload.HasValue;
          wishFlag = aspect.SkipFanArtDownload;
          break;
        case nameof(MovieFlags.OnlyLocalMedia):
          wished = aspect.OnlyLocalMedia.HasValue;
          wishFlag = aspect.OnlyLocalMedia;
          break;
        case nameof(MovieFlags.CacheOfflineFanArt):
          wished = aspect.CacheOfflineFanArt.HasValue;
          wishFlag = aspect.CacheOfflineFanArt;
          break;
        case nameof(MovieFlags.CacheLocalFanArt):
          wished = aspect.CacheLocalFanArt.HasValue;
          wishFlag = aspect.CacheLocalFanArt;
          break;
        case nameof(MovieFlags.CacheOfflineMovieFanArt):
          wished = aspect.CacheOfflineMovieFanArt.HasValue;
          wishFlag = aspect.CacheOfflineMovieFanArt;
          break;
        case nameof(MovieFlags.CacheLocalMovieFanArt):
          wished = aspect.CacheLocalMovieFanArt.HasValue;
          wishFlag = aspect.CacheLocalMovieFanArt;
          break;
        case nameof(MovieFlags.CacheOfflineSeriesFanArt):
          wished = aspect.CacheOfflineSeriesFanArt.HasValue;
          wishFlag = aspect.CacheOfflineSeriesFanArt;
          break;
        case nameof(MovieFlags.CacheLocalSeriesFanArt):
          wished = aspect.CacheLocalSeriesFanArt.HasValue;
          wishFlag = aspect.CacheLocalSeriesFanArt;
          break;
        case nameof(MovieFlags.IncludeActorDetails):
          wished = aspect.IncludeActorDetails.HasValue;
          wishFlag = aspect.IncludeActorDetails;
          break;
        case nameof(MovieFlags.IncludeCharacterDetails):
          wished = aspect.IncludeCharacterDetails.HasValue;
          wishFlag = aspect.IncludeCharacterDetails;
          break;
        case nameof(MovieFlags.IncludeDirectorDetails):
          wished = aspect.IncludeDirectorDetails.HasValue;
          wishFlag = aspect.IncludeDirectorDetails;
          break;
        case nameof(MovieFlags.IncludeWriterDetails):
          wished = aspect.IncludeWriterDetails.HasValue;
          wishFlag = aspect.IncludeWriterDetails;
          break;
        case nameof(MovieFlags.IncludeTVNetworkDetails):
          wished = aspect.IncludeTVNetworkDetails.HasValue;
          wishFlag = aspect.IncludeTVNetworkDetails;
          break;
        case nameof(MovieFlags.IncludeProductionCompanyDetails):
          wished = aspect.IncludeProductionCompanyDetails.HasValue;
          wishFlag = aspect.IncludeProductionCompanyDetails;
          break;
        case nameof(MovieFlags.IncludeArtistDetails):
          wished = aspect.IncludeArtistDetails.HasValue;
          wishFlag = aspect.IncludeArtistDetails;
          break;
        case nameof(MovieFlags.IncludeComposerDetails):
          wished = aspect.IncludeComposerDetails.HasValue;
          wishFlag = aspect.IncludeComposerDetails;
          break;
        case nameof(MovieFlags.IncludeMusicLabelDetails):
          wished = aspect.IncludeMusicLabelDetails.HasValue;
          wishFlag = aspect.IncludeMusicLabelDetails;
          break;
        case nameof(MovieFlags.IncludeConductorDetails):
          wished = aspect.IncludeConductorDetails.HasValue;
          wishFlag = aspect.IncludeConductorDetails;
          break;
      }
    }

    /// <summary>The part of the request a field of a block belongs to; null when the request sends none.</summary>
    private static MovieFlags AspectOf(MetadataSettingsRequest request, string block)
    {
      return block switch
      {
        "movie" => request.Movie,
        "series" => request.Series,
        "audio" => request.Audio,
        "video" => request.Video,
        "subtitle" => request.Subtitle,
        _ => null
      };
    }

    /// <summary>
    /// What the request asks of one of the two counts, or null when it asks nothing - which is also the answer
    /// for a field that is not a count, so a caller can ask for any field the same way.
    /// </summary>
    private static int? CountWish(MetadataSettingsRequest request, FieldMap map)
    {
      if (request == null || map.Kind != FieldKind.Count)
        return null;

      MovieFlags aspect = AspectOf(request, map.Block);
      if (aspect == null)
        return null;

      if (map.Field == nameof(MovieFlags.MaximumActorCount))
        return aspect.MaximumActorCount;
      return map.Field == nameof(MovieFlags.MaximumCharacterCount) ? aspect.MaximumCharacterCount : (int?)null;
    }

    /// <summary>
    /// The sentence saying why a request asked for a count that cannot be stored, or null when every count it
    /// asked for could be. Asked for the whole request at once and before anything is applied, because half a
    /// wish stored is worse than none: the caller would read back a state nobody asked for, and would have no
    /// way of telling which half moved.
    /// </summary>
    private static string CountRefusal(MetadataSettingsRequest request)
    {
      foreach (FieldMap map in FIELDS)
      {
        int? wish = CountWish(request, map);
        if (!wish.HasValue || (wish.Value >= MINIMUM_KEEP_COUNT && wish.Value <= MAXIMUM_KEEP_COUNT))
          continue;

        return "The number of " + CountedItems(map.Field) + " the " + map.Block
            + " extractor keeps has to be a whole number between " + MINIMUM_KEEP_COUNT + " and "
            + MAXIMUM_KEEP_COUNT + ".";
      }

      return null;
    }

    /// <summary>What a count counts, in the words a refusal shows rather than the property's name.</summary>
    private static string CountedItems(string field)
    {
      return field == nameof(MovieFlags.MaximumActorCount) ? "actors" : "characters";
    }

    /// <summary>Writes one read value into the answer block it belongs to.</summary>
    private static void Apply(FieldMap map, OnlineFlags libraries, MovieFlags aspect, object stored)
    {
      if (map.Kind == FieldKind.Text)
      {
        string text = stored as string ?? "";
        switch (map.Field)
        {
          case nameof(OnlineFlags.MovieLanguageCulture):
            libraries.MovieLanguageCulture = text;
            break;
          case nameof(OnlineFlags.SeriesLanguageCulture):
            libraries.SeriesLanguageCulture = text;
            break;
          case nameof(OnlineFlags.MusicLanguageCulture):
            libraries.MusicLanguageCulture = text;
            break;
          case nameof(OnlineFlags.SubtitleLanguageCulture):
            libraries.SubtitleLanguageCulture = text;
            break;
        }

        return;
      }

      if (map.Kind == FieldKind.Count)
      {
        // A number the settings class holds as anything other than a whole number is left unanswered rather
        // than guessed at - a screen with no row for it cannot store a number it was never shown.
        if (stored is int number)
        {
          switch (map.Field)
          {
            case nameof(MovieFlags.MaximumActorCount):
              aspect.MaximumActorCount = number;
              break;
            case nameof(MovieFlags.MaximumCharacterCount):
              aspect.MaximumCharacterCount = number;
              break;
          }
        }

        return;
      }

      bool flag = stored is bool value && value;
      switch (map.Field)
      {
        case nameof(MovieFlags.SkipOnlineSearches):
          aspect.SkipOnlineSearches = flag;
          break;
        case nameof(MovieFlags.SkipFanArtDownload):
          aspect.SkipFanArtDownload = flag;
          break;
        case nameof(MovieFlags.OnlyLocalMedia):
          aspect.OnlyLocalMedia = flag;
          break;
        case nameof(MovieFlags.CacheOfflineFanArt):
          aspect.CacheOfflineFanArt = flag;
          break;
        case nameof(MovieFlags.CacheLocalFanArt):
          aspect.CacheLocalFanArt = flag;
          break;
        case nameof(MovieFlags.CacheOfflineMovieFanArt):
          aspect.CacheOfflineMovieFanArt = flag;
          break;
        case nameof(MovieFlags.CacheLocalMovieFanArt):
          aspect.CacheLocalMovieFanArt = flag;
          break;
        case nameof(MovieFlags.CacheOfflineSeriesFanArt):
          aspect.CacheOfflineSeriesFanArt = flag;
          break;
        case nameof(MovieFlags.CacheLocalSeriesFanArt):
          aspect.CacheLocalSeriesFanArt = flag;
          break;
        case nameof(MovieFlags.IncludeActorDetails):
          aspect.IncludeActorDetails = flag;
          break;
        case nameof(MovieFlags.IncludeCharacterDetails):
          aspect.IncludeCharacterDetails = flag;
          break;
        case nameof(MovieFlags.IncludeDirectorDetails):
          aspect.IncludeDirectorDetails = flag;
          break;
        case nameof(MovieFlags.IncludeWriterDetails):
          aspect.IncludeWriterDetails = flag;
          break;
        case nameof(MovieFlags.IncludeTVNetworkDetails):
          aspect.IncludeTVNetworkDetails = flag;
          break;
        case nameof(MovieFlags.IncludeProductionCompanyDetails):
          aspect.IncludeProductionCompanyDetails = flag;
          break;
        case nameof(MovieFlags.IncludeArtistDetails):
          aspect.IncludeArtistDetails = flag;
          break;
        case nameof(MovieFlags.IncludeComposerDetails):
          aspect.IncludeComposerDetails = flag;
          break;
        case nameof(MovieFlags.IncludeMusicLabelDetails):
          aspect.IncludeMusicLabelDetails = flag;
          break;
        case nameof(MovieFlags.IncludeConductorDetails):
          aspect.IncludeConductorDetails = flag;
          break;
      }
    }

    /// <summary>
    /// Finds a settings class among the assemblies this process has loaded, by full name. The extractor
    /// plugins are loaded by the server's plugin system, so a name this cannot find is a plugin that is not
    /// there - not a typo to chase.
    /// </summary>
    private static Type FindLoadedType(string fullName)
    {
      foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        Type found = assembly.GetType(fullName, false);
        if (found != null)
          return found;
      }

      return null;
    }

    #endregion
  }
}
