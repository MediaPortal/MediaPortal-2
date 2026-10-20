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
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// The home menu's pictures, checked against the two things that say what they are: the workflow files, whose
  /// action Ids are the names the reference's skins filed the tile pictures under, and the shipped skins, whose
  /// <c>images/HomeMenu</c> directories have to still carry a picture of every one of those names.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <see cref="MainMenuEntry.HomeImageId"/> claims that a menu entry's tile picture is the file named by the
  /// workflow action's own Id - which is exactly how the reference's Nereus <c>HomeMenuModel</c> keyed its home
  /// content, and how both rescued skins name their tiles: <c>images/HomeMenu/&lt;id&gt;.png</c> in Nereus,
  /// <c>images/HomeMenu/&lt;id&gt;Focus.png</c> and <c>NoFocus</c> in WMC. A claim typed by hand goes stale the
  /// moment somebody rewrites a workflow file or reshuffles a skin folder, so it is re-read here rather than
  /// remembered: the Ids from the plugins' own XML (not the skins' - the same rule
  /// <c>TestHomeMenuContributions</c> set for the names), and the files from the skins that ride with the
  /// front end.
  /// </para>
  /// <para>
  /// The last test is the whole road in one walk: the shipped skin goes on, the skin's own
  /// <c>HomeMenuTileTemplate</c> is asked for a tile, and the tile that comes back has to be holding the
  /// skin's picture - the converter, the format the skin spells the name in, and the catalog are all in that
  /// one assertion, with no C# of the test in between beyond putting the control on a root.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestHomeMenuImages
  {
    /// <summary>The state the old client's home screen was; the same constant the contributions' test keys on.</summary>
    private const string HOME_STATE = "7F702D9C-F2DD-42da-9ED8-0BA92F07787F";

    /// <summary>A workflow element that carries a <c>Home-&gt;</c> name - and its Id, which is what this file is about.</summary>
    private static readonly Regex CONTRIBUTION =
        new Regex(@"<(?<element>\w+)[^>]*Name=""(?<name>Home->[^""]*)""[^>]*/?>",
            RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex ACTION_ID = new Regex(@"Id=""(?<v>\{?[0-9a-fA-F-]+\}?)""", RegexOptions.Compiled);
    private static readonly Regex SOURCE_STATES = new Regex(@"SourceStates=""(?<v>[^""]*)""", RegexOptions.Compiled);

    /// <summary>
    /// Every entry's <see cref="MainMenuEntry.HomeImageId"/> is the Id of the workflow element that contributes
    /// it, written the way the skins write it - lower-case, no braces.
    /// </summary>
    [Test]
    public void EveryEntriesPictureIsNamedByTheWorkflowsOwnActionId()
    {
      Dictionary<string, string> fromTheTree = HomeActionIds();
      List<MainMenuEntry> menu = MainMenu.Build(Array.Empty<AspectViewModel>(), false);

      Assert.That(menu, Is.Not.Empty, "a menu with no entries could not be checked and should not read as checked");

      foreach (MainMenuEntry entry in menu)
      {
        Assert.That(entry.Mp2Action, Is.Not.Null,
            entry.Label + " has no workflow action to name its picture, and this menu claims none is invented");
        Assert.That(entry.HomeImageId, Is.Not.Null.And.Not.Empty,
            entry.Mp2Action + " carries no home picture name, so its tile could not ask the skin for one");

        string? xmlId = null;
        bool named = fromTheTree.TryGetValue(entry.Mp2Action!, out xmlId);
        Assert.That(named, Is.True,
            entry.Mp2Action + " is on the menu but no plugin's workflow XML contributes it any more - the same "
            + "drift the contributions' test reads, now from the picture's side");

        string expected = Normalize(xmlId!);
        Assert.That(Normalize(entry.HomeImageId!), Is.EqualTo(expected),
            entry.Mp2Action + "'s tile asks the skin for '" + entry.HomeImageId + "', but the workflow file of "
            + "that name says the action is '" + xmlId + "' - and the skins file their pictures under the "
            + "action's Id, so the wrong Id is no picture at all");
      }

      Assert.That(menu.Select(entry => Normalize(entry.HomeImageId!)).Distinct().Count(), Is.EqualTo(menu.Count),
          "no two entries name the same picture: the skins file one tile per action Id, and so does this menu");
    }

    /// <summary>
    /// Both shipped skins still carry the tile picture of every entry: Nereus under the bare Id, WMC under the
    /// Id's <c>Focus</c> and <c>NoFocus</c> spellings - except local media, which the reference WMC home screen
    /// never had a tile for. That one exception is asserted too: a WMC that grew the picture would fail here
    /// and make its documentation tell the truth again.
    /// </summary>
    [AvaloniaTest]
    public void TheShippedSkinsCarryEveryHomePicture()
    {
      Application app = Application.Current!;
      string previous = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      try
      {
        List<MainMenuEntry> menu = MainMenu.Build(Array.Empty<AspectViewModel>(), false);

        Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");
        SkinImages nereus = SkinEngine.Worn(app)!.Images;
        foreach (MainMenuEntry entry in menu)
          Assert.That(nereus.Has("HomeMenu/" + entry.HomeImageId), Is.True,
              "Nereus carries no tile picture named '" + entry.HomeImageId + "' (the " + entry.Mp2Action
              + " entry), so its tile would fall back to a letter where the reference drew its own art");
        SkinEngine.Clear(app);

        Assert.That(SkinEngine.Apply(app, "wmc", out problem), Is.True, problem ?? "");
        SkinImages wmc = SkinEngine.Worn(app)!.Images;
        foreach (MainMenuEntry entry in menu)
        {
          bool localMedia = entry.Mp2Action == "Home->BrowseLocalMedia";
          Assert.That(wmc.Has("HomeMenu/" + entry.HomeImageId + "Focus"), Is.Not.EqualTo(localMedia),
              (localMedia
                  ? "WMC grew a tile picture for local media, which the reference home screen never had - "
                  : "WMC lost the tile picture '") + entry.HomeImageId + (localMedia
                      ? "' - the exception is documented in the skin and in MainMenu, so update both"
                      : "Focus' that the reference spelled out of the same action Id"));
          if (!localMedia)
            Assert.That(wmc.Has("HomeMenu/" + entry.HomeImageId + "NoFocus"), Is.True,
                "WMC's unfocused spelling of " + entry.HomeImageId + " is gone with the focused one");
        }
        SkinEngine.Clear(app);
      }
      finally
      {
        SkinEngine.SkinsFolder = previous;
      }
    }

    /// <summary>
    /// The shortcut rail's pictures, asked for the way the model spells them - with the extension, as they
    /// stand on disk. The catalog keeps names without it, so a lookup that demanded the spelling to match
    /// byte for byte answered every shortcut with no picture at all: nine tiles of nothing but letters,
    /// where the reference drew its MediaMenu art.
    /// </summary>
    [AvaloniaTest]
    public void TheShortcutPicturesAnswerToTheirSpellingOnDisk()
    {
      Application app = Application.Current!;
      string previous = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      try
      {
        Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");
        try
        {
          SkinImages nereus = SkinEngine.Worn(app)!.Images;
          foreach (string spelled in new[]
          {
              "MediaMenu/genre.png", "MediaMenu/year.png", "MediaMenu/actor.png", "MediaMenu/album.png"
          })
          {
            Assert.That(nereus.Has(spelled), Is.True,
                "the shortcut rail asks the worn skin for '" + spelled + "' and the catalog turned it down");
            Assert.That(nereus.Has(spelled.Substring(0, spelled.LastIndexOf('.'))), Is.True,
                "and the extension-free spelling the catalog writes its keys in still answers too");
            Assert.That(nereus.Load(spelled), Is.InstanceOf<Bitmap>(),
                "'" + spelled + "' is on disk and has to read as a picture, not null");
          }

          CultureInfo culture = CultureInfo.InvariantCulture;
          Assert.That(SkinImageConverter.Instance.Convert("MediaMenu/genre.png", typeof(object), "{0}", culture),
              Is.InstanceOf<Bitmap>(),
              "the shortcut template binds the disk spelling through the converter - the rail's icon, end to end");
          Assert.That(SkinImageMissingConverter.Instance.Convert("MediaMenu/genre.png", typeof(bool), "{0}", culture),
              Is.False,
              "and with the icon answered, the tile keeps its letter badge away");
        }
        finally
        {
          SkinEngine.Clear(app);
        }
      }
      finally
      {
        SkinEngine.SkinsFolder = previous;
      }
    }

    /// <summary>
    /// The whole road at once: with the shipped Nereus worn, the home screen's realized tile for a real entry
    /// holds that entry's own picture from the skin - and with the shipped WMC worn, the one entry WMC has no
    /// picture for holds no picture and says so with its badge. A template whose converter, name format, or
    /// catalog lookup breaks answers this test without a bitmap, which is the failure no other test here can
    /// see. The tiles are taken from a shown window rather than built by hand, because the reference's bargain
    /// is that the <i>screen</i> wears the skin - the list, the container, and the swap between both.
    /// </summary>
    [AvaloniaTest]
    public void TheSkinsTileHandsTheEntryTheSkinsOwnPicture()
    {
      Application app = Application.Current!;
      string previous = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      HomeViewModel home = new HomeViewModel();
      ShellWindow? window = null;
      try
      {
        MainMenuEntry local = home.Menu.Single(entry => entry.Mp2Action == "Home->BrowseLocalMedia");

        Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");
        window = new ShellWindow(new HomeView(home, new WorkflowManager()));
        window.Show();
        Pump();

        Control tile = TileOf(window, local);
        // The tile carries two pictures: its own home menu image and the small 'this shelf is playing' mark that
        // sits over it (invisible until something plays). The home menu image is the large one.
        Image picture = LargestPicture(tile);
        Assert.That(picture.Source, Is.InstanceOf<Bitmap>(),
            "Nereus' home menu asked the skin for the local-media entry's picture and came back without one - the "
            + "converter, the format the skin spells the name in, or the file behind it");
        Assert.That(BadgeOf(tile, local).IsVisible, Is.False,
            "a tile holding its picture does not also shout its first letter next to it");

        window.Close();
        SkinEngine.Clear(app);
        Pump();

        Assert.That(SkinEngine.Apply(app, "wmc", out problem), Is.True, problem ?? "");
        window = new ShellWindow(new HomeView(home, new WorkflowManager()));
        window.Show();
        Pump();

        Control bare = TileOf(window, local);
        // WMC has no local-media tile picture - the reference screen never had one - so the tile's own
        // picture slot has to come back empty rather than half a file. The tile wears other pictures too
        // (the reference's Focus.frame behind the stood-on tile, the "is playing" mark on its corner), so
        // the claim is about the slot, not about the biggest picture on the tile.
        Assert.That(bare.GetVisualDescendants().OfType<Image>().Any(image => image.Source == null), Is.True,
            "a tile for an entry no skin drew has an empty picture slot, not a hole where a file should be");
        // Where the old WMC tile filled the hole with a lone letter, the reference's home menu answered a
        // missing picture with the entry's whole name - MyMainMenuItemDataTemplate is huge text, white when
        // the walker stands on it - and so does this skin's tile: the framed place holds the word.
        Assert.That(bare.GetVisualDescendants().OfType<TextBlock>()
            .Any(t => t.Text == local.Label && t.IsVisible), Is.True,
            "and the word is what fills the hole, so the tile never stands there with neither art nor letter");
        window.Close();
        SkinEngine.Clear(app);
      }
      finally
      {
        if (window != null)
        {
          try { window.Close(); } catch (ObjectDisposedException) { }
        }
        SkinEngine.SkinsFolder = previous;
      }
    }

    [AvaloniaTest]
    public void RightFromTheNereusMenuFocusesItsFirstShortcut()
    {
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        Assert.That(FrontendTheme.Apply(app, "nereus", out string? themeProblem), Is.EqualTo("nereus"),
            themeProblem ?? "");
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        HomeViewModel home = new HomeViewModel();
        Assert.That(home.IsNereusHome, Is.True, "the view model must read the applied Nereus skin layout");
        MainMenuEntry movies = home.DisplayMenu.Single(entry =>
            entry.HomeImageId == "80d2e2cc-baaa-4750-807b-f37714153751");
        home.SelectedEntry = movies;
        home.ShowContentAsync(movies).GetAwaiter().GetResult();

        window = new ShellWindow(new HomeView(home, new WorkflowManager(),
            sectionScreen: delegate (HomeSection section) { return null!; }));
        window.Show();
        Pump();

        ScrollViewer content = window.GetVisualDescendants().OfType<ScrollViewer>()
            .Single(viewer => viewer.Name == "nereusContentPane");
        Button firstShortcut = content.GetVisualDescendants().OfType<Button>()
            .First(button => button.Focusable);
        Assert.That(firstShortcut.IsVisible && firstShortcut.IsEnabled, Is.True,
            "the first shortcut must be an available target for remote focus");
        HomeContentTile shortcutTile = (HomeContentTile)firstShortcut.Tag!;
        Assert.That(shortcutTile.Shortcut?.Label, Is.EqualTo("Genre"),
            "the first group is the Nereus shortcut rail, in the reference's order");

        ListBox menu = window.GetVisualDescendants().OfType<ListBox>()
            .Single(list => list.Name == "menuList");
        Assert.That(menu.Focus(), Is.True, "the menu must accept focus before its outgoing navigation is tested");
        Pump();
        Assert.That(menu.IsKeyboardFocusWithin, Is.True, "the keyboard focus must actually be on the menu");
        menu.RaiseEvent(new KeyEventArgs
        {
          RoutedEvent = InputElement.KeyDownEvent,
          Key = Key.Right,
          Source = menu
        });
        Pump();

        Visual? focused = window.FocusManager.GetFocusedElement() as Visual;
        Assert.That(firstShortcut.IsKeyboardFocusWithin, Is.True,
            "Right leaves the vertical menu and reaches the first focusable home-content tile; focus is "
            + (focused == null ? "nowhere" : focused.GetType().Name));
      }
      finally
      {
        if (window != null)
          window.Close();
        SkinEngine.Clear(app);
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out _);
      }
    }

    /// <summary>
    /// The road on through the home content: Right walks out of the menu onto the shortcut column, and Right
    /// again has to walk off that column into the titles beside it. A viewer who can reach the shortcuts but
    /// not the media behind them is stuck in a rail of nine buttons, which is no home screen at all.
    /// </summary>
    [AvaloniaTest]
    public void RightFromAShortcutTileReachesTheTitlesBesideIt()
    {
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        Assert.That(FrontendTheme.Apply(app, "nereus", out string? themeProblem), Is.EqualTo("nereus"),
            themeProblem ?? "");
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        HomeViewModel home = new HomeViewModel();
        MainMenuEntry movies = home.DisplayMenu.Single(entry =>
            entry.HomeImageId == "80d2e2cc-baaa-4750-807b-f37714153751");
        home.SelectedEntry = movies;
        home.ShowContentAsync(movies).GetAwaiter().GetResult();

        // Without a server to ask, no list brought any titles and the pane would hold nothing but the
        // shortcut rail - so the wall gets the titles the test walks onto, the same tiles the real screen
        // builds when a shelf answers.
        HomeContentGroup titles = new HomeContentGroup("Latest added", "poster1", 1500, 2);
        titles.AddTitle(new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Alien", null, 1979, null, null, false)));
        titles.AddTitle(new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Blade Runner", null, 1982, null, null, false)));
        home.ContentGroups.Add(titles);

        window = new ShellWindow(new HomeView(home, new WorkflowManager(),
            sectionScreen: delegate (HomeSection section) { return null!; }));
        window.Show();
        Pump();

        ListBox menu = window.GetVisualDescendants().OfType<ListBox>().Single(list => list.Name == "menuList");
        Assert.That(menu.Focus(), Is.True);
        Pump();
        menu.RaiseEvent(new KeyEventArgs
        {
          RoutedEvent = InputElement.KeyDownEvent,
          Key = Key.Right,
          Source = menu
        });
        Pump();

        ScrollViewer content = window.GetVisualDescendants().OfType<ScrollViewer>()
            .Single(viewer => viewer.Name == "nereusContentPane");
        Button? onShortcut = content.GetVisualDescendants().OfType<Button>()
            .FirstOrDefault(button => button.IsKeyboardFocusWithin);
        Assert.That(onShortcut, Is.Not.Null, "Right from the menu stands on a tile of the pane");
        Assert.That(((HomeContentTile) onShortcut!.Tag!).IsShortcut, Is.True,
            "and the tile it stands on is a shortcut, the column the menu opens onto");

        onShortcut.RaiseEvent(new KeyEventArgs
        {
          RoutedEvent = InputElement.KeyDownEvent,
          Key = Key.Right,
          Source = onShortcut
        });
        Pump();

        Visual? walked = window.FocusManager.GetFocusedElement() as Visual;
        Button? arrived = walked as Button;
        HomeContentTile? stoodOn = arrived?.Tag as HomeContentTile;
        Assert.That(stoodOn != null && !stoodOn.IsShortcut, Is.True,
            "Right walks off the shortcut column into the titles beside it; the walk ended on "
            + (stoodOn == null
                ? (walked == null ? "nothing" : walked.GetType().Name)
                : "the shortcut '" + stoodOn.Label + "'"));
      }
      finally
      {
        if (window != null)
          window.Close();
        SkinEngine.Clear(app);
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out _);
      }
    }

    /// <summary>
    /// The converters with nothing to answer: no name bound, no skin worn, no application running, a name the
    /// skin never heard, and a format that is no format all give the same quiet answer - no picture, badge
    /// visible - rather than an exception on the way to a television.
    /// </summary>
    [Test]
    public void TheConvertersAnswerWithoutAPictureRatherThanWithAnException()
    {
      CultureInfo culture = CultureInfo.InvariantCulture;

      Assert.That(SkinImageConverter.Instance.Convert("80d2e2cc-baaa-4750-807b-f37714153751", typeof(object),
          "HomeMenu/{0}", culture), Is.Null,
          "no application is running here, so no skin is worn and no picture can be answered");
      Assert.That(SkinImageMissingConverter.Instance.Convert("80d2e2cc-baaa-4750-807b-f37714153751", typeof(bool),
          "HomeMenu/{0}", culture), Is.True,
          "and the badge takes the tile over exactly then");

      Assert.That(SkinImageConverter.Instance.Convert(null, typeof(object), "HomeMenu/{0}", culture), Is.Null,
          "an entry this front end added has no Id to ask with, and says so by holding nothing");
      Assert.That(SkinImageMissingConverter.Instance.Convert(null, typeof(bool), "HomeMenu/{0}", culture), Is.True);
      Assert.That(SkinImageConverter.Instance.Convert("", typeof(object), "HomeMenu/{0}", culture), Is.Null);

      Assert.That(SkinImageConverter.Instance.Convert("no-such-picture", typeof(object), "HomeMenu/{0}", culture),
          Is.Null, "a name the worn skin carries no file for is no picture, not a crash");
      Assert.That(SkinImageConverter.Instance.Convert("x", typeof(object), "HomeMenu/{0", culture), Is.Null,
          "a template with a typo in its format says no sentence a file can answer, and the tile draws bare");
      Assert.That(SkinImageConverter.Instance.Convert("x", typeof(object), "HomeMenu/{1}", culture), Is.Null,
          "a format asking for an argument the binding does not hand over is the same typo in another spelling");
    }

    /// <summary>
    /// The realized tile a shown home screen builds for one menu entry: the list the screen names, the item
    /// container whose data is that entry. The screen realizes its first dozen rows on show, so the entries
    /// these tests pick are always among them - and if one ever is not, the assertion says which.
    /// </summary>
    private static Control TileOf(ShellWindow window, MainMenuEntry entry)
    {
      ListBox menu = window.GetVisualDescendants().OfType<ListBox>()
          .First(list => list.Name == "menuList");
      menu.UpdateLayout();
      Control? container = menu.GetVisualDescendants().OfType<Control>()
          .FirstOrDefault(c => ReferenceEquals(c.DataContext, entry) && c is ListBoxItem);
      Assert.That(container, Is.Not.Null, "the home screen realized no tile for " + entry.Mp2Action
          + " - realised containers: " + string.Join(", ", menu.GetVisualDescendants().OfType<ListBoxItem>()
              .Select(i => i.DataContext?.GetType().Name ?? "null"))
          + "; items source count: " + (menu.Items?.Count ?? -1)
          + "; item template: " + (menu.ItemTemplate?.GetType().Name ?? "null"));
      return container!;
    }

    /// <summary>
    /// The tile's own picture: a tile can carry a second small picture over it (the 'is playing' mark), so the
    /// picture a tile was built around is picked by size rather than by counting.
    /// </summary>
    private static Image LargestPicture(Control tile)
    {
      IEnumerable<Image> pictures = tile.GetVisualDescendants().OfType<Image>().ToList();
      Assert.That(pictures.Any(), Is.True, "the tile was written with a picture slot at all");
      return pictures.OrderBy(image => image.Height is double h ? -h : 0).First();
    }

    /// <summary>The tile's letter badge: the text block wearing the entry's own first letter as its text.</summary>
    private static TextBlock BadgeOf(Control tile, MainMenuEntry entry)    {
      TextBlock badge = tile.GetVisualDescendants().OfType<TextBlock>()
          .FirstOrDefault(t => t.Text == entry.Badge);
      Assert.That(badge, Is.Not.Null, "the tile was written with a badge for the no-picture case");
      return badge!;
    }

    /// <summary>Let the dispatcher build what showing queued - the same pump the window tests run on.</summary>
    private static void Pump()
    {
      for (int round = 0; round < 10; round++)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(10);
      }
      Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// Every home contribution the plugin workflow files make, mapped to the action's own Id, keyed by the
    /// <c>Home-&gt;</c> name. Read the way <c>TestHomeMenuContributions</c> reads the same files - as one element
    /// per contribution, name and Id, and nothing else worth resolving.
    /// </summary>
    private static Dictionary<string, string> HomeActionIds()
    {
      string root = MetadataPath("UiSourceRoot");
      Dictionary<string, string> ids = new Dictionary<string, string>(StringComparer.Ordinal);

      foreach (string file in Directory.EnumerateFiles(root, "*.xml", SearchOption.AllDirectories)
          .Where(f => f.Contains("\\workflow\\") || f.Contains("/workflow/"))
          .Where(f => !f.Contains("\\Skins\\") && !f.Contains("/Skins/"))
          .OrderBy(f => f, StringComparer.Ordinal))
      {
        foreach (Match element in CONTRIBUTION.Matches(File.ReadAllText(file)))
        {
          Match states = SOURCE_STATES.Match(element.Value);
          if (!states.Success || !states.Groups["v"].Value
                  .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                  .Any(state => string.Equals(state.Trim(), HOME_STATE, StringComparison.OrdinalIgnoreCase)))
            continue;

          Match id = ACTION_ID.Match(element.Value);
          Assert.That(id.Success, Is.True,
              element.Groups["name"].Value + " in " + Path.GetFileName(file) + " contributes a home entry with "
              + "no Id, which leaves its picture name with no answer in this file");

          string name = element.Groups["name"].Value;
          Assert.That(ids.ContainsKey(name), Is.False,
              name + " is contributed by more than one workflow file, which leaves 'the' action Id without an "
              + "answer - the contributions' test walks these same elements and assumes one line each");
          ids[name] = id.Groups["v"].Value;
        }
      }

      Assert.That(ids, Is.Not.Empty,
          "not one plugin names a home action any more, which cannot be true of a tree that still has the "
          + "media plugin in it - the test is looking in the wrong place");
      return ids;
    }

    /// <summary>
    /// An action Id the way the skins write it in their file names: braces off, letters lower. The configuration
    /// plugin writes its Id in braces; both skins' tile files do not, and neither spelling is asked of anybody.
    /// </summary>
    private static string Normalize(string id)
    {
      return id.Trim('{', '}').ToLowerInvariant();
    }

    /// <summary>
    /// A directory the test project was told about at build time - see <c>TestSkinFromDisk</c> for why a test
    /// cannot derive these from the working directory.
    /// </summary>
    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestHomeMenuImages).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      string path = Path.GetFullPath(value!.Value!);
      Assert.That(Directory.Exists(path), Is.True, name + " points at '" + path + "', which is not a directory");
      return path;
    }
  }
}
