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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One row of a place column: a watched place or a search match, by name with what surrounds it underneath.
  /// </summary>
  public sealed class WeatherPlaceRow
  {
    public WeatherPlaceRow(WeatherCityBookmark bookmark)
    {
      Bookmark = bookmark;
    }

    public WeatherCityBookmark Bookmark { get; }

    public string Name
    {
      get { return Bookmark.Name; }
    }

    /// <summary>The state and country line; empty for a saved place the geocoder said nothing around.</summary>
    public string Detail
    {
      get { return Bookmark.Detail; }
    }

    /// <summary>Coordinates in small print, so "Berlin, DE" at 52.52 13.39 is never confused with its namesake.</summary>
    public string Where
    {
      get { return Bookmark.Lat.ToString("0.00", CultureInfo.InvariantCulture) + "  "
          + Bookmark.Lon.ToString("0.00", CultureInfo.InvariantCulture); }
    }
  }

  /// <summary>
  /// One row of the forecast column: the day, the words, the span of temperatures, the rain chance. The
  /// weather screen itself shows its days as wall cards now; this row dresses the skin's
  /// "WeatherDayTemplate", which every skin must still answer to.
  /// </summary>
  public sealed class WeatherDayRow
  {
    private readonly WeatherDay _day;
    private readonly CultureInfo _culture;
    private readonly string _degree;

    public WeatherDayRow(WeatherDay day, bool metric, CultureInfo culture)
    {
      _day = day;
      _culture = culture;
      _degree = metric ? "C" : "F";
    }

    public WeatherDay Day
    {
      get { return _day; }
    }

    /// <summary>The worn skin's picture for this day's midday entry - a bare number like "30", or "na";
    /// null when that entry carried no condition code at all.</summary>
    public string? Icon
    {
      get { return _day.Icon; }
    }

    public bool Metric { get; }

    /// <summary>"Saturday 3 January", in the viewer's language - the API's own words cover the sky, not the calendar.</summary>
    public string DateText
    {
      get { return _day.Date.ToString("dddd d MMMM", _culture.DateTimeFormat); }
    }

    public string Overview
    {
      get { return _day.Description; }
    }

    /// <summary>"6°C / 1°C" - the day's span, unit on both ends, so nobody reads a Fahrenheit low under a
    /// Celsius high.</summary>
    public string Range
    {
      get
      {
        return _day.High.ToString("0", _culture.NumberFormat) + "°" + _degree + " / "
            + _day.Low.ToString("0", _culture.NumberFormat) + "°" + _degree;
      }
    }

    public string Rain
    {
      get
      {
        return "rain " + Math.Round(_day.PrecipitationChance * 100).ToString("0", _culture.NumberFormat) + "%";
      }
    }
  }

  /// <summary>
  /// The weather screen: the sky over one watched place, standing as the bands of the designed wall - today,
  /// the hours, the days coming, and the air - in the room of a screen rather than beside the home menu. The
  /// bands and their cards come from <see cref="WeatherWallCards"/>, the same builder the home's weather wall
  /// calls, so the two walls wear one writing and cannot drift apart.
  /// </summary>
  /// <remarks>
  /// <para>
  /// What a screen adds over the wall: the viewer chooses which watched place the wall speaks for - the
  /// screen's "Location" command walks to the picker - and the place list is re-read from its file on every
  /// showing, because the settings page of its own adds and removes places while this frame waits behind
  /// on the workflow stack. The first saved place is the standing answer, because the first place is home.
  /// </para>
  /// <para>
  /// The read answers from a quarter-hour-old brief of the same place, which is the home wall's own freshness
  /// law - the wall at home and the screen must not ask the same sky twice in one minute - and "Refresh" is
  /// the one command that throws the held brief away and asks again right now. A refusal costs the screen its
  /// bands and says so on the status line; the screen asks again on the next showing rather than on every key.
  /// </para>
  /// </remarks>
  public partial class WeatherViewModel : ObservableObject, IDisposable
  {
    /// <summary>How young a held brief has to be for a showing to answer from it - the home wall's own quarter.</summary>
    private const long BRIEF_FRESH_MS = 15 * 60 * 1000;

    private readonly WeatherApiClient? _client;
    private readonly string? _settingsPath;
    private readonly CultureInfo _culture;
    private CancellationTokenSource? _running;
    private WeatherWallBrief? _brief;
    private long _briefAt;
    private double _briefLat;
    private double _briefLon;

    [ObservableProperty]
    private WeatherPlaceRow? _currentPlace;

    [ObservableProperty]
    private string _status = "Not read yet.";

    /// <param name="client">Where to read the sky from, or null for a screen that says it has nowhere.</param>
    /// <param name="settingsPath">Where the watched places live; the user's own file by default.</param>
    /// <param name="cultureName">
    /// Which culture the status line numbers itself in - the machine's own by default, and named here so that
    /// a test can be somebody from Vienna. The cards themselves speak with the application's current culture,
    /// exactly as the home wall's do, since one builder dresses both.
    /// </param>
    public WeatherViewModel(WeatherApiClient? client = null, string? settingsPath = null,
        string? cultureName = null)
    {
      _settingsPath = settingsPath;
      _client = client;
      _culture = CultureInfo.CurrentCulture;
      if (!string.IsNullOrWhiteSpace(cultureName))
      {
        try
        {
          _culture = CultureInfo.GetCultureInfo(cultureName.Trim());
        }
        catch (CultureNotFoundException)
        {
          // Invariant numbers beat an exception on a weather screen.
        }
      }

      ReloadPlaces();
      if (_client == null)
        Status = "No weather client";
    }

    /// <summary>
    /// The word the wall of places is asked under. The skin's dialog frame reads its heading off the dialog's
    /// own body, so the picker names itself above the watched places the same way the household's wall says
    /// "Select User" above the faces.
    /// </summary>
    public string Heading { get { return "Weather places"; } }

    /// <summary>The watched places, home first, as their file last had them.</summary>
    public ObservableCollection<WeatherPlaceRow> Places { get; } = new ObservableCollection<WeatherPlaceRow>();

    /// <summary>The bands of the weather wall, in the order the design stands them.</summary>
    public ObservableCollection<HomeContentGroup> Bands { get; } = new ObservableCollection<HomeContentGroup>();

    /// <summary>Whether the screen has a client to ask at all - the picker and the walk check this first.</summary>
    public bool HasClient
    {
      get { return _client != null; }
    }

    /// <summary>
    /// Takes the watched places from their file again, so a place added or removed in the settings is on the
    /// picker without a restart. The place being shown keeps its place when it survived the edit; a removed
    /// one hands the screen to the first of those that stayed.
    /// </summary>
    public void ReloadPlaces()
    {
      WeatherPlacesSettings settings = WeatherPlacesSettings.Load(_settingsPath, out string? problem);
      WeatherPlaceRow? was = CurrentPlace;

      Places.Clear();
      foreach (WeatherPlaceEntry entry in settings.Places)
        Places.Add(new WeatherPlaceRow(new WeatherCityBookmark(entry.Name, entry.Detail, entry.Lat, entry.Lon)));

      WeatherPlaceRow? kept = was == null
          ? null
          : Places.FirstOrDefault(row => row.Bookmark.Lat == was.Bookmark.Lat
              && row.Bookmark.Lon == was.Bookmark.Lon);
      CurrentPlace = kept ?? Places.FirstOrDefault();

      if (Places.Count == 0 && problem == null && _client != null)
        Status = "No place watched yet - add one in the settings, and the screen opens on it";
      else if (problem != null)
        Status = problem;
    }

    /// <summary>
    /// Runs every time the workflow shows this screen: the places come off their file again (the settings page
    /// may have edited them while this frame waited behind), and a place whose brief grew older than the
    /// quarter is read again. A fresh brief answers at once - the wall at home asked the same sky minutes ago.
    /// </summary>
    public void ScreenOpened()
    {
      ReloadPlaces();
      if (_brief == null || Environment.TickCount64 - _briefAt >= BRIEF_FRESH_MS)
        _ = ReadAsync(CurrentPlace, force: false);
    }

    /// <summary>Asks the sky for the place being shown, whatever the held brief says.</summary>
    [RelayCommand]
    private Task RefreshAsync()
    {
      return ReadAsync(CurrentPlace, force: true);
    }

    /// <summary>
    /// Makes <paramref name="place"/> the one the screen speaks for. From the picker this is the whole answer:
    /// the old place's bands leave, the new one's are read.
    /// </summary>
    public void ChoosePlace(WeatherPlaceRow place)
    {
      CurrentPlace = place;
      _ = ReadAsync(place, force: false);
    }

    partial void OnCurrentPlaceChanged(WeatherPlaceRow? value)
    {
      if (_client == null)
        Status = "No weather client";
    }

    /// <summary>
    /// Reads one place and puts its bands up. One read at a time: a walker through the picker must not end
    /// with the weather of the place they left because that answer was the slowest.
    /// </summary>
    private async Task ReadAsync(WeatherPlaceRow? place, bool force)
    {
      if (_client == null)
      {
        Status = "No weather client";
        return;
      }

      if (place == null)
      {
        Status = "No place watched yet - add one in the settings, and the screen opens on it";
        return;
      }

      WeatherPlaceEntry entry = new WeatherPlaceEntry
      {
        Name = place.Name, Detail = place.Detail, Lat = place.Bookmark.Lat, Lon = place.Bookmark.Lon
      };

      // The held brief only answers for the place it was read for, and only while it is young.
      bool holds = !force && _brief != null && Environment.TickCount64 - _briefAt < BRIEF_FRESH_MS
          && _briefLat == entry.Lat && _briefLon == entry.Lon;
      if (holds)
      {
        Show(_brief!);
        return;
      }

      CancellationTokenSource? previous = _running;
      CancellationTokenSource cancellation = new CancellationTokenSource();
      _running = cancellation;
      previous?.Cancel();

      try
      {
        WeatherWallBrief? brief = await WeatherWallCards.ReadBriefAsync(_client, entry, cancellation.Token);
        if (brief == null)
        {
          Status = place.Name + " did not answer";
          return;
        }

        _brief = brief;
        _briefAt = Environment.TickCount64;
        _briefLat = entry.Lat;
        _briefLon = entry.Lon;
        if (ReferenceEquals(place, CurrentPlace))
          Show(brief);
      }
      catch (OperationCanceledException)
      {
        // A newer pick won; it reports its own weather.
      }
      catch (Exception ex)
      {
        Status = "Reading the weather failed: " + ex.Message;
      }
      finally
      {
        if (ReferenceEquals(_running, cancellation))
        {
          _running = null;
          cancellation.Dispose();
        }
      }
    }

    /// <summary>Swaps the wall over to this brief and says what came in, on the line at the screen's foot.</summary>
    private void Show(WeatherWallBrief brief)
    {
      Bands.Clear();
      WeatherWallCards.BuildBands(Bands, brief);

      NumberFormatInfo numbers = _culture.NumberFormat;
      string degree = brief.Metric ? "C" : "F";
      string sky = brief.Now == null
          ? ""
          : brief.Now.Description + " " + brief.Now.Temp.ToString("0", numbers) + "°" + degree;
      Status = brief.Place + ": " + sky + ", "
          + brief.Days.Count + (brief.Days.Count == 1 ? " day" : " days") + " ahead";
    }

    public void Dispose()
    {
      _running?.Cancel();
      _running?.Dispose();
      _client?.Dispose();
    }
  }

  /// <summary>
  /// The settings page's half of the weather: the watched places, the search that adds one, the key that takes
  /// one away, and the viewer's own API key. This is the old weather screen's place management, moved behind
  /// the settings door - the screen itself shows weather now, and who watches which places is a setting.
  /// </summary>
  /// <remarks>
  /// Every answer saves itself the moment it is given, exactly like the television's settings page: a couch
  /// viewer who walks away mid-edit leaves a file that says what they meant, and no "Apply" is waiting to be
  /// pressed by nobody.
  /// </remarks>
  public partial class WeatherPlacesEditorViewModel : ObservableObject, IDisposable
  {
    private readonly WeatherApiClient? _client;
    private readonly WeatherPlacesSettings _settings;
    private readonly string? _settingsPath;

    [ObservableProperty]
    private WeatherPlaceRow? _selectedPlace;

    [ObservableProperty]
    private WeatherPlaceRow? _selectedFound;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _apiKey = "";

    [ObservableProperty]
    private string _status = "";

    public WeatherPlacesEditorViewModel(WeatherApiClient? client = null, string? settingsPath = null)
    {
      _client = client;
      _settingsPath = settingsPath;
      _settings = WeatherPlacesSettings.Load(settingsPath, out string? problem);
      foreach (WeatherPlaceEntry entry in _settings.Places)
        Places.Add(new WeatherPlaceRow(new WeatherCityBookmark(entry.Name, entry.Detail, entry.Lat, entry.Lon)));

      _apiKey = _settings.ApiKey;
      Status = problem ?? Places.Count + (Places.Count == 1 ? " place watched" : " places watched")
          + (Places.Count > 0 ? ", the first is home" : "");
    }

    /// <summary>The watched places, home first, in the order they were added.</summary>
    public ObservableCollection<WeatherPlaceRow> Places { get; } = new ObservableCollection<WeatherPlaceRow>();

    /// <summary>What the last search found, until another search replaces it.</summary>
    public ObservableCollection<WeatherPlaceRow> Found { get; } = new ObservableCollection<WeatherPlaceRow>();

    private bool CanSearch
    {
      get { return _client != null; }
    }

    private bool CanAddFound
    {
      get { return SelectedFound != null; }
    }

    private bool CanRemove
    {
      get { return SelectedPlace != null; }
    }

    /// <summary>Asks the geocoder for places whose name the viewer typed.</summary>
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task SearchAsync()
    {
      if (_client == null)
      {
        Status = "No weather client";
        return;
      }

      string query = SearchText.Trim();
      if (query.Length == 0)
      {
        Status = "Nothing typed to search for";
        return;
      }

      try
      {
        var found = await _client.FindCitiesAsync(query);
        Found.Clear();
        SelectedFound = null;
        foreach (WeatherCityBookmark bookmark in found)
          Found.Add(new WeatherPlaceRow(bookmark));
        if (Found.Count == 0)
        {
          Status = "Nothing on earth answers to '" + query + "'";
          return;
        }

        SelectedFound = Found[0];
        Status = Found.Count + (Found.Count == 1 ? " place" : " places") + " called '" + query
            + "' - pick one and press OK to keep it";
      }
      catch (Exception ex)
      {
        // Including the key rotation running out: the shared demo keys are spent things, and when all three
        // have answered "too many requests" the page says that instead of pretending the town does not exist.
        Status = "The search refused: " + ex.Message;
      }
    }

    /// <summary>Keeps the picked match: onto the list, into the file, home stays first.</summary>
    [RelayCommand(CanExecute = nameof(CanAddFound))]
    private void AddSelectedFound()
    {
      WeatherPlaceRow? match = SelectedFound;
      if (match == null)
        return;

      if (Places.Any(row => row.Bookmark.Lat == match.Bookmark.Lat && row.Bookmark.Lon == match.Bookmark.Lon))
      {
        Status = match.Name + " is already on the list";
        return;
      }

      Places.Add(match);
      _settings.Places.Add(new WeatherPlaceEntry
      {
        Name = match.Name, Detail = match.Detail, Lat = match.Bookmark.Lat, Lon = match.Bookmark.Lon
      });
      string saved = SaveSettings(out bool wasSaved);
      Status = match.Name + (wasSaved ? " added and saved" : " added") + (wasSaved ? "" : ", but " + saved);
    }

    /// <summary>Takes the picked place off the list and out of the file.</summary>
    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void RemoveSelectedPlace()
    {
      WeatherPlaceRow? place = SelectedPlace;
      if (place == null)
        return;

      Places.Remove(place);
      _settings.Places.RemoveAll(entry => entry.Name == place.Name && entry.Lat == place.Bookmark.Lat
          && entry.Lon == place.Bookmark.Lon);
      string saved = SaveSettings(out bool wasSaved);
      string gone = place.Name + " is off the list";
      Status = wasSaved ? gone : gone + ", but " + saved;
      SelectedPlace = Places.Count > 0 ? Places[0] : null;
      if (Places.Count == 0)
        Status += " - that was the only one, and the weather screen has nothing to show until a new one is added";
    }

    partial void OnApiKeyChanged(string value)
    {
      if (_settings.ApiKey == value)
        return;

      // The saved key rides on the next screen opening - the client that answers THIS page was built before
      // the typing, and its search keeps asking with the key it started on.
      _settings.ApiKey = value;
      string saved = SaveSettings(out bool wasSaved);
      Status = value.Length == 0
          ? (wasSaved ? "No own key - the shared keys answer again" : "No own key, but " + saved)
          : wasSaved ? "Your own key saved; the weather screens ask with it from now on"
              : "Key kept for this page, but " + saved;
    }

    partial void OnSelectedFoundChanged(WeatherPlaceRow? value)
    {
      AddSelectedFoundCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedPlaceChanged(WeatherPlaceRow? value)
    {
      RemoveSelectedPlaceCommand.NotifyCanExecuteChanged();
    }

    private string SaveSettings(out bool saved)
    {
      return _settings.Save(_settingsPath, out saved);
    }

    public void Dispose()
    {
      _client?.Dispose();
    }
  }
}
