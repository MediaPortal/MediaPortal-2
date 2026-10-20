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
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// Who is watching - the reference's login screen in this front end's shape: the people the server
  /// keeps, one row each, and the answer a pick leaves behind. A person picked here owns the skin's
  /// answers from that moment on, because the skin settings file carries their name - the reference's
  /// per-user settings in this port's one honest key - and the next start remembers them instead of
  /// asking again. "Nobody in particular" keeps the answers shared, which is what every household had
  /// before anybody was ever asked, and a server that keeps no people says so rather than showing an
  /// empty form that lies.
  /// </summary>
  /// <remarks>
  /// The provider is a delegate, like the users settings screen's, because the viewer may point at
  /// another server between visits. Picking is not a sign-in: a person with a password still has to say
  /// it to the server itself, and the home screen's sign-in line stays the one place that does. This
  /// screen only says who the room is watching for - the reference drew exactly that line between its
  /// login screen and its server authentication.
  /// </remarks>
  public partial class ViewerPickerViewModel : ObservableObject
  {
    private readonly Func<IUsersApi?> _clientProvider;
    private readonly Action<string?> _picked;

    public ViewerPickerViewModel(Func<IUsersApi?> clientProvider, Action<string?> picked)
    {
      _clientProvider = clientProvider;
      _picked = picked;
    }

    /// <summary>The people to pick from. Machine profiles are not people and never show.</summary>
    public ObservableCollection<UserInfo> People { get; } = new ObservableCollection<UserInfo>();

    public string Heading => "Who is watching?";

    public string Subtitle =>
        "The people the server keeps. Whichever one is picked owns the skin's answers from now on - the "
        + "clock, the colours, the shelves - and is asked again only when the household says so under "
        + "Settings → Users. Nobody need sign in for this; a person's password belongs to the server, "
        + "not to the room.";

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private UserInfo? _selected;

    /// <summary>The answer the last start left behind, so the screen can say what stands today.</summary>
    public string WatchingAs
    {
      get
      {
        string remembered = ClientSettings.Load(null, out _).RememberedViewer;
        return remembered.Length == 0 ? "everybody together" : remembered;
      }
    }

    /// <summary>Asks the server who it keeps. The screen calls it when it opens.</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
      IUsersApi? api = _clientProvider();
      if (api == null)
      {
        Status = "No server connected, so the screen has no names to show. \"Watch on your own\" still "
            + "works - the answers just stay everybody's.";
        return;
      }

      try
      {
        UsersAnswer answer = await api.GetUsersAsync(CancellationToken.None);
        if (!string.IsNullOrEmpty(answer.Error))
        {
          Status = answer.Error;
          return;
        }

        People.Clear();
        foreach (UserInfo user in answer.Users ?? Array.Empty<UserInfo>())
          if (user.Kind != "Client")
            People.Add(user);

        Status = People.Count == 0
            ? "This server keeps no people to pick - the watching stays everybody's."
            : string.Format("Right now the room watches as {0}.", WatchingAs);
      }
      catch (Exception ex)
      {
        Status = "The server could not name its people (" + ex.Message + ").";
      }
    }

    /// <summary>The highlighted person, picked - what Enter and the remote's OK do on their row.</summary>
    [RelayCommand]
    public void PickSelected()
    {
      if (Selected != null)
        Pick(Selected.Name);
      else
        Status = "Pick a name first, or press \"Watch on your own\".";
    }

    /// <summary>A name pressed: remembered, and it owns the skin's answers from here.</summary>
    [RelayCommand]
    public void Pick(string name)
    {
      if (string.IsNullOrWhiteSpace(name))
        return;

      Remember(name);
      Status = string.Format("Watching as {0}. {0}'s skin answers are used from now on.", name);
      _picked(name);
    }

    /// <summary>
    /// "Nobody in particular": continue with the shared answers, and stop being asked - the household
    /// said nobody owns the watching, which is the same answer as turning the asking off. Settings →
    /// Users is the door back, because asking again is a thing the household decides, not one the
    /// screen guesses.
    /// </summary>
    [RelayCommand]
    public void WatchOnOwn()
    {
      ClientSettings settings = ClientSettings.Load(null, out _);
      settings.RememberedViewer = "";
      settings.AskForViewer = false;
      settings.Save(null, out _);
      OnPropertyChanged(nameof(WatchingAs));
      Status = "Everybody together, and the next start will not ask. Settings → Users can ask again.";
      _picked(null);
    }

    /// <summary>Writes the answer where the next start - and the next screen - will read it.</summary>
    private void Remember(string name)
    {
      ClientSettings settings = ClientSettings.Load(null, out _);
      settings.RememberedViewer = name;
      settings.Save(null, out _);
      OnPropertyChanged(nameof(WatchingAs));
    }
  }
}
