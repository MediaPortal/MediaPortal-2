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
using System.IO;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using NUnit.Framework;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// The key bindings as data: the defaults must be exactly the keys the screens compared against before the
  /// map existed, an edited file must reach the screens, and a broken file must not silence a remote.
  /// </summary>
  [TestFixture]
  public class TestKeyBindings
  {
    [Test]
    public void TheDefaultsAnswerTheKeysTheScreensWereWrittenWith()
    {
      KeyBindings defaults = KeyBindings.TheDefaults();

      Assert.That(defaults.Is(KeyAction.Back, Key.Escape), Is.True);
      Assert.That(defaults.Is(KeyAction.Back, Key.Back), Is.True);
      Assert.That(defaults.Is(KeyAction.Menu, Key.F16), Is.True);
      // The information key of a remote belongs to the screen's own menu, which is what the reference bound it to
      // (master_mediamenu.xaml:400); the details question sits beside it on a letter of its own rather than
      // sharing the key, because one key cannot answer two questions.
      Assert.That(defaults.Is(KeyAction.MediaMenu, Key.I), Is.True);
      Assert.That(defaults.Is(KeyAction.MediaMenu, Key.Apps), Is.True);
      Assert.That(defaults.Is(KeyAction.Details, Key.D), Is.True);
      Assert.That(defaults.Is(KeyAction.Details, Key.I), Is.False,
          "and the info key is not quietly still the details as well");
      Assert.That(defaults.Is(KeyAction.PlayPause, Key.MediaPlayPause), Is.True);
      Assert.That(defaults.Is(KeyAction.Find, Key.F), Is.True);
      Assert.That(defaults.Is(KeyAction.Watched, Key.W), Is.True);
      Assert.That(defaults.Is(KeyAction.View, Key.V), Is.True);
      Assert.That(defaults.Is(KeyAction.Media, Key.M), Is.True);

      Assert.That(defaults.Is(KeyAction.Menu, Key.F5), Is.False, "and nothing else means them");
      Assert.That(defaults.Is(KeyAction.Back, Key.F16), Is.False);
    }

    [Test]
    public void AnEditedFileRefitsTheRemoteAndAnUnknownKeyIsIgnored()
    {
      string file = Path.Combine(Path.GetTempPath(), "mp2-keys-" + Guid.NewGuid().ToString("N") + ".json");
      File.WriteAllText(file, "{ 'bindings': { 'Menu': ['F5', 'NotAKeyAnywhere'], 'Back': ['BrowserBack'] } }"
          .Replace('\'', '"'));
      try
      {
        KeyBindings read = KeyBindings.Load(file, out string problem);
        Assert.That(problem, Is.Null);
        Assert.That(read.Is(KeyAction.Menu, Key.F5), Is.True, "the remote's menu button reports F5 on this machine");
        Assert.That(read.Is(KeyAction.Menu, Key.F16), Is.False,
            "and the action now answers the viewer's key instead of the written one");

        KeyBindings merged = KeyBindings.TheDefaults();
        foreach (var entry in read.Bindings)
          merged.Bindings[entry.Key] = entry.Value;
        Assert.That(merged.Is(KeyAction.Menu, Key.F5), Is.True);
        Assert.That(merged.Is(KeyAction.Details, Key.D), Is.True,
            "an action the file forgot keeps its default keys - half a file silences nothing");
        Assert.That(merged.Is(KeyAction.Back, Key.BrowserBack), Is.True, "a key the front end never heard of works");
      }
      finally
      {
        File.Delete(file);
      }
    }

    /// <summary>
    /// The file a viewer has on disk when the front end has moved a question to a key of its own since the file
    /// was written. Until the 26th of October the media menu and the details were the same I, and the split left
    /// every existing file binding the details to the keys the menu had taken - which took the letter away from
    /// the details and gave one key to two questions. Such an entry is the leftover of a split and not a choice,
    /// so it is read as that; what a viewer chose themselves is never read away.
    /// </summary>
    [Test]
    public void AFileWrittenBeforeTheSplitStillAnswersEachQuestionWithItsOwnKey()
    {
      string file = Path.Combine(Path.GetTempPath(), "mp2-keys-" + Guid.NewGuid().ToString("N") + ".json");
      try
      {
        // The file as it stands on a machine that has been running the front end since before the split.
        File.WriteAllText(file, ("{ 'bindings': { 'Back': ['Escape', 'Back'], 'Menu': ['F16'], "
            + "'Details': ['I', 'Apps'], 'PlayPause': ['MediaPlayPause'], 'Find': ['F'], 'Watched': ['W'], "
            + "'View': ['V'], 'Media': ['M'] } }").Replace('\'', '"'));

        KeyBindings merged = KeyBindings.Merged(KeyBindings.Load(file, out _), KeyBindings.TheDefaults(),
            out string? repaired);

        Assert.That(merged.Is(KeyAction.Details, Key.D), Is.True,
            "the details answer the letter of their own word again");
        Assert.That(merged.Is(KeyAction.MediaMenu, Key.I), Is.True,
            "and the screen's menu keeps the key the reference gave it");
        Assert.That(merged.Is(KeyAction.MediaMenu, Key.Apps), Is.True,
            "the remote's info button, which reaches this code as Apps, included");
        Assert.That(merged.Is(KeyAction.Back, Key.Escape), Is.True, "everything else stands as the file has it");
        Assert.That(repaired, Is.Not.Null.And.Contains("Details"), "and the repair says so to the console");

        // The same file with a key a viewer really did choose, which is nobody's leftover and stands.
        File.WriteAllText(file, "{ 'bindings': { 'Details': ['J'] } }".Replace('\'', '"'));
        merged = KeyBindings.Merged(KeyBindings.Load(file, out _), KeyBindings.TheDefaults(), out repaired);
        Assert.That(merged.Is(KeyAction.Details, Key.J), Is.True, "a viewer's own letter is theirs");
        Assert.That(repaired, Is.Null, "and nothing is repaired away that was chosen");
      }
      finally
      {
        File.Delete(file);
      }
    }

    [Test]
    public void ABrokenFileLeavesTheKeysAnsweringWhatTheyWereWrittenTo()
    {
      string file = Path.Combine(Path.GetTempPath(), "mp2-keys-" + Guid.NewGuid().ToString("N") + ".json");
      File.WriteAllText(file, "{ this is not json ");
      try
      {
        KeyBindings read = KeyBindings.Load(file, out string problem);
        Assert.That(problem, Is.Not.Null.And.Not.Empty, "and says so to whoever asked");
        Assert.That(read.Bindings, Is.Empty, "a file that cannot be read binds nothing of its own");

        KeyBindings defaults = KeyBindings.TheDefaults();
        Assert.That(defaults.Is(KeyAction.Back, Key.Escape), Is.True,
            "the remote keeps working on a machine whose JSON a viewer half-edited");
      }
      finally
      {
        File.Delete(file);
      }
    }

    [Test]
    public void TheWrittenKeysCanBeReadBackForTheScreenThatHasToShowThem()
    {
      Assert.That(KeyBindings.TheDefaults().KeysFor(KeyAction.Back),
          Is.EquivalentTo(new[] { Key.Escape, Key.Back }));
      Assert.That(KeyBindings.TheDefaults().KeysFor(KeyAction.Menu), Is.EquivalentTo(new[] { Key.F16 }));
    }

    /// <summary>
    /// The proof that the map is not a shelf ornament: a field marked for the keyboard, a shell around it,
    /// and a binding file that moved the menu key onto F5. F16 does nothing to that field now, and F5 calls
    /// the keyboard - the file decided it, not the code.
    /// </summary>
    [AvaloniaTest]
    public void ARefittedRemoteIsWornByTheScreensWithoutARecompile()
    {
      KeyBindings refitted = KeyBindings.TheDefaults();
      refitted.Bindings[KeyAction.Menu.ToString()] = new System.Collections.Generic.List<string> { "F5" };
      KeyBindings.Override(refitted);
      try
      {
        var box = new TextBox { Text = "" };
        VirtualKeyboardService.SetTrigger(box, VirtualKeyboardTrigger.Menu);
        var window = new ShellWindow(box);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        box.Focus();
        box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.F16, Source = box });
        Dispatcher.UIThread.RunJobs();
        Assert.That(window.KeyboardOpen, Is.False, "the written key lost its job when the file moved it");

        box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.F5, Source = box });
        Dispatcher.UIThread.RunJobs();
        Assert.That(window.KeyboardOpen, Is.True, "the viewer's key answers from the file");

        window.Close();
      }
      finally
      {
        KeyBindings.Override(null);
      }
    }
  }
}
