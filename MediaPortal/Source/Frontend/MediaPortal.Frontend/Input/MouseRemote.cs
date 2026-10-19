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
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace MediaPortal.Frontend.Input
{
  /// <summary>
  /// Makes a mouse double-click on a list mean what the remote's Enter means on that same list.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Every action in this front end - open a tile, play a title, choose a group - lives behind the Enter key,
  /// because a television is operated with a remote and not with a mouse. That is still right; what it left
  /// behind is a machine on a desk that can be clicked but not steered: the mouse could select a row - Avalonia
  /// does that on its own - and there was nothing in the world from which the row would know it was meant to be
  /// opened. So rather than give every screen a second path written twice, this turns the double-click into the
  /// key press a remote would have sent: one double-click, one <see cref="Key.Enter"/>, the window's own tested
  /// handler decides what that means for the list under the pointer.
  /// </para>
  /// <para>
  /// The key is not raised inside the pointer event: the click that becomes a double-click is still selecting
  /// its row at that moment, and Enter asked about a row that has not finished being selected opens yesterday's
  /// one. It is posted to the dispatcher instead, which runs it after the click has done its work - and not
  /// marking the press handled serves the same end: the list still gets to finish its own mouse business.
  /// </para>
  /// </remarks>
  public static class MouseRemote
  {
    /// <summary>
    /// Routes a double-click on each of <paramref name="lists"/> through the Enter handler of
    /// <paramref name="root"/>.
    /// </summary>
    /// <param name="root">The control whose key handlers decide what Enter means - normally the window.</param>
    /// <param name="lists">The lists a viewer may want to open something from.</param>
    public static void Attach(Control root, params Control[] lists)
    {
      if (root == null)
        throw new ArgumentNullException(nameof(root));

      foreach (Control list in lists)
      {
        if (list == null)
          continue;

        Control target = list;
        target.AddHandler(
            InputElement.PointerPressedEvent,
            delegate (object? sender, PointerPressedEventArgs args)
            {
              if (args.ClickCount != 2)
                return;

              Dispatcher.UIThread.Post(delegate
              {
                // The focus moves first for the same reason the key comes after the click: the screens ask
                // "who is focused" to decide what Enter is for, and a clicked row should answer that like a
                // walked-to one. Where the focus cannot be taken - a window that is not showing - nothing is
                // sent, rather than an Enter aimed at whoever happens to hold the focus.
                if (!target.Focus())
                  return;

                root.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Enter,
                    Source = target
                });
              });
            },
            RoutingStrategies.Tunnel);
      }
    }
  }
}
