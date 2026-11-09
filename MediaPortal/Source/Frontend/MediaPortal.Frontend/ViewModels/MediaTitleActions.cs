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
using MediaPortal.Frontend.Api;

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
    Refresh,

    /// <summary>
    /// The short film a title advertises itself with. Offered only where the library knows one, and standing for
    /// the question rather than the answer: a title with three trailers asks back which of them, and the lines of
    /// that question are <see cref="TrailerChoice"/>.
    /// </summary>
    Trailer,

    /// <summary>
    /// One of the trailers of a title, on the second step of the same dialog. Carries the trailer it stands for
    /// on <see cref="MediaTitleAction.Trailer"/>.
    /// </summary>
    TrailerChoice,

    /// <summary>
    /// One picture size of a trailer, on the step after it: a page at a video site is not one film but several,
    /// and which one is meant cannot be read off the page. Carries the film it would play on
    /// <see cref="MediaTitleAction.Trailer"/> and the size on <see cref="MediaTitleAction.Quality"/>.
    /// </summary>
    TrailerQuality
  }

  /// <summary>
  /// One line of a title's action list, in the shape the guide already uses for its own list of answers
  /// (<see cref="TvProgramAction"/>): a word to read, a kind to press, and nothing else.
  /// </summary>
  public sealed class MediaTitleAction
  {
    public MediaTitleAction(MediaTitleActionKind kind, string word, MediaTrailer? trailer = null,
        Services.TrailerQuality? quality = null)
    {
      Kind = kind;
      Word = word;
      Trailer = trailer;
      Quality = quality;
    }

    /// <summary>What pressing the line does.</summary>
    public MediaTitleActionKind Kind { get; }

    /// <summary>The word on the line.</summary>
    public string Word { get; }

    /// <summary>
    /// The trailer this line stands for, which only a <see cref="MediaTitleActionKind.TrailerChoice"/> or a
    /// <see cref="MediaTitleActionKind.TrailerQuality"/> line has. Null on every other line.
    /// </summary>
    public MediaTrailer? Trailer { get; }

    /// <summary>
    /// The picture size this line stands for, which only a <see cref="MediaTitleActionKind.TrailerQuality"/> line
    /// has. Null on every other line, and null is "the one that is there without asking".
    /// </summary>
    public Services.TrailerQuality? Quality { get; }

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

      // The short film a title advertises itself with, offered where the library knows one. It stands next to the
      // reading of the title's facts because that is the same question from the same moment - a viewer who has not
      // seen the film yet wants to know what it is - and before the two lines that change something, which the
      // reference also kept at the end of its list.
      //
      // Offered even when nothing about the trailer can be played, because a title whose tagging tool wrote an
      // address no player here opens still has a trailer, and the viewer standing there is the only person who can
      // do something about it. Pressing the line then says what could not be made of it.
      if (title.HasTrailers)
        actions.Add(new MediaTitleAction(MediaTitleActionKind.Trailer, "Trailer"));

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

    /// <summary>
    /// The lines of the question the <i>Trailer</i> line asks back when a title has more than one trailer: one
    /// line per trailer, in the order the library named them.
    /// </summary>
    public static List<MediaTitleAction> TrailerChoicesOf(TitleViewModel? title)
    {
      var choices = new List<MediaTitleAction>();
      if (title == null)
        return choices;

      // Several trailers of one title are commonly all of one kind - three films from a video site, which is
      // what a tagging tool writes when it copies whatever a studio publishes - and then every line would carry
      // the same three words. Numbered, they are at least two lines a viewer can point at.
      bool several = title.Trailers.Count > 1;
      int at = 0;
      foreach (MediaTrailer trailer in title.Trailers)
      {
        at++;
        string words = WordsFor(trailer);
        if (several && words.StartsWith("Trailer ", StringComparison.Ordinal))
          words = "Trailer " + at + words.Substring("Trailer".Length);
        choices.Add(new MediaTitleAction(MediaTitleActionKind.TrailerChoice, words, trailer));
      }
      return choices;
    }

    /// <summary>
    /// The lines of the question "which picture size", which the <i>Trailer</i> line asks of a film that lives at
    /// a video site. The trailer is carried along on every line, so that the answer is one press away from being
    /// a whole ask; the words and their order are the resolver's (<see cref="Services.VideoSiteStream.Qualities"/>),
    /// because they are the shapes that site was measured to answer and not a taste.
    /// </summary>
    public static List<MediaTitleAction> QualityChoicesOf(MediaTrailer? trailer)
    {
      var choices = new List<MediaTitleAction>();
      if (trailer == null)
        return choices;

      foreach (Services.TrailerQuality quality in Services.VideoSiteStream.Qualities)
        choices.Add(new MediaTitleAction(MediaTitleActionKind.TrailerQuality, quality.Display, trailer, quality));

      return choices;
    }

    /// <summary>
    /// Whether this trailer has to be asked about: a film at a video site stands behind a page in several sizes,
    /// while a file on the shelf and an address that already is a film are one answer and are played at once.
    /// </summary>
    public static bool AsksForQuality(MediaTrailer? trailer)
    {
      return trailer != null && Services.VideoSiteStream.IsSitePage(trailer.Source);
    }

    /// <summary>
    /// What standing on the <i>Trailer</i> line plays straight away: the title's one and only trailer. Nothing
    /// comes back when the title carries several, because then the line was a question rather than an answer and
    /// the host has to ask which of them - with <see cref="TrailerChoicesOf"/>, in the same dialog.
    /// </summary>
    public static MediaTitleAction? TrailerToPlay(TitleViewModel? title)
    {
      List<MediaTitleAction> choices = TrailerChoicesOf(title);
      return choices.Count == 1 ? choices[0] : null;
    }

    /// <summary>
    /// What one trailer is called on a line: its own name, and what kind of thing it is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The kind is said because the two kinds are two different evenings to a viewer: a film the library holds
    /// starts at once and plays as smooth as the title itself, while a film at a video site needs that site to
    /// be answering and arrives at whatever picture its page has. A wall of three lines that all just said
    /// "Trailer" would hide that behind the third word.
    /// </para>
    /// <para>
    /// A trailer whose address nothing can open says so on its line rather than being left out. It is the
    /// library's own trailer, and a list that silently drops it leaves the viewer guessing whether the film was
    /// never there or was merely lost on the way.
    /// </para>
    /// </remarks>
    public static string WordsFor(MediaTrailer trailer)
    {
      string name = string.IsNullOrWhiteSpace(trailer.Label) ? "Trailer" : trailer.Label!.Trim();
      if (!trailer.IsPlayable)
        return name + " (no address to play)";
      return name + " (" + trailer.KindWord + ")";
    }
  }

  /// <summary>
  /// What a trailer stands for: which trailer of which title. Handed to the host together with the line that was
  /// pressed, because several trailers of one title share the word "Trailer" on their lines, and because the
  /// player has to be told whose film this is - the name it wears is the title's, not the file's.
  /// </summary>
  /// <param name="Title">The title whose trailer was asked for.</param>
  /// <param name="Trailer">The trailer itself.</param>
  /// <param name="Quality">
  /// The picture size the viewer stood on, which only a trailer behind a video site's page is ever asked about.
  /// Null means "the one that is there without asking", which is also what a file on the shelf is answered with.
  /// </param>
  public sealed record TrailerRequest(TitleViewModel Title, MediaTrailer Trailer,
      Services.TrailerQuality? Quality = null);

  /// <summary>
  /// A screen that asked the trailer question and can therefore carry its answer: the media browser's menu and the
  /// home wall's dialog.
  /// </summary>
  /// <remarks>
  /// A trailer behind a video site's page cannot be answered on the spot - the page has to be asked, and a big
  /// picture has to be brought over first - so the answer arrives a while after the press, sometimes as a refusal.
  /// It belongs where the question was asked, which is the only place a viewer is still looking for it. Saying it
  /// anywhere else means saying it in a playback nudge whose line is too short for a sentence, and a viewer is
  /// left reading half a word over a shelf.
  /// </remarks>
  public interface ITrailerAnswerHost
  {
    /// <summary>
    /// Puts the sentence into the dialog the trailer was asked in, and puts that dialog up if it has been taken
    /// away in the meantime. Empty words take the dialog away again.
    /// </summary>
    void AnswerTrailer(string words);
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
    private readonly Func<TitleViewModel, MediaTitleAction, Task> _press;
    private TitleViewModel? _about;
    private string _status = "";

    /// <param name="press">
    /// What a line does: told which title was asked about and which line was stood on. It is called after the
    /// dialog has been taken away, so that whatever opens opens over the wall and not over a question that has
    /// already been answered.
    /// </param>
    public MediaTitleActionsModel(Func<TitleViewModel, MediaTitleAction, Task> press)
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

      // A title may advertise itself with more than one short film, and then the word "Trailer" is not yet an
      // answer to anything. The dialog asks back by putting its own lines out - the same dialog, now holding one
      // line per film - instead of opening a second frame over the first, which is what the reference did nowhere
      // and what would leave the viewer standing in a room they did not walk into.
      if (action.Kind == MediaTitleActionKind.Trailer)
      {
        MediaTitleAction? only = MediaTitleActions.TrailerToPlay(title);
        if (only == null)
        {
          AskForTrailer(title);
          return;
        }
        action = only;

        // A film behind a video site's page is several films in several sizes, and the page says nothing about
        // which was meant. The same dialog asks that next - see MediaTitleActions.AsksForQuality.
        if (MediaTitleActions.AsksForQuality(action.Trailer))
        {
          AskForQuality(action.Trailer);
          return;
        }
      }

      bool answeredInPlace = action.Kind == MediaTitleActionKind.Refresh;
      if (answeredInPlace)
        Status = "";
      else
        Close();

      try
      {
        await _press(title, action).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Status = "Asking the library failed: " + ex.Message;
      }
    }

    /// <summary>
    /// Turns the open dialog into the question "which of these", for a title with several trailers. The heading
    /// keeps naming the title, so the viewer never loses sight of which film the films belong to.
    /// </summary>
    private void AskForTrailer(TitleViewModel title)
    {
      Actions.Clear();
      foreach (MediaTitleAction choice in MediaTitleActions.TrailerChoicesOf(title))
        Actions.Add(choice);
      Status = "Which trailer?";
      Raise();
    }

    /// <summary>
    /// Turns the open dialog into the question "which picture size", for a trailer that stands behind a page at a
    /// video site rather than in a file. One press is left: every line carries the film and the size.
    /// </summary>
    private void AskForQuality(MediaTrailer? trailer)
    {
      Actions.Clear();
      foreach (MediaTitleAction choice in MediaTitleActions.QualityChoicesOf(trailer))
        Actions.Add(choice);
      Status = "";
      Raise();
    }

    private void Raise([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
