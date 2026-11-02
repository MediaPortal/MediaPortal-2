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
          Is.EqualTo(new[] { MediaTitleActionKind.Information, MediaTitleActionKind.SetUnplayed }),
          "the reference offers a title at a hundred per cent its way back to unplayed and never the mark it "
          + "already carries (AbstractWatchedAction.cs:49-52, SetUnwatched.cs:27-37)");
      Assert.That(actions.Select(a => a.Word),
          Is.EqualTo(new[] { "Media information", "Mark unwatched" }),
          "the reference's list is sorted by the string each action carries, which puts Media information (A.0) "
          + "above the watched marks (C.1) and registers Set unplayed first of those two (plugin.xml:270-272)");
    }

    [Test]
    public void ATitleNobodyHasFinishedIsOfferedTheMarkAndTheInformation()
    {
      List<MediaTitleAction> actions = MediaTitleActions.For(Title("Alien: Earth", 0), true, true);

      Assert.That(KindsOf(actions),
          Is.EqualTo(new[] { MediaTitleActionKind.Information, MediaTitleActionKind.SetPlayed }),
          "a title below a hundred per cent is the reference's \"not watched\", which is offered Set played "
          + "(SetWatched.cs:27-37)");
      Assert.That(actions.Select(a => a.Word),
          Is.EqualTo(new[] { "Media information", "Mark watched" }));
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
      Assert.That(KindsOf(actions), Is.EqualTo(new[] { MediaTitleActionKind.SetUnplayed }),
          "and the mark is still offered, since that one question the wall can ask of the server itself");
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

      /// <summary>What the next answer should say how far the title is watched.</summary>
      internal int AnswersPercentage { get; set; }

      /// <summary>What the next answer should say how often the title was finished.</summary>
      internal int AnswersPlayCount { get; set; }

      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string body = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
        string[] segments = request.RequestUri!.AbsolutePath.Split('/');
        string idText = segments[segments.Length - 2];
        if (!Guid.TryParse(idText, out Guid askedFor))
          askedFor = Guid.Empty;

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
      var model = new MediaTitleActionsModel(async (title, kind) =>
      {
        MediaPlayback after = await client.ReportPlaybackAsync(title.Title.Id,
            kind == MediaTitleActionKind.SetPlayed ? 100 : 0);
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
          Is.EqualTo(new[] { "Media information", "Mark unwatched" }),
          "the dialog of a watched title stands the reference's two lines for it - the reading first, the way "
          + "back second (plugin.xml:270-272, sorted by MediaItemsActionModel.cs:376)");
      Assert.That(browser.TitleActions.All(a => a.IsCommand), Is.True,
          "every line the dialog stands has an answer behind it; the sentence explaining why there is none is "
              + "spoken by the dialog's own words rather than pressed");

      var press = (CommunityToolkit.Mvvm.Input.IAsyncRelayCommand<MediaTitleAction>)
          browser.RunTitleActionCommand;
      await press.ExecuteAsync(browser.TitleActions.Last());

      Assert.That(server.Told, Is.EqualTo(new[] { 0 }),
          "pressing the way back reports nought, which is the reference's Set unplayed "
          + "(AbstractWatchedAction.cs:75-80)");
      Assert.That(browser.Selected.Seen, Is.False,
          "and the row is rewritten from the server's answer, so the tick that stood on it is off it now");
      Assert.That(browser.MenuVisible, Is.False,
          "the dialog is put away by the answer, as it was when the button did this by hand");
    }

    [AvaloniaTest]
    public void TheWallAsksTheSameTableAndGetsTheMarksWithoutTheInformationLine()
    {
      MarkedServer server = new MarkedServer { AnswersPercentage = 100, AnswersPlayCount = 1 };
      var home = new HomeViewModel(new ServerApiClient(SERVER, new HttpClient(server)));
      TitleViewModel unwatched = Title("Alien: Earth", 0);

      home.OpenTitleActions(unwatched);

      Assert.That(home.TitleActions.IsOpen, Is.True, "the tile asked its question and a dialog has to stand up");
      Assert.That(home.TitleActions.Actions.Select(a => a.Word), Is.EqualTo(new[] { "Mark watched" }),
          "the wall offers the mark - which it can ask of the server itself - and withholds the reading it has "
          + "nowhere to show");

      home.CloseTitleActions();
      Assert.That(home.TitleActions.IsOpen, Is.False);

      home.OpenTitleActions(null);
      Assert.That(home.TitleActions.IsOpen, Is.False,
          "a square showing no title - a shortcut, a note, a weather card - is refused, as the reference's list "
          + "never applied to anything but the library's objects (MediaItemsActionModel.cs:113-118)");
      Assert.That(home.Status, Is.Not.Empty, "and the refusal is said rather than left as silence");
    }

    #endregion
  }
}
