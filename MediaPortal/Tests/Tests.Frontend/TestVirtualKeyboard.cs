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
using System.Linq;
using Avalonia.Controls;
using NUnit.Framework;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// The on-screen keyboard: the reference's layouts key for key, the reference's state rules rule for rule,
  /// and the culture's own layout picked the way the reference picks it. Where the reference's files are
  /// strange - the superscript one that inserts an inverted exclamation mark, the degree sign that carries the
  /// German tilde - the strange reading is the tested one, because it is what the reference ships.
  /// </summary>
  [TestFixture]
  public class TestVirtualKeyboard
  {
    [Test]
    public void EveryGridIsFiveRowsAndEndsWithSpaceAndDone()
    {
      foreach (VirtualKeyboardLayout layout in new[] { VirtualKeyboardLayout.English, VirtualKeyboardLayout.German })
      {
        for (int shifted = 0; shifted < 2; shifted++)
        {
          for (int altGr = 0; altGr < 2; altGr++)
          {
            VirtualKeyboardKey[][] grid = layout.For(shifted == 1, altGr == 1);
            Assert.That(grid.Length, Is.EqualTo(5), "five rows, as the reference draws them");
            VirtualKeyboardKey[] last = grid[grid.Length - 1];
            Assert.That(last.Select(k => k.Type), Is.EquivalentTo(new[] { VirtualKeyType.Space, VirtualKeyType.Done }),
                "the last row is a space and the done button in every grid of both languages");
          }
        }
      }
    }

    [Test]
    public void TheNumberRowCountsFourteenKeysAndEndsOnBackspace()
    {
      VirtualKeyboardKey[] row = VirtualKeyboardLayout.English.For(false, false)[0];
      Assert.That(row.Length, Is.EqualTo(14), "the reference's widest row, which is why the row above the letters is full");
      Assert.That(row[row.Length - 1].Type, Is.EqualTo(VirtualKeyType.Backspace));
      Assert.That(row[0].Legend, Is.EqualTo("`"), "the normal grid opens on the grave accent");
      Assert.That(VirtualKeyboardLayout.English.For(true, false)[0][0].Legend, Is.EqualTo("~"),
          "and the shifted one on its tilde");
    }

    [Test]
    public void TheReferenceSOwnQuirksArePortedNotCorrected()
    {
      // The shifted Alt-Gr grid draws the superscript one and inserts an inverted exclamation mark: that is
      // what the reference's VirtualKeyboardStyle.xaml command parameter says, and a corrected port would be
      // a different keyboard than the one the reference ships.
      VirtualKeyboardKey superscript = FindKey(VirtualKeyboardLayout.English.For(true, true), "¹");
      Assert.That(superscript.Insert, Is.EqualTo("¡"),
          "the key reads one and writes the upside-down bang, exactly as the reference's file spells it");

      // The figure dash on the normal number row inserts an ordinary hyphen.
      VirtualKeyboardKey dash = FindKey(VirtualKeyboardLayout.English.For(false, false), "‒");
      Assert.That(dash.Insert, Is.EqualTo("-"));

      // And the German layout hangs its tilde on the key drawn as a degree sign.
      VirtualKeyboardKey degree = FindKey(VirtualKeyboardLayout.German.For(true, false), "°");
      Assert.That(degree.Insert, Is.EqualTo("~"),
          "the German viewer finds the tilde under the degree sign, because the German layout file puts it there");
    }

    [Test]
    public void GermanSpeaksQwertzAndItsOwnLetters()
    {
      string normal = string.Concat(VirtualKeyboardLayout.German.For(false, false)
          .SelectMany(row => row).Where(k => k.Type == VirtualKeyType.Text).Select(k => k.Legend));
      Assert.That(normal, Does.Contain("ß").And.Contain("ü").And.Contain("ö").And.Contain("ä"),
          "the sharp s and the umlauts are in the normal grid where a German keyboard has them");
      Assert.That(VirtualKeyboardLayout.German.For(false, false)[1][0].Legend, Is.EqualTo("q"),
          "and the letter row still starts at q - QWERTZ is the same start as the English row");
    }

    [Test]
    public void TheCulturePicksItsLayoutTheWayTheReferenceFallsBack()
    {
      Assert.That(VirtualKeyboardLayout.ForCulture("de"), Is.SameAs(VirtualKeyboardLayout.German));
      Assert.That(VirtualKeyboardLayout.ForCulture("de-DE"), Is.SameAs(VirtualKeyboardLayout.German));
      Assert.That(VirtualKeyboardLayout.ForCulture(" DE "), Is.SameAs(VirtualKeyboardLayout.German),
          "the name is trimmed and caseless before it is asked");

      // The reference carries no layout beyond its own language and English, and every other language falls
      // back to the English file - the same answer a null or an empty culture name gives.
      Assert.That(VirtualKeyboardLayout.ForCulture("en"), Is.SameAs(VirtualKeyboardLayout.English));
      Assert.That(VirtualKeyboardLayout.ForCulture("it"), Is.SameAs(VirtualKeyboardLayout.English));
      Assert.That(VirtualKeyboardLayout.ForCulture(null), Is.SameAs(VirtualKeyboardLayout.English));
      Assert.That(VirtualKeyboardLayout.ForCulture(""), Is.SameAs(VirtualKeyboardLayout.English));
    }

    [Test]
    public void ShiftAndCapsExcludeEachOtherTheWayTheControlsDo()
    {
      VirtualKeyboardModel model = new VirtualKeyboardModel(VirtualKeyboardLayout.English);
      model.Begin("");

      model.Shift = true;
      Assert.That(model.Caps, Is.False, "turning shift on turns caps lock off");
      model.Caps = true;
      Assert.That(model.Shift, Is.False, "and the other way round");
    }

    [Test]
    public void ATypedCharacterSpendsTheShiftButNotTheCapsLock()
    {
      VirtualKeyboardModel model = new VirtualKeyboardModel(VirtualKeyboardLayout.English);
      model.Begin("");
      VirtualKeyboardKey a = FindKey(model.Rows, "a");

      model.Shift = true;
      Assert.That(model.Rows[2][0].Legend, Is.EqualTo("A"), "and the grid that answers is the shifted one");
      model.Press(a);
      Assert.That(model.Shift, Is.False, "one letter pays the shift, which is why the second letter is bare again");

      model.Caps = true;
      model.Press(a);
      Assert.That(model.Caps, Is.True, "caps lock is not spent - it stays until the viewer takes it off");

      model.Shift = true;
      model.Press(model.Rows.Last().First(k => k.Type == VirtualKeyType.Space));
      Assert.That(model.Shift, Is.True, "a space does not spend the shift that was paid for");
    }

    [Test]
    public void TheGridsFollowTheThreeStateOfTheReference()
    {
      VirtualKeyboardModel model = new VirtualKeyboardModel(VirtualKeyboardLayout.German);
      model.Begin("");
      Assert.That(model.Rows, Is.SameAs(VirtualKeyboardLayout.German.For(false, false)));

      model.Shift = true;
      Assert.That(model.Rows, Is.SameAs(VirtualKeyboardLayout.German.For(true, false)));
      model.AltGr = true;
      Assert.That(model.Rows, Is.SameAs(VirtualKeyboardLayout.German.For(true, true)),
          "shift and Alt-Gr together show the shifted special grid, exactly as the IsVisible expressions read");
      model.Shift = false;
      Assert.That(model.Rows, Is.SameAs(VirtualKeyboardLayout.German.For(false, true)));
      model.AltGr = false;
      Assert.That(model.Rows, Is.SameAs(VirtualKeyboardLayout.German.For(false, false)));
    }

    [Test]
    public void TheTextIsBuiltSpentAndCleanedAsTheReferenceBuildsIt()
    {
      VirtualKeyboardModel model = new VirtualKeyboardModel(VirtualKeyboardLayout.English);
      model.Begin("hi");
      Assert.That(model.Text, Is.EqualTo("hi"), "the field's own text starts the edit");

      model.Press(FindKey(model.Rows, "x"));
      model.Press(model.Rows.Last().First(k => k.Type == VirtualKeyType.Space));
      Assert.That(model.Text, Is.EqualTo("hix "), "a character and then a space are appended");

      model.Press(model.Rows[0].First(k => k.Type == VirtualKeyType.Backspace));
      Assert.That(model.Text, Is.EqualTo("hix"), "backspace takes the last character");

      VirtualKeyboardKey back = model.Rows[0].First(k => k.Type == VirtualKeyType.Backspace);
      model.Begin(null);
      Assert.That(model.Text, Is.Empty, "opening without a text opens empty - and with all modifiers off");
      Assert.That(model.Shift, Is.False);
      Assert.That(model.Caps, Is.False);
      Assert.That(model.AltGr, Is.False);
      Assert.That(model.Press(back), Is.False, "backspace on empty text does not throw");

      Assert.That(model.Press(VirtualKeyboardKey.Done), Is.True,
          "the done button answers done - closing the keyboard stays the presenter's business");
    }

    [Test]
    public void EveryPressThatChangesSomethingSaysSo()
    {
      VirtualKeyboardModel model = new VirtualKeyboardModel(VirtualKeyboardLayout.English);
      int changes = 0;
      model.Changed += delegate { changes++; };
      model.Begin("");
      model.Press(FindKey(model.Rows, "k"));
      model.Shift = true;
      Assert.That(changes, Is.GreaterThanOrEqualTo(3),
          "the screen redraws on every press a viewer can see, which is what the Changed event is for");
    }

    [Test]
    public void AFieldCarriesItsTriggerAndNothingElseIsDisturbed()
    {
      var box = new TextBox();
      Assert.That(VirtualKeyboardService.GetTrigger(box), Is.EqualTo(VirtualKeyboardTrigger.None),
          "an unmarked field never calls the keyboard");

      VirtualKeyboardService.SetTrigger(box, VirtualKeyboardTrigger.EnterOrMenu);
      Assert.That(VirtualKeyboardService.GetTrigger(box), Is.EqualTo(VirtualKeyboardTrigger.EnterOrMenu));

      VirtualKeyboardService.SetTrigger(box, VirtualKeyboardTrigger.Menu);
      Assert.That(VirtualKeyboardService.GetTrigger(box), Is.EqualTo(VirtualKeyboardTrigger.Menu),
          "re-marking the field replaces the deal instead of stacking a second one");
    }

    /// <summary>The one text key of a grid whose legend is the given word - and a loud word if there is none.</summary>
    private static VirtualKeyboardKey FindKey(VirtualKeyboardKey[][] grid, string legend)
    {
      VirtualKeyboardKey[] keys = grid.SelectMany(row => row)
          .Where(key => key.Type == VirtualKeyType.Text && key.Legend == legend).ToArray();
      Assert.That(keys.Length, Is.EqualTo(1),
          "the grid was written with exactly one key reading '" + legend + "'");
      return keys[0];
    }
  }
}
