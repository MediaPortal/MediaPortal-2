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
using System.IO;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.ViewModels;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The escalating skip ladder the reference video player puts behind its left and right keys, measured
  /// rung for rung against the reference's own state machine (<c>ExtendedVideoSkip</c>).
  /// </summary>
  [TestFixture]
  public class TestSkipStepLadder
  {
    private static readonly Func<int, bool> ALWAYS = delegate { return true; };

    // ---- the ladder's own arithmetic -----------------------------------------------------------------

    [Test]
    public void PressesHeldOneAfterTheOtherClimbTheReferenceLadder()
    {
      SkipStepLadder ladder = new SkipStepLadder(null);

      ladder.Press(1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(15), "the first press is the smallest rung");
      ladder.Press(1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(30));
      ladder.Press(1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(60));
      ladder.Press(1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(180), "four presses ask for three minutes, not four times fifteen");
    }

    [Test]
    public void AListWithoutADontSkipRungGetsOneAndIsSorted()
    {
      SkipStepLadder ladder = new SkipStepLadder("60, 5, x, -3, 30");

      ladder.Press(1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(5), "the list is sorted, and 5 is the first real rung");
      // One press back from the first real rung lands on the 0 that joined the list, which reads "don't skip".
      ladder.Press(-1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(0));
    }

    [Test]
    public void TurningAroundAtTheBottomFlipsToTheFirstStepTheOtherWay()
    {
      SkipStepLadder ladder = new SkipStepLadder("15,30,60");

      ladder.Press(1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(15));
      // Still at the first rung (index 1): the opposite press may not step back onto 0 here the way it can
      // after a longer climb - the reference turns around at its bottom index, which it has left already,
      // so it steps one rung back instead: onto the "don't skip" rung.
      ladder.Press(-1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(0), "one rung back is the don't-skip rung");
      // Now the ladder sits at index 0 pointing backwards; the next opposite press turns it around for good.
      ladder.Press(-1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(-15));
      ladder.Press(-1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(-30));
    }

    [Test]
    public void TurningAroundFromTheTopWalksBackDownBeforeFlipping()
    {
      SkipStepLadder ladder = new SkipStepLadder("15,30,60");

      ladder.Press(1, ALWAYS);
      ladder.Press(1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(30));
      ladder.Press(-1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(15), "the way back walks down the ladder, it does not teleport");
      ladder.Press(-1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(0));
      ladder.Press(-1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(-15));
    }

    [Test]
    public void ARefusedStepStaysParkedAtTheBorder()
    {
      SkipStepLadder ladder = new SkipStepLadder("15,30,60");

      // Two presses in, the ladder wants 30 - and the media says no. The press parks: further presses in
      // the same direction do not climb (the reference checks validity before climbing), so the jump the
      // timer eventually commits is still the refused one, thrown at the border instead.
      Assert.That(ladder.Press(1, ALWAYS), Is.EqualTo(SkipStepAction.Skip));
      Assert.That(ladder.Press(1, seconds => seconds <= 20), Is.EqualTo(SkipStepAction.ToEnd));
      Assert.That(ladder.PendingIsValid, Is.False);
      Assert.That(ladder.Press(1, seconds => seconds <= 20), Is.EqualTo(SkipStepAction.ToEnd),
          "parked at the end, the ladder does not climb on");
      Assert.That(ladder.Elapsed(), Is.EqualTo(SkipStepAction.ToEnd));
    }

    [Test]
    public void TheBeginningAnswersTheSameWayBackwards()
    {
      SkipStepLadder ladder = new SkipStepLadder("15,30");

      Assert.That(ladder.Press(-1, seconds => seconds >= -5), Is.EqualTo(SkipStepAction.ToStart));
      Assert.That(ladder.Elapsed(), Is.EqualTo(SkipStepAction.ToStart));
    }

    [Test]
    public void TheWaitCommittingTheJumpResetsTheClimbButKeepsTheDirection()
    {
      SkipStepLadder ladder = new SkipStepLadder("15,30,60");

      ladder.Press(1, ALWAYS);
      ladder.Press(1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(30));
      Assert.That(ladder.Elapsed(), Is.EqualTo(SkipStepAction.Skip));
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(0), "the ladder fell back to its bottom rung");
      Assert.That(ladder.Direction, Is.EqualTo(1), "but it still points forwards");
      ladder.Press(1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(15), "the next run starts climbing from the smallest step again");
    }

    [Test]
    public void ADontSkipRungCommitsNothing()
    {
      SkipStepLadder ladder = new SkipStepLadder("15,30");

      ladder.Press(1, ALWAYS);
      Assert.That(ladder.Press(-1, ALWAYS), Is.EqualTo(SkipStepAction.Nothing));
      Assert.That(ladder.Elapsed(), Is.EqualTo(SkipStepAction.Nothing));
    }

    [Test]
    public void CancelForgetsThePendingJump()
    {
      SkipStepLadder ladder = new SkipStepLadder("15,30");

      ladder.Press(1, ALWAYS);
      ladder.Press(1, ALWAYS);
      ladder.Cancel();
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(0));
      Assert.That(ladder.Elapsed(), Is.EqualTo(SkipStepAction.Nothing), "an instant skip dropped the ladder's jump");
    }

    [Test]
    public void AHopelessListFallsBackToTheReferenceLadder()
    {
      SkipStepLadder ladder = new SkipStepLadder("nonsense,,,");

      ladder.Press(1, ALWAYS);
      Assert.That(ladder.PendingStepSeconds, Is.EqualTo(15));
    }

    // ---- the words the reference's SkipStepOSD shows ---------------------------------------------------

    [Test]
    public void TheOsdWordsAreTheReferenceFormatStepUnitOnes()
    {
      Assert.That(SkipStepLadder.FormatStep(0), Is.EqualTo("Don't skip"), "[Media.DontSkip]");
      Assert.That(SkipStepLadder.FormatStep(15), Is.EqualTo("+ 15 sec"));
      Assert.That(SkipStepLadder.FormatStep(-30), Is.EqualTo("- 30 sec"));
      Assert.That(SkipStepLadder.FormatStep(90), Is.EqualTo("+ 1 min"), "whole units, the reference truncates");
      Assert.That(SkipStepLadder.FormatStep(-180), Is.EqualTo("- 3 min"));
      Assert.That(SkipStepLadder.FormatStep(3600), Is.EqualTo("+ 1 hrs"));
      Assert.That(SkipStepLadder.FormatStep(7200), Is.EqualTo("+ 2 hrs"));
      // The reference's own arithmetic quirk, kept because the reference ships it: only one, two and three
      // whole hours are spelled as hours, anything longer is written out in minutes.
      Assert.That(SkipStepLadder.FormatStep(14400), Is.EqualTo("+ 240 min"));
    }

    // ---- the same ladder behind the now-playing screen ------------------------------------------------

    private FakePlayer _player = null!;
    private NowPlayingViewModel _screen = null!;
    private readonly Queue<Action> _queued = new Queue<Action>();

    [SetUp]
    public void SetUp()
    {
      _player = new FakePlayer();
      _screen = new NowPlayingViewModel(_player, _queued.Enqueue);
      Pump();
      _screen.Play(MediaLocation.FromFile(Path.Combine(Path.GetTempPath(), "mp2-ladder.mkv")));
      Pump();
      _player.LengthInMs = 10 * 60_000;
      _player.TimeInMs = 60_000;
    }

    [TearDown]
    public void TearDown()
    {
      _screen.Dispose();
    }

    private void Pump()
    {
      while (_queued.Count > 0)
        _queued.Dequeue()();
    }

    [Test]
    public void TheScreenClimbsTheLadderAndJumpsWhenTheWaitRunsOut()
    {
      Assert.That(_screen.SkipStepForward(), Is.EqualTo(SkipStepAction.Skip));
      Assert.That(_screen.PendingSkipStepSeconds, Is.EqualTo(15));
      Assert.That(_player.TimeInMs, Is.EqualTo(60_000), "nothing has jumped yet - the film waits for the pressing to stop");

      _screen.SkipStepForward();
      Assert.That(_screen.PendingSkipStepSeconds, Is.EqualTo(30));
      _screen.SkipStepElapsed();
      Assert.That(_player.TimeInMs, Is.EqualTo(90_000), "two presses then a wait is a thirty second jump, not two jumps");
    }

    [Test]
    public void AtTheEndTheLadderThrowsTheFilmToTheEnd()
    {
      _player.TimeInMs = 10 * 60_000 - 10_000;

      Assert.That(_screen.SkipStepForward(), Is.EqualTo(SkipStepAction.ToEnd),
          "fifteen seconds from ten before the end does not fit");
      _screen.SkipStepElapsed();
      Assert.That(_player.TimeInMs, Is.EqualTo(10 * 60_000));
    }

    [Test]
    public void AtTheBeginningTheLadderThrowsTheFilmToTheBeginning()
    {
      _player.TimeInMs = 5_000;

      Assert.That(_screen.SkipStepBackward(), Is.EqualTo(SkipStepAction.ToStart));
      _screen.SkipStepElapsed();
      Assert.That(_player.TimeInMs, Is.EqualTo(0));
    }

    [Test]
    public void AnInstantSkipDropsThePendingLadderJump()
    {
      _screen.SkipStepForward();
      _screen.SkipStepForward();
      _screen.CancelSkipStep();
      _screen.SkipStepElapsed();
      Assert.That(_player.TimeInMs, Is.EqualTo(60_000), "the cancelled ladder has nothing left to jump");
    }
  }
}
