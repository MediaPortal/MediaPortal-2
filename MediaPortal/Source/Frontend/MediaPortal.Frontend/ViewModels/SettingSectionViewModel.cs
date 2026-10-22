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
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One settings screen the server opened: its words, its rows, and the two presses that read and write
  /// it. This is the editor half of the plugin settings screen; it is built when a section is opened and
  /// thrown away when the viewer goes back, so nothing outlives the visit it was read for.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A save sends the rows that differ from the server's last answer and only those. The reference's
  /// configuration windows wrote whole settings classes and depended on being the only editor in the
  /// house; here two people can be editing the same server from two televisions, and a screen that pushed
  /// everything it showed would quietly undo whatever the other one had just saved. A difference is
  /// nothing to argue about, and it is also what lets the server keep saying "I do not know that field".
  /// </para>
  /// <para>
  /// What comes back from a save is adopted whole, because the server's answer - not what was typed - is
  /// what is now stored. A section that was never read cannot save: an empty form written over settings
  /// somebody did configure is the accident this rule exists for, exactly as in the server settings row.
  /// </para>
  /// </remarks>
  public sealed partial class SettingSectionViewModel : ObservableObject
  {
    private readonly string _key;
    private readonly Func<IPluginSettingsApi?> _clientProvider;
    private bool _loaded;
    private IReadOnlyList<string> _restartKeys = Array.Empty<string>();
    private string _title = "";
    private string _description = "";
    private string _owner = "";
    private string _status = "";
    private string _restartNote = "";

    public SettingSectionViewModel(SettingSectionSummary summary, Func<IPluginSettingsApi?> clientProvider)
    {
      _key = summary.Key;
      _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
      _title = summary.Title;
      _description = summary.Description;
      _owner = summary.Owner;
    }

    /// <summary>How the server names this section in the address. Never shown.</summary>
    public string Key => _key;

    /// <summary>What the section is called, in the server's words.</summary>
    public string Title
    {
      get => _title;
      private set => SetProperty(ref _title, value);
    }

    /// <summary>The sentence under the title, from the same place.</summary>
    public string Description
    {
      get => _description;
      private set => SetProperty(ref _description, value);
    }

    /// <summary>Whose settings these are - the library's, the server's, one plugin's.</summary>
    public string Owner
    {
      get => _owner;
      private set => SetProperty(ref _owner, value);
    }

    /// <summary>The rows the server listed, in the order it listed them.</summary>
    public ObservableCollection<SettingFieldViewModel> Fields { get; } =
        new ObservableCollection<SettingFieldViewModel>();

    /// <summary>What the last read or write did.</summary>
    public string Status
    {
      get => _status;
      private set => SetProperty(ref _status, value);
    }

    /// <summary>
    /// The fields this last save stored but the running server cannot act on yet. Empty unless a save
    /// actually moved one, so a screen that says "needs a restart" always means it.
    /// </summary>
    public string RestartNote
    {
      get => _restartNote;
      private set => SetProperty(ref _restartNote, value);
    }

    /// <summary>Whether the rows here came from the server. Nothing is sent before they did.</summary>
    public bool IsLoaded => _loaded;

    /// <summary>Whether at least one row holds something other than what the server answered.</summary>
    public bool HasChanges
    {
      get
      {
        foreach (SettingFieldViewModel row in Fields)
        {
          if (row.IsChanged)
            return true;
        }
        return false;
      }
    }

    /// <summary>Read the section's fields. Called when the editor opens, and by ReloadCommand.</summary>
    [RelayCommand]
    public async Task ReloadAsync()
    {
      IPluginSettingsApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet - set one under \"Server address\" first.";
        return;
      }

      try
      {
        Apply(await client.GetSettingSectionAsync(_key));
        Status = "Read from the server.";
        SayWhatCannotBeDrawn();
      }
      catch (ServerApiException e)
      {
        Status = e.StatusCode == 404 || e.StatusCode == 405
            ? "This server does not offer that section over the API - it is probably an older build."
            : "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
    }

    /// <summary>
    /// Send the rows the viewer changed and have the server store them.
    /// </summary>
    /// <remarks>
    /// Refused before anything is sent when a row cannot say what it holds: the server stops a whole
    /// change over one bad field and stores nothing, and a screen that already knew that has no business
    /// making the viewer wait for a round trip to be told it.
    /// </remarks>
    [RelayCommand]
    public async Task SaveAsync()
    {
      if (!_loaded)
      {
        Status = "Nothing loaded from the server yet - reload first, then save.";
        return;
      }

      IPluginSettingsApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet.";
        return;
      }

      SettingSectionWish wish = new SettingSectionWish();
      foreach (SettingFieldViewModel field in Fields)
      {
        if (!field.IsChanged)
          continue;

        if (!field.TryGetValue(out object value))
        {
          Status = "Nothing was sent - " + field.Label + ": " + field.Error;
          return;
        }

        wish.Values[field.Key] = value;
      }

      if (wish.Values.Count == 0)
      {
        Status = "Nothing was changed - nothing was sent.";
        return;
      }

      try
      {
        Apply(await client.PutSettingSectionAsync(_key, wish));

        RestartNote = RestartWords();
        Status = RestartNote.Length == 0 ? "Saved on the server." : RestartNote;
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

    /// <summary>
    /// Take an answer as what is stored: the words, and the value of every row the server listed. Rows the
    /// server no longer lists go, and rows it has started to list appear, because the section's table is
    /// the server's and not this screen's.
    /// </summary>
    private void Apply(SettingSectionState answer)
    {
      Title = answer.Title;
      Description = answer.Description;
      Owner = answer.Owner;

      if (AnswersSameRows(answer))
      {
        for (int i = 0; i < answer.Fields.Count; i++)
          Fields[i].Adopt(answer.Fields[i]);
      }
      else
      {
        Fields.Clear();
        foreach (SettingFieldState field in answer.Fields)
          Fields.Add(new SettingFieldViewModel(field));
      }

      // The line below is set back to empty and refilled from this answer, so a note is never left standing
      // from a save the viewer has since re-read past.
      RestartNote = "";
      _restartKeys = answer.RestartNeededFor;
      OnPropertyChanged(nameof(HasChanges));
      _loaded = true;
    }

    /// <summary>Whether the answer lists exactly the rows already standing, so they are refilled in place.</summary>
    private bool AnswersSameRows(SettingSectionState answer)
    {
      if (answer.Fields.Count != Fields.Count)
        return false;

      for (int i = 0; i < answer.Fields.Count; i++)
      {
        if (Fields[i].Key != answer.Fields[i].Key)
          return false;
      }

      return true;
    }

    /// <summary>
    /// The one sentence about fields that were stored but cannot act yet, naming them by the words their
    /// rows show - or by the server's key when no row carries one, which happens the moment the answer
    /// names a field the screen was never shown. Empty when the answer named none, which is what a read
    /// always answers.
    /// </summary>
    private string RestartWords()
    {
      if (_restartKeys.Count == 0)
        return "";

      // Named by the word the row shows rather than by the key underneath it: the key is how the two
      // machines agree, and a person reading this off a television wants the word they were just looking at.
      string names = "";
      foreach (string key in _restartKeys)
      {
        string named = key;
        foreach (SettingFieldViewModel field in Fields)
        {
          if (field.Key == key && field.Label.Length > 0)
          {
            named = field.Label;
            break;
          }
        }

        names += (names.Length == 0 ? "" : ", ") + named;
      }

      return "Stored, but only takes effect after the server restarts: " + names + ".";
    }

    /// <summary>
    /// Says so when the server offered a kind this screen has no control for. The row stands there drawing
    /// nothing rather than being hidden away, because a section that quietly dropped a field would be
    /// hiding the one thing the viewer needs to know: that the server said more than this television can
    /// hear.
    /// </summary>
    private void SayWhatCannotBeDrawn()
    {
      string names = "";
      foreach (SettingFieldViewModel field in Fields)
      {
        if (!field.IsUndrawable)
          continue;
        names += (names.Length == 0 ? "" : ", ") + field.Label + " (" + field.Kind + ")";
      }

      if (names.Length == 0)
        return;

      Status = "The server offers a kind this screen cannot draw: " + names
          + ". Those fields are shown as nothing and are never sent.";
    }
  }
}
