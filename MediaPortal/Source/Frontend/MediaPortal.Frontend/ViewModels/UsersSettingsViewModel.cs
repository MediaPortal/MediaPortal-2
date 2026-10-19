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
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// Who may sign in on the server - the reference's "Settings -> Users" window, in the front end's own
  /// shape. The store is the server's (the very one the token endpoint reads when somebody signs in); this
  /// screen lists what it keeps and works the three answers that window had: a name, the password that
  /// belongs to it, and removal. Nothing here is local - a server without the endpoint says so in the
  /// status line rather than showing an empty form that lies.
  /// </summary>
  /// <remarks>
  /// The provider is a delegate rather than the client itself because the viewer may point at a different
  /// server between visits, exactly like the server-settings and metadata screens. The password box holds
  /// what is being typed and nothing more: a stored password never comes to this side of the wire, so the
  /// box starts empty every time a user is picked, and an empty box on Save means "keep what is there",
  /// while the "remove the password" answer is its own button - one gesture, one meaning.
  /// </remarks>
  public partial class UsersSettingsViewModel : ObservableObject
  {
    private readonly Func<IUsersApi?> _clientProvider;

    public UsersSettingsViewModel(Func<IUsersApi?> clientProvider)
    {
      _clientProvider = clientProvider;
    }

    public ObservableCollection<UserInfo> Users { get; } = new ObservableCollection<UserInfo>();

    public string Heading => "Users";

    public string Subtitle =>
        "Who may sign in on the server. The names, the passwords and the removals are the server's own "
        + "store - the same one that answers when somebody signs in - and every change is written there "
        + "the moment it is made.";

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private UserInfo? _selected;

    [ObservableProperty]
    private string _nameText = "";

    [ObservableProperty]
    private string _passwordText = "";

    /// <summary>Whether the highlight stands on somebody. The working rows appear with a picked user.</summary>
    public bool HasSelection => Selected != null;

    partial void OnSelectedChanged(UserInfo? value)
    {
      NameText = value?.Name ?? "";
      PasswordText = "";
      OnPropertyChanged(nameof(HasSelection));
      OnPropertyChanged(nameof(SelectionSays));
    }

    /// <summary>The quiet sentence under the editor: who is picked and what is already true about them.</summary>
    public string SelectionSays
    {
      get
      {
        if (Selected == null)
          return "Pick a user on the left to change them, or type a new name to add one.";
        string says = Selected.Name;
        says += Selected.IsYou ? " - this is who you are signed in as" : "";
        says += Selected.Kind == "Client" ? " - a machine's profile, not a person's" : "";
        says += Selected.HasPassword ? " - has a password" : " - signs in without a password";
        return says;
      }
    }

    /// <summary>Asks the server who it keeps. Called when the section opens and by the reload button.</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
      IUsersApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet - set one under \"Server address\" first.";
        return;
      }

      try
      {
        UsersAnswer answer = await client.GetUsersAsync();
        Users.Clear();
        foreach (UserInfo user in answer.Users ?? Array.Empty<UserInfo>())
          Users.Add(user);
        Status = answer.Error != null
            ? answer.Error
            : string.Format("Read {0} user(s) from the server.", Users.Count);
        RaiseSelection();
      }
      catch (ServerApiException e)
      {
        Status = e.StatusCode == 404 || e.StatusCode == 405
            ? "This server does not offer its users over the API - it is probably an older build."
            : "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    /// <summary>Adds the typed name, with the typed password (or none). The store's name rules are the server's.</summary>
    [RelayCommand]
    public async Task AddAsync()
    {
      string name = NameText.Trim();
      if (name.Length == 0)
      {
        Status = "A user needs a name.";
        return;
      }

      IUsersApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet.";
        return;
      }

      try
      {
        UserAnswer answer = await client.CreateUserAsync(new CreateUserWish
        {
          Name = name,
          Password = PasswordText.Length == 0 ? null : PasswordText
        });
        PasswordText = "";
        await LoadAsync();
        // The list has been read back, and what the viewer keeps reading is the server's answer to the
        // press, not the count the read-back brought.
        Status = answer.Error != null
            ? "The server would not add '" + name + "': " + answer.Error
            : string.Format("'{0}' was added.", name);
      }
      catch (ServerApiException e)
      {
        Status = "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    /// <summary>
    /// Renames the picked user and, when a new password was typed, changes it too. An empty password box
    /// keeps the current password - removing one is its own answer, below, because empty-box-means-nothing
    /// and empty-box-means-remove would otherwise be the same gesture with two meanings.
    /// </summary>
    [RelayCommand]
    public async Task SaveSelectedAsync()
    {
      UserInfo? who = Selected;
      if (who == null)
      {
        Status = "Nothing is picked.";
        return;
      }

      IUsersApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet.";
        return;
      }

      string name = NameText.Trim();
      if (name.Length == 0)
      {
        Status = "A user needs a name.";
        return;
      }

      try
      {
        UserAnswer answer = await client.UpdateUserAsync(who.ProfileId, new UpdateUserWish
        {
          Name = name,
          Password = PasswordText.Length == 0 ? null : PasswordText
        });
        await LoadAsync();
        Status = answer.Error != null
            ? "The server would not change '" + who.Name + "': " + answer.Error
            : string.Format("'{0}' saved.", name);
      }
      catch (ServerApiException e)
      {
        Status = "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    /// <summary>Takes the typed-away password off the picked user. The box is not the answer here; this press is.</summary>
    [RelayCommand]
    public async Task RemovePasswordAsync()
    {
      UserInfo? who = Selected;
      if (who == null)
      {
        Status = "Nothing is picked.";
        return;
      }

      IUsersApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet.";
        return;
      }

      try
      {
        UserAnswer answer = await client.UpdateUserAsync(who.ProfileId, new UpdateUserWish { Password = "" });
        await LoadAsync();
        Status = answer.Error != null
            ? "The server would not change '" + who.Name + "': " + answer.Error
            : string.Format("'{0}' signs in without a password now.", who.Name);
      }
      catch (ServerApiException e)
      {
        Status = "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    /// <summary>
    /// Removes the picked user. The signed-in one cannot remove themselves - the server refuses it in words,
    /// and those words land in the status line rather than being pre-empted here, so the screen shows the
    /// server's own rule rather than a second one invented on top of it.
    /// </summary>
    [RelayCommand]
    public async Task RemoveSelectedAsync()
    {
      UserInfo? who = Selected;
      if (who == null)
      {
        Status = "Nothing is picked.";
        return;
      }

      IUsersApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet.";
        return;
      }

      try
      {
        UserAnswer answer = await client.DeleteUserAsync(who.ProfileId);
        await LoadAsync();
        Status = answer.Error != null
            ? "The server would not remove '" + who.Name + "': " + answer.Error
            : string.Format("'{0}' was removed.", who.Name);
      }
      catch (ServerApiException e)
      {
        Status = "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    /// <summary>Keeps the picked user's identity and lets the picked user's name stand after a reload.</summary>
    private void RaiseSelection()
    {
      UserInfo? picked = Selected;
      if (picked == null)
        return;

      foreach (UserInfo candidate in Users)
        if (candidate.ProfileId == picked.ProfileId)
        {
          Selected = candidate;
          return;
        }

      Selected = null;
    }
  }
}
