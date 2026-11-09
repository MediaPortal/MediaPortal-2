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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Services;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The list of answers a title can be given, which the media browser's dialog and the home wall's dialog both
  /// read out of one table.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The table stands in for the reference's <c>MediaItemsActionModel.FillItemsList</c>, which asked every
  /// registered action whether it applied to the object in hand and put the survivors up sorted by the string the
  /// plugin file gave each of them (<c>MediaItemsActionModel.cs:373-392</c>, <c>:376</c>). Both halves of that are
  /// measured here - which lines a given title produces, and in which order - because the order was the reference's
  /// arrangement and not a happenstance, and because a line that appears next to its own opposite would be the
  /// reference's own bug: its two watched entries ask for opposite watched states and can never stand together
  /// (<c>AbstractWatchedAction.cs:49-52</c>, <c>SetUnwatched.cs:27-37</c>, <c>SetWatched.cs:27-37</c>).
  /// </para>
  /// <para>
  /// The presses are measured too, up to the point where a screen takes over: what a mark sends to the server and
  /// what the row says afterwards is the same playback report the browser already sent by hand, and a table that
  /// offered a line nothing answered for would be worse than the single button it replaced.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestMediaTitleActions
  {
    private static readonly Uri SERVER = new Uri("http://127.0.0.1:1/MediaPortal/Server/");

    /// <summary>
    /// One title as the server hands it over, with the one figure the table reads: how far this viewer got.
    /// </summary>
    private static TitleViewModel Title(string name, int? percentage, int playCount = 0)
    {
      return new TitleViewModel(new MediaTitle(Guid.NewGuid(), name, null, 2024, null, null, false,
          PlayCount: playCount, Percentage: percentage));
    }

    private static MediaTitleActionKind[] KindsOf(List<MediaTitleAction> actions)
    {
      return actions.Select(a => a.Kind).ToArray();
    }

    #region Which lines a title produces, in the reference's order

    [Test]
    public void ATitleWatchedThroughIsOfferedTheWayBackAndNotTheWayForwardAgain()
    {
      List<MediaTitleAction> actions = MediaTitleActions.For(Title("Alien", 100, 2), true, true);

      Assert.That(KindsOf(actions),
          Is.EqualTo(new[]
          {
            MediaTitleActionKind.Information, MediaTitleActionKind.SetUnplayed, MediaTitleActionKind.Refresh
          }),
          "the reference offers a title at a hundred per cent its way back to unplayed and never the mark it "
          + "already carries (AbstractWatchedAction.cs:49-52, SetUnwatched.cs:27-37), and the refresh line stands "
          + "behind them because its key is C.3 (plugin.xml:275)");
      Assert.That(actions.Select(a => a.Word),
          Is.EqualTo(new[] { "Media information", "Mark unwatched", "Refresh" }),
          "the reference's list is sorted by the string each action carries, which puts Media information (A.0) "
          + "above the watched marks (C.1) and registers Set unplayed first of those two (plugin.xml:270-272)");
    }

    [Test]
    public void ATitleNobodyHasFinishedIsOfferedTheMarkAndTheInformation()
    {
      List<MediaTitleAction> actions = MediaTitleActions.For(Title("Alien: Earth", 0), true, true);

      Assert.That(KindsOf(actions),
          Is.EqualTo(new[]
          {
              MediaTitleActionKind.Information, MediaTitleActionKind.SetPlayed, MediaTitleActionKind.Refresh
          }),
          "a title below a hundred per cent is the reference's \"not watched\", which is offered Set played "
          + "(SetWatched.cs:27-37), and the ask to read its file again, which the reference offers whatever the "
          + "number says (RefreshMediaItem.cs:44-49)");
      Assert.That(actions.Select(a => a.Word),
          Is.EqualTo(new[] { "Media information", "Mark watched", "Refresh" }));
    }

    [Test]
    public void ATenPercentagesTitleIsStillSomethingToFinishAndNotSomethingToUndo()
    {
      // The number the reference reads the watched state off is a hundred, not "anything above nothing"
      // (AbstractWatchedAction.cs:49-50), so a title halfway stands on the same side of the line as one nobody
      // has opened.
      List<MediaTitleAction> actions = MediaTitleActions.For(Title("Army of Thieves", 40, 0), true, true);

      Assert.That(actions.Any(a => a.Kind == MediaTitleActionKind.SetPlayed), Is.True,
          "a title forty per cent watched has to be offered the way forward");
      Assert.That(actions.Any(a => a.Kind == MediaTitleActionKind.SetUnplayed), Is.False,
          "and it has not earned the way back yet, which belongs to a hundred per cent");
    }

    [Test]
    public void APlayCountWithoutAPercentageStillCountsAsWatched()
    {
      // A server that answers no percentage - one older than the field - still says how often a title was
      // finished, and the reference took a count above nought to mean a hundred (PlayableMediaItem.cs:130-137).
      List<MediaTitleAction> actions = MediaTitleActions.For(Title("Nimmerland", null, 1), true, true);

      Assert.That(actions.Any(a => a.Kind == MediaTitleActionKind.SetUnplayed), Is.True,
          "a title the library says was watched once is offered the way back even where no percentage was written");
    }

    [Test]
    public void AWallThatShowsNoFactsLeavesTheInformationLineAway()
    {
      // The reference pushed its media information screen from anywhere; this front end writes a title's facts
      // into the browser's detail panel, which the home wall has no door to. A line that could be pressed and
      // would do nothing is the thing the reference's own availability question existed to prevent
      // (MediaItemsActionModel.cs:378).
      List<MediaTitleAction> actions = MediaTitleActions.For(Title("Alien", 100, 2), true, false);

      Assert.That(actions.Any(a => a.Kind == MediaTitleActionKind.Information), Is.False,
          "the wall was offered a line it has nowhere to take the viewer to");
      Assert.That(KindsOf(actions), Is.EqualTo(new[] { MediaTitleActionKind.SetUnplayed, MediaTitleActionKind.Refresh }),
          "and the two lines the wall can ask of the server itself are still offered");
    }

    [Test]
    public void ATitleNobodyAnswersForSaysSoInsteadOfOfferingALine()
    {
      List<MediaTitleAction> actions = MediaTitleActions.For(Title("A file on this machine", 0), false, true);

      Assert.That(actions, Has.Count.EqualTo(1),
          "the reference hid its menu when nothing applied (MediaItemsActionModel.cs:402); this front end says "
          + "why in one line instead of leaving the viewer with a frame with a hole in it");
      Assert.That(actions[0].Kind, Is.EqualTo(MediaTitleActionKind.SaidOnly));
      Assert.That(actions[0].IsCommand, Is.False, "a line that explains itself is not an answer to be pressed");
    }

    [Test]
    public void NoTitleProducesNoLinesAtAll()
    {
      Assert.That(MediaTitleActions.For(null, true, true), Has.Count.EqualTo(0),
          "a dialog about no title has nothing to say, and inventing a title for it would be worse");
    }

    #endregion

    #region What a press does

    /// <summary>
    /// A server that answers the one question a line of this dialog asks: how far a title was watched. It writes
    /// down what it was told and answers with what a library answers - its own figures back, not the caller's.
    /// </summary>
    private class MarkedServer : HttpMessageHandler
    {
      /// <summary>The percentages this server was told, in the order it was told them.</summary>
      internal List<int> Told { get; } = new List<int>();

      /// <summary>Every path this server was asked about, in the order it was asked.</summary>
      internal List<string> Asked { get; } = new List<string>();

      /// <summary>What the next answer should say how far the title is watched.</summary>
      internal int AnswersPercentage { get; set; }

      /// <summary>What the next answer should say how often the title was finished.</summary>
      internal int AnswersPlayCount { get; set; }

      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;
        Asked.Add(path);
        string body = request.Content == null
            ? ""
            : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
        string[] segments = path.Split('/');
        string idText = segments[segments.Length - 2];
        if (!Guid.TryParse(idText, out Guid askedFor))
          askedFor = Guid.Empty;

        // The one line that asks for something rather than reporting a number: it carries no percentage, and a
        // server that wrote one down for it would have the tests above read a report that never happened.
        if (path.EndsWith("/refresh", StringComparison.OrdinalIgnoreCase))
        {
          string took = "{\"id\":\"" + askedFor.ToString("D") + "\",\"error\":null}";
          return new HttpResponseMessage(HttpStatusCode.OK)
          {
            RequestMessage = request,
            Content = new StringContent(took, Encoding.UTF8, "application/json")
          };
        }

        int percentage = 0;
        int at = body.IndexOf("percentage", StringComparison.OrdinalIgnoreCase);
        if (at >= 0)
        {
          int digits = body.IndexOfAny(new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }, at);
          int end = digits;
          while (end < body.Length && char.IsDigit(body[end]))
            end++;
          if (digits >= 0 && end > digits)
            percentage = int.Parse(body.Substring(digits, end - digits));
        }

        // Only a report of how far a title was watched belongs to the record; a screen that asked for the title
        // or for its picture passed this way with nothing to write down, and a test reading the record would
        // otherwise count the asking as the telling.
        if (HttpMethod.Post.Equals(request.Method) && path.EndsWith("/playback", StringComparison.OrdinalIgnoreCase))
          Told.Add(percentage);

        string json = "{\"id\":\"" + askedFor.ToString("D") + "\",\"playCount\":" + AnswersPlayCount
            + ",\"lastPlayed\":null,\"watched\":" + (AnswersPercentage >= 100 ? "true" : "false")
            + ",\"error\":null,\"resume\":" + (AnswersPercentage > 0 && AnswersPercentage < 100
                ? AnswersPercentage.ToString() : "null")
            + ",\"percentage\":" + AnswersPercentage + "}";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
      }
    }

    [AvaloniaTest]
    public async Task PressingTheWayBackTellsTheServerNoughtAndPutsTheDialogAwayFirst()
    {
      MarkedServer server = new MarkedServer { AnswersPercentage = 0, AnswersPlayCount = 2 };
      var client = new ServerApiClient(SERVER, new HttpClient(server));
      TitleViewModel watched = Title("Alien", 100, 2);

      List<string> order = new List<string>();
      var model = new MediaTitleActionsModel(async (title, action) =>
      {
        MediaPlayback after = await client.ReportPlaybackAsync(title.Title.Id,
            action.Kind == MediaTitleActionKind.SetPlayed ? 100 : 0);
        order.Add("asked");
        Assert.That(after.Watched, Is.False,
            "the answer the screen folded into its row is not the one the library gives for a title at nought");
        order.Add("reported");
      });

      model.Open(watched, true, false);
      MediaTitleAction back = model.Actions.First(a => a.Kind == MediaTitleActionKind.SetUnplayed);
      await model.PressAsync(back);

      Assert.That(server.Told, Is.EqualTo(new[] { 0 }),
          "the reference's Set unplayed is the same playback report carrying nought "
          + "(AbstractWatchedAction.cs:75-80), and nought is what has to reach the server");
      Assert.That(order, Is.EqualTo(new[] { "asked", "reported" }));
      Assert.That(model.IsOpen, Is.False,
          "the dialog is put away before the answer is given, so that what an answer opens stands over the wall "
          + "and not over a question that has already been answered");
    }

    [AvaloniaTest]
    public void ALineThatOnlyExplainsIsNotAnAnswerAndLeavesTheDialogStanding()
    {
      int presses = 0;
      var model = new MediaTitleActionsModel((title, kind) =>
      {
        presses++;
        return Task.CompletedTask;
      });

      model.Open(Title("A file on this machine", 0), false, false);
      MediaTitleAction only = model.Actions[0];
      Assert.That(only.IsCommand, Is.False);

      Assert.DoesNotThrowAsync(() => model.PressAsync(only));
      Assert.That(presses, Is.EqualTo(0), "nothing was asked, because the line only said why nothing can be asked");
      Assert.That(model.IsOpen, Is.True, "and the viewer is left standing at the explanation, not thrown out");

      model.Close();
      Assert.That(model.IsOpen, Is.False);
    }

    /// <summary>
    /// The browser's own title dialog, which used to carry two hard-built buttons and now carries the lines the
    /// table produces. Measured end to end because the dialog is where a mistake would be silent: a row that is
    /// never added, or added for the wrong title, simply never appears, and a viewer cannot tell that from a
    /// library that has nothing to say.
    /// </summary>
    [AvaloniaTest]
    public async Task TheBrowsersTitleDialogStandsItsLinesOffTheSameTableAndAnswersThem()
    {
      MarkedServer server = new MarkedServer { AnswersPercentage = 0, AnswersPlayCount = 2 };
      var client = new ServerApiClient(SERVER, new HttpClient(server));
      var browser = new MediaBrowserViewModel(client, null);
      TitleViewModel watched = Title("Alien", 100, 2);
      browser.Titles.Add(watched);
      browser.Selected = watched;

      browser.ShowMenuCommand.Execute(null);

      Assert.That(browser.TitleActions.Select(a => a.Word),
          Is.EqualTo(new[] { "Media information", "Mark unwatched", "Refresh" }),
          "the dialog of a watched title stands the reference's lines for it - the reading first, the way back "
          + "second, the ask to look at the file again third (plugin.xml:270-275, sorted by "
          + "MediaItemsActionModel.cs:376)");
      Assert.That(browser.TitleActions.All(a => a.IsCommand), Is.True,
          "every line the dialog stands has an answer behind it; the sentence explaining why there is none is "
              + "spoken by the dialog's own words rather than pressed");

      var press = (CommunityToolkit.Mvvm.Input.IAsyncRelayCommand<MediaTitleAction>)
          browser.RunTitleActionCommand;
      await press.ExecuteAsync(browser.TitleActions.First(a => a.Kind == MediaTitleActionKind.SetUnplayed));

      Assert.That(server.Told, Is.EqualTo(new[] { 0 }),
          "pressing the way back reports nought, which is the reference's Set unplayed "
          + "(AbstractWatchedAction.cs:75-80); the server was asked: " + string.Join(" | ", server.Asked));
      Assert.That(browser.Selected.Seen, Is.False,
          "and the row is rewritten from the server's answer, so the tick that stood on it is off it now");
      Assert.That(browser.MenuVisible, Is.False,
          "the dialog is put away by the answer, as it was when the button did this by hand");
    }

    /// <summary>
    /// The reference's <i>Refresh</i> line, measured to the wire. What matters here is which title is asked about
    /// and how often: an endpoint that took the id from the wrong place would quietly ask about whatever title was
    /// added last, and the viewer would watch the shelf for a change that was never planned.
    /// </summary>
    [AvaloniaTest]
    public async Task PressingTheAskToReadTheFileAgainAsksTheServerAboutThisTitleAlone()
    {
      MarkedServer server = new MarkedServer();
      var client = new ServerApiClient(SERVER, new HttpClient(server));
      var browser = new MediaBrowserViewModel(client, null);
      TitleViewModel alien = Title("Alien", 0);
      browser.Titles.Add(alien);
      browser.Selected = alien;

      MediaTitleAction refresh = MediaTitleActions.For(alien, true, true)
          .First(a => a.Kind == MediaTitleActionKind.Refresh);
      browser.ShowMenu();
      await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand<MediaTitleAction>)browser.RunTitleActionCommand)
          .ExecuteAsync(refresh);

      Assert.That(server.Asked.Count(path => path.EndsWith("/refresh", StringComparison.OrdinalIgnoreCase)),
          Is.EqualTo(1),
          "one press, one ask - the reference's entry calls the directory once (RefreshMediaItem.cs:60-69); the "
          + "server was asked: " + string.Join(" | ", server.Asked));
      Assert.That(server.Asked.Last(),
          Is.EqualTo("/MediaPortal/Server/api/media/items/" + alien.Title.Id.ToString("D") + "/refresh"),
          "asked of this title and of no other");
      Assert.That(server.Told, Has.Count.EqualTo(0),
          "and it is not a playback report: nothing was watched, so no number may be written");
      Assert.That(browser.MenuVisible, Is.True,
          "the dialog stays under the press: an ask that shows nothing moving on the wall would otherwise leave "
          + "the viewer with a button that took the question away and changed nothing, which reads as broken");
      Assert.That(browser.MenuAnswer, Does.Contain("read"),
          "the dialog says what its own button asked for, in its own line under the buttons - the screen's status "
          + "line only speaks while the shelf is empty, so it is no place for an answer to a press");
    }

    /// <summary>
    /// The reference's <i>Media information</i> pushed a screen of its own over the list
    /// (<c>MediaInfoAction.cs:69-90</c> → <c>MediaInfoVideo.xaml</c>): the title's picture, its facts beside it,
    /// and no wall of tiles standing in the way. Measured here as the wall going away and coming back, because the
    /// facts themselves belong to the details panel and are tested where they are read.
    /// </summary>
    [AvaloniaTest]
    public async Task PressingTheReadingOfTheTitleTakesTheWallAwayAndBackPutsItBack()
    {
      MarkedServer server = new MarkedServer { AnswersPercentage = 100, AnswersPlayCount = 3 };
      var client = new ServerApiClient(SERVER, new HttpClient(server));
      var browser = new MediaBrowserViewModel(client, null);
      TitleViewModel alien = Title("Alien", 100, 3);
      browser.Titles.Add(alien);
      browser.Selected = alien;

      MediaTitleAction reading = MediaTitleActions.For(alien, true, true)
          .First(a => a.Kind == MediaTitleActionKind.Information);
      await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand<MediaTitleAction>)browser.RunTitleActionCommand)
          .ExecuteAsync(reading);

      Assert.That(browser.InfoShown, Is.True,
          "the reading stands up: a line whose press left the wall exactly as it was is the dead end the viewer "
          + "rightly refuses, whatever the panel beside the shelf was already showing");
      Assert.That(browser.ShowsSideDetails, Is.True,
          "the facts stand on the title's own picture while the reading lasts");
      Assert.That(browser.MenuVisible, Is.False,
          "the dialog goes for this answer, as it does for every answer that opens something of its own");

      browser.CloseInfo();
      Assert.That(browser.InfoShown, Is.False,
          "back puts the wall of tiles back, which is the reference's screen being left and the list behind it "
          + "being there again");
    }

    /// <summary>
    /// The reading does not wait for the viewer's standing answer to the details question: somebody who pressed
    /// the line asked to read this title, whatever they usually wear on this screen.
    /// </summary>
    [AvaloniaTest]
    public async Task TheReadingStandsItsFactsUpOnAScreenWearingNoDetails()
    {
      MarkedServer server = new MarkedServer { AnswersPercentage = 0, AnswersPlayCount = 0 };
      var client = new ServerApiClient(SERVER, new HttpClient(server));
      var browser = new MediaBrowserViewModel(client, null) { ShowDetails = false };
      TitleViewModel alien = Title("Alien", 0);
      browser.Titles.Add(alien);
      browser.Selected = alien;

      MediaTitleAction reading = MediaTitleActions.For(alien, true, true)
          .First(a => a.Kind == MediaTitleActionKind.Information);
      await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand<MediaTitleAction>)browser.RunTitleActionCommand)
          .ExecuteAsync(reading);

      Assert.That(browser.ShowsSideDetails, Is.True,
          "the reading stands its facts up even on a screen whose details column is switched off");
      Assert.That(browser.ShowDetails, Is.False,
          "and it does that without rewriting the viewer's standing answer - the switch stays off for every later "
          + "visit, because one press on one title was never a decision about how the screen is worn");
    }

    /// <summary>
    /// The same ask on the home wall, where the dialog is a screen of its own: the answer has to stand in the
    /// dialog, because the wall's own status line is worn in a head band the content pane covers.
    /// </summary>
    [Test]
    public async Task TheAskToReadTheFileAgainLeavesItsDialogStandingAndAnswersInIt()
    {
      MediaTitleActionsModel model = null!;
      var asked = new List<MediaTitleActionKind>();
      model = new MediaTitleActionsModel(delegate (TitleViewModel title, MediaTitleAction action)
      {
        asked.Add(action.Kind);
        model.Say("The library will read " + title.Name + " again and say what it finds");
        return Task.CompletedTask;
      });

      TitleViewModel alien = Title("Alien", 0);
      model.Open(alien, true, false);

      await model.PressAsync(model.Actions.First(a => a.Kind == MediaTitleActionKind.Refresh));

      Assert.That(asked, Is.EqualTo(new[] { MediaTitleActionKind.Refresh }), "one press, one answer");
      Assert.That(model.IsOpen, Is.True,
          "the dialog the ask came out of keeps standing under it - nothing on the wall moved, so a dialog that "
          + "went away would leave the viewer holding a button that appears to have done nothing");
      Assert.That(model.Status, Does.Contain("read"),
          "and the answer is said in the dialog's own line, under the buttons");

      await model.PressAsync(model.Actions.First(a => a.Kind == MediaTitleActionKind.SetPlayed));
      Assert.That(model.IsOpen, Is.False,
          "a mark rewrites the tile the viewer is looking at, so its dialog is put away as it always was");
    }

    [Test]
    public void TheAskToReadTheFileAgainStandsBehindTheMarkAndDoesNotCareWhereTheFileLies()
    {
      // The reference looked at the item's own resources before offering this line, because the item was in its
      // hand (RefreshMediaItem.cs:44-49). Here the file sits on the server, and a path this machine cannot open
      // may be exactly right there - so the line is offered and the server refuses with its reason if there is
      // nothing for it to read.
      TitleViewModel onTheServer = new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Frontera", null, 2021,
          null, "smb://nas/filme/frontera.mkv", false, Percentage: 0));

      Assert.That(MediaTitleActions.For(onTheServer, true, false).Select(a => a.Word),
          Is.EqualTo(new[] { "Mark watched", "Refresh" }),
          "the order is the reference's keys: C.1 for the mark, C.3 for the ask (plugin.xml:273, :275)");
    }

    [AvaloniaTest]
    public void TheWallAsksTheSameTableAndGetsTheMarksWithoutTheInformationLine()
    {
      MarkedServer server = new MarkedServer { AnswersPercentage = 100, AnswersPlayCount = 1 };
      var home = new HomeViewModel(new ServerApiClient(SERVER, new HttpClient(server)));
      TitleViewModel unwatched = Title("Alien: Earth", 0);

      home.OpenTitleActions(unwatched);

      Assert.That(home.TitleActions.IsOpen, Is.True, "the tile asked its question and a dialog has to stand up");
      Assert.That(home.TitleActions.Actions.Select(a => a.Word), Is.EqualTo(new[] { "Mark watched", "Refresh" }),
          "the wall offers the two lines it can ask of the server itself - the mark and the ask to read the file "
          + "again - and withholds the reading it has nowhere to show");

      home.CloseTitleActions();
      Assert.That(home.TitleActions.IsOpen, Is.False);

      home.OpenTitleActions(null);
      Assert.That(home.TitleActions.IsOpen, Is.False,
          "a square showing no title - a shortcut, a note, a weather card - is refused, as the reference's list "
          + "never applied to anything but the library's objects (MediaItemsActionModel.cs:113-118)");
      Assert.That(home.Status, Is.Not.Empty, "and the refusal is said rather than left as silence");
    }

    #region Der Trailer und seine Größe

    /// <summary>
    /// One trailer of one title is an answer, several are a question, and a question is answered by the same
    /// dialog becoming it rather than by a second dialog standing over the first.
    /// </summary>
    [Test]
    public void TheTrailerOfOneTitleIsAnAnswerAndNotAQuestion()
    {
      TitleViewModel one = WithTrailers("Frozen II", Page("bwzLiQZDw2I"));

      MediaTitleAction? only = MediaTitleActions.TrailerToPlay(one);
      Assert.That(only, Is.Not.Null, "one film behind the line is one answer");
      Assert.That(only!.Trailer, Is.SameAs(one.Trailers[0]), "and the line carries that film into the ask");
      Assert.That(only.Word, Is.EqualTo("Trailer (YouTube)"),
          "the kind stands on the line, because a film at a site and a film on the shelf are two different evenings");

      TitleViewModel three = WithTrailers("Alien: Romulus", Page("bwzLiQZDw2I"), Page("BbBttfs0PB4"),
          Page("Eao6ApZ3uZ0"));
      Assert.That(MediaTitleActions.TrailerToPlay(three), Is.Null,
          "three films behind one line are a question, and a question cannot be played");
      Assert.That(MediaTitleActions.TrailerChoicesOf(three).Select(a => a.Word),
          Is.EqualTo(new[] { "Trailer 1 (YouTube)", "Trailer 2 (YouTube)", "Trailer 3 (YouTube)" }),
          "numbered, because three lines of one and the same words cannot be pointed at from across the room");
    }

    [Test]
    public void ATrailerWhoseAddressNothingOpensSaysSoOnItsLine()
    {
      MediaTrailer unplayable = new MediaTrailer(0, "Teaser", null,
          "plugin://plugin.video.youtube/play/?video_id=VWqJifMMgZE");
      Assert.That(MediaTitleActions.WordsFor(unplayable), Is.EqualTo("Teaser (no address to play)"),
          "the library's own trailer is named and its problem is said, not silently left out of the list");
      Assert.That(MediaTitleActions.WordsFor(Page("bwzLiQZDw2I")), Is.EqualTo("Trailer (YouTube)"),
          "and an address that leads to a film is named for the site it stands at");
    }

    [Test]
    public void ThePictureSizeIsAskedOfAPageAndNotOfAFilm()
    {
      Assert.That(MediaTitleActions.AsksForQuality(Page("bwzLiQZDw2I")), Is.True,
          "a page is several films in several sizes and names none of them");
      Assert.That(MediaTitleActions.AsksForQuality(OnTheShelf("romulus-teaser.mp4")), Is.False,
          "a file the library holds is one film in the one size it was cut in");
      Assert.That(MediaTitleActions.AsksForQuality(new MediaTrailer(2, null, "https://www.youtube.com/watch?v=bwzLiQZDw2I",
          "plugin://plugin.video.youtube/play/?video_id=bwzLiQZDw2I")), Is.False,
          "and a plug-in address is no page: the resolver does not read it, the player is handed what the server made of it");
      Assert.That(MediaTitleActions.AsksForQuality(null), Is.False, "nothing to ask about is no question");
      Assert.That(MediaTitleActions.QualityChoicesOf(null), Is.Empty, "and produces no lines either");
    }

    [Test]
    public void EverySizeCarriesTheFilmItWasAskedFor()
    {
      MediaTrailer page = Page("bwzLiQZDw2I");
      List<MediaTitleAction> lines = MediaTitleActions.QualityChoicesOf(page);

      Assert.That(lines, Has.Count.EqualTo(VideoSiteStream.Qualities.Length), "one line per measured size");
      foreach (MediaTitleAction line in lines)
      {
        Assert.That(line.Kind, Is.EqualTo(MediaTitleActionKind.TrailerQuality));
        Assert.That(line.Trailer, Is.SameAs(page),
            "the film travels on the line, so that one press is the whole ask rather than half of one");
        Assert.That(line.Quality, Is.Not.Null, "and the size is carried along with it");
        Assert.That(line.Word, Is.EqualTo(line.Quality!.Display), "and the line is the size's own words");
      }

      Assert.That(lines.Select(a => a.Word), Is.EqualTo(VideoSiteStream.Qualities.Select(q => q.Display)),
          "in the resolver's order, which is smallest first - the top line is the film that starts at once");
    }

    /// <summary>
    /// The whole walk of the dialog for a trailer that stands behind a page: the trailer line, then the size
    /// lines, then one ask. Nothing is played while the size is still unknown, because starting the smallest
    /// film and then fetching a second one over it is the noise this question exists to prevent.
    /// </summary>
    [Test]
    public async Task TheTrailerOfAPageIsAnsweredWithASizeBeforeAnythingIsPlayed()
    {
      var presses = new List<MediaTitleAction>();
      MediaTitleActionsModel model = new MediaTitleActionsModel(delegate (TitleViewModel title,
          MediaTitleAction action)
      {
        presses.Add(action);
        return Task.CompletedTask;
      });

      TitleViewModel frozen = WithTrailers("Frozen II", Page("bwzLiQZDw2I"));
      model.Open(frozen, true, false);

      await model.PressAsync(model.Actions.First(a => a.Kind == MediaTitleActionKind.Trailer));

      Assert.That(presses, Is.Empty, "the film is not asked for while its size is unknown");
      Assert.That(model.IsOpen, Is.True,
          "the dialog that was standing becomes the size question - a second dialog over the first would be a "
          + "room the viewer never walked into, and the back key would have to know about it");
      Assert.That(model.Status, Is.Empty,
          "and the lines are the question on their own: a sentence over them would only have to be worded badly");
      Assert.That(model.Actions.Select(a => a.Kind),
          Is.EqualTo(VideoSiteStream.Qualities.Select(q => MediaTitleActionKind.TrailerQuality)),
          "one line per size, and nothing else left standing");

      await model.PressAsync(model.Actions.First(a => a.Word.StartsWith("bis 720p", StringComparison.Ordinal)));

      Assert.That(presses, Has.Count.EqualTo(1), "one press on one size is the whole ask");
      Assert.That(presses[0].Kind, Is.EqualTo(MediaTitleActionKind.TrailerQuality));
      Assert.That(presses[0].Quality!.Word, Is.EqualTo("bis 720p"), "the size that was stood on reaches the ask");
      Assert.That(presses[0].Trailer, Is.SameAs(frozen.Trailers[0]), "with the film it was stood on for");
      Assert.That(model.IsOpen, Is.False, "and the dialog is put away, since its answer left it");
    }

    [Test]
    public async Task TheTrailerOnTheShelfIsPlayedWithoutBeingAskedAboutAnything()
    {
      var presses = new List<MediaTitleAction>();
      MediaTitleActionsModel model = new MediaTitleActionsModel(delegate (TitleViewModel title,
          MediaTitleAction action)
      {
        presses.Add(action);
        return Task.CompletedTask;
      });

      TitleViewModel alien = WithTrailers("Alien: Romulus", OnTheShelf("romulus-teaser.mp4"));
      model.Open(alien, true, false);

      await model.PressAsync(model.Actions.First(a => a.Kind == MediaTitleActionKind.Trailer));

      Assert.That(presses, Has.Count.EqualTo(1),
          "a file is one film in one size, so a second question would be a stall in front of an answer");
      Assert.That(presses[0].Quality, Is.Null, "and no size is pretended for a file");
      Assert.That(model.IsOpen, Is.False, "the dialog is put away as every other line puts it away");
    }

    /// <summary>
    /// The wall's side of a trailer that has to be looked up at a page: the answer arrives after the dialog was
    /// taken away, and the dialog comes back for it.
    /// </summary>
    /// <remarks>
    /// The wall wears its status line in a head band that the content pane covers while a dialog stands over it,
    /// so a sentence written there is written where nobody is looking; and a playback nudge is one short word
    /// wide, which cuts a sentence in half. The dialog the press came out of is the one place left.
    /// </remarks>
    [Test]
    public async Task TheWallTakesItsDialogBackForTheAnswerToATrailer()
    {
      HomeViewModel home = new HomeViewModel();
      var asks = new List<TrailerRequest>();
      home.TrailerRequested += delegate (object? sender, TrailerRequest request) { asks.Add(request); };

      TitleViewModel frozen = WithTrailers("Frozen II", Page("bwzLiQZDw2I"));
      home.OpenTitleActions(frozen);

      await home.TitleActions.PressAsync(
          home.TitleActions.Actions.First(a => a.Kind == MediaTitleActionKind.Trailer));
      Assert.That(home.TitleActions.Actions.Any(a => a.Kind == MediaTitleActionKind.TrailerQuality), Is.True,
          "the standing dialog became the size question");

      await home.TitleActions.PressAsync(home.TitleActions.Actions.First(
          a => a.Kind == MediaTitleActionKind.TrailerQuality && a.Quality!.Word == "bis 720p"));

      Assert.That(asks, Has.Count.EqualTo(1), "one press on one size is one ask");
      Assert.That(asks[0].Quality!.Word, Is.EqualTo("bis 720p"), "in the size that was stood on");
      Assert.That(home.TitleActions.IsOpen, Is.False, "and the dialog went with the press, as a wall dialog does");

      string waiting = VideoSiteStream.WaitWords(VideoSiteStream.Qualities[1]);
      home.AnswerTrailer(waiting);
      Assert.That(home.TitleActions.IsOpen, Is.True,
          "the answer brings the dialog back, because it is the only place the viewer is still looking");
      Assert.That(home.TitleActions.Status, Is.EqualTo(waiting), "and the sentence stands in it, whole");

      home.AnswerTrailer("");
      Assert.That(home.TitleActions.IsOpen, Is.False, "an answered question is taken away again");
    }

    /// <summary>
    /// A trailer at a video site, as a tagging tool writes it: the page, twice over - once as the address to
    /// open and once as the address the library holds.
    /// </summary>
    private static MediaTrailer Page(string id)
    {
      string page = "https://www.youtube.com/watch?v=" + id;
      return new MediaTrailer(3, null, page, page);
    }

    /// <summary>
    /// A trailer the library holds as a file.
    /// </summary>
    private static MediaTrailer OnTheShelf(string name)
    {
      string file = @"D:\Mediaportal\Trailer\" + name;
      return new MediaTrailer(1, null, file, file);
    }

    private static TitleViewModel WithTrailers(string name, params MediaTrailer[] trailers)
    {
      return new TitleViewModel(new MediaTitle(Guid.NewGuid(), name, null, 2024, null,
          "local://filme/" + name, false, Percentage: 0, Trailers: trailers));
    }

    #endregion

    #endregion
  }
}
