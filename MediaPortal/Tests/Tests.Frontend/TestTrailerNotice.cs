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

using NUnit.Framework;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// The dialog a trailer speaks in: the wait while its film is looked up or brought over, then the reason.
  /// </summary>
  /// <remarks>
  /// A trailer is not answered on the spot and not in one word, so it gets a dialog of its own rather than a line
  /// of the shelf's question or a playback nudge - both of which are built for one short word and cut a sentence
  /// in half. What the dialog has to get right is small: it says its piece in one piece, it wears the frame's own
  /// answer row like every other dialog of this front end, and it says when it is done so that nobody leaves an
  /// empty box standing.
  /// </remarks>
  [TestFixture]
  public class TestTrailerNotice
  {
    [Test]
    public void TheTrailerDialogSaysOneWholeThingAndNamesItself()
    {
      // The errand begins with the wait, and the wait says nothing: what the machine does behind a page at a video
      // site is nothing to read from across the room, so the dialog shows its "Loading ..." shape (the view draws
      // that word and the moving bar) and the data only says that the work is on.
      TrailerNoticeModel notice = new();
      Assert.That(notice.Waiting, Is.True, "a dialog raised for a trailer waits from the first moment");
      Assert.That(notice.Text, Is.Empty, "while it waits the dialog carries no sentence");

      // The answer comes to the same dialog, in one piece - which is the whole point of giving it its own place.
      string refusal = "nothing could be found behind the page - the resolver had no answer.";
      notice.Say(refusal);
      Assert.That(notice.Text, Is.EqualTo(refusal), "the reason stands whole, not cut at the end of a line");
      Assert.That(notice.Waiting, Is.False, "an answer is not a wait, so the moving bar is off");

      // The frame reads its heading off the body, so a dialog that brings no heading stands about unnamed.
      Assert.That(notice.Heading, Is.EqualTo("Trailer"), "the box says what it is about");
    }

    [Test]
    public void TheTrailerDialogWearsTheFramesOwnAnswerRow()
    {
      TrailerNoticeModel notice = new();

      // The way out of a dialog is the frame's answer row, dressed by the skin. A dialog that answers to nothing
      // here would be read as a screen the viewer is locked in.
      Assert.That(notice, Is.InstanceOf<IDialogAnswers>(),
          "the frame puts its answer row under this dialog, as under every other one");
      IDialogAnswers answers = (IDialogAnswers) notice;
      Assert.That(answers.DialogOkWord, Is.EqualTo("OK"), "one word to read it and be done");
      Assert.That(DialogAnswersRowIsUp.Instance.Convert(answers, typeof(bool), null,
          System.Globalization.CultureInfo.CurrentCulture), Is.True,
          "the row is up, so the word is actually on the screen");
      Assert.That(answers.DialogCancelWord, Is.Empty, "there is nothing to refuse about a notice");

      int times = 0;
      notice.Finished += delegate { times++; };
      answers.DialogOkCommand!.Execute(null);
      Assert.That(times, Is.EqualTo(1), "pressing the frame's answer takes the box away");
    }

    [Test]
    public void TheTrailerDialogStandsToBeLeftWhileItWaits()
    {
      TrailerNoticeModel notice = new();
      IDialogAnswers answers = (IDialogAnswers) notice;

      // A few minutes of work is no reason to hold a viewer in a box. While the wait is on the dialog its answer
      // still works: the box goes away, the errand does not - an answer that comes later raises its own box.
      Assert.That(notice.Waiting, Is.True, "the dialog is standing in its waiting shape");
      int times = 0;
      notice.Finished += delegate { times++; };
      answers.DialogOkCommand!.Execute(null);
      Assert.That(times, Is.EqualTo(1), "the wait can be walked away from");
    }

    [Test]
    public void TheTrailerDialogSaysWhenItIsDone()
    {
      TrailerNoticeModel notice = new();
      int times = 0;
      notice.Finished += delegate { times++; };

      notice.Finish();
      Assert.That(times, Is.EqualTo(1),
          "the dialog says when it has nothing left to say, so that whoever raised it takes it away");
    }
  }
}
