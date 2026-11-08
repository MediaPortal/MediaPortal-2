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

using MediaPortal.Frontend.Player;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// Whether the spectrum's listener is to be put where the playing player stands - the whole of the agreement
  /// between two engines that are looking at one song and are paced by nothing in common.
  /// </summary>
  /// <remarks>
  /// Left to itself the listener is seconds behind the sound, because it starts when a screen asks for the bars
  /// and runs on its own decoder, which was measured five seconds apart on a real song. Mending is not free
  /// either, since the engine drops the pictures it was drawing, so the policy is what a viewer ends up seeing
  /// and is worth asking about without two engines running.
  /// </remarks>
  [TestFixture]
  public class TestTheListenerKeepsPace
  {
    [Test]
    public void AVerseBehindIsMendedAsSoonAsItIsNoticed()
    {
      // The ordinary case: a screen was opened over a song already running, and its listener started at nought.
      Assert.That(TapAgreement.WantsMending(listenerMs: 400, playingMs: 5400, msSinceLastMending: long.MaxValue),
          Is.True, "a listener five seconds behind its song was left behind");
    }

    [Test]
    public void ADifferenceTheEyeCannotPickOutIsLeftAlone()
    {
      // A fifth of a second in a strip of bars that is itself falling and rising is nothing to look at, while a
      // seek costs the strip its pictures for a moment.
      Assert.That(TapAgreement.WantsMending(listenerMs: 1000, playingMs: 1000 + TapAgreement.ApartWorthMendingMs - 1,
          msSinceLastMending: long.MaxValue), Is.False, "a difference nobody sees cost the listener its pictures");
    }

    [Test]
    public void ALargeDifferenceIsMendedAgainSoonerThanASmallOne()
    {
      // Both are worth mending on their own, but the one that has the listener in another part of the song is
      // worth chasing, while the other is worth waiting for.
      Assert.That(TapAgreement.WantsMending(listenerMs: 0, playingMs: TapAgreement.FarApartMs * 2,
          msSinceLastMending: TapAgreement.MendFarApartWaitMs), Is.True,
          "a listener the song has left behind had to wait");
      Assert.That(TapAgreement.WantsMending(listenerMs: 0,
          playingMs: TapAgreement.ApartWorthMendingMs + 100, msSinceLastMending: TapAgreement.MendFarApartWaitMs),
          Is.False, "a small difference was chased as though it were a large one");
    }

    [Test]
    public void AMendingJustMadeIsNotFollowedByAnother()
    {
      Assert.That(TapAgreement.WantsMending(listenerMs: 0, playingMs: TapAgreement.FarApartMs * 2,
          msSinceLastMending: 0), Is.False, "the same difference was sought twice in one breath");
      Assert.That(TapAgreement.WantsMending(listenerMs: 0, playingMs: TapAgreement.FarApartMs * 2,
          msSinceLastMending: TapAgreement.MendFarApartWaitMs + 1), Is.True,
          "a listener still behind could not be mended again");
    }

    [Test]
    public void APlayerThatHasNotStartedIsNothingToAgreeWith()
    {
      // libVLC answers nought or less for a media that has not got going yet, and both engines standing at
      // nought is agreement rather than a difference to mend.
      Assert.That(TapAgreement.WantsMending(listenerMs: 0, playingMs: 0, msSinceLastMending: long.MaxValue),
          Is.False, "a media that never started was sought with a seek");
      Assert.That(TapAgreement.WantsMending(listenerMs: 0, playingMs: -1, msSinceLastMending: long.MaxValue),
          Is.False, "a media answering nothing was sought with a seek");
    }

    [Test]
    public void AListenerThatHasLostTheFileIsNotPushedAtIt()
    {
      // Where the listener answered "I hold nothing" a seek would not answer the question that matters.
      Assert.That(TapAgreement.WantsMending(listenerMs: -1, playingMs: 5000, msSinceLastMending: long.MaxValue),
          Is.False, "a listener without a file was told to stand somewhere in it");
    }
  }
}
