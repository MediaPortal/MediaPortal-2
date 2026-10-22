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
  /// The settings a household may edit from a television but nobody can reach, because the thing that used
  /// to edit them was a window on the server's own desktop and a headless server has no desktop. This screen
  /// asks the server what it is willing to be told, lists that, and edits what the viewer walks up to.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>The server owns the table.</b> The list is read when the screen opens and is never kept here: the
  /// day somebody opens another section in the server's catalog, it stands on this television without this
  /// assembly being rebuilt, which is the whole reason there is one hub row instead of one row per section.
  /// The front end has no idea what a media library can be told, and is not supposed to.
  /// </para>
  /// <para>
  /// <b>One screen, two halves.</b> The list and the editor share a screen, so opening a section does not
  /// push a frame onto a trail the viewer then has to unwind; the editor is the same screen with a section
  /// in it, and Back puts the list back. That is also why Back reads the summaries again - a count shown
  /// before a save would otherwise be the count from before it.
  /// </para>
  /// <para>
  /// A server that cannot be reached, or that refuses the call, leaves a sentence and no form: an empty
  /// editor is the one thing this screen must never show, because it looks like a server with no settings
  /// and reads as an invitation to save one.
  /// </para>
  /// </remarks>
  public sealed partial class PluginSettingsViewModel : ObservableObject
  {
    private readonly Func<IPluginSettingsApi?> _clientProvider;
    private string _status = "";
    private bool _isBusy;
    private SettingSectionViewModel? _section;

    public PluginSettingsViewModel(Func<IPluginSettingsApi?> clientProvider)
    {
      _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
    }

    /// <summary>
    /// What the server offers, as it offered it. Read again on open and on Back, and never written to here.
    /// </summary>
    public ObservableCollection<SettingSectionSummary> Sections { get; } =
        new ObservableCollection<SettingSectionSummary>();

    /// <summary>
    /// Whether the server listed anything. A screen that showed an empty list and a screen that showed a
    /// server with nothing open are different sentences, and the view says the one that is true.
    /// </summary>
    public bool HasSections => Sections.Count > 0;

    /// <summary>What the last call did, or why it could not.</summary>
    public string Status
    {
      get => _status;
      private set => SetProperty(ref _status, value);
    }

    /// <summary>Whether a call is out. The view rests its presses while one is.</summary>
    public bool IsBusy
    {
      get => _isBusy;
      private set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// The section being edited, or nothing while the list is up. The editor half of the view is this
    /// section's, so a null here is the list and anything else is the editor.
    /// </summary>
    public SettingSectionViewModel? Section
    {
      get => _section;
      private set
      {
        if (SetProperty(ref _section, value))
        {
          OnPropertyChanged(nameof(HasSection));
          OnPropertyChanged(nameof(Title));
        }
      }
    }

    /// <summary>Whether an editor stands up. The inverse, for the view's visibility bindings.</summary>
    public bool HasSection => Section != null;

    /// <summary>What the list is called, and what the editor is called while one is open.</summary>
    public string Title => Section == null ? "Plugin settings" : Section.Title;

    /// <summary>Read the list of sections. Called when the row opens, and by ReloadCommand.</summary>
    [RelayCommand]
    public async Task ReloadAsync()
    {
      IPluginSettingsApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet - set one under \"Server address\" first.";
        return;
      }

      IsBusy = true;
      try
      {
        SettingSectionsAnswer answer = await client.GetSettingSectionsAsync();
        Sections.Clear();
        foreach (SettingSectionSummary summary in answer.Sections)
          Sections.Add(summary);

        Status = Sections.Count == 0
            ? "The server has opened no settings for editing over its API."
            : string.Format("The server offers {0} section(s) to edit.", Sections.Count);
      }
      catch (ServerApiException e)
      {
        Status = e.StatusCode == 404 || e.StatusCode == 405
            ? "This server does not offer its settings over the API - it is probably an older build."
            : "The server refused: " + e.Message;
      }
      catch (Exception e)
      {
        Status = "The server could not be reached: " + e.Message;
      }
      finally
      {
        IsBusy = false;
        OnPropertyChanged(nameof(HasSections));
      }
    }

    /// <summary>
    /// Open one section: read its fields and show the editor. A section that will not read is not shown -
    /// the sentence the server gave lands in this screen's status line instead, because an editor of
    /// nothing would be a lie drawn around a real heading.
    /// </summary>
    [RelayCommand]
    public async Task OpenAsync(SettingSectionSummary summary)
    {
      if (summary == null)
      {
        Status = "Nothing is picked.";
        return;
      }

      IPluginSettingsApi? client = _clientProvider();
      if (client == null)
      {
        Status = "No server address yet - set one under \"Server address\" first.";
        return;
      }

      SettingSectionViewModel section = new SettingSectionViewModel(summary, _clientProvider);
      IsBusy = true;
      try
      {
        await section.ReloadAsync();
      }
      finally
      {
        IsBusy = false;
      }

      if (!section.IsLoaded)
      {
        Section = null;
        Status = summary.Title + ": " + section.Status;
        return;
      }

      Section = section;
      Status = section.Status;
    }

    /// <summary>Press one of the list's rows. The row is a summary's, so the ask comes back here.</summary>
    public void Open(SettingSectionSummary summary)
    {
      _ = OpenAsync(summary);
    }

    /// <summary>
    /// Put the list back and read it again, so a count shown beside a section is the count the server has
    /// now rather than the one it had before the last save.
    /// </summary>
    [RelayCommand]
    public async Task BackAsync()
    {
      Section = null;
      await ReloadAsync();
    }
  }
}
