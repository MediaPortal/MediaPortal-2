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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.Messaging;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Provides a workflow model for selecting matching media items.
  /// </summary>
  public class MediaItemMatchModel : IWorkflowModel, IDisposable
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

    protected AbstractProperty _isSearchingProperty;
    protected AbstractProperty _selectedInfoProperty;
    protected AbstractProperty _focusedItemProperty;
    protected AbstractProperty _manualIdProperty;
    protected AsynchronousMessageQueue _messageQueue = null;

    protected TaskCompletionSource<IEnumerable<MediaItemAspect>> _selectionComplete = null;

    #endregion

    #region Ctor

    public MediaItemMatchModel()
    {
      _isSearchingProperty = new WProperty(typeof(bool), false);
      _selectedInfoProperty = new WProperty(typeof(string), String.Empty);
      _manualIdProperty = new WProperty(typeof(string), String.Empty);
      _focusedItemProperty = new SProperty(typeof(object), null);
      _focusedItemProperty.Attach(OnItemFocusedChanged);

      _matchList = new ItemsList();
      _selectionComplete = new TaskCompletionSource<IEnumerable<MediaItemAspect>>();
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

    public async Task OpenSelectMatchDialogAsync(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      ClearData();
      if(!aspects.ContainsKey(MovieAspect.ASPECT_ID) && !aspects.ContainsKey(EpisodeAspect.ASPECT_ID) && !aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        _selectionComplete.SetResult(null);
        return;
      }

      IsSearching = true;
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseMatch");

      IEnumerable<object> matches  = new List<object>();
      if (aspects.ContainsKey(MovieAspect.ASPECT_ID))
      {
        MovieInfo info = new MovieInfo();
        info.FromMetadata(aspects);
        _searchItem = info;
        matches = await OnlineMatcherService.Instance.FindMatchingMoviesAsync(info);
      }
      else if (aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
      {
        EpisodeInfo info = new EpisodeInfo();
        info.FromMetadata(aspects);
        _searchItem = info;
        matches = await OnlineMatcherService.Instance.FindMatchingEpisodesAsync(info);
      }
      else if (aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        TrackInfo info = new TrackInfo();
        info.FromMetadata(aspects);
        _searchItem = info;
        matches = await OnlineMatcherService.Instance.FindMatchingTracksAsync(info);
      }

      IsSearching = false;
      foreach(BaseInfo info in matches)
      {
        var item = CreateItem(info);
        if (item != null)
          _matchList.Add(item);
      }
      _matchList.FireChange();
    }

    public void SetAutoMatch(ListItem item)
    {
      if (item == null)
        return;

      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
      _selectionComplete.SetResult((IEnumerable<MediaItemAspect>)item.AdditionalProperties[KEY_ASPECTS]);
      ClearData();
    }

    public void SetManualMatch()
    {
      if (string.IsNullOrWhiteSpace(ManualId))
        return;

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      Guid mediaAspectId = Guid.Empty;
      if (_searchItem is MovieInfo movie)
      {
        MovieInfo cleanMovie = new MovieInfo();
        cleanMovie.MovieName = " "; //To make SetMetadata store the aspects
        if (ManualId.StartsWith("tt", StringComparison.InvariantCultureIgnoreCase))
          cleanMovie.ImdbId = ManualId;
        else if (int.TryParse(ManualId, out int movieDbId))
          cleanMovie.MovieDbId = movieDbId;
        cleanMovie.SetMetadata(aspects);
        mediaAspectId = MovieAspect.ASPECT_ID;
      }
      else if (_searchItem is EpisodeInfo episode)
      {
        EpisodeInfo cleanEpisode = new EpisodeInfo();
        cleanEpisode.SeriesName = " "; //To make SetMetadata store the aspects
        cleanEpisode.SeasonNumber = episode.SeasonNumber;
        cleanEpisode.EpisodeNumbers = episode.EpisodeNumbers;
        if (int.TryParse(ManualId, out int tvDbSeriesId))
          cleanEpisode.SeriesTvdbId = tvDbSeriesId;
        cleanEpisode.SetMetadata(aspects);
        mediaAspectId = EpisodeAspect.ASPECT_ID;
      }
      else if (_searchItem is TrackInfo track)
      {
        TrackInfo cleanTrack = new TrackInfo();
        cleanTrack.TrackName = " "; //To make SetMetadata store the aspects
        if (ManualId.IndexOf("-", StringComparison.InvariantCultureIgnoreCase) > 2)
          cleanTrack.MusicBrainzId = ManualId;
        else if (int.TryParse(ManualId, out int audioDbId))
          cleanTrack.AudioDbId = audioDbId;
        cleanTrack.SetMetadata(aspects);
        mediaAspectId = AudioAspect.ASPECT_ID;
      }
      if (aspects?.ContainsKey(ExternalIdentifierAspect.ASPECT_ID) ?? false)
      {
        _selectionComplete.SetResult(aspects.Where(a => a.Key == ExternalIdentifierAspect.ASPECT_ID || a.Key == mediaAspectId).
          SelectMany(a => a.Value));
        ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
        ClearData();
      }
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
      if (item is MovieInfo movie)
      {
        listItem.SetLabel(KEY_NAME, $"{movie.MovieName.Text}{(movie.ReleaseDate ==  null ? "" : $" ({movie.ReleaseDate.Value.Year})")}" +
          $"{(string.IsNullOrWhiteSpace(movie.OriginalName) || string.Compare(movie.MovieName.Text, movie.OriginalName, true) == 0 ? "" : $" [{movie.OriginalName}]")}");
        StringBuilder infoText = new StringBuilder();
        if (!string.IsNullOrEmpty(movie.ImdbId))
          infoText.AppendLine($"imdb.com: {movie.ImdbId}");
        if (movie.MovieDbId > 0)
          infoText.AppendLine($"themoviedb.org: {movie.MovieDbId}");
        if (!movie.Summary.IsEmpty)
        {
          infoText.AppendLine("");
          infoText.AppendLine(movie.Summary.Text);
        }
        listItem.SetLabel(KEY_INFO, infoText.ToString());
        IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        movie.SetMetadata(aspects);
        if (aspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        {
          listItem.AdditionalProperties[KEY_ASPECTS] = aspects.Where(a => a.Key == ExternalIdentifierAspect.ASPECT_ID || a.Key == MovieAspect.ASPECT_ID).
            SelectMany(a => a.Value);
          return listItem;
        }
      }
      else if (item is EpisodeInfo episode)
      {
        listItem.SetLabel(KEY_NAME, $"{episode.SeriesName}{(episode.SeriesFirstAired == null ? "" : $" ({episode.SeriesFirstAired.Value.Year})")}" +
          $" S{(episode.SeasonNumber.HasValue ? episode.SeasonNumber.Value.ToString("00") : "??")}{(episode.EpisodeNumbers.Count > 0 ? string.Join("", episode.EpisodeNumbers.Select(e => "E" + e.ToString("00"))) : "E??")}" +
          $"{(episode.EpisodeName.IsEmpty ? "" : $": {episode.EpisodeName.Text}" )}");
        StringBuilder infoText = new StringBuilder();
        if (!string.IsNullOrEmpty(episode.SeriesImdbId))
          infoText.AppendLine($"imdb.com: {episode.SeriesImdbId}");
        if (!string.IsNullOrEmpty(episode.ImdbId))
          infoText.AppendLine($"imdb.com: {episode.ImdbId}");
        if (episode.SeriesMovieDbId > 0)
          infoText.AppendLine($"themoviedb.org: {episode.SeriesMovieDbId}");
        if (episode.MovieDbId > 0)
          infoText.AppendLine($"themoviedb.org: {episode.MovieDbId}");
        if (episode.SeriesTvdbId > 0)
          infoText.AppendLine($"thetvdb.com: {episode.SeriesTvdbId}");
        if (episode.TvdbId > 0)
          infoText.AppendLine($"thetvdb.com: {episode.TvdbId}");
        if (episode.SeriesTvMazeId > 0)
          infoText.AppendLine($"tvmaze.com: {episode.SeriesTvMazeId}");
        if (episode.TvMazeId > 0)
          infoText.AppendLine($"tvmaze.com: {episode.TvMazeId}");
        if (!episode.Summary.IsEmpty)
        {
          infoText.AppendLine("");
          infoText.AppendLine(episode.Summary.Text);
        }
        listItem.SetLabel(KEY_INFO, infoText.ToString());
        IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        episode.SetMetadata(aspects);
        if (aspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        {
          listItem.AdditionalProperties[KEY_ASPECTS] = aspects.Where(a => a.Key == ExternalIdentifierAspect.ASPECT_ID || a.Key == EpisodeAspect.ASPECT_ID).
            SelectMany(a => a.Value);
          return listItem;
        }
      }
      else if (item is TrackInfo track)
      {
        listItem.SetLabel(KEY_NAME, $"{(string.IsNullOrWhiteSpace(track.Album) ? "" : $"{track.Album}: ")}{track.TrackName}" +
          $"{(track.Artists.Count > 0 ? $" ({string.Join(", ", track.Artists)})" : "")}");
        StringBuilder infoText = new StringBuilder();
        if (!string.IsNullOrEmpty(track.AlbumMusicBrainzId))
          infoText.AppendLine($"musicbrainz.org: {track.AlbumMusicBrainzId}");
        if (!string.IsNullOrEmpty(track.MusicBrainzId))
          infoText.AppendLine($"musicbrainz.org: {track.MusicBrainzId}");
        if (track.AlbumAudioDbId > 0)
          infoText.AppendLine($"theaudiodb.com: {track.AlbumAudioDbId}");
        if (track.AudioDbId > 0)
          infoText.AppendLine($"theaudiodb.com: {track.AudioDbId}");
        if (track.ReleaseDate.HasValue)
        {
          infoText.AppendLine("");
          infoText.AppendLine(track.ReleaseDate.Value.ToShortDateString());
        }
        listItem.SetLabel(KEY_INFO, infoText.ToString());
        IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        track.SetMetadata(aspects);
        if (aspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        {
          listItem.AdditionalProperties[KEY_ASPECTS] = aspects.Where(a => a.Key == ExternalIdentifierAspect.ASPECT_ID || a.Key == AudioAspect.ASPECT_ID).
            SelectMany(a => a.Value);
          return listItem;
        }
      }
      return null;
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
        _selectionComplete = new TaskCompletionSource<IEnumerable<MediaItemAspect>>();
      }
    }

    protected void DisconnectedError()
    {
      // Called when a remote call crashes because the server was disconnected. We don't do anything here because
      // we automatically move to the overview state in the OnMessageReceived method when the server disconnects.
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID_MIMATCH; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      ClearData();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      ClearData();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {

    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {

    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Perhaps we'll add menu actions later for different convenience procedures.
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
