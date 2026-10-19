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
  /// The credentials the server offers to the network's shares - the reference's
  /// "Settings -> Netzwerkzugriff" dialog, in the front end's own shape. What the reference's setup window
  /// kept for one root path, this screen reads and writes through the server's own endpoint, and shows the
  /// dictionary the provider actually keeps: paths, users, and the word of whether a password rides on an
  /// entry. A stored password never comes to this side of the wire, so the password box starts empty every
  /// time and an empty box on Save means "keep what is there" - the same bargain the users screen makes.
  /// </summary>
  /// <remarks>
  /// The provider is a delegate rather than the client itself because the viewer may point at a different
  /// server between visits, exactly like the users and metadata screens. A server without the neighborhood
  /// plugin answers 503, and the screen passes that sentence on: an empty form that pretends to be about to
  /// store something is the lie this whole screen avoids.
  /// </remarks>
  public partial class NetworkCredentialsViewModel : ObservableObject
  {
    private readonly Func<INetworkCredentialsApi?> _clientProvider;

    public NetworkCredentialsViewModel(Func<INetworkCredentialsApi?> clientProvider)
    {
      _clientProvider = clientProvider;
    }

    public ObservableCollection<NetworkCredentialInfo> Credentials { get; } =
        new ObservableCollection<NetworkCredentialInfo>();

    public string Heading => "Network credentials";

    public string Subtitle =>
        "What the server says when a shared folder asks who is calling. The entries live in the server's "
        + "network plugin, one per path; the root entry speaks for every share that names no path of its "
        + "own, and a stored password is never shown back - only that there is one.";

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private NetworkCredentialInfo? _selected;

    [ObservableProperty]
    private string _pathText = "";

    [ObservableProperty]
    private string _userText = "";

    [ObservableProperty]
    private string _passwordText = "";

    [ObservableProperty]
    private string _domainText = "";

    /// <summary>The server's root path, kept from the last answer so the empty path box can say what it means.</summary>
    [ObservableProperty]
    private string _rootPath = "";

    /// <summary>Whether the highlight stands on a stored entry. The removal row appears with a picked one.</summary>
    public bool HasSelection => Selected != null;

    partial void OnSelectedChanged(NetworkCredentialInfo? value)
    {
      if (value != null)
      {
        PathText = value.Path;
        UserText = value.UserName;
        DomainText = value.Domain;
        PasswordText = "";
      }
      OnPropertyChanged(nameof(HasSelection));
      OnPropertyChanged(nameof(SelectionSays));
    }

    /// <summary>The quiet sentence under the editor: who is picked and what is already true about them.</summary>
    public string SelectionSays
    {
      get
      {
        if (Selected == null)
          return RootPath.Length > 0
              ? "Pick an entry on the left to change it, or leave the path empty to store at the root: " + RootPath
              : "Pick an entry on the left to change it, or fill in a path and a user to add one.";
        return Selected.Path
            + (Selected.Domain.Length > 0 ? " as " + Selected.Domain + "\\" + Selected.UserName : " as " + Selected.UserName)
            + (Selected.HasPassword ? " - has a password" : " - no password yet");
      }
    }

    /// <summary>Asks the server what it offers to shares. Called when the section opens and by the reload button.</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
      INetworkCredentialsApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet - set one under \"Server address\" first.";
        return;
      }

      try
      {
        NetworkCredentialsAnswer answer = await client.GetNetworkCredentialsAsync();
        Apply(answer, "Read.");
      }
      catch (ServerApiException e)
      {
        Status = e.StatusCode == 503
            ? "This server has no network neighborhood plugin - there is nothing to hand a share."
            : e.StatusCode == 404 || e.StatusCode == 405
                ? "This server does not offer its network credentials over the API - it is probably an older build."
                : "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    /// <summary>
    /// Stores the typed entry. An empty password on an existing entry keeps its password (the box was never
    /// filled with it); a new entry with an empty password is stored without one, which is a legal answer a
    /// share can give.
    /// </summary>
    [RelayCommand]
    public async Task StoreAsync()
    {
      INetworkCredentialsApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet - set one under \"Server address\" first.";
        return;
      }
      if (UserText.Trim().Length == 0)
      {
        Status = "A credential without a user name is no credential.";
        return;
      }

      try
      {
        NetworkCredentialsAnswer answer = await client.StoreNetworkCredentialAsync(new NetworkCredentialWish
        {
          Path = PathText.Trim(),
          UserName = UserText.Trim(),
          Password = PasswordText.Length > 0 ? PasswordText : null,
          Domain = DomainText.Trim()
        });
        PasswordText = "";
        Apply(answer, "Stored.");
      }
      catch (ServerApiException e)
      {
        Status = e.StatusCode == 503
            ? "This server has no network neighborhood plugin - there is nothing to hand a share."
            : "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    /// <summary>Forgets the picked entry. Emptying the path box and storing is how the reference turned the whole thing off.</summary>
    [RelayCommand]
    public async Task RemoveSelectedAsync()
    {
      NetworkCredentialInfo? picked = Selected;
      await RemoveAsync(picked?.Path);
    }

    /// <summary>Forgets every entry - the reference's "don't use credentials", which clears the dictionary.</summary>
    [RelayCommand]
    public async Task RemoveAllAsync()
    {
      await RemoveAsync("");
    }

    private async Task RemoveAsync(string? path)
    {
      INetworkCredentialsApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet - set one under \"Server address\" first.";
        return;
      }

      try
      {
        NetworkCredentialsAnswer answer = await client.RemoveNetworkCredentialAsync(path ?? "");
        Apply(answer, (path ?? "").Length == 0
            ? "Removed every credential."
            : "Removed the credential for " + path + ".");
      }
      catch (ServerApiException e)
      {
        Status = e.StatusCode == 503
            ? "This server has no network neighborhood plugin - there is nothing to hand a share."
            : "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    /// <summary>
    /// Replaces the list with the answer's entries, keeps the root the answer named, and says what just
    /// happened - or, when the answer carries an error, says that instead of a success nobody saw.
    /// </summary>
    private void Apply(NetworkCredentialsAnswer answer, string did)
    {
      Credentials.Clear();
      foreach (NetworkCredentialInfo entry in answer.Credentials ?? Array.Empty<NetworkCredentialInfo>())
        Credentials.Add(entry);
      RootPath = answer.RootPath ?? "";
      Status = answer.Error ?? did + " " + Credentials.Count
          + (Credentials.Count == 1 ? " credential stands on the server." : " credentials stand on the server.");
      OnPropertyChanged(nameof(SelectionSays));
    }
  }
}
