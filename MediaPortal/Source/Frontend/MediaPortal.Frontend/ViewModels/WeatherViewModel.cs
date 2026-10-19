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
  /// One row of the forecast column: the day, the words, the span of temperatures, the rain chance.
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
  /// The weather: the places the viewer watches, the sky over the selected one right now, and the days coming
  /// over it, from the same provider MP2's old weather plugin read.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The screen's shape is the plugin's - places, current, forecast - and so is its memory: the watched places
  /// live in <see cref="WeatherPlacesSettings"/> the way the plugin kept its locations list, and the first place
  /// is what the screen opens on, because the first place is home.
  /// </para>
  /// <para>
  /// A place is read when it is selected and when the viewer presses refresh, and not from a clock: the old
  /// plugin's collector woke every fifteen minutes for a home-screen strip, and this screen is only looked at
  /// while it is on. What a refusal costs is one place's panel - the words of the refusal land in the status
  /// line and in the panel, and the other places keep their weather.
  /// </para>
  /// </remarks>
  public partial class WeatherViewModel : ObservableObject, IDisposable
  {
    private readonly WeatherApiClient? _client;
    private readonly WeatherPlacesSettings _settings;
    private readonly string? _settingsPath;
    private readonly CultureInfo _culture;
    private CancellationTokenSource? _running;
    private bool _ready;

    [ObservableProperty]
    private WeatherPlaceRow? _selectedPlace;

    [ObservableProperty]
    private WeatherPlaceRow? _selectedFound;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _status = "Not read yet.";

    [ObservableProperty]
    private string _currentTitle = "";

    [ObservableProperty]
    private string _currentLine = "";

    [ObservableProperty]
    private string _currentDetail = "";

    /// <summary>The worn skin's picture for the sky right now, named as the reference's weather pictures are
    /// (a bare number like "32", or "na"); null when the answer carried no condition code.</summary>
    [ObservableProperty]
    private string? _currentIcon;

    /// <param name="client">Where to read the sky from, or null for a screen that says it has nowhere.</param>
    /// <param name="settingsPath">Where the watched places live; the user's own file by default.</param>
    /// <param name="cultureName">
    /// Which culture numbers and day names are shown in - the machine's own by default, and named here so that a
    /// test can be somebody from Vienna and see Celsius and German weekdays.
    /// </param>
    public WeatherViewModel(WeatherApiClient? client = null, string? settingsPath = null,
        string? cultureName = null)
    {
      _settingsPath = settingsPath;
      _settings = WeatherPlacesSettings.Load(settingsPath, out string? problem);
      _culture = CultureInfo.InvariantCulture;
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
      else
      {
        _culture = CultureInfo.CurrentUICulture;
      }

      // The screen does not invent a client: a viewer who passed null gets the honest "No weather client" the
      // same way the news screen does, and the application wires the real one with the saved key on it.
      _client = client;
      foreach (WeatherPlaceEntry entry in _settings.Places)
        Places.Add(new WeatherPlaceRow(new WeatherCityBookmark(entry.Name, entry.Detail, entry.Lat,
            entry.Lon)));

      if (_client == null)
      {
        Status = "No weather client";
        _ready = true;
        return;
      }

      if (problem != null)
        Status = problem;

      _ready = true;
      if (Places.Count > 0)
        SelectedPlace = Places[0];
      else if (problem == null)
        Status = "No place watched yet - search for one below, and from then on the screen opens on it";
    }

    /// <summary>The watched places, home first, in the order they were added.</summary>
    public ObservableCollection<WeatherPlaceRow> Places { get; } = new ObservableCollection<WeatherPlaceRow>();

    /// <summary>What the last search found, until it is closed or another search replaces it.</summary>
    public ObservableCollection<WeatherPlaceRow> Found { get; } = new ObservableCollection<WeatherPlaceRow>();

    /// <summary>The days coming, today first, for the selected place.</summary>
    public ObservableCollection<WeatherDayRow> Days { get; } = new ObservableCollection<WeatherDayRow>();

    private bool CanRead
    {
      get { return _client != null && SelectedPlace != null; }
    }

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

    /// <summary>Reads the selected place again.</summary>
    [RelayCommand(CanExecute = nameof(CanRead))]
    private async Task ReadAsync()
    {
      WeatherPlaceRow? place = SelectedPlace;
      if (_client == null || place == null)
        return;

      // One read at a time: a walker through the places column must not end with the weather of the place they
      // left because that answer was the slowest.
      CancellationTokenSource? previous = _running;
      CancellationTokenSource cancellation = new CancellationTokenSource();
      _running = cancellation;
      previous?.Cancel();

      IsBusy = true;
      try
      {
        WeatherAnswer answer = await _client.ReadAsync(place.Bookmark, cancellation.Token);
        if (answer.Error != null)
        {
          CurrentTitle = place.Name;
          CurrentLine = "";
          CurrentDetail = "";
          CurrentIcon = null;
          Days.Clear();
          Status = place.Name + " did not answer: " + answer.Error;
          return;
        }

        Show(answer);
      }
      catch (OperationCanceledException)
      {
        // A newer selection won; it reports its own weather.
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
          IsBusy = false;
        }
        cancellation.Dispose();
      }
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

      IsBusy = true;
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
        // have answered "too many requests" the screen says that instead of pretending the town does not exist.
        Status = "The search refused: " + ex.Message;
      }
      finally
      {
        IsBusy = false;
      }
    }

    /// <summary>Keeps the picked search match: onto the places column, into the file, and selected.</summary>
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

      WeatherPlaceRow row = match;
      Places.Add(row);
      _settings.Places.Add(new WeatherPlaceEntry
      {
        Name = row.Name, Detail = row.Detail, Lat = row.Bookmark.Lat, Lon = row.Bookmark.Lon
      });
      string saved = SaveSettings(out bool wasSaved);
      SelectedPlace = row;
      Status = row.Name + (wasSaved ? " added and saved" : " added") + (wasSaved ? "" : ", but " + saved);
    }

    /// <summary>Takes the selected place off the list. The weather it carried stays on screen until another is picked.</summary>
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
        Status += " - that was the only one, so the screen opens on its search box now";
    }

    public void Dispose()
    {
      _running?.Cancel();
      _running?.Dispose();
      _client?.Dispose();
    }

    partial void OnSelectedPlaceChanged(WeatherPlaceRow? value)
    {
      ReadCommand.NotifyCanExecuteChanged();
      RemoveSelectedPlaceCommand.NotifyCanExecuteChanged();
      if (!_ready || value == null)
        return;
      if (_client == null)
      {
        Status = "No weather client";
        return;
      }
      ReadCommand.Execute(null);
    }

    partial void OnSelectedFoundChanged(WeatherPlaceRow? value)
    {
      AddSelectedFoundCommand.NotifyCanExecuteChanged();
    }

    private void Show(WeatherAnswer answer)
    {
      bool metric = _client!.IsMetric;
      NumberFormatInfo numbers = _culture.NumberFormat;
      string degree = metric ? "C" : "F";
      string speed = metric ? "m/s" : "mph";

      CurrentTitle = answer.City.Name + (answer.City.Detail.Length > 0 ? ", " + answer.City.Detail : "");
      CurrentLine = Temperature(answer.Now!.Temp, numbers, degree) + ", " + answer.Now.Description;
      CurrentDetail = "feels like " + Temperature(answer.Now.FeelsLike, numbers, degree)
          + " · " + answer.Now.Humidity + "% humidity · " + answer.Now.Pressure + " hPa"
          + " · wind " + answer.Now.WindDirection + " " + answer.Now.WindSpeed.ToString("0.0", numbers) + " " + speed;
      CurrentIcon = answer.Now.Icon;

      Days.Clear();
      foreach (WeatherDay day in answer.Days ?? Array.Empty<WeatherDay>())
        Days.Add(new WeatherDayRow(day, metric, _culture));

      Status = answer.City.Name + ": " + answer.Now.Description + " "
          + Temperature(answer.Now.Temp, numbers, degree) + ", "
          + Days.Count + (Days.Count == 1 ? " day" : " days") + " ahead";
    }

    /// <summary>"8°C" - one degree sign, whichever side of the Atlantic the number was measured on.</summary>
    private static string Temperature(double value, NumberFormatInfo numbers, string degree)
    {
      return value.ToString("0", numbers) + "°" + degree;
    }

    private string SaveSettings(out bool saved)
    {
      return _settings.Save(_settingsPath, out saved);
    }
  }
}
