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
  /// <b>Why the library's own pictures are not here.</b> The screen asks for the <c>plugin</c> area only.
  /// The reference hung the answers about a title's pictures - how many of each kind it keeps, and whether
  /// the ones lying next to the files are copied - under its <c>/Configuration/Settings/Media</c> node,
  /// beside the media itself and never under a plugin node, so those rows are asked for by area and shown
  /// where a household looks for pictures: the media settings screen. Nothing is duplicated for that; the
  /// server keeps one answer per question and says which screen it belongs to.
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

    /// <summary>
    /// The one section this screen is the editor of, or <c>null</c> for the screen that lists everything the
    /// server offers. A media page in the settings wall is the second kind: the wall already <i>is</i> the list,
    /// so the page opened through one door shows that door's fields and nothing else - the reference's
    /// configuration screen did exactly that, one page per node of the tree, with the wall of pages beside it
    /// rather than a list inside the page.
    /// </summary>
    private readonly SettingSectionSummary? _only;

    private string _status = "";
    private bool _isBusy;
    private SettingSectionViewModel? _section;

    public PluginSettingsViewModel(Func<IPluginSettingsApi?> clientProvider)
        : this(clientProvider, null)
    {
    }

    /// <param name="clientProvider">Where the server is reached, asked again every read.</param>
    /// <param name="only">
    /// The one section to open, for a screen that stands behind a door of its own. Left out, the screen is the
    /// list of everything the server offers in the plugin area.
    /// </param>
    public PluginSettingsViewModel(Func<IPluginSettingsApi?> clientProvider, SettingSectionSummary? only)
    {
      _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
      _only = only;
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

    /// <summary>
    /// Whether this screen has a list of its own at all. Behind a door of the media wall it has none - the wall
    /// is the list - and a page that offered a list of one was a page asking the viewer to choose what they have
    /// already chosen.
    /// </summary>
    public bool HasList => _only == null;

    /// <summary>Whether the list half of the view stands up: a list, and nothing open in front of it.</summary>
    public bool ShowsList => HasList && Section == null;

    /// <summary>
    /// What the screen says about itself above everything else. A page behind a door speaks of its own section,
    /// not of the server's plugins; the wall it stands in already said where it came from.
    /// </summary>
    public string Intro => _only == null
        ? "These are settings the server keeps for its own parts and plugins, stored on its machine. What a "
          + "server is willing to be told is the server's answer, so this list is read from there and never "
          + "kept here."
        : "This page holds what the server keeps about \"" + _only.Title + "\", stored on its machine and read "
          + "from there when the page opens. It stands among this household's media settings because the "
          + "answers are about the library; the keeping is the server's.";

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
          OnPropertyChanged(nameof(ShowsList));
        }
      }
    }

    /// <summary>Whether an editor stands up. The inverse, for the view's visibility bindings.</summary>
    public bool HasSection => Section != null;

    /// <summary>What the list is called, and what the editor is called while one is open.</summary>
    public string Title => Section?.Title ?? _only?.Title ?? "Plugin settings";

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

      if (_only != null)
      {
        // Behind a door of its own there is nothing to list; opening the page is opening the section. Read again
        // every time, because the answer is the server's and another television may have answered differently
        // while this page was closed.
        await OpenAsync(_only).ConfigureAwait(true);
        return;
      }

      IsBusy = true;
      try
      {
        SettingSectionsAnswer answer = await client.GetSettingSectionsAsync(SettingSectionAreas.Plugin);
        Sections.Clear();
        foreach (SettingSectionSummary summary in answer.Sections)
        {
          // Asked by area, and filtered again here: a picture question belongs to the media screen, and an
          // older server that answers every area at once must not push one onto this screen. A section naming
          // no area at all is from a server that never grouped anything, and stays where it was.
          if (summary.Area == SettingSectionAreas.Media)
            continue;
          Sections.Add(summary);
        }

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
      if (_only != null)
      {
        // There is no list to put back - the wall outside this page is the way back, and the hub works that
        // key. What the screen can do is read its one section again.
        await OpenAsync(_only).ConfigureAwait(true);
        return;
      }

      Section = null;
      await ReloadAsync();
    }
  }
}
