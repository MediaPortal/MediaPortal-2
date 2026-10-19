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
  /// online searches and fanart download, per kind of media, and the online libraries' languages.
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
  /// whether cached artwork copies are kept, and in which language the online libraries answer - not the
  /// extractors' tuning like year-matching patterns, which the reference also kept out of its settings
  /// window. And what takes effect when is said plainly: these are read when the extractors work, so a change
  /// speaks to the next import pass, not to the library as it stands - the flag
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

    /// <summary>
    /// One field of the answer, and where it lives: which settings class holds it and which property of that
    /// class spells it. Resolved by reflection at request time, so this assembly never names an extractor.
    /// </summary>
    private readonly struct FieldMap
    {
      internal FieldMap(string block, string typeName, string property, string field, bool text = false)
      {
        Block = block;
        TypeName = typeName;
        Property = property;
        Field = field;
        Text = text;
      }

      internal string Block { get; }
      internal string TypeName { get; }
      internal string Property { get; }
      internal string Field { get; }

      /// <summary>Whether the property is a language name rather than a switch.</summary>
      internal bool Text { get; }
    }

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

      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.SkipOnlineSearches), nameof(MovieFlags.SkipOnlineSearches)),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.SkipFanArtDownload), nameof(MovieFlags.SkipFanArtDownload)),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.OnlyLocalMedia), nameof(MovieFlags.OnlyLocalMedia)),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.CacheOfflineFanArt), nameof(MovieFlags.CacheOfflineFanArt)),
      new FieldMap("series", SERIES_TYPE, nameof(MovieFlags.CacheLocalFanArt), nameof(MovieFlags.CacheLocalFanArt)),

      new FieldMap("audio", AUDIO_TYPE, nameof(MovieFlags.SkipOnlineSearches), nameof(MovieFlags.SkipOnlineSearches)),
      new FieldMap("audio", AUDIO_TYPE, nameof(MovieFlags.SkipFanArtDownload), nameof(MovieFlags.SkipFanArtDownload)),
      new FieldMap("audio", AUDIO_TYPE, nameof(MovieFlags.CacheOfflineFanArt), nameof(MovieFlags.CacheOfflineFanArt)),
      new FieldMap("audio", AUDIO_TYPE, nameof(MovieFlags.CacheLocalFanArt), nameof(MovieFlags.CacheLocalFanArt)),

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
      new FieldMap("online", ONLINE_TYPE, nameof(OnlineFlags.MovieLanguageCulture), nameof(OnlineFlags.MovieLanguageCulture), true),
      new FieldMap("online", ONLINE_TYPE, nameof(OnlineFlags.SeriesLanguageCulture), nameof(OnlineFlags.SeriesLanguageCulture), true),
      new FieldMap("online", ONLINE_TYPE, nameof(OnlineFlags.MusicLanguageCulture), nameof(OnlineFlags.MusicLanguageCulture), true),
      new FieldMap("online", ONLINE_TYPE, nameof(OnlineFlags.SubtitleLanguageCulture), nameof(OnlineFlags.SubtitleLanguageCulture), true)
    };

    #endregion

    #region The shapes of the answer and the wish

    /// <summary>
    /// The switches of one extractor. Every field is nullable in both directions: null in an answer means the
    /// server has no such field to speak of (the extractor is not installed, or the block does not carry that
    /// switch), and null in a request means "leave this one as it is".
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
    /// fields that are present - and the answer is the whole state after the change.
    /// </summary>
    [HttpPut("settings/metadata")]
    public ActionResult<MetadataSettingsResult> PutMetadataSettings([FromBody] MetadataSettingsRequest request)
    {
      if (request == null)
        return BadRequest(new ErrorResponse { Error = "no settings were sent" });

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

          if (wished)
          {
            object wish = map.Text ? (object)(wishText ?? "").Trim() : wishFlag.Value;
            if (map.Text)
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

          if (map.Text)
            online["online"] ??= new OnlineFlags();
          else
            aspects[map.Block] ??= new MovieFlags();

          Apply(map, online["online"], aspects[map.Block], stored);
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

    /// <summary>What the request asks of one field, if anything.</summary>
    private static void ReadWish(MetadataSettingsRequest request, FieldMap map,
        out bool wished, out bool? wishFlag, out string wishText)
    {
      wished = false;
      wishFlag = null;
      wishText = null;

      MovieFlags aspect = map.Block switch
      {
        "movie" => request.Movie,
        "series" => request.Series,
        "audio" => request.Audio,
        "video" => request.Video,
        "subtitle" => request.Subtitle,
        _ => null
      };
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
      }
    }

    /// <summary>Writes one read value into the answer block it belongs to.</summary>
    private static void Apply(FieldMap map, OnlineFlags libraries, MovieFlags aspect, object stored)
    {
      if (map.Text)
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
