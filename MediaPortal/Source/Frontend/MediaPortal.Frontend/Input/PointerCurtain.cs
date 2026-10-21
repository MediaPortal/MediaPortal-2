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

using System.Runtime.InteropServices;

namespace MediaPortal.Frontend.Input
{
  /// <summary>
  /// The curtain that keeps the pointer off the television, drawn where nothing in this program can lift it: the
  /// operating system's own count of how often the pointer has been asked to be shown.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A pointer is painted while that count stands at zero or above, and not painted below it - for every window,
  /// popup and layer of this thread, whatever element any of them believes the pointer is standing on. That is
  /// the whole reason for a curtain rather than an empty cursor: an empty cursor is an answer to the question
  /// "what should the pointer look like here", and the answer is only asked of the element under the pointer at
  /// the moment that element is still there. A home content rebuild tears that element away and builds new ones,
  /// and in the moment between the two the framework has no answer left, hands the platform its arrow, and shows
  /// it - one frame, on every switch between two menu items with media, straight through the resting pointer.
  /// </para>
  /// <para>
  /// Drawing the curtain one fold deeper than anybody needs absorbs exactly those single showings: a fold that a
  /// passing <c>ShowCursor</c> lifts leaves the next fold in place, so the pointer stays off the picture while
  /// the framework sorts itself out again. Lifting is done by counting up to zero, which lands on zero from
  /// whatever depth the curtain happens to have, and drawing is idempotent, so it may be repeated as often as
  /// anything is heard from the mouse.
  /// </para>
  /// </remarks>
  internal static class PointerCurtain
  {
    /// <summary>How many folds deep the curtain is drawn. Two: one the framework may lift by accident and one
    /// that stays, which is the difference between a pointer that flickers and a pointer that is gone.</summary>
    private const int FOLDS = 2;

    // Whether this curtain drew at all since it was last lifted. Reading the count is only ever safe behind a
    // drawn curtain, so whether one is drawn has to be remembered rather than asked.
    private static bool _drawn;

    /// <summary>
    /// Draws the curtain, or tightens it if it is already drawn. Repeated as often as anything is heard from the
    /// mouse or laid out, which is why it has to end at the same depth however deep it stands.
    /// </summary>
    internal static void Draw()
    {
      if (!_drawn)
      {
        // The first fold of an episode is drawn without asking where the count stands, because asking would
        // mean lifting the curtain that is not drawn yet - a pointer being asked about is a pointer blinking
        // out of sight for the asking.
        for (int i = 0; i < FOLDS; i++)
          Desktop.ShowCursor(false);

        _drawn = true;
        return;
      }

      // Tightening. Where the count stands is read from behind the curtain, which never uncovers anything, and
      // as many folds are added as are missing - none, when the curtain still stands where it was drawn.
      int at = Count();
      for (int i = 0; i < at + FOLDS; i++)
        Desktop.ShowCursor(false);
    }

    /// <summary>
    /// Lifts the curtain entirely: the count is walked up to zero, from whatever depth it was drawn to, so a
    /// pointer that is wanted again is a pointer that is seen.
    /// </summary>
    internal static void Lift()
    {
      while (Desktop.ShowCursor(true) < 0)
      {
        // Counted up one fold at a time until the operating system is willing to paint again. Bounded: the count
        // cannot go below the number of times it was drawn, and every tightening leaves it at FOLDS deep.
      }

      _drawn = false;
    }

    /// <summary>
    /// The count as it stands, read without changing it: one fold down and the same fold back. Only ever asked
    /// behind a drawn curtain, where both halves of that pair stay below zero and nothing is painted either way.
    /// </summary>
    private static int Count()
    {
      Desktop.ShowCursor(false);
      return Desktop.ShowCursor(true);
    }

    /// <summary>The one thing Win32 is asked here, and it is asked of the thread that paints this window.</summary>
    private static class Desktop
    {
      [DllImport("user32.dll")]
      internal static extern int ShowCursor(bool bShow);
    }
  }
}
