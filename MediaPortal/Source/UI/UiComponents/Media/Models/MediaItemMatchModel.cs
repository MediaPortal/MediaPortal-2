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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Provides a workflow model for selecting matching media items.
  /// </summary>
  public class MediaItemMatchModel : IDisposable
  {
    #region Consts

    public const string STR_MODEL_ID_MIMATCH = "692FA8C3-41A5-43DD-8C12-C857C9C75E72";
    public static readonly Guid MODEL_ID_MIMATCH = new Guid(STR_MODEL_ID_MIMATCH);

    public const string KEY_NAME = "Name";
    public const string KEY_INFO = "Info";
    public const string KEY_ASPECTS = "Aspects";

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected ItemsList _matchList = null;
    protected object _searchItem = null;
    protected bool _isVirtual = false;
    protected System.Timers.Timer _liveSearchTimer = new System.Timers.Timer(3000);

    protected AbstractProperty _isSearchingProperty;
    protected AbstractProperty _isManualSearchProperty;
    protected AbstractProperty _isAutomaticSearchProperty;
    protected AbstractProperty _selectedInfoProperty;
    protected AbstractProperty _focusedItemProperty;
    protected AbstractProperty _manualIdProperty;
    protected AsynchronousMessageQueue _messageQueue = null;

    protected List<Regex> _episodeRegEx = new List<Regex>
    {
      new Regex(@"(?<series>[^\\]+)\WS(?<seasonnum>\d+)[\s|\.|\-|_]{0,1}E((?<episodenum>\d+)[\-_]?)+", RegexOptions.IgnoreCase),
      new Regex(@"(?<series>[^\\]+)\WS(?<seasonnum>\d+)[\s|\.|\-|_]{0,1}E((?<episodenum>\d+)[\-_]?)+E(?<endepisodenum>\d+)+", RegexOptions.IgnoreCase),
    };
    protected Regex _titleYearRegEx = new Regex(@"(?<title>[^\\|\/]+?)\s*[\[\(]?(?<year>(19|20)\d{2})", RegexOptions.IgnoreCase);

    protected TaskCompletionSource<IEnumerable<MediaItemAspect>> _selectionComplete = null;
    protected IEnumerable<MediaItemAspect> _matchedAspects = null;
    protected readonly IEnumerable<Guid> _wantedAspects = new Guid[] { ExternalIdentifierAspect.ASPECT_ID, MediaAspect.ASPECT_ID, MovieAspect.ASPECT_ID,
      SeriesAspect.ASPECT_ID, EpisodeAspect.ASPECT_ID, AudioAlbumAspect.ASPECT_ID, AudioAspect.ASPECT_ID, VideoAspect.ASPECT_ID, ReimportAspect.ASPECT_ID };

    #endregion

    #region Ctor

    public MediaItemMatchModel()
    {
      _isSearchingProperty = new WProperty(typeof(bool), false);
      _isManualSearchProperty = new WProperty(typeof(bool), false);
      _isAutomaticSearchProperty = new WProperty(typeof(bool), true);
      _isAutomaticSearchProperty.Attach(OnAutomaticSearchChanged);
      _selectedInfoProperty = new WProperty(typeof(string), String.Empty);
      _manualIdProperty = new WProperty(typeof(string), String.Empty);
      _manualIdProperty.Attach(OnManualIdChanged);
      _focusedItemProperty = new SProperty(typeof(object), null);
      _focusedItemProperty.Attach(OnItemFocusedChanged);

      _liveSearchTimer.AutoReset = false;
      _liveSearchTimer.Elapsed += LiveSearchTimeout_Elapsed;
      _matchList = new ItemsList();
      _selectionComplete = new TaskCompletionSource<IEnumerable<MediaItemAspect>>();
    }

    private void LiveSearchTimeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      SetManualMatch();
    }

    public void Dispose()
    {
      _matchList = null;
  }

    #endregion

    #region Public properties (Also accessed from the GUI)

    public ItemsList MatchList
    {
      get
      {
        lock (_syncObj)
          return _matchList;
      }
    }

    public AbstractProperty IsSearchingProperty
    {
      get { return _isSearchingProperty; }
    }

    public bool IsSearching
    {
      get { return (bool)_isSearchingProperty.GetValue(); }
      set { _isSearchingProperty.SetValue(value); }
    }

    public AbstractProperty IsManualSearchProperty
    {
      get { return _isManualSearchProperty; }
    }

    public bool IsManualSearch
    {
      get { return (bool)_isManualSearchProperty.GetValue(); }
      set { _isManualSearchProperty.SetValue(value); }
    }

    public AbstractProperty IsAutomaticSearchProperty
    {
      get { return _isAutomaticSearchProperty; }
    }

    public bool IsAutomaticSearch
    {
      get { return (bool)_isAutomaticSearchProperty.GetValue(); }
      set { _isAutomaticSearchProperty.SetValue(value); }
    }

    public AbstractProperty SelectedInformationProperty
    {
      get { return _selectedInfoProperty; }
    }

    public string SelectedInformation
    {
      get { return (string)_selectedInfoProperty.GetValue(); }
      set { _selectedInfoProperty.SetValue(value); }
    }

    public AbstractProperty ManualIdProperty
    {
      get { return _manualIdProperty; }
    }

    public string ManualId
    {
      get { return (string)_manualIdProperty.GetValue(); }
      set { _manualIdProperty.SetValue(value); }
    }

    public AbstractProperty FocusedItemProperty
    {
      get { return _focusedItemProperty; }
    }

    public object FocusedItem
    {
      get { return _focusedItemProperty.GetValue(); }
      set { _focusedItemProperty.SetValue(value); }
    }

    #endregion

    #region Public methods

    public bool IsValidMediaItem(MediaItem mediaItem)
    {
      if (mediaItem == null)
        return false;

      if (mediaItem.IsStub)
        return false;

      if (mediaItem.Aspects.ContainsKey(VideoAspect.ASPECT_ID) || mediaItem.Aspects.ContainsKey(AudioAlbumAspect.ASPECT_ID) ||
        mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID) || mediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
        return true;

      return false;
    }

    public async Task OpenSelectMatchDialogAsync(MediaItem mediaItem)
    {
      ClearData();
      if (!IsValidMediaItem(mediaItem))
      {
        _selectionComplete.SetResult(null);
        return;
      }

      _searchItem = null;
      if (mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
      {
        MovieInfo info = new MovieInfo();
        info.FromMetadata(mediaItem.Aspects);
        _searchItem = info;
      }
      else if (mediaItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
      {
        EpisodeInfo info = new EpisodeInfo();
        info.FromMetadata(mediaItem.Aspects);
        _searchItem = info;
      }
      else if (mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        TrackInfo info = new TrackInfo();
        info.FromMetadata(mediaItem.Aspects);
        _searchItem = info;
      }
      else if (mediaItem.Aspects.ContainsKey(AudioAlbumAspect.ASPECT_ID))
      {
        AlbumInfo info = new AlbumInfo();
        info.FromMetadata(mediaItem.Aspects);
        _searchItem = info;
      }
      else if (mediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
      {
        SeriesInfo info = new SeriesInfo();
        info.FromMetadata(mediaItem.Aspects);
        _searchItem = info;
      }
      else if (mediaItem.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
      {
        IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
        IContentDirectory cd = scm?.ContentDirectory;
        var shares = await cd?.GetSharesAsync(null, SharesFilter.All);
        IList<MultipleMediaItemAspect> providerResourceAspects;
        if (shares != null && MediaItemAspect.TryGetAspects(mediaItem.Aspects, ProviderResourceAspect.Metadata, out providerResourceAspects))
        {
          var pra = providerResourceAspects.FirstOrDefault(p => p.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_PRIMARY);
          if (pra != null)
          {
            var resPath = ResourcePath.Deserialize(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH));
            var share = shares.FirstOrDefault(s => s.BaseResourcePath.IsSameOrParentOf(resPath));
            if (share?.MediaCategories?.Contains("Movie") ?? false)
            {
              MovieInfo info = new MovieInfo();
              info.FromMetadata(mediaItem.Aspects);
              _searchItem = info;
              ServiceRegistration.Get<ILogger>().Info("MediaItemMatchModel: Found video item '{0}' to be a movie", resPath);
            }
            else if (share?.MediaCategories?.Contains("Series") ?? false)
            {
              EpisodeInfo info = new EpisodeInfo();
              info.FromMetadata(mediaItem.Aspects);
              _searchItem = info;
              ServiceRegistration.Get<ILogger>().Info("MediaItemMatchModel: Found video item '{0}' to be a series episode", resPath);
            }
          }
        }
      }
      if(_searchItem == null)
      {
        ServiceRegistration.Get<ILogger>().Error("Error reimporting media item '{0}'. No valid aspects found.", mediaItem.MediaItemId);
        _selectionComplete.SetResult(null);
        return;
      }
      _isVirtual = mediaItem.IsVirtual;

      _matchedAspects = null;
      _selectionComplete = new TaskCompletionSource<IEnumerable<MediaItemAspect>>();
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseMatch", async(s, g) =>
      {
        if (_matchedAspects != null)
        {
          IDictionary<Guid, IList<MediaItemAspect>> aspects = MediaItemAspect.GetAspects(_matchedAspects);

          //Download detailed information if possible. The server might no be allowed to use online sources.
          if (mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
          {
            MovieInfo info = new MovieInfo();
            info.FromMetadata(MediaItemAspect.GetAspects(_matchedAspects));
            await OnlineMatcherService.Instance.FindAndUpdateMovieAsync(info);
            info.SetMetadata(aspects, true);
          }
          else if (mediaItem.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
          {
            EpisodeInfo info = new EpisodeInfo();
            info.FromMetadata(MediaItemAspect.GetAspects(_matchedAspects));
            await OnlineMatcherService.Instance.FindAndUpdateEpisodeAsync(info);
            info.SetMetadata(aspects, true);
          }
          else if (mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
          {
            TrackInfo info = new TrackInfo();
            info.FromMetadata(MediaItemAspect.GetAspects(_matchedAspects));
            await OnlineMatcherService.Instance.FindAndUpdateTrackAsync(info);
            info.SetMetadata(aspects, true);
          }
          _selectionComplete.SetResult(aspects.Where(a => _wantedAspects.Contains(a.Key)).SelectMany(a => a.Value));
        }
        else
        {
          _selectionComplete.SetResult(null);
        }
      });
      await DoSearchAsync();
    }

    protected async Task DoSearchAsync()
    {
      try
      {
        IsSearching = true;
        _matchList.Clear();
        SelectedInformation = "";

        MovieInfo movieSearchinfo = null;
        EpisodeInfo episodeSearchinfo = null;
        TrackInfo trackSearchinfo = null;
        SeriesInfo seriesSearchinfo = null;
        AlbumInfo albumSearchinfo = null;
        if (IsManualSearch)
        {
          if (_searchItem is MovieInfo movie)
          {
            movieSearchinfo = new MovieInfo();
            if (ManualId.StartsWith("tt", StringComparison.InvariantCultureIgnoreCase))
              movieSearchinfo.ImdbId = ManualId;
            else if (int.TryParse(ManualId, out int movieDbId))
              movieSearchinfo.MovieDbId = movieDbId;
            else //Fallabck to name search
            {
              var match = _titleYearRegEx.Match(ManualId);
              if (match.Success)
              {
                movieSearchinfo.MovieName = match.Groups["title"].Value;
                if (int.TryParse(match.Groups["year"].Value, out int year))
                  movieSearchinfo.ReleaseDate = new DateTime(year, 1, 1);
              }
              else
              {
                movieSearchinfo.MovieName = ManualId;
              }
            }
          }
          else if (_searchItem is EpisodeInfo episode)
          {
            episodeSearchinfo = new EpisodeInfo();
            episodeSearchinfo.SeasonNumber = episode.SeasonNumber;
            episodeSearchinfo.EpisodeNumbers = episode.EpisodeNumbers;
            if (ManualId.StartsWith("tt", StringComparison.InvariantCultureIgnoreCase))
              episodeSearchinfo.SeriesImdbId = ManualId;
            else if (int.TryParse(ManualId, out int tvDbSeriesId))
              episodeSearchinfo.SeriesTvdbId = tvDbSeriesId;
            else //Fallabck to name search
            {
              var match = _episodeRegEx.FirstOrDefault(r => r.IsMatch(ManualId))?.Match(ManualId);
              if (match?.Success == true)
              {
                episodeSearchinfo.SeriesName = match.Groups["series"].Value;
                if (int.TryParse(match.Groups["seasonnum"].Value, out int season))
                  episodeSearchinfo.SeasonNumber = season;
                var group = match.Groups["episodenum"];
                if (group.Length > 0)
                {
                  episodeSearchinfo.EpisodeNumbers.Clear();
                  if (group.Captures.Count > 1)
                  {
                    foreach (Capture capture in group.Captures)
                    {
                      if (int.TryParse(capture.Value, out int episodeNum))
                        episodeSearchinfo.EpisodeNumbers.Add(episodeNum);
                    }
                  }
                  else if (match.Groups["endepisodenum"].Length > 0)
                  {
                    int start;
                    if (int.TryParse(group.Value, out start))
                    {
                      group = match.Groups["endepisodenum"];
                      if (group.Length > 0 && int.TryParse(group.Value, out int end))
                      {
                        for (int e = start; e <= end; e++)
                          episodeSearchinfo.EpisodeNumbers.Add(e);
                      }
                    }
                  }
                  else
                  {
                    foreach (Capture capture in group.Captures)
                    {
                      if (int.TryParse(capture.Value, out int episodeNum))
                        episodeSearchinfo.EpisodeNumbers.Add(episodeNum);
                    }
                  }
                }
                else
                {
                  episodeSearchinfo.SeriesName = ManualId;
                }
              }
            }
          }
          else if (_searchItem is TrackInfo track)
          {
            trackSearchinfo = new TrackInfo();
            if (Guid.TryParse(ManualId, out Guid guid))
              trackSearchinfo.MusicBrainzId = ManualId;
            else if (int.TryParse(ManualId, out int audioDbId))
              trackSearchinfo.AudioDbId = audioDbId;
            else //Fallabck to name search
              trackSearchinfo.TrackName = ManualId;
          }
          else if (_searchItem is SeriesInfo series)
          {
            seriesSearchinfo = new SeriesInfo();
            if (ManualId.StartsWith("tt", StringComparison.InvariantCultureIgnoreCase))
              seriesSearchinfo.ImdbId = ManualId;
            else if (int.TryParse(ManualId, out int tvDbSeriesId))
              seriesSearchinfo.TvdbId = tvDbSeriesId;
            else //Fallabck to name search
            {
              var match = _titleYearRegEx.Match(ManualId);
              if (match.Success)
              {
                seriesSearchinfo.SeriesName = match.Groups["title"].Value;
                if (int.TryParse(match.Groups["year"].Value, out int year))
                  seriesSearchinfo.FirstAired = new DateTime(year, 1, 1);
              }
              else
              {
                seriesSearchinfo.SeriesName = ManualId;
              }
            }
          }
          else if (_searchItem is AlbumInfo album)
          {
            albumSearchinfo = new AlbumInfo();
            if (Guid.TryParse(ManualId, out Guid guid))
              albumSearchinfo.MusicBrainzGroupId = ManualId;
            else if (int.TryParse(ManualId, out int audioDbId))
              albumSearchinfo.AudioDbId = audioDbId;
            else //Fallabck to name search
            {
              var match = _titleYearRegEx.Match(ManualId);
              if (match.Success)
              {
                albumSearchinfo.Album = match.Groups["title"].Value;
                if (int.TryParse(match.Groups["year"].Value, out int year))
                  albumSearchinfo.ReleaseDate = new DateTime(year, 1, 1);
              }
              else
              {
                albumSearchinfo.Album = ManualId;
              }
            }
          }
        }
        else
        {
          if (_searchItem is MovieInfo)
          {
            movieSearchinfo = (MovieInfo)_searchItem;
          }
          else if (_searchItem is EpisodeInfo)
          {
            episodeSearchinfo = (EpisodeInfo)_searchItem;
          }
          else if (_searchItem is TrackInfo)
          {
            trackSearchinfo = (TrackInfo)_searchItem;
          }
          else if (_searchItem is SeriesInfo)
          {
            seriesSearchinfo = (SeriesInfo)_searchItem;
          }
          else if (_searchItem is AlbumInfo)
          {
            albumSearchinfo = (AlbumInfo)_searchItem;
          }
        }

        IEnumerable<object> matches = new List<object>();
        if (_searchItem is MovieInfo)
          matches = await OnlineMatcherService.Instance.FindMatchingMoviesAsync(movieSearchinfo);
        else if (_searchItem is EpisodeInfo)
          matches = await OnlineMatcherService.Instance.FindMatchingEpisodesAsync(episodeSearchinfo);
        else if (_searchItem is TrackInfo)
          matches = await OnlineMatcherService.Instance.FindMatchingTracksAsync(trackSearchinfo);
        else if (_searchItem is SeriesInfo)
          matches = await OnlineMatcherService.Instance.FindMatchingSeriesAsync(seriesSearchinfo);
        else if (_searchItem is AlbumInfo)
          matches = await OnlineMatcherService.Instance.FindMatchingAlbumsAsync(albumSearchinfo);

        IsSearching = false;

        foreach (BaseInfo info in matches)
        {
          var item = CreateItem(info);
          if (item != null)
            _matchList.Add(item);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error reimporting media item '{0}'", ex, _searchItem?.ToString() ?? "?");
      }
      finally
      {
        IsSearching = false;
        _matchList.FireChange();
      }
    }

    public void SetMatch(ListItem item)
    {
      if (item == null)
        return;

      _matchedAspects = (IEnumerable<MediaItemAspect>)item.AdditionalProperties[KEY_ASPECTS];
      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
      ClearData();
    }

    public async void SetManualMatch()
    {
      if (string.IsNullOrWhiteSpace(ManualId))
        return;

      await DoSearchAsync();
    }

    public Task<IEnumerable<MediaItemAspect>> WaitForMatchSelectionAsync()
    {
      return _selectionComplete.Task;
    }

    #endregion

    #region Private and protected methods

    protected ListItem CreateItem(BaseInfo item)
    {
      ListItem listItem = new ListItem();
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      if (item is MovieInfo movie)
      {
        listItem.SetLabel(KEY_NAME, $"{movie.MovieName.Text}{(movie.ReleaseDate ==  null ? "" : $" ({movie.ReleaseDate.Value.Year})")}" +
          $"{(string.IsNullOrWhiteSpace(movie.OriginalName) || string.Compare(movie.MovieName.Text, movie.OriginalName, true) == 0 ? "" : $" [{movie.OriginalName}]")}");
        StringBuilder infoText = new StringBuilder();
        if (!string.IsNullOrEmpty(movie.ImdbId))
          infoText.Append($"imdb.com: {movie.ImdbId}\n");
        if (movie.MovieDbId > 0)
          infoText.Append($"themoviedb.org: {movie.MovieDbId}\n");
        if (!movie.Summary.IsEmpty)
          infoText.Append(movie.Summary.Text);

        listItem.SetLabel(KEY_INFO, infoText.ToString());
        movie.SetMetadata(aspects, true);
      }
      else if (item is EpisodeInfo episode)
      {
        listItem.SetLabel(KEY_NAME, $"{episode.SeriesName}{(episode.SeriesFirstAired == null || episode.SeriesName.Text.EndsWith($"({episode.SeriesFirstAired.Value.Year})") ? "" : $" ({episode.SeriesFirstAired.Value.Year})")}" +
          $" S{(episode.SeasonNumber.HasValue ? episode.SeasonNumber.Value.ToString("00") : "??")}{(episode.EpisodeNumbers.Count > 0 ? string.Join("", episode.EpisodeNumbers.Select(e => "E" + e.ToString("00"))) : "E??")}" +
          $"{(episode.EpisodeName.IsEmpty ? "" : $": {episode.EpisodeName.Text}" )}");
        StringBuilder infoText = new StringBuilder();
        if (episode.TvdbId > 0)
          infoText.Append($"thetvdb.com: {episode.TvdbId}\n");
        if (!string.IsNullOrEmpty(episode.ImdbId))
          infoText.Append($"imdb.com: {episode.ImdbId}\n");
        if (episode.MovieDbId > 0)
          infoText.Append($"themoviedb.org: {episode.MovieDbId}\n");
        if (!episode.Summary.IsEmpty)
          infoText.Append(episode.Summary.Text);

        listItem.SetLabel(KEY_INFO, infoText.ToString());
        episode.SetMetadata(aspects, true);
      }
      else if (item is TrackInfo track)
      {
        listItem.SetLabel(KEY_NAME, $"{(string.IsNullOrWhiteSpace(track.Album) ? "" : $"{track.Album}: ")}{track.TrackName}" +
          $"{(track.Artists.Count > 0 ? $" [{string.Join(", ", track.Artists)}]" : "")}");
        StringBuilder infoText = new StringBuilder();
        if (!string.IsNullOrEmpty(track.MusicBrainzId))
          infoText.Append($"musicbrainz.org: {track.MusicBrainzId}\n");
        if (track.AudioDbId > 0)
          infoText.Append($"theaudiodb.com: {track.AudioDbId}\n");
        if (track.ReleaseDate.HasValue)
          infoText.Append(track.ReleaseDate.Value.ToShortDateString());
        
        listItem.SetLabel(KEY_INFO, infoText.ToString());
        track.SetMetadata(aspects, true);
      }
      else if (item is SeriesInfo series)
      {
        listItem.SetLabel(KEY_NAME, $"{series.SeriesName}{(series.FirstAired == null || series.SeriesName.Text.EndsWith($"({series.FirstAired.Value.Year})") ? "" : $" ({series.FirstAired.Value.Year})")}");
        StringBuilder infoText = new StringBuilder();
        if (series.TvdbId > 0)
          infoText.Append($"thetvdb.com: {series.TvdbId}\n");
        if (!string.IsNullOrEmpty(series.ImdbId))
          infoText.Append($"imdb.com: {series.ImdbId}\n");
        if (series.MovieDbId > 0)
          infoText.Append($"themoviedb.org: {series.MovieDbId}\n");
        if (!series.Description.IsEmpty)
          infoText.Append(series.Description.Text);

        listItem.SetLabel(KEY_INFO, infoText.ToString());
        series.SetMetadata(aspects, true);
      }
      else if (item is AlbumInfo album)
      {
        listItem.SetLabel(KEY_NAME, $"{album.Album}{(album.ReleaseDate.HasValue ? $" ({album.ReleaseDate.Value.Year})" : "")}" +
          $"{(album.Artists.Count > 0 ? $" [{string.Join(", ", album.Artists)}]" : "")}");
        StringBuilder infoText = new StringBuilder();
        if (!string.IsNullOrEmpty(album.MusicBrainzGroupId))
          infoText.Append($"musicbrainz.org: {album.MusicBrainzGroupId}\n");
        if (album.AudioDbId > 0)
          infoText.Append($"theaudiodb.com: {album.AudioDbId}\n");
        if (!album.Description.IsEmpty)
          infoText.Append(album.Description.Text);

        listItem.SetLabel(KEY_INFO, infoText.ToString());
        album.SetMetadata(aspects, true);
      }
      if (aspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
      {
        if (_wantedAspects.Contains(MediaAspect.ASPECT_ID))
          MediaItemAspect.SetAttribute(aspects, MediaAspect.ATTR_ISVIRTUAL, _isVirtual);
        var reimportAspect = MediaItemAspect.GetOrCreateAspect(aspects, ReimportAspect.Metadata);
        reimportAspect.SetAttribute(ReimportAspect.ATTR_SEARCH, ManualId);
        listItem.AdditionalProperties[KEY_ASPECTS] = aspects.Where(a => _wantedAspects.Contains(a.Key)).SelectMany(a => a.Value);
        return listItem;
      }
      return null;
    }

    async void OnAutomaticSearchChanged(AbstractProperty prop, object oldVal)
    {
      if (prop.HasValue() && (bool)prop.GetValue() && (bool)oldVal == false)
      {
        await DoSearchAsync();
      }
    }

    void OnManualIdChanged(AbstractProperty prop, object oldVal)
    {
      if (prop.HasValue() && !string.IsNullOrWhiteSpace((string)prop.GetValue()))
      {
        _liveSearchTimer.Stop();
        _liveSearchTimer.Start();
      }
    }

    void OnItemFocusedChanged(AbstractProperty prop, object oldVal)
    {
      if (prop.HasValue())
        SelectedInformation = ((ListItem)prop.GetValue())?.Labels[KEY_INFO].Evaluate() ?? "";
    }

    protected void ClearData()
    {
      lock (_syncObj)
      {
        _matchList.Clear();
        _searchItem = null;
        SelectedInformation = String.Empty;
      }
    }

    protected void DisconnectedError()
    {
      // Called when a remote call crashes because the server was disconnected. We don't do anything here because
      // we automatically move to the overview state in the OnMessageReceived method when the server disconnects.
    }

    #endregion
  }
}
