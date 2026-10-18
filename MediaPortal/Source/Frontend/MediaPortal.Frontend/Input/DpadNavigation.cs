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
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MediaPortal.Frontend.Input
{
  /// <summary>
  /// Moves focus between the areas of a screen with a remote control's directional keys.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Avalonia already navigates with the keyboard, but it is tuned for a desktop: <c>Tab</c> walks the tree and
  /// what the arrow keys do is spread over controls and themes. A ten-foot interface wants one rule instead, and
  /// this is it: <em>a control that has a use for a direction keeps it, and the ring is what happens to a
  /// direction nobody wanted.</em> A list therefore moves through its items with up/down - it handles those keys
  /// itself, and the ring never sees them - while a button, which has no use for an arrow, hands it to the ring
  /// and the ring moves the focus to the next area of the screen.
  /// </para>
  /// <para>
  /// The screen hands over an explicit <em>ring</em>: the controls that the directional keys move between, in
  /// the order they are arranged in. Explicit beats derived, because "which control is next" is a decision about
  /// the layout that the layout should make, not something inferred from the visual tree at the moment the key
  /// arrives. The ring wraps at both ends; on a screen with four reachable areas and no mouse, refusing to move
  /// at the last one means the viewer has to walk back.
  /// </para>
  /// </remarks>
  public static class DpadNavigation
  {
    /// <summary>
    /// Installs the handler on <paramref name="root"/>. The keys are watched while they bubble, so a control that
    /// wants a direction for itself - a list working its way through its items - takes it before the ring sees it.
    /// </summary>
    /// <param name="root">The control whose key events are watched, normally the window.</param>
    /// <param name="ring">The controls to move between, in the order they are arranged on screen.</param>
    public static void Attach(Control root, IReadOnlyList<Control> ring)
    {
      if (root == null)
        throw new ArgumentNullException(nameof(root));
      if (ring == null)
        throw new ArgumentNullException(nameof(ring));
      if (ring.Count == 0)
        throw new ArgumentException("a ring with no controls has nothing to move between", nameof(ring));

      root.AddHandler(
          InputElement.KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Handled)
              return;
            Control? target = NextReachableInRing(ring, args.Key);
            if (target == null)
              return;

            // The key is claimed only when the focus really moved. Reporting success while the viewer's key
            // achieved nothing would also take away the chance that anything else answers it - which is how a
            // control that cannot take the focus ends up swallowing the remote entirely.
            if (target.Focus())
              args.Handled = true;
          },
          RoutingStrategies.Bubble);
    }

    /// <summary>
    /// Which control the given key moves the focus to, walking past the areas of the ring that cannot take the
    /// focus. <c>null</c> means the key is not a directional one, or that nothing in the ring could be reached.
    /// </summary>
    /// <remarks>
    /// Skipping matters as soon as a screen can offer less than it was built for. The first run of the server
    /// screen is the example: with an empty address both the test and the save button are disabled, and a ring
    /// that insists on the next control in order leaves the viewer trapped in the field with no way to the close
    /// button. A remote has no mouse to fall back on, so the areas that are not there are walked over rather than
    /// walked into.
    /// </remarks>
    public static Control? NextReachableInRing(IReadOnlyList<Control> ring, Key key)
    {
      int direction = DirectionOf(key);
      if (direction == 0)
        return null;
      if (ring.Count == 0)
        throw new ArgumentException("a ring with no controls has nothing to move between", nameof(ring));

      int from = IndexOfFocused(ring);
      if (from < 0)
      {
        // Nothing in the ring has the focus; the answer to "go anywhere" is the first area that can take it.
        for (int i = 0; i < ring.Count; i++)
          if (CanTakeFocus(ring[i]))
            return ring[i];
        return null;
      }

      // One lap at most. If every area is unreachable - or the only reachable one is the one that already has the
      // focus - the key is nobody's, which the caller reports by not claiming it.
      for (int step = 1; step <= ring.Count; step++)
      {
        Control candidate = ring[(((from + direction * step) % ring.Count) + ring.Count) % ring.Count];
        if (!ReferenceEquals(candidate, ring[from]) && CanTakeFocus(candidate))
          return candidate;
      }

      return null;
    }

    private static bool CanTakeFocus(Control control)
    {
      // A hidden area is not an area: the home screen keeps the playback button out of the ring on a machine
      // without a player, but the buttons it does put in can still be disabled by the state of the screen, and
      // Avalonia refuses focus to both kinds.
      return control.IsVisible && control.IsEnabled && control.Focusable;
    }

    private static int DirectionOf(Key key)
    {
      switch (key)
      {
        case Key.Right:
        case Key.Down:
          return 1;
        case Key.Left:
        case Key.Up:
          return -1;
        default:
          return 0;
      }
    }

    /// <summary>
    /// Which control the given key moves focus to, or <c>null</c> when the key is not a directional one. Split
    /// out of the event handler because "what does Right mean here" is the whole behaviour, and it is the part
    /// worth testing without a window, a focus scope or a display.
    /// </summary>
    public static Control? NextInRing(IReadOnlyList<Control> ring, Key key)
    {
      int direction = DirectionOf(key);
      if (direction == 0)
        return null;

      int from = IndexOfFocused(ring);
      if (from < 0)
        return ring[0]; // Nothing in the ring has the focus yet; the first area is the answer to "go anywhere".

      int count = ring.Count;
      return ring[((from + direction) % count + count) % count];
    }

    private static int IndexOfFocused(IReadOnlyList<Control> ring)
    {
      for (int i = 0; i < ring.Count; i++)
        if (ring[i].IsFocused)
          return i;
      return -1;
    }
  }
}
