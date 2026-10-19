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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.Views
{
  /// <summary>How a marked text field calls the on-screen keyboard.</summary>
  public enum VirtualKeyboardTrigger
  {
    /// <summary>The field never calls it by itself - the screen can still open it by hand.</summary>
    None,

    /// <summary>The remote's Menu key on the focused field. The one key no field of ours has claimed.</summary>
    Menu,

    /// <summary>Menu, and also Enter - for fields where Enter means nothing else. That is the reference's
    /// rule there (OK on a focused editor calls the keyboard), kept for the fields that are free of it.</summary>
    EnterOrMenu
  }

  /// <summary>
  /// The question every text field of the front end should be able to answer on a television: how does a
  /// viewer type without a keyboard. The reference answers it with <c>DialogVirtualKeyboard</c> - a skin
  /// dialog its editors call when the viewer presses OK on them (TextControl.OnKeyPressed) - and this class
  /// is the call, adapted: the reference's screens let the keyboard own OK and kept confirmation on the
  /// on-screen buttons, while our screens have long made Enter-on-the-field the confirmation, so a field that
  /// already answers Enter asks for the keyboard with the remote's Menu key instead, and only the fields that
  /// answer nothing to Enter get the reference's own key.
  /// </summary>
  public class VirtualKeyboardService
  {
    private VirtualKeyboardService()
    {
    }

    /// <summary>
    /// Marks a text field as typable by the on-screen keyboard and says which key calls it. Marked fields are
    /// the XAML decision, like every other decision about what a screen shows.
    /// </summary>
    public static readonly AttachedProperty<VirtualKeyboardTrigger> TriggerProperty =
        AvaloniaProperty.RegisterAttached<VirtualKeyboardService, TextBox, VirtualKeyboardTrigger>(
            "VirtualKeyboard", VirtualKeyboardTrigger.None);

    static VirtualKeyboardService()
    {
      TriggerProperty.Changed.AddClassHandler<TextBox>(OnTriggerChanged);
    }

    public static void SetTrigger(TextBox box, VirtualKeyboardTrigger value)
    {
      box.SetValue(TriggerProperty, value);
    }

    public static VirtualKeyboardTrigger GetTrigger(TextBox box)
    {
      return box.GetValue(TriggerProperty);
    }

    private static void OnTriggerChanged(TextBox box, AvaloniaPropertyChangedEventArgs args)
    {
      // One subscription for the life of the field; the handler asks the field's current answer, so changing
      // the mark between None and Menu never leaves a stale listener behind.
      box.RemoveHandler(TextBox.KeyDownEvent, OnFieldKeyDown);
      box.AddHandler(TextBox.KeyDownEvent, OnFieldKeyDown, RoutingStrategies.Bubble);
    }

    private static void OnFieldKeyDown(object? sender, KeyEventArgs args)
    {
      if (args.Handled || !(sender is TextBox box))
        return;

      VirtualKeyboardTrigger trigger = box.GetValue(TriggerProperty);
      if (trigger == VirtualKeyboardTrigger.None)
        return;

      bool menu = args.Key == Key.F16;
      bool enter = args.Key == Key.Enter || args.Key == Key.Return;
      if (menu || (enter && trigger == VirtualKeyboardTrigger.EnterOrMenu))
      {
        args.Handled = true;
        Open(box);
      }
    }

    /// <summary>
    /// Asks this field's shell window to show the keyboard over its screen. The layout is the one the
    /// reference would pick for the machine's language - German keys for German, the base layout otherwise.
    /// </summary>
    public static void Open(TextBox box)
    {
      ShellWindow? shell = Avalonia.Controls.TopLevel.GetTopLevel(box) as ShellWindow;
      shell?.ShowVirtualKeyboard(box);
    }
  }
}
