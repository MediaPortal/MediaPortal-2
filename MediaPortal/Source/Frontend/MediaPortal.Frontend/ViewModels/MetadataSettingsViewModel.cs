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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One switch or one language on the metadata screen. A row exists only because the server said it has
  /// that field - a screen never shows a switch for an extractor this server does not have.
  /// </summary>
  public sealed partial class MetadataRow : ObservableObject
  {
    public MetadataRow(string block, string field, string label, bool isText, bool isOn, string text)
    {
      Block = block;
      Field = field;
      Label = label;
      IsText = isText;
      _isOn = isOn;
      _text = text;
    }

    /// <summary>Which extractor the value lives in: movie, series, audio, video, subtitle, online.</summary>
    public string Block { get; }

    /// <summary>Which property of that extractor's settings the value is.</summary>
    public string Field { get; }

    /// <summary>What the screen prints beside it.</summary>
    public string Label { get; }

    /// <summary>Whether the editor is a text box (a language) rather than a checkbox.</summary>
    public bool IsText { get; }

    /// <summary>The inverse, for the view's visibility bindings.</summary>
    public bool IsToggle => !IsText;

    [ObservableProperty]
    private bool _isOn;

    [ObservableProperty]
    private string _text;
  }

  /// <summary>
  /// What the server asks the internet - and itself - when it builds the library: the extractors' online
  /// searches and artwork download per kind of media, and the language the online libraries answer in.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The row loads itself when it opens, like the server settings row beside it, and it says so when the
  /// server has no such endpoint rather than showing an empty form that would save nonsense. What the server
  /// does not have is not shown at all: the answer leaves a block out when its extractor is not installed,
  /// and a row is only ever built around a value that arrived.
  /// </para>
  /// <para>
  /// Saving sends the fields this screen actually shows, and only those; everything else stays as it is on
  /// the server. And what a save changes takes effect with the next import pass - the library as it stands
  /// keeps what it learned - which the status line repeats, because a screen of switches that promises
  /// "saved" without saying when it speaks invites a viewer to re-import in vain.
  /// </para>
  /// </remarks>
  public sealed partial class MetadataSettingsViewModel : ObservableObject
  {
    private readonly Func<IMetadataSettingsApi?> _apiProvider;
    private bool _loaded;

    [ObservableProperty]
    private string _status = "";

    public MetadataSettingsViewModel(Func<IMetadataSettingsApi?> apiProvider)
    {
      _apiProvider = apiProvider ?? throw new ArgumentNullException(nameof(apiProvider));
    }

    /// <summary>Every field this server spoke of, in reading order: movies, series, music, videos, subtitles, online.</summary>
    public ObservableCollection<MetadataRow> Rows { get; } = new ObservableCollection<MetadataRow>();

    /// <summary>Read the extractors' settings. Called when the row opens, and by ReloadCommand.</summary>
    [RelayCommand]
    public async Task ReloadAsync()
    {
      IMetadataSettingsApi? api = _apiProvider();
      if (api == null)
      {
        Status = "No server address yet - set one under \"Server address\" first.";
        return;
      }

      try
      {
        RebuildRows(await api.GetMetadataSettingsAsync());
        _loaded = true;
        Status = "Loaded from the server. What changes here speaks to the next import pass.";
      }
      catch (ServerApiException e)
      {
        Status = e.StatusCode == 404 || e.StatusCode == 405
            ? "This server does not offer its metadata settings over the API - it is probably an older build."
            : "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    /// <summary>Fill the rows from an answer without contacting the server (tests).</summary>
    public void LoadFrom(MetadataSettingsAnswer answer)
    {
      RebuildRows(answer);
      _loaded = true;
      Status = "Loaded from the server. What changes here speaks to the next import pass.";
    }

    /// <summary>Send every field the screen shows and have the server store it.</summary>
    /// <remarks>
    /// Refuses to save a row that never loaded - an empty wish cannot change much, but a half-read screen
    /// saving over a server somebody else just configured is the accident this rule is for.
    /// </remarks>
    [RelayCommand]
    public async Task SaveAsync()
    {
      if (!_loaded)
      {
        Status = "Nothing loaded from the server yet - reload first, then save.";
        return;
      }

      IMetadataSettingsApi? api = _apiProvider();
      if (api == null)
      {
        Status = "No server address yet.";
        return;
      }

      try
      {
        RebuildRows(await api.PutMetadataSettingsAsync(BuildWish()));
        Status = "Saved on the server. It speaks to the next import pass, not to the library as it stands.";
      }
      catch (ServerApiException e)
      {
        Status = e.StatusCode == 400
            ? "The server rejected the values: " + e.Message
            : "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    #region Building rows out of answers

    /// <summary>
    /// The switches of one extractor, with the words the screen shows for each. The labels keep the server's
    /// own polarity - a ticked box is the flag being on - because a screen that quietly inverts a flag is the
    /// one thing a settings screen must never be.
    /// </summary>
    private void AddAspect(string block, string aspect, MetadataFlags? flags)
    {
      if (flags == null)
        return;

      AddFlag(block, nameof(MetadataFlags.SkipOnlineSearches), Rows,
          aspect + " - skip the searches on the internet");
      AddFlag(block, nameof(MetadataFlags.SkipFanArtDownload), Rows,
          aspect + " - skip the artwork download");
      AddFlag(block, nameof(MetadataFlags.OnlyLocalMedia), Rows,
          aspect + " - take collection entries only from the files beside the media");
      AddFlag(block, nameof(MetadataFlags.CacheOfflineFanArt), Rows,
          aspect + " - keep own copies of artwork that sits on drives which sleep");
      AddFlag(block, nameof(MetadataFlags.CacheLocalFanArt), Rows,
          aspect + " - keep own copies of the artwork that sits beside the media");
      AddFlag(block, nameof(MetadataFlags.CacheOfflineMovieFanArt), Rows,
          aspect + " - keep own copies of the movies' artwork on drives which sleep");
      AddFlag(block, nameof(MetadataFlags.CacheLocalMovieFanArt), Rows,
          aspect + " - keep own copies of the movies' artwork beside the media");
      AddFlag(block, nameof(MetadataFlags.CacheOfflineSeriesFanArt), Rows,
          aspect + " - keep own copies of the series' artwork on drives which sleep");
      AddFlag(block, nameof(MetadataFlags.CacheLocalSeriesFanArt), Rows,
          aspect + " - keep own copies of the series' artwork beside the media");
    }

    private void AddOnline(MetadataFlags? flags)
    {
      if (flags == null)
        return;

      AddFlag("online", nameof(MetadataFlags.OnlyBasicFanArt), Rows,
          "Artwork - only the basic kinds: posters, banners, backdrops, thumbnails");
      AddFlag("online", nameof(MetadataFlags.UseSecureWebCommunication), Rows,
          "Online libraries - use https where they offer it");
      AddFlag("online", nameof(MetadataFlags.UseMovieAudioLanguageIfUnmatched), Rows,
          "Movies - take the file's own audio language when none of the preferred ones matches");
      AddFlag("online", nameof(MetadataFlags.UseSeriesAudioLanguageIfUnmatched), Rows,
          "Series - take the file's own audio language when none of the preferred ones matches");
      AddFlag("online", nameof(MetadataFlags.UseMusicAudioLanguageIfUnmatched), Rows,
          "Music - take the file's own audio language when none of the preferred ones matches");
      AddText("online", nameof(MetadataFlags.MovieLanguageCulture), Rows,
          "Language the movie searches answer in");
      AddText("online", nameof(MetadataFlags.SeriesLanguageCulture), Rows,
          "Language the series searches answer in");
      AddText("online", nameof(MetadataFlags.MusicLanguageCulture), Rows,
          "Language the music searches answer in");
      AddText("online", nameof(MetadataFlags.SubtitleLanguageCulture), Rows,
          "Language the subtitle searches answer in");
    }

    private void AddFlag(string block, string field, System.Collections.Generic.ICollection<MetadataRow> into,
        string label)
    {
      bool? value = FlagValue(block, field);
      if (value.HasValue)
        Rows.Add(new MetadataRow(block, field, label, false, value.Value, ""));
    }

    private void AddText(string block, string field, System.Collections.Generic.ICollection<MetadataRow> into,
        string label)
    {
      MetadataFlags flags = Flags(block);
      string text = flags == null ? null : TextValue(flags, field);
      if (text != null)
        Rows.Add(new MetadataRow(block, field, label, true, false, text));
    }

    private void RebuildRows(MetadataSettingsAnswer answer)
    {
      Rows.Clear();
      // The rows are built out of the answer's values, and the value lookups below read it back; the answer
      // has to be the one being drawn while it is drawn.
      lastAnswer = answer;
      AddAspect("movie", "Movies", answer.Movie);
      AddAspect("series", "Series", answer.Series);
      AddAspect("audio", "Music", answer.Audio);
      AddAspect("video", "Home videos", answer.Video);
      AddAspect("subtitle", "Subtitles", answer.Subtitle);
      AddOnline(answer.OnlineLibraries);
    }

    private MetadataFlags Flags(string block) => block switch
    {
      "movie" => lastAnswer?.Movie,
      "series" => lastAnswer?.Series,
      "audio" => lastAnswer?.Audio,
      "video" => lastAnswer?.Video,
      "subtitle" => lastAnswer?.Subtitle,
      "online" => lastAnswer?.OnlineLibraries,
      _ => null
    };

    private MetadataSettingsAnswer lastAnswer;

    private bool? FlagValue(string block, string field)
    {
      MetadataFlags flags = Flags(block);
      if (flags == null)
        return null;

      return field switch
      {
        nameof(MetadataFlags.SkipOnlineSearches) => flags.SkipOnlineSearches,
        nameof(MetadataFlags.SkipFanArtDownload) => flags.SkipFanArtDownload,
        nameof(MetadataFlags.OnlyLocalMedia) => flags.OnlyLocalMedia,
        nameof(MetadataFlags.CacheOfflineFanArt) => flags.CacheOfflineFanArt,
        nameof(MetadataFlags.CacheLocalFanArt) => flags.CacheLocalFanArt,
        nameof(MetadataFlags.CacheOfflineMovieFanArt) => flags.CacheOfflineMovieFanArt,
        nameof(MetadataFlags.CacheLocalMovieFanArt) => flags.CacheLocalMovieFanArt,
        nameof(MetadataFlags.CacheOfflineSeriesFanArt) => flags.CacheOfflineSeriesFanArt,
        nameof(MetadataFlags.CacheLocalSeriesFanArt) => flags.CacheLocalSeriesFanArt,
        nameof(MetadataFlags.OnlyBasicFanArt) => flags.OnlyBasicFanArt,
        nameof(MetadataFlags.UseSecureWebCommunication) => flags.UseSecureWebCommunication,
        nameof(MetadataFlags.UseMovieAudioLanguageIfUnmatched) => flags.UseMovieAudioLanguageIfUnmatched,
        nameof(MetadataFlags.UseSeriesAudioLanguageIfUnmatched) => flags.UseSeriesAudioLanguageIfUnmatched,
        nameof(MetadataFlags.UseMusicAudioLanguageIfUnmatched) => flags.UseMusicAudioLanguageIfUnmatched,
        _ => null
      };
    }

    private static string TextValue(MetadataFlags flags, string field)
    {
      return field switch
      {
        nameof(MetadataFlags.MovieLanguageCulture) => flags.MovieLanguageCulture,
        nameof(MetadataFlags.SeriesLanguageCulture) => flags.SeriesLanguageCulture,
        nameof(MetadataFlags.MusicLanguageCulture) => flags.MusicLanguageCulture,
        nameof(MetadataFlags.SubtitleLanguageCulture) => flags.SubtitleLanguageCulture,
        _ => null
      };
    }

    #endregion

    #region Building the wish out of the rows

    private MetadataSettingsWish BuildWish()
    {
      return new MetadataSettingsWish
      {
        Movie = WishFor("movie"),
        Series = WishFor("series"),
        Audio = WishFor("audio"),
        Video = WishFor("video"),
        Subtitle = WishFor("subtitle"),
        OnlineLibraries = WishFor("online")
      };
    }

    /// <summary>
    /// The flags of one block as the rows now hold them. A field the screen has no row for stays null, and
    /// null tells the server to leave that one exactly as it is.
    /// </summary>
    private MetadataFlags WishFor(string block)
    {
      if (!Rows.Any(row => row.Block == block))
        return null;

      return new MetadataFlags
      {
        SkipOnlineSearches = Toggled(block, nameof(MetadataFlags.SkipOnlineSearches)),
        SkipFanArtDownload = Toggled(block, nameof(MetadataFlags.SkipFanArtDownload)),
        OnlyLocalMedia = Toggled(block, nameof(MetadataFlags.OnlyLocalMedia)),
        CacheOfflineFanArt = Toggled(block, nameof(MetadataFlags.CacheOfflineFanArt)),
        CacheLocalFanArt = Toggled(block, nameof(MetadataFlags.CacheLocalFanArt)),
        CacheOfflineMovieFanArt = Toggled(block, nameof(MetadataFlags.CacheOfflineMovieFanArt)),
        CacheLocalMovieFanArt = Toggled(block, nameof(MetadataFlags.CacheLocalMovieFanArt)),
        CacheOfflineSeriesFanArt = Toggled(block, nameof(MetadataFlags.CacheOfflineSeriesFanArt)),
        CacheLocalSeriesFanArt = Toggled(block, nameof(MetadataFlags.CacheLocalSeriesFanArt)),
        OnlyBasicFanArt = Toggled(block, nameof(MetadataFlags.OnlyBasicFanArt)),
        UseSecureWebCommunication = Toggled(block, nameof(MetadataFlags.UseSecureWebCommunication)),
        UseMovieAudioLanguageIfUnmatched =
            Toggled(block, nameof(MetadataFlags.UseMovieAudioLanguageIfUnmatched)),
        UseSeriesAudioLanguageIfUnmatched =
            Toggled(block, nameof(MetadataFlags.UseSeriesAudioLanguageIfUnmatched)),
        UseMusicAudioLanguageIfUnmatched =
            Toggled(block, nameof(MetadataFlags.UseMusicAudioLanguageIfUnmatched)),
        MovieLanguageCulture = Written(block, nameof(MetadataFlags.MovieLanguageCulture)),
        SeriesLanguageCulture = Written(block, nameof(MetadataFlags.SeriesLanguageCulture)),
        MusicLanguageCulture = Written(block, nameof(MetadataFlags.MusicLanguageCulture)),
        SubtitleLanguageCulture = Written(block, nameof(MetadataFlags.SubtitleLanguageCulture))
      };
    }

    private bool? Toggled(string block, string field)
    {
      MetadataRow row = Rows.FirstOrDefault(r => r.Block == block && r.Field == field && !r.IsText);
      return row == null ? (bool?)null : row.IsOn;
    }

    private string Written(string block, string field)
    {
      MetadataRow row = Rows.FirstOrDefault(r => r.Block == block && r.Field == field && r.IsText);
      return row == null ? null : (row.Text ?? "").Trim();
    }

    #endregion
  }
}
