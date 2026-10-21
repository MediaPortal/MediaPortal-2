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

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using MediaPortal.Frontend.Views;

namespace MediaPortal.Frontend.Theming
{
  /// <summary>
  /// The skin's grip on the controls no screen draws itself.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Avalonia 12's Fluent theme computes its chrome colours at build time and reads no resource for them -
  /// measured, a button sits on <c>#33ffffff</c> and a list on <c>#2b2b2b</c> whatever any palette says - so a
  /// palette alone left the controls Fluent's and the television only half skinned. These styles say what a
  /// button, a list, a row, a text box and the progress line are painted with, out of the same palette keys the
  /// screens use, and an author style outranks a control theme.
  /// </para>
  /// <para>
  /// They are built for one palette at a time by <see cref="FrontendTheme.Apply"/>, which adds them to the
  /// application only while a real skin is in charge and takes them off for the default palette - that
  /// television keeps exactly the pixels it always had. The states are the states a remote has: resting, the
  /// walk-over, and the press. A selected row takes the accent, because that is where both skins put their
  /// brightest colour, and its text goes to the screen's own background, which is the WMC selection bar in
  /// colour terms.
  /// </para>
  /// <para>
  /// What this does not touch: scroll bars, check boxes' boxes, sliders and flyouts stay Fluent's own - no skin
  /// colour file has anything to say about them, and inventing colours for them would be guessing, not rescuing.
  /// </para>
  /// </remarks>
  public static class FrontendChrome
  {
    /// <summary>
    /// The chrome for one palette, as styles to add to an application.
    /// </summary>
    /// <exception cref="System.ArgumentException">No such palette.</exception>
    public static Styles Build(string palette)
    {
      IBrush panel = FrontendTheme.Brush(palette, "PanelBrush");
      IBrush raised = FrontendTheme.Brush(palette, "PanelRaisedBrush");
      IBrush pressed = FrontendTheme.Brush(palette, "PanelPressedBrush");
      IBrush line = FrontendTheme.Brush(palette, "OutlineBrush");
      IBrush text = FrontendTheme.Brush(palette, "TextBrush");
      IBrush buttonText = FrontendTheme.Brush(palette, "ButtonTextBrush");
      IBrush accent = FrontendTheme.Brush(palette, "AccentBrush");
      IBrush screen = FrontendTheme.Brush(palette, "ScreenBackgroundBrush");

      Styles styles = new Styles();

      Add(styles, new Style(r => r.OfType<Button>()),
          (Button.BackgroundProperty, panel), (Button.BorderBrushProperty, line),
          (Button.ForegroundProperty, buttonText));
      // The two hover styles carry the same condition the skins' hover rules carry: while the window says the
      // pointer is out of sight, nothing may dress itself as stood on. See the comment at the top of the Nereus
      // skin's styles file, which explains the class; the chrome is author style aimed at every button and row in
      // the front end, so it has to hold its tongue here too or a hovered "Refresh" button would stay lit.
      Add(styles, new Style(r => r.Is<Window>()
              .Not(x => x.Class(ShellWindow.PointerRestingClass))
              .Descendant().OfType<Button>().Class(":pointerover")),
          (Button.BackgroundProperty, raised));
      Add(styles, new Style(r => r.OfType<Button>().Class(":pressed")),
          (Button.BackgroundProperty, pressed));

      Add(styles, new Style(r => r.OfType<ListBox>()),
          (ListBox.BackgroundProperty, panel), (ListBox.BorderBrushProperty, line));
      Add(styles, new Style(r => r.OfType<ListBoxItem>()),
          (ListBoxItem.ForegroundProperty, text));
      Add(styles, new Style(r => r.Is<Window>()
              .Not(x => x.Class(ShellWindow.PointerRestingClass))
              .Descendant().OfType<ListBoxItem>().Class(":pointerover")),
          (ListBoxItem.BackgroundProperty, raised));
      Add(styles, new Style(r => r.OfType<ListBoxItem>().Class(":selected")),
          (ListBoxItem.BackgroundProperty, accent), (ListBoxItem.ForegroundProperty, screen));

      Add(styles, new Style(r => r.OfType<TextBox>()),
          (TextBox.BackgroundProperty, panel), (TextBox.BorderBrushProperty, line),
          (TextBox.ForegroundProperty, text), (TextBox.CaretBrushProperty, text));

      Add(styles, new Style(r => r.OfType<ProgressBar>()),
          (ProgressBar.BackgroundProperty, panel), (ProgressBar.ForegroundProperty, accent));
      Add(styles, new Style(r => r.OfType<CheckBox>()),
          (CheckBox.ForegroundProperty, text));
      Add(styles, new Style(r => r.OfType<Separator>()),
          (Separator.BackgroundProperty, line));

      return styles;
    }

    private static void Add(Styles owner, Style style,
        params (Avalonia.AvaloniaProperty property, IBrush brush)[] setters)
    {
      foreach ((Avalonia.AvaloniaProperty property, IBrush brush) in setters)
        style.Setters.Add(new Setter(property, brush));

      owner.Add(style);
    }
  }
}
