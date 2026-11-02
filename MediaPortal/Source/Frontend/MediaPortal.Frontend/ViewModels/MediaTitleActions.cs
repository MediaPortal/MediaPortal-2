/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
    General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// What pressing a line of a title's action list does. The lines are named after the answers the reference
  /// registered for a media object, and a line that cannot be answered here does not appear at all.
  /// </summary>
  public enum MediaTitleActionKind
  {
    /// <summary>A line that only says something, because the title has no answer to be had. Never pressed.</summary>
    SaidOnly,

    /// <summary>The reference's <i>Media information</i>, the first line of its list (plugin.xml:270).</summary>
    Information,

    /// <summary>The reference's <i>Set unplayed</i> (plugin.xml:272), the way back from a watched mark.</summary>
    SetUnplayed,

    /// <summary>The reference's <i>Set played</i> (plugin.xml:273), for a title that is not watched through.</summary>
    SetPlayed,

    /// <summary>
    /// The reference's <i>Refresh</i> (plugin.xml:275, <c>RefreshMediaItem.cs:60-69</c>): ask the library to read
    /// the title's file again, keeping the metadata it found.
    /// </summary>
    Refresh
  }

  /// <summary>
  /// One line of a title's action list, in the shape the guide already uses for its own list of answers
  /// (<see cref="TvProgramAction"/>): a word to read, a kind to press, and nothing else.
  /// </summary>
  public sealed class MediaTitleAction
  {
    public MediaTitleAction(MediaTitleActionKind kind, string word)
    {
      Kind = kind;
      Word = word;
    }

    /// <summary>What pressing the line does.</summary>
    public MediaTitleActionKind Kind { get; }

    /// <summary>The word on the line.</summary>
    public string Word { get; }

    /// <summary>Whether a press has anything behind it - the line that only explains something never has.</summary>
    public bool IsCommand
    {
      get { return Kind != MediaTitleActionKind.SaidOnly; }
    }

    public override string ToString()
    {
      return Word;
    }
  }

  /// <summary>
  /// The list of answers a title can be given, worked out the way the reference worked its list out: every
  /// registered action was asked whether it applied to this one object, and what it said decided whether the
  /// line was put up (<c>MediaItemsActionModel.cs:373-392</c>).
  /// </summary>
  /// <remarks>
  /// <para>
  /// The order is the reference's and not a taste: its list was sorted by the <c>Sort</c> string each action
  /// carried in the plugin file, alphabetically (<c>MediaItemsActionModel.cs:376</c>), which puts <i>Media
  /// information</i> (A.0) first and the two watched marks together under C.1 - where <i>Set unplayed</i>
  /// stands first only because it is registered first (plugin.xml:272 before plugin.xml:273), and where the two
  /// never appear together anyway since each asks for the opposite watched state
  /// (<c>AbstractWatchedAction.cs:49-56</c>, <c>SetUnwatched.cs:27-37</c>, <c>SetWatched.cs:27-37</c>).
  /// </para>
  /// <para>
  /// Several of the reference's lines are missing from this list, and each is missing because this front end
  /// has nothing that could answer it rather than because nobody thought of it. <i>Add to playlist</i>
  /// (<c>AddSingleToPlaylist.cs:37-42</c>) hands the title to a queue, and this front end has no queue - it
  /// opens one title at a time. <i>Reimport</i> and <i>Subtitle search</i> open a match picker belonging to
  /// their plugins (<c>ReimportMediaItem.cs:44-73</c>, <c>SearchForSubtitles.cs:50-63</c>), which is a search
  /// across online scrapers rather than a call to the library; <i>Eject</i> asks a drive this front end does
  /// not browse (<c>EjectMediaItem.cs:41-85</c>); and <i>Delete from storage</i> deletes the file itself
  /// (<c>DeleteFromStorage.cs:97-127</c>), which is a decision for whoever owns the library rather than a
  /// missing button.
  /// </para>
  /// </remarks>
  public static class MediaTitleActions
  {
    /// <summary>
    /// The answers one title has, in the reference's order.
    /// </summary>
    /// <param name="title">The title the list is about; nothing is offered for no title.</param>
    /// <param name="serverAnswers">
    /// Whether a server stands behind the title, which the reference decides differently per line: its watched
    /// marks ask for an object the library manages (<c>AbstractWatchedAction.cs:51-56</c> against
    /// <c>AbstractMediaItemAction.cs:44-47</c>), and a title this front end only knows locally has no library to
    /// be told anything.
    /// </param>
    /// <param name="hostShowsInformation">
    /// Whether the screen asking has somewhere to put the details. The reference pushed its media information
    /// screen from anywhere (<c>MediaInfoAction.cs:53-62</c>, deferred until the menu was left); this front end
    /// shows a title's facts in the browser's detail panel, so the line is only offered where reading them has
    /// an answer, and is withheld rather than left to do nothing.
    /// </param>
    public static List<MediaTitleAction> For(TitleViewModel? title, bool serverAnswers,
        bool hostShowsInformation)
    {
      List<MediaTitleAction> actions = new List<MediaTitleAction>();
      if (title == null)
        return actions;

      if (serverAnswers && hostShowsInformation)
        actions.Add(new MediaTitleAction(MediaTitleActionKind.Information, "Media information"));

      // The reference reads the watched state off one number - this viewer's percentage - and offers exactly one
      // of the two marks (AbstractWatchedAction.cs:49-52), which is the same number this row already reads for
      // its tick.
      bool watched = title.Seen;
      if (serverAnswers && watched)
        actions.Add(new MediaTitleAction(MediaTitleActionKind.SetUnplayed, "Mark unwatched"));
      if (serverAnswers && !watched)
        actions.Add(new MediaTitleAction(MediaTitleActionKind.SetPlayed, "Mark watched"));

      // The reference asked a title whether it had a file to look at again before offering this line, and it
      // could ask because the item was in its hand (RefreshMediaItem.cs:44-49: a resource locator has to exist).
      // Here the file is on the server and not here - a path this machine cannot open may be the right one there,
      // which is the same reason a row is never called unplayable for a path only the server can read - so every
      // title the library handed over is offered the line, and the server answers with its refusal when its own
      // condition is not met.
      if (serverAnswers)
        actions.Add(new MediaTitleAction(MediaTitleActionKind.Refresh, "Refresh"));

      if (actions.Count == 0)
      {
        // The reference hid its menu when nothing applied to the object (MediaItemsActionModel.cs:402). This
        // front end says the same thing out loud instead, because a press that opens an empty frame teaches the
        // viewer nothing about why the frame is empty.
        actions.Add(new MediaTitleAction(MediaTitleActionKind.SaidOnly,
            serverAnswers
                ? "There is nothing to change about this title"
                : "This title is not on the server, so there is nothing to tell"));
      }

      return actions;
    }
  }

  /// <summary>
  /// The model of the question "what shall be done with this title", for a screen that raises the question as a
  /// dialog of its own - the home wall, which has no detail panel to write into and no shelf to stand a dialog
  /// over. The media browser asks the same table but renders it inside its own screen, so it builds the list
  /// through <see cref="MediaTitleActions.For"/> without this model.
  /// </summary>
  /// <remarks>
  /// The dialog is a question about one title and holds no knowledge of its own: what a press means is handed in
  /// as the delegate below, which keeps the answer with the screen that can give it. That is how the guide's own
  /// action dialog works, whose model is the guide (<c>TvProgramActionsView</c>) - a copy of the model could
  /// only disagree with the wall about which tile was pressed.
  /// </remarks>
  public sealed class MediaTitleActionsModel : INotifyPropertyChanged
  {
    private readonly Func<TitleViewModel, MediaTitleActionKind, Task> _press;
    private TitleViewModel? _about;
    private string _status = "";

    /// <param name="press">
    /// What a line does: told which title was asked about and which answer was stood on. It is called after the
    /// dialog has been taken away, so that whatever opens opens over the wall and not over a question that has
    /// already been answered.
    /// </param>
    public MediaTitleActionsModel(Func<TitleViewModel, MediaTitleActionKind, Task> press)
    {
      _press = press ?? throw new ArgumentNullException(nameof(press));
    }

    /// <summary>The lines the dialog shows, in the reference's order.</summary>
    public ObservableCollection<MediaTitleAction> Actions { get; } =
        new ObservableCollection<MediaTitleAction>();

    /// <summary>The title the dialog is about, or nothing while no dialog stands open.</summary>
    public TitleViewModel? About
    {
      get { return _about; }
      private set
      {
        _about = value;
        Raise();
      }
    }

    /// <summary>
    /// Whether a dialog stands open, which is what the host's back key looks at before it leaves the screen.
    /// </summary>
    public bool IsOpen
    {
      get { return _about != null; }
    }

    /// <summary>
    /// The heading, which is the reference's dialog heading <c>[Media.MediaItemsActionDialogHeader]</c> = "Media
    /// item actions" (plugin.xml:125-128).
    /// </summary>
    public string Heading
    {
      get { return "Media item actions"; }
    }

    /// <summary>
    /// Which title the question is about, written under the heading: a dialog that only said "Media item actions"
    /// over a wall of tiles would leave the viewer looking for the tile they pressed.
    /// </summary>
    public string Words
    {
      get { return _about == null ? "" : _about.Name; }
    }

    /// <summary>What the last press did, said where the press happened.</summary>
    public string Status
    {
      get { return _status; }
      private set
      {
        _status = value ?? "";
        Raise();
      }
    }

    /// <summary>
    /// Puts a sentence into the dialog that stands open, for the answer a host gives while the question is still
    /// on screen. A screen's own line of speech is no place for such an answer: the home wall wears its status
    /// line in a head band that its content pane covers, and the browser's stands over a shelf that is only read
    /// when it is empty - a sentence written there is written where nobody is looking.
    /// </summary>
    public void Say(string words)
    {
      Status = words;
    }

    /// <summary>Said when the answer was given and the dialog has to be taken away.</summary>
    public event Action? Closed;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Puts the question up for one title: what can be asked of it now, in the reference's order, with nothing
    /// asked of the server first - every line here is decided by numbers this row already carries.
    /// </summary>
    public void Open(TitleViewModel title, bool serverAnswers, bool hostShowsInformation)
    {
      if (title == null)
        throw new ArgumentNullException(nameof(title));

      About = title;
      Status = "";
      Actions.Clear();
      foreach (MediaTitleAction action in MediaTitleActions.For(title, serverAnswers, hostShowsInformation))
        Actions.Add(action);

      Raise();
    }

    /// <summary>Takes the dialog away, which leaves the viewer standing on the tile they pressed.</summary>
    public void Close()
    {
      if (_about == null)
        return;

      About = null;
      Closed?.Invoke();
    }

    /// <summary>
    /// Gives the answer that is stood on. The dialog is put away first and the answer is given afterwards, so
    /// that what an answer opens - a detail reading, a rewritten tile - stands over the wall rather than over a
    /// question that has already been answered.
    /// </summary>
    /// <remarks>
    /// One kind of answer is kept under the dialog on purpose: the ask to read a title's file again. The library
    /// marks the title dirty and hands the folder to its importer worker
    /// (<c>MediaLibrary.cs:1008-1034</c>), which reads it when it comes to it, so at the moment of the press
    /// nothing on the wall moves - and a button that takes the question away and leaves the wall exactly as it
    /// was reads as a broken button rather than as an order given. The ask therefore leaves the dialog standing
    /// and answers in it, in the line under the buttons, and the viewer puts the dialog away themselves.
    /// </remarks>
    public async Task PressAsync(MediaTitleAction? action)
    {
      TitleViewModel? title = _about;
      if (title == null || action == null || !action.IsCommand)
        return;

      bool answeredInPlace = action.Kind == MediaTitleActionKind.Refresh;
      if (answeredInPlace)
        Status = "";
      else
        Close();

      try
      {
        await _press(title, action.Kind).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Status = "Asking the library failed: " + ex.Message;
      }
    }

    private void Raise([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
