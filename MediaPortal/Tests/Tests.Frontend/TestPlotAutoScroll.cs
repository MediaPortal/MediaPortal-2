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
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// The arithmetic behind Nereus' self-reading plot: wait out the delay, run at the speed, stop at the
  /// bottom or run round - the three switches (EnableAutoScrolling's delay, speed and loop) reduced to the
  /// one function the scroller obeys.
  /// </summary>
  [TestFixture]
  public class TestPlotAutoScroll
  {
    [Test]
    public void TheReadingWaitsOutItsDelayThenRunsAtItsSpeed()
    {
      Assert.That(PlotAutoScroll.OffsetAt(0, 2, 20, 500, true), Is.EqualTo(0));
      Assert.That(PlotAutoScroll.OffsetAt(2, 2, 20, 500, true), Is.EqualTo(0),
          "the reference waited the delay before the first pixel moved");
      Assert.That(PlotAutoScroll.OffsetAt(5, 2, 20, 500, true), Is.EqualTo(60),
          "and then ran at its pixels per second");
    }

    [Test]
    public void AStoryThatFitsNeverMoves()
    {
      Assert.That(PlotAutoScroll.OffsetAt(100, 0, 60, 0, true), Is.EqualTo(0),
          "nothing hangs below the panel, so there is nothing to scroll to");
    }

    [Test]
    public void WithoutTheLoopTheReadingStopsAtTheBottom()
    {
      Assert.That(PlotAutoScroll.OffsetAt(10, 2, 20, 100, false), Is.EqualTo(100), "the bottom, not past it");
      Assert.That(PlotAutoScroll.OffsetAt(1000, 2, 20, 100, false), Is.EqualTo(100),
          "and it stays there - the switch said no running round");
    }

    [Test]
    public void WithTheLoopTheReadingStartsAgainFromTheTop()
    {
      Assert.That(PlotAutoScroll.OffsetAt(7, 2, 20, 100, true), Is.EqualTo(0),
          "a full round at the speed brings it back to the first line");
      Assert.That(PlotAutoScroll.OffsetAt(7.5, 2, 20, 100, true), Is.EqualTo(10), "and it keeps on reading");
    }
  }
}
