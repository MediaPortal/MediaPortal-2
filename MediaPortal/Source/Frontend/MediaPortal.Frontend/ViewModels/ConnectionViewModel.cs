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
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Discovery;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.ViewModels;

/// <summary>
/// Picking the server, from the television.
/// </summary>
/// <remarks>
/// <para>
/// This exists because the alternative was a command-line argument: correct for a gate or an ssh session, useless
/// to somebody holding a remote. The screen looks for the servers on the network and lists them, so that picking
/// one is a matter of pressing OK on a name; typing an address is the way out when the network will not answer,
/// not the way in.
/// </para>
/// <para>
/// Nothing is saved before it has been talked to. That holds for a detected server as much as for a typed one -
/// an announcement on UDP is a claim, so <see cref="ServerLocator"/> asks the machine over its API first and only
/// a server that answered appears in the list. For the typed address the rule is visible as a button: it is the
/// same two requests <c>--check</c> makes, so what the screen promises the front end really does afterwards.
/// </para>
/// <para>
/// The awaits here keep their context on purpose, exactly as <see cref="HomeViewModel"/> does: everything after them
/// writes properties that a screen is bound to.
/// </para>
/// </remarks>
public partial class ConnectionViewModel : ObservableObject
{
  private readonly string? _settingsPath;
  private readonly SsdpSearchClient _searcher;
  private readonly ServerLocator _locator;
  private readonly SsdpSearchOptions _searchOptions;
  private CancellationTokenSource? _runningTest;
  private CancellationTokenSource? _runningScan;
  private bool _hasScanned;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanTest))]
  [NotifyCanExecuteChangedFor(nameof(TestCommand))]
  private string _serverUrlText;

  [ObservableProperty]
  private string _status;

  [ObservableProperty]
  private bool _isBusy;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanRescan))]
  [NotifyPropertyChangedFor(nameof(ListHeading))]
  [NotifyCanExecuteChangedFor(nameof(RescanCommand))]
  private bool _isScanning;

  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
  private bool _tested;

  [ObservableProperty]
  private DetectedServer? _selectedServer;

  /// <summary>
  /// The servers that answered the search, in the order they are listed.
  /// </summary>
  public ObservableCollection<DetectedServer> Servers { get; } = new ObservableCollection<DetectedServer>();

  /// <summary>
  /// Whether anything was found. Drives the sentence that tells the user to type the address instead.
  /// </summary>
  public bool HasServers => Servers.Count > 0;

  /// <summary>
  /// The sentence above the list. It says which of the three states the screen is in - searching, something found,
  /// nothing found - because an empty box on a television is ambiguous, and "nothing answered" and "nobody asked
  /// yet" need different things from the viewer.
  /// </summary>
  public string ListHeading
  {
    get
    {
      if (IsScanning) return "Looking for MediaPortal servers on this network ...";
      if (Servers.Count > 0) return "Servers that answered. Select one:";
      return _hasScanned
          ? "Nothing on this network answered. Type the address below."
          : "Not searched yet. Use Search again, or type the address below.";
    }
  }

  /// <summary>
  /// Whether the screen starts a search by itself when it opens. The screens do it; the view model only says
  /// whether that is wanted, so a caller drawing the screen without a network (the render probe, a test) can say
  /// no and get a frame that does not depend on what is plugged in.
  /// </summary>
  public bool AutoScan { get; set; } = true;

  /// <param name="settings">Address to offer as the starting point; <c>null</c> starts with an empty field.</param>
  /// <param name="settingsPath">
  /// Where a save writes. <c>null</c> means the file the platform picked (<see cref="ServerSettings.DefaultFilePath"/>);
  /// tests pass a path in a temporary folder so that running the suite cannot touch what a real user saved.
  /// </param>
  /// <param name="readProblem">
  /// The sentence <see cref="ServerSettings.Load"/> produced when the saved file could not be read, if there was one.
  /// It goes on the screen instead of into a log: a front end that quietly ignores a broken settings file is
  /// investigated for an hour by somebody who could have been told in one line.
  /// </param>
  /// <param name="searcher">
  /// How the network is asked. Only tests replace it, to point the search at a fake server on a port of their own
  /// instead of at whatever is plugged in.
  /// </param>
  /// <param name="locator">How an answer to the search is checked before it is listed.</param>
  /// <param name="searchOptions">Search target, window and endpoints. Tests use it to search one local port for
  /// half a second rather than the whole network for three.</param>
  public ConnectionViewModel(ServerSettings? settings, string? settingsPath = null, string? readProblem = null,
                             SsdpSearchClient? searcher = null, ServerLocator? locator = null,
                             SsdpSearchOptions? searchOptions = null)
  {
    _settingsPath = settingsPath;
    _searcher = searcher ?? new SsdpSearchClient();
    _locator = locator ?? new ServerLocator();
    _searchOptions = searchOptions ?? SsdpSearchOptions.ForNetworkSearch();
    _serverUrlText = settings?.ServerUrl ?? "";

    bool hasSaved = settings != null && !string.IsNullOrWhiteSpace(settings.ServerUrl);
    if (readProblem != null)
      _status = readProblem + " Search again, or enter the address.";
    else if (hasSaved)
      _status = "The saved address is shown. Search again, or test it.";
    else
      // Nothing has been asked yet at this point - the screen starts the search when it opens, and that says so in
      // its own words - so this sentence only states what the viewer can do, which is also what a view model without
      // a screen in front of it has to say.
      _status = "Nothing has been saved yet. Search the network, or type the address of the MediaPortal server.";
  }

  /// <summary>
  /// Raised when the address was written to the settings file, with the URL that was saved. The screen closes itself
  /// in reaction to this; the view model does not know about windows.
  /// </summary>
  public event Action<string>? Saved;

  /// <summary>
  /// Where the address would be saved, shown so that "it did not stick after a reboot" has an answer.
  /// </summary>
  public string SettingsFile => _settingsPath ?? ServerSettings.DefaultFilePath;

  /// <summary>
  /// Whether the typed text is an address worth trying.
  /// </summary>
  /// <remarks>
  /// The screen binds the test button's <c>IsEnabled</c> to this rather than trusting the command: measured on this
  /// Avalonia version, a button whose command reports that it cannot execute still arrives on screen enabled and
  /// simply swallows the press. On a television that is indistinguishable from a broken button, so the state has to
  /// be visible.
  /// </remarks>
  public bool CanTest => new ServerSettings { ServerUrl = ServerUrlText?.Trim() ?? "" }.HasUsableUrl;

  private bool CanTestAddress()
  {
    return CanTest;
  }

  private bool CanSave()
  {
    return Tested;
  }

  private bool CanRescanNow()
  {
    return CanRescan;
  }

  /// <summary>
  /// Whether a search can be started: not while one is running, and not while a test is asking a typed address.
  /// </summary>
  public bool CanRescan => !IsScanning && !IsBusy;

  /// <summary>
  /// Asks the network which MediaPortal servers are there and lists the ones that answer.
  /// </summary>
  /// <remarks>
  /// The list is only filled with servers that answered their own API, so an entry can be saved as it stands;
  /// see <see cref="ServerLocator"/>. Whatever the search produced, the sentence says so - including "the network
  /// could not be searched", which on a machine with no working IPv4 multicast is the truth the user needs rather
  /// than an empty box.
  /// </remarks>
  [RelayCommand(CanExecute = nameof(CanRescanNow))]
  public async Task RescanAsync()
  {
    if (IsScanning) return;

    CancellationTokenSource? previous = _runningScan;
    CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    _runningScan = cancellation;
    previous?.Cancel();

    IsScanning = true;
    SelectedServer = null;
    Servers.Clear();
    RaiseHasServers();
    Status = "Looking for MediaPortal servers on this network ...";

    try
    {
      SsdpSearchResult result = await _searcher.SearchAsync(_searchOptions, cancellation.Token);
      IReadOnlyList<DetectedServer> found = await _locator.LocateAsync(result.Announcements, cancellation.Token);
      foreach (DetectedServer server in found) Servers.Add(server);
      RaiseHasServers();

      if (found.Count > 0)
      {
        Status = found.Count == 1
            ? "One server found. Select it and save it."
            : found.Count + " servers found. Select one and save it.";

        // One server on the network: picking it should cost one key. But not while somebody is typing an address of
        // their own into the field - what they typed outranks what the network happened to say, and a screen that
        // replaces text under the caret is a screen that loses what the viewer wrote.
        if (found.Count == 1 && string.IsNullOrWhiteSpace(ServerUrlText)) SelectedServer = found[0];
      }
      else if (result.SendFailures.Count > 0)
      {
        Status = "The network could not be searched (" + string.Join("; ", result.SendFailures) +
                 "). Type the address below instead.";
      }
      else
      {
        Status = result.MessagesReceived > 0
            ? "Nothing on this network answered as a MediaPortal server. Type the address below."
            : "No device answered the search. Type the address below.";
      }
    }
    catch (OperationCanceledException)
    {
      Status = "Search cancelled. Type the address below if you know it.";
    }
    catch (Exception ex)
    {
      Status = string.Format("{0}: {1}", ex.GetType().Name, ex.Message);
    }
    finally
    {
      if (ReferenceEquals(_runningScan, cancellation))
      {
        _runningScan = null;
        _hasScanned = true;
        IsScanning = false; // Raises ListHeading.
      }
      cancellation.Dispose();
    }
  }

  /// <summary>
  /// Puts the list's state onto the screen. The collection changes by itself and does not raise a property
  /// changed event for <see cref="HasServers"/>, so every place that fills it says so here.
  /// </summary>
  private void RaiseHasServers()
  {
    OnPropertyChanged(nameof(HasServers));
    OnPropertyChanged(nameof(ListHeading));
  }

  partial void OnSelectedServerChanged(DetectedServer? value)
  {
    if (value == null) return;

    // The address in the box becomes the picked server's, so that what gets saved is what the list showed.
    // Tested is set after the assignment because writing the text clears the confirmation - and here the
    // confirmation is not needed: the entry is in the list only because its own API answered a moment ago.
    ServerUrlText = value.BaseUrl.ToString().TrimEnd('/');
    Tested = true;
    Status = value.Name + " answered at " + value.AddressLine + ". Save it to use this server.";
  }

  /// <summary>
  /// Asks the typed address whether a MediaPortal server answers. Success fills <see cref="Status"/> with what the
  /// server said it is and enables saving; failure puts the server's own sentence there, or the reason it could not
  /// be reached, and leaves the field alone so the mistake can be corrected.
  /// </summary>
  [RelayCommand(CanExecute = nameof(CanTestAddress))]
  public async Task TestAsync()
  {
    Tested = false;

    if (!CanTestAddress())
    {
      Status = "'" + ServerUrlText + "' is not an http or https address.";
      return;
    }

    CancellationTokenSource? previous = _runningTest;
    CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    _runningTest = cancellation;
    previous?.Cancel(); // Its own task notices, reports nothing, and disposes itself below.

    Uri baseUrl = new Uri(ServerUrlText.Trim());
    IsBusy = true;
    Status = "Asking " + baseUrl + " ...";

    // Not kept in a field: the screen may be testing several addresses in a row, and a client per attempt means the
    // answer can only ever have come from the address that is in the box right now.
    using (ServerApiClient client = new ServerApiClient(baseUrl))
    {
      try
      {
        HealthInfo health = await client.GetHealthAsync(cancellation.Token);
        ServerInfo info = await client.GetServerInfoAsync(cancellation.Token);
        Status = string.Format("{0} - {1}", health.Name, info.Describe());
        Tested = true;
      }
      catch (ServerApiException ex)
      {
        Status = "That is not answering as a MediaPortal server: " + ex.Message;
      }
      catch (OperationCanceledException)
      {
        Status = "Superseded by a newer test, or " + baseUrl + " did not answer in time.";
      }
      catch (Exception ex)
      {
        Status = string.Format("{0}: {1}", ex.GetType().Name, ex.Message);
      }
      finally
      {
        if (ReferenceEquals(_runningTest, cancellation))
        {
          _runningTest = null;
          IsBusy = false;
        }
        cancellation.Dispose();
      }
    }
  }

  /// <summary>
  /// Writes the tested address to the settings file. Refuses to save anything that has not just been confirmed to be
  /// a server - see the remark on the class.
  /// </summary>
  [RelayCommand(CanExecute = nameof(CanSave))]
  public void Save()
  {
    if (!Tested)
    {
      Status = "Test the address first, so that a typo does not become the saved server.";
      return;
    }

    string trimmed = ServerUrlText.Trim();
    string message = new ServerSettings { ServerUrl = trimmed }.Save(_settingsPath, out bool saved);
    Status = saved
        ? "Saved. The front end will use " + trimmed + " next time." + " (" + message + ")"
        : message;
    if (saved)
      Saved?.Invoke(trimmed);
  }

  partial void OnServerUrlTextChanged(string value)
  {
    // Anything typed is a fresh address, so the confirmation from the previous one no longer applies.
    Tested = false;

    // ... and neither does the entry in the list, if the text stopped matching it. Dropping the selection keeps
    // the screen honest about where the address that is about to be saved came from, and lets the user press the
    // same entry again to get its confirmation back.
    if (SelectedServer != null && !SameAddress(value, SelectedServer.BaseUrl)) SelectedServer = null;
  }

  private static bool SameAddress(string? text, Uri url)
  {
    if (string.IsNullOrWhiteSpace(text)) return false;
    return string.Equals(text.Trim().TrimEnd('/'), url.ToString().TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
  }
}
