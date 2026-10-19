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
using System.Text;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// What pressing a key of the virtual keyboard does. The reference knows exactly these: a character to put
  /// into the edited text, a backspace, a space, the done button that closes the keyboard, and the blank
  /// placeholder keys it draws - disabled - to keep the rows of a grid aligned.
  /// </summary>
  public enum VirtualKeyType
  {
    Text,
    Backspace,
    Space,
    Done,
    Disabled
  }

  /// <summary>
  /// One key of the virtual keyboard: what it shows and what it puts into the text.
  /// </summary>
  /// <remarks>
  /// Legend and inserted text are kept apart because the reference's layouts do not always agree between the
  /// two - and where they disagree, the difference is ported, not corrected: the Alt-Gr-shifted key that is
  /// written as the superscript one but puts an inverted exclamation mark into the text is what the
  /// reference's own layout file says (<c>VirtualKeyboardStyle.xaml</c>, the Shift-AltGr grid), and a German
  /// viewer who learned where the tilde lives finds it under the degree sign, because the German layout hangs
  /// the tilde on the key drawn as a degree sign (<c>VirtualKeyboardStyle_de.xaml</c>).
  /// </remarks>
  public sealed class VirtualKeyboardKey
  {
    public VirtualKeyboardKey(string legend, string insert, VirtualKeyType type)
    {
      Legend = legend ?? throw new ArgumentNullException(nameof(legend));
      Insert = insert ?? "";
      Type = type;
    }

    /// <summary>What stands on the key.</summary>
    public string Legend { get; }

    /// <summary>What the key puts into the edited text (only meaningful for text keys).</summary>
    public string Insert { get; }

    public VirtualKeyType Type { get; }

    public bool IsEnabled => Type != VirtualKeyType.Disabled;

    internal static VirtualKeyboardKey Ch(char character)
    {
      return new VirtualKeyboardKey(character.ToString(), character.ToString(), VirtualKeyType.Text);
    }

    internal static VirtualKeyboardKey Ch(string legend, string insert)
    {
      return new VirtualKeyboardKey(legend, insert, VirtualKeyType.Text);
    }

    /// <summary>The reference draws a small ring above the line on its backspace keys.</summary>
    internal static readonly VirtualKeyboardKey Backspace =
        new VirtualKeyboardKey("˿", "", VirtualKeyType.Backspace);

    internal static readonly VirtualKeyboardKey Space = new VirtualKeyboardKey("", "", VirtualKeyType.Space);

    /// <summary>The reference's row-ender, word for word out of its strings file.</summary>
    internal static readonly VirtualKeyboardKey Done = new VirtualKeyboardKey("Done", "", VirtualKeyType.Done);

    internal static readonly VirtualKeyboardKey Off = new VirtualKeyboardKey("", "", VirtualKeyType.Disabled);

    public override string ToString()
    {
      return Type + ":" + (Legend.Length > 0 ? Legend : Insert);
    }
  }
  /// <summary>
  /// The four grids the on-screen keyboard can show - normal, shifted, Alt-Gr and shifted Alt-Gr - as five
  /// rows of keys each, ported key for key from the reference's layout files.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference draws all four grids on top of each other and shows exactly one of them: normal when
  /// nothing is modified, shifted when Shift <i>or</i> Caps Lock is on, and the Alt-Gr pair when Alt-Gr is on
  /// (<c>VirtualKeyboardStyle.xaml</c>, the <c>Grid.IsVisible</c> expressions). This class makes the same
  /// question a method.
  /// </para>
  /// <para>
  /// The rows keep the reference's column counts - fourteen keys in the number row down to ten in the bottom
  /// letter row - because that is what makes the keyboard look like a keyboard: the short rows carry wider
  /// keys, exactly as the reference's per-row stretched grids do. Where a shifted grid has fewer real keys
  /// than the normal grid, the reference fills the rest with disabled blanks, and those blanks are ported too.
  /// </para>
  /// </remarks>
  public sealed class VirtualKeyboardLayout
  {
    private readonly VirtualKeyboardKey[][] _normal;
    private readonly VirtualKeyboardKey[][] _shifted;
    private readonly VirtualKeyboardKey[][] _altGr;
    private readonly VirtualKeyboardKey[][] _shiftedAltGr;

    internal VirtualKeyboardLayout(VirtualKeyboardKey[][] normal, VirtualKeyboardKey[][] shifted,
        VirtualKeyboardKey[][] altGr, VirtualKeyboardKey[][] shiftedAltGr)
    {
      _normal = normal;
      _shifted = shifted;
      _altGr = altGr;
      _shiftedAltGr = shiftedAltGr;
    }

    // The layouts below read like the reference's XAML - a list of keys - so they spell a plain text key as
    // shortly as that file did; the short names forward to the key's own factories.
    private static VirtualKeyboardKey Ch(char character)
    {
      return VirtualKeyboardKey.Ch(character);
    }

    private static VirtualKeyboardKey Ch(string legend, string insert)
    {
      return VirtualKeyboardKey.Ch(legend, insert);
    }

    /// <summary>The grid the given modifiers ask for - the reference's visibility expressions, as a choice.</summary>
    public VirtualKeyboardKey[][] For(bool shifted, bool altGr)
    {
      if (altGr)
        return shifted ? _shiftedAltGr : _altGr;
      return shifted ? _shifted : _normal;
    }

    /// <summary>The last row, the same in every grid and both languages: a wide space and a done button.</summary>
    private static readonly VirtualKeyboardKey[][] LastRow =
    {
      new[] { VirtualKeyboardKey.Space, VirtualKeyboardKey.Done }
    };
    /// <summary>
    /// The English layout, key for key from the reference's <c>VirtualKeyboardStyle.xaml</c> - which is also
    /// what the reference falls back to for every language it has no own layout file for. The figure dash on
    /// the number row inserts an ordinary hyphen, because that is what its command parameter says.
    /// </summary>
    public static readonly VirtualKeyboardLayout English = new VirtualKeyboardLayout(
        new[]
        {
          new[]
          {
            Ch('`'), Ch('1'), Ch('2'), Ch('3'), Ch('4'), Ch('5'), Ch('6'), Ch('7'), Ch('8'), Ch('9'), Ch('0'),
            Ch("‒", "-"), Ch('='), VirtualKeyboardKey.Backspace
          },
          new[]
          {
            Ch('q'), Ch('w'), Ch('e'), Ch('r'), Ch('t'), Ch('y'), Ch('u'), Ch('i'), Ch('o'), Ch('p'),
            Ch('['), Ch(']'), Ch('\\')
          },
          new[]
          {
            Ch('a'), Ch('s'), Ch('d'), Ch('f'), Ch('g'), Ch('h'), Ch('j'), Ch('k'), Ch('l'), Ch(';'), Ch('\'')
          },
          new[]
          {
            Ch('z'), Ch('x'), Ch('c'), Ch('v'), Ch('b'), Ch('n'), Ch('m'), Ch(','), Ch('.'), Ch('/')
          },
          LastRow[0]
        },
        new[]
        {
          new[]
          {
            Ch('~'), Ch('!'), Ch('@'), Ch('#'), Ch('$'), Ch('%'), Ch('^'), Ch('&'), Ch('*'), Ch('('), Ch(')'),
            Ch('_'), Ch('+'), VirtualKeyboardKey.Backspace
          },
          new[]
          {
            Ch('Q'), Ch('W'), Ch('E'), Ch('R'), Ch('T'), Ch('Y'), Ch('U'), Ch('I'), Ch('O'), Ch('P'),
            Ch('{'), Ch('}'), Ch('|')
          },
          new[]
          {
            Ch('A'), Ch('S'), Ch('D'), Ch('F'), Ch('G'), Ch('H'), Ch('J'), Ch('K'), Ch('L'), Ch(':'), Ch('"')
          },
          new[]
          {
            Ch('Z'), Ch('X'), Ch('C'), Ch('V'), Ch('B'), Ch('N'), Ch('M'), Ch('<'), Ch('>'), Ch('?')
          },
          LastRow[0]
        },
        new[]
        {
          new[]
          {
            VirtualKeyboardKey.Off, Ch('¡'), Ch('²'), Ch('³'), Ch('¤'), Ch('€'), Ch('¼'), Ch('½'), Ch('¾'),
            Ch('‘'), Ch('’'), Ch('¥'), Ch('*'), VirtualKeyboardKey.Backspace
          },
          new[]
          {
            Ch('ä'), Ch('å'), Ch('é'), Ch('®'), Ch('Þ'), Ch('ü'), Ch('ú'), Ch('í'), Ch('ó'), Ch('ö'),
            Ch('«'), Ch('»'), Ch('¬')
          },
          new[]
          {
            Ch('á'), Ch('ß'), Ch('ð'), VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, VirtualKeyboardKey.Off,
            VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, Ch('¶'), Ch('´')
          },
          new[]
          {
            Ch('æ'), VirtualKeyboardKey.Off, Ch('©'), VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, Ch('ñ'),
            Ch('µ'), Ch('ç'), VirtualKeyboardKey.Off, Ch('¿')
          },
          LastRow[0]
        },
        // The reference's Shift-AltGr grid - identical in its English and its German layout file, down to the
        // key that is drawn as a superscript one but inserts an inverted exclamation mark.
        new[]
        {
          new[]
          {
            VirtualKeyboardKey.Off, Ch("¹", "¡"), VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, Ch('£'),
            VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, VirtualKeyboardKey.Off,
            VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, Ch('÷'),
            VirtualKeyboardKey.Backspace
          },
          new[]
          {
            Ch('Ä'), Ch('Å'), Ch('É'), VirtualKeyboardKey.Off, Ch('Þ'), Ch('Ü'), Ch('Ú'), Ch('Í'), Ch('Ó'),
            Ch('Ö'), VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, Ch('¦')
          },
          new[]
          {
            Ch('Á'), Ch('§'), Ch('Ð'), VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, VirtualKeyboardKey.Off,
            VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, Ch('Ø'), Ch('°'), Ch('¨')
          },
          new[]
          {
            Ch('Æ'), VirtualKeyboardKey.Off, Ch('¢'), VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, Ch('Ñ'),
            VirtualKeyboardKey.Off, Ch('Ç'), VirtualKeyboardKey.Off, VirtualKeyboardKey.Off
          },
          LastRow[0]
        });

    internal VirtualKeyboardKey[][] ShiftedAltGrRows => _shiftedAltGr;
    /// <summary>
    /// The German layout, key for key from the reference's <c>VirtualKeyboardStyle_de.xaml</c>: QWERTZ, the
    /// umlauts on their own keys, and the reference's own quirks kept where they stand - the degree-sign key
    /// that types a tilde, and the Alt-Gr rows whose blanks fill whole rows the German file really leaves
    /// empty. Its Shift-AltGr grid is the English file's, byte for byte, so the same object carries both.
    /// </summary>
    public static readonly VirtualKeyboardLayout German = new VirtualKeyboardLayout(
        new[]
        {
          new[]
          {
            Ch('`'), Ch('1'), Ch('2'), Ch('3'), Ch('4'), Ch('5'), Ch('6'), Ch('7'), Ch('8'), Ch('9'), Ch('0'),
            Ch('ß'), VirtualKeyboardKey.Backspace
          },
          new[]
          {
            Ch('q'), Ch('w'), Ch('e'), Ch('r'), Ch('t'), Ch('z'), Ch('u'), Ch('i'), Ch('o'), Ch('p'),
            Ch('ü'), Ch('+')
          },
          new[]
          {
            Ch('a'), Ch('s'), Ch('d'), Ch('f'), Ch('g'), Ch('h'), Ch('j'), Ch('k'), Ch('l'), Ch('ö'), Ch('ä'),
            Ch('\'')
          },
          new[]
          {
            Ch('y'), Ch('x'), Ch('c'), Ch('v'), Ch('b'), Ch('n'), Ch('m'), Ch(','), Ch('.'), Ch("‒", "-")
          },
          LastRow[0]
        },
        new[]
        {
          new[]
          {
            Ch("°", "~"), Ch('!'), Ch('"'), Ch('§'), Ch('$'), Ch('%'), Ch('&'), Ch('/'), Ch('('), Ch(')'),
            Ch('='), Ch('?'), VirtualKeyboardKey.Backspace
          },
          new[]
          {
            Ch('Q'), Ch('W'), Ch('E'), Ch('R'), Ch('T'), Ch('Z'), Ch('U'), Ch('I'), Ch('O'), Ch('P'),
            Ch('Ü'), Ch('*')
          },
          new[]
          {
            Ch('A'), Ch('S'), Ch('D'), Ch('F'), Ch('G'), Ch('H'), Ch('J'), Ch('K'), Ch('L'), Ch('Ö'), Ch('Ä'),
            Ch('\'')
          },
          new[]
          {
            Ch('Y'), Ch('X'), Ch('C'), Ch('V'), Ch('B'), Ch('N'), Ch('M'), Ch(';'), Ch(':'), Ch('_')
          },
          LastRow[0]
        },
        new[]
        {
          new[]
          {
            VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, Ch('²'), Ch('³'), VirtualKeyboardKey.Off,
            VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, Ch('{'), Ch('['), Ch(']'), Ch('}'), Ch('\\'),
            VirtualKeyboardKey.Backspace
          },
          new[]
          {
            Ch('@'), VirtualKeyboardKey.Off, Ch('€'), VirtualKeyboardKey.Off, VirtualKeyboardKey.Off,
            VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, VirtualKeyboardKey.Off,
            VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, Ch('~')
          },
          new[]
          {
            VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, VirtualKeyboardKey.Off,
            VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, VirtualKeyboardKey.Off,
            VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, VirtualKeyboardKey.Off
          },
          new[]
          {
            Ch('|'), VirtualKeyboardKey.Off, VirtualKeyboardKey.Off, VirtualKeyboardKey.Off,
            VirtualKeyboardKey.Off, Ch('µ'), VirtualKeyboardKey.Off, VirtualKeyboardKey.Off,
            VirtualKeyboardKey.Off, VirtualKeyboardKey.Off
          },
          LastRow[0]
        },
        English.ShiftedAltGrRows);

    /// <summary>
    /// The layout the reference would pick for this culture: its German file answers German, and everything
    /// else answers the base file - the same fallback its layout lookup walked (exact culture, then parent,
    /// then the layout without a language at all).
    /// </summary>
    public static VirtualKeyboardLayout ForCulture(string? cultureName)
    {
      if (!string.IsNullOrWhiteSpace(cultureName) &&
          cultureName.Trim().ToLowerInvariant().StartsWith("de", StringComparison.Ordinal))
      {
        return German;
      }
      return English;
    }
  }

  /// <summary>
  /// The state a press of the on-screen keyboard answers from: which modifiers are on, which grid that asks
  /// for, and what the keyboard does to the text. The reference's <c>VirtualKeyboardControl</c> keeps exactly
  /// these three booleans and exactly these rules, and this class repeats them.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Shift and Caps Lock exclude each other (turning one on turns the other off), Caps Lock survives a typed
  /// character while Shift does not, a space does not spend the Shift that was paid for, and the whole state
  /// starts clean every time the keyboard is opened - each one is a rule of the reference's control, not a
  /// decision made here.
  /// </para>
  /// <para>
  /// The diacritic keys the reference's control area also carries (the accents that compose a character from
  /// two presses) are not ported: they belong to a control region the shipped layout files leave out, and
  /// inventing one would be exactly the kind of guess this port refuses.
  /// </para>
  /// </remarks>
  public sealed class VirtualKeyboardModel
  {
    private readonly StringBuilder _text = new StringBuilder();
    private bool _shift;
    private bool _caps;
    private bool _altGr;

    public VirtualKeyboardModel(VirtualKeyboardLayout layout)
    {
      Layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    public VirtualKeyboardLayout Layout { get; }

    /// <summary>Raised whenever a press changes anything a viewer can see - the grid, or the text.</summary>
    public event EventHandler? Changed;

    /// <summary>Whether the next character comes shifted. Spent by the next character, like the reference's.</summary>
    public bool Shift
    {
      get { return _shift; }
      set
      {
        _shift = value;
        if (_shift)
          _caps = false;
        OnChanged();
      }
    }

    /// <summary>Whether everything comes shifted until further notice - the one that survives a typed key.</summary>
    public bool Caps
    {
      get { return _caps; }
      set
      {
        _caps = value;
        if (_caps)
          _shift = false;
        OnChanged();
      }
    }

    /// <summary>The third state of the reference's keyboard: the special-character grids.</summary>
    public bool AltGr
    {
      get { return _altGr; }
      set
      {
        _altGr = value;
        OnChanged();
      }
    }

    /// <summary>The grid the current modifiers ask for, as five rows of keys.</summary>
    public VirtualKeyboardKey[][] Rows => Layout.For(_shift || _caps, _altGr);

    /// <summary>The text the keyboard is building, kept here exactly like the reference keeps it there.</summary>
    public string Text => _text.ToString();

    /// <summary>Starts the edit from the text the field already held, with all modifiers off.</summary>
    public void Begin(string? initialText)
    {
      // InitializeStates, and the text of the field that asked for the keyboard.
      _shift = false;
      _caps = false;
      _altGr = false;
      _text.Clear();
      _text.Append(initialText ?? "");
      OnChanged();
    }

    /// <summary>
    /// Presses one key of the grid. Returns <c>true</c> when the press was the done button - the closing is
    /// the presenter's business, and the reference decides that division the same way round.
    /// </summary>
    public bool Press(VirtualKeyboardKey key)
    {
      switch (key.Type)
      {
        case VirtualKeyType.Text:
          _text.Append(key.Insert);
          // A typed character spends the shift but never the caps lock.
          _shift = false;
          OnChanged();
          return false;

        case VirtualKeyType.Space:
          _text.Append(' ');
          OnChanged();
          return false;

        case VirtualKeyType.Backspace:
          if (_text.Length > 0)
            _text.Length--;
          OnChanged();
          return false;

        case VirtualKeyType.Done:
          return true;

        default:
          return false;
      }
    }

    private void OnChanged()
    {
      Changed?.Invoke(this, EventArgs.Empty);
    }
  }
}
