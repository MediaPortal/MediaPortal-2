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
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using NUnit.Framework;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// Skins read off disk: a machine that names a skins folder in <c>MP2_SKINS_DIR</c> wears the folders under it,
  /// written and edited as plain files - the reference's bargain that a skin is installed by its folder existing
  /// and changed by opening one of its files. These tests build a throwaway skin folder and say out loud that
  /// the engine really reads it, and hold the rule that such a folder has to be <em>asked for</em>: with nothing
  /// named, the skins built into this front end are what it wears.
  /// </summary>
  [TestFixture]
  public class TestSkinFromDisk
  {
    private string _root = "";
    private string? _previousFolder;

    [SetUp]
    public void BuildAThrowawaySkinFolder()
    {
      _previousFolder = SkinEngine.SkinsFolder;

      _root = Path.Combine(Path.GetTempPath(), "mp2-disk-skin-" + Guid.NewGuid().ToString("N"));
      string skin = Path.Combine(_root, "disktest");
      Directory.CreateDirectory(skin);

      File.WriteAllText(Path.Combine(skin, "skin.xml"),
          "<Skin Name=\"disktest\" Title=\"Disk Test\" Author=\"tests\" Version=\"1\">"
          + "<Description>A skin written by a test, read off disk.</Description>"
          + "<Theme Name=\"default\" Directory=\".\">"
          + "<File Role=\"colors\" Name=\"colors.xaml\"/>"
          + "<File Role=\"styles\" Name=\"styles.xaml\"/>"
          + "</Theme></Skin>");

      File.WriteAllText(Path.Combine(skin, "colors.xaml"),
          "<ResourceDictionary xmlns='https://github.com/avaloniaui'"
          + " xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>"
          + "<Color x:Key='DiskColor'>#FF123456</Color>"
          + "<SolidColorBrush x:Key='DiskBrush' Color='{StaticResource DiskColor}'/>"
          + "</ResourceDictionary>");

      File.WriteAllText(Path.Combine(skin, "styles.xaml"),
          "<Styles xmlns='https://github.com/avaloniaui'"
          + " xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>"
          + "<Style Selector='Button'>"
          + "<Setter Property='Background' Value='{DynamicResource DiskBrush}'/>"
          + "</Style></Styles>");
    }

    [TearDown]
    public void TakeTheThrowawayAway()
    {
      Application? app = Application.Current;
      if (app != null)
        SkinEngine.Clear(app);

      SkinEngine.SkinsFolder = _previousFolder;
      try
      {
        Directory.Delete(_root, true);
      }
      catch (IOException)
      {
        // A test folder that will not go away is a leftover, not a failure; temp cleans up.
      }
    }

    [AvaloniaTest]
    public void AFolderOnDiskIsInstalledAndWorn()
    {
      Application app = Application.Current!;
      SkinEngine.SkinsFolder = _root;

      SkinManifest? manifest = SkinEngine.Find("disktest");
      Assert.That(manifest, Is.Not.Null, "a folder with a skin.xml on disk is an installed skin");
      Assert.That(manifest!.Title, Is.EqualTo("Disk Test"));

      Assert.That(SkinEngine.Apply(app, "disktest", out string? problem), Is.True, problem ?? "");
      Assert.That(app.Resources.TryGetResource("DiskBrush", null, out object? brush), Is.True,
          "the colours of a disk skin answer as soon as it is on");
      Assert.That(((SolidColorBrush)brush!).Color, Is.EqualTo(Color.Parse("#FF123456")));

      WornSkin? worn = SkinEngine.Worn(app);
      Assert.That(worn, Is.Not.Null);
      Assert.That(worn!.Styles.Count, Is.EqualTo(1), "the styles file of the folder came with it");
    }

    [AvaloniaTest]
    public void EditingTheFileOnDiskChangesTheSkinWornNextTime()
    {
      Application app = Application.Current!;
      SkinEngine.SkinsFolder = _root;
      Assert.That(SkinEngine.Apply(app, "disktest", out _), Is.True);

      // The whole point of reading off disk: the file is the skin, so a person editing it changes the next
      // wearing without building anything.
      File.WriteAllText(Path.Combine(_root, "disktest", "colors.xaml"),
          "<ResourceDictionary xmlns='https://github.com/avaloniaui'"
          + " xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>"
          + "<SolidColorBrush x:Key='DiskBrush' Color='#FF654321'/>"
          + "</ResourceDictionary>");

      Assert.That(SkinEngine.Apply(app, "disktest", out string? problem), Is.True, problem ?? "");
      Assert.That(app.Resources.TryGetResource("DiskBrush", null, out object? brush), Is.True);
      Assert.That(((SolidColorBrush)brush!).Color, Is.EqualTo(Color.Parse("#FF654321")),
          "the edited file, not the old one, is what the skin wears now");
    }

    /// <summary>
    /// The picture a theme stands on. The reference skins carried their own backgrounds as files in the skin
    /// folder, and a skin whose "background" is only ever a colour entry is a colour chart, not a skin - so a
    /// manifest may name one, the engine reads it like every other skin file, and it comes off again together
    /// with the skin.
    /// </summary>
    [AvaloniaTest]
    public void AThemeThatNamesAPictureStandsOnIt()
    {
      Application app = Application.Current!;
      SkinEngine.SkinsFolder = _root;
      string skinFolder = Path.Combine(_root, "disktest");

      // A three-pixel picture written by the test itself - the same kind of file a viewer drops into a skin
      // folder, just a small one.
      string picture = Path.Combine(skinFolder, "backdrop.png");
      using (WriteableBitmap bitmap = new WriteableBitmap(new PixelSize(3, 3), new Vector(96, 96)))
        bitmap.Save(picture);

      File.WriteAllText(Path.Combine(skinFolder, "skin.xml"),
          "<Skin Name=\"disktest\" Title=\"Disk Test\" Author=\"tests\" Version=\"1\">"
          + "<Description>A skin with a picture behind it.</Description>"
          + "<Theme Name=\"default\" Directory=\".\" Background=\"backdrop.png\">"
          + "<File Role=\"colors\" Name=\"colors.xaml\"/>"
          + "</Theme></Skin>");

      Assert.That(SkinEngine.Apply(app, "disktest", out string? problem), Is.True, problem ?? "");
      Assert.That(app.Resources.TryGetResource("SkinBackgroundImage", null, out object? image), Is.True,
          "the named picture is answerable as soon as the skin is on");
      Assert.That(image, Is.InstanceOf<IImage>());

      SkinEngine.Clear(app);
      Assert.That(app.Resources.TryGetResource("SkinBackgroundImage", null, out _), Is.False,
          "and it leaves with the skin, not behind it");

      // A name that points at no picture is said, and the skin stays off - a half-worn skin with a missing
      // background would show a colour where somebody meant a photograph.
      File.WriteAllText(Path.Combine(skinFolder, "skin.xml"),
          "<Skin Name=\"disktest\" Title=\"Disk Test\" Author=\"tests\" Version=\"1\">"
          + "<Description>The picture is gone.</Description>"
          + "<Theme Name=\"default\" Directory=\".\" Background=\"gone.png\">"
          + "<File Role=\"colors\" Name=\"colors.xaml\"/>"
          + "</Theme></Skin>");

      Assert.That(SkinEngine.Apply(app, "disktest", out problem), Is.False);
      Assert.That(problem, Does.Contain("gone.png"));
    }

    [AvaloniaTest]
    public void ABrokenSkinFileIsSaidAndNothingHalfDresses()
    {
      Application app = Application.Current!;
      SkinEngine.SkinsFolder = _root;

      File.WriteAllText(Path.Combine(_root, "disktest", "colors.xaml"),
          "<ResourceDictionary xmlns='https://github.com/avaloniaui'><NotAKey");

      Assert.That(SkinEngine.Apply(app, "disktest", out string? problem), Is.False,
          "a file that does not parse is not worn");
      Assert.That(problem, Does.Contain("disktest"));
      Assert.That(SkinEngine.Worn(app), Is.Null, "and the application wears no half of it");
    }

    /// <summary>
    /// The part of the skin that is not a colour: a list that names a template key draws its items from the
    /// skin's file while a skin that answers is on, and from its own tree the moment none is. This is the
    /// reference's Contents_Template bargain, and the reason a skin can change a screen's shape without a
    /// line of C# changing anywhere.
    /// </summary>
    [AvaloniaTest]
    public void AListThatNamesAKeyDrawsItsItemsFromTheSkin()
    {
      // A second throwaway skin, this one speaking only the template key.
      string root = Path.Combine(Path.GetTempPath(), "mp2-tile-skin-" + Guid.NewGuid().ToString("N"));
      string skin = Path.Combine(root, "tiletalk");
      Directory.CreateDirectory(skin);
      File.WriteAllText(Path.Combine(skin, "skin.xml"),
          "<Skin Name=\"tiletalk\" Title=\"Tile Talk\" Author=\"tests\" Version=\"1\">"
          + "<Description>A skin that only redraws a tile.</Description>"
          + "<Theme Name=\"default\" Directory=\".\">"
          + "<File Role=\"templates\" Name=\"templates.xaml\"/>"
          + "</Theme></Skin>");
      File.WriteAllText(Path.Combine(skin, "templates.xaml"),
          "<ResourceDictionary xmlns='https://github.com/avaloniaui'"
          + " xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>"
          + "<DataTemplate x:Key='HomeMenuTileTemplate'>"
          + "<Border Height='77'><TextBlock Text='from the skin'/></Border>"
          + "</DataTemplate></ResourceDictionary>");

      try
      {
        Application app = Application.Current!;
        SkinEngine.SkinsFolder = root;

        ListBox list = new ListBox();
        IDataTemplate? viewTemplate = list.ItemTemplate;

        SkinTemplates.SetTileTemplate(list, "HomeMenuTileTemplate");
        Assert.That(list.ItemTemplate, Is.SameAs(viewTemplate),
            "while no skin answers the key, the view's own tree is what it draws");

        Assert.That(SkinEngine.Apply(app, "tiletalk", out string? problem), Is.True, "apply said: " + (problem ?? "nothing"));
        Assert.That(list.ItemTemplate, Is.Not.SameAs(viewTemplate),
                "putting the skin on swaps the tree the list builds its items from, while it stands");
        Assert.That(SkinEngine.TryTileTemplate("HomeMenuTileTemplate"), Is.SameAs(list.ItemTemplate),
            "and what it swapped in is the skin's own template, key for key");

        SkinEngine.Clear(app);
        Assert.That(list.ItemTemplate, Is.SameAs(viewTemplate),
            "taking the skin off hands the view back the tree it was written with");
      }
      finally
      {
        SkinEngine.SkinsFolder = _previousFolder;
        try { Directory.Delete(root, true); } catch (IOException) { }
      }
    }

    /// <summary>
    /// The same bargain against the real home screen and the real Nereus on disk: the menu there names its
    /// key, so when the shipped skin is worn the menu's tiles come from the skin's file, and the selection
    /// and the focus are untouched by the swap - only the trees change underneath them.
    /// </summary>
    [AvaloniaTest]
    public void TheHomeMenuWearsTheShippedSkinsTile()
    {
      Application app = Application.Current!;

      // The shipped copy, not a viewer's folder: this test is about the files that ride with the assembly.
      SkinEngine.SkinsFolder = null;

      HomeView home = new HomeView(new HomeViewModel(), new WorkflowManager());
      home.ApplyTemplate();

      Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");
      Assert.That(SkinEngine.TryTileTemplate("HomeMenuTileTemplate"), Is.Not.Null,
          "the shipped Nereus speaks the home menu's key");

      SkinEngine.Clear(app);
    }

    /// <summary>
    /// WMC answers the shelf's four tile keys too: the grid drew its pictures rounded on a gradient ground in
    /// its own file (GridViewMovieItemTemplate and siblings), and a shelf where WMC stays silent would be the
    /// front end's own face, which is exactly what wearing a skin promises against. Nereus is checked beside it
    /// - both skins now speak every key the shelf asks, each one with its own sizes.
    /// </summary>
    [AvaloniaTest]
    public void BothShippedSkinsSpeakTheShelfsTiles()
    {
      Application app = Application.Current!;
      SkinEngine.SkinsFolder = null;

      string[] keys = new string[]
      {
          "MediaTilePosterTemplate", "MediaTileBannerTemplate", "MediaTileThumbnailTemplate",
          "MediaTileCoverTemplate",
      };

      Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");
      foreach (string key in keys)
        Assert.That(SkinEngine.TryTileTemplate(key), Is.Not.Null,
            "the shipped Nereus speaks " + key);
      SkinEngine.Clear(app);

      Assert.That(SkinEngine.Apply(app, "wmc", out problem), Is.True, problem ?? "");
      foreach (string key in keys)
        Assert.That(SkinEngine.TryTileTemplate(key), Is.Not.Null,
            "the shipped WMC speaks " + key + "; its grid wore these tiles and the shelf may not fall back"
            + " to the front end's own face under this skin");
      SkinEngine.Clear(app);
    }

   /// <summary>
   /// The shipped WMC stands on a picture of its own - the reference's blue home wash, taken from the old
   /// skin folder and carried with the assembly. A manifest that names a file the assembly does not carry
   /// would refuse the skin at wear time, so this is the test that the picture and the manifest agree.
   /// Nereus is checked too, from the other side: it names no picture and must not answer with one.
   /// </summary>
   [AvaloniaTest]
   public void TheShippedWmcStandsOnItsOwnPicture()
   {
     Application app = Application.Current!;
     SkinEngine.SkinsFolder = null;

     Assert.That(SkinEngine.Apply(app, "wmc", out string? problem), Is.True, problem ?? "");
     Assert.That(app.Resources.TryGetResource("SkinBackgroundImage", null, out object? image), Is.True,
         "the WMC that rides with the assembly names a background, and the file is there to read");
     Assert.That(image, Is.InstanceOf<IImage>());

     SkinEngine.Clear(app);
     Assert.That(app.Resources.TryGetResource("SkinBackgroundImage", null, out _), Is.False);

     Assert.That(SkinEngine.Apply(app, "nereus", out problem), Is.True, problem ?? "");
     Assert.That(app.Resources.TryGetResource("SkinBackgroundImage", null, out _), Is.False,
         "Nereus stands on black like it always did, and says so by naming no picture");

     SkinEngine.Clear(app);
   }

    /// <summary>
    /// A skin is hundreds of pictures before it is anything else, and the reference screens pulled them out of
    /// the worn skin by name - <c>ImageSource="Square_small.png"</c> answered from the folder, not from the
    /// assembly. The engine lists a skin's images directories when it puts the skin on and loads a picture the
    /// first time a screen asks for it, so wearing Nereus means having its four hundred and forty-five
    /// pictures to hand without carrying four hundred and forty-five bitmaps in memory.
    /// </summary>
    [AvaloniaTest]
    public void TheSkinCarriesItsPicturesByName()
    {
      Application app = Application.Current!;
      SkinEngine.SkinsFolder = _root;
      string skin = Path.Combine(_root, "disktest");

      Directory.CreateDirectory(Path.Combine(skin, "images", "HomeContentIcons"));
      using (WriteableBitmap square = new WriteableBitmap(new PixelSize(3, 3), new Vector(96, 96)))
        square.Save(Path.Combine(skin, "images", "Square_small.png"));
      using (WriteableBitmap album = new WriteableBitmap(new PixelSize(5, 5), new Vector(96, 96)))
        album.Save(Path.Combine(skin, "images", "HomeContentIcons", "album.png"));
      File.WriteAllText(Path.Combine(skin, "images", "notes.txt"), "not a picture, not catalogued");

      Assert.That(SkinEngine.Apply(app, "disktest", out string? problem), Is.True, problem ?? "");
      WornSkin? worn = SkinEngine.Worn(app);
      Assert.That(worn, Is.Not.Null);

      SkinImages images = worn!.Images;
      Assert.That(images.Has("Square_small"), Is.True, "a picture at the top of the images directory answers "
          + "by the bare name the reference screens spelled");
      Assert.That(images.Has("HomeContentIcons/album"), Is.True, "and one in a folder answers by its path");
      Assert.That(images.Has("album"), Is.True, "a name that is used once answers bare too");
      Assert.That(images.Has("notes"), Is.False, "only picture files are pictures");

      Bitmap? picture = images.Load("Square_small");
      Assert.That(picture, Is.Not.Null, "asking for the picture reads it");
      Assert.That(picture!.PixelSize.Width, Is.GreaterThan(0), "it is a picture with pixels in it");
      Assert.That(images.Load("Square_small"), Is.SameAs(picture), "and it is read once, not once per asking");
      Assert.That(images.Load("no.such.picture"), Is.Null, "a name the skin does not carry is answered with "
          + "nothing, not with a crash - a screen without its picture draws without it");

      SkinEngine.Clear(app);
    }

    /// <summary>
    /// The same promise against the real shipped folders: Nereus with its four hundred and forty-five pictures
    /// and WMC with its four hundred and fifty-three are part of this front end now, sitting in the skin folder
    /// as files - and the engine knows them by name the moment the skin goes on.
    /// </summary>
    [AvaloniaTest]
    public void TheShippedSkinsCarryTheirRealPictures()
    {
      Application app = Application.Current!;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");

      Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");
      SkinImages nereus = SkinEngine.Worn(app)!.Images;
      Assert.That(nereus.Count, Is.GreaterThan(400),
          "the Nereus pictures came along; a skin without its images directory is a colour chart again");
      Assert.That(nereus.Has("Square_small"), Is.True,
          "the tile overlay the reference home screen drew with is answerable by its reference name");
      Bitmap? square = nereus.Load("Square_small");
      Assert.That(square, Is.Not.Null, "and the file really reads as a picture");
      SkinEngine.Clear(app);

      Assert.That(SkinEngine.Apply(app, "wmc", out problem), Is.True, problem ?? "");
      SkinImages wmc = SkinEngine.Worn(app)!.Images;
      Assert.That(wmc.Count, Is.GreaterThan(400), "and the same for WMC");
      string onePicture = wmc.Names.First(n => n.Contains('/'));
      Assert.That(wmc.Load(onePicture), Is.Not.Null, onePicture + " does not read as a picture");
      SkinEngine.Clear(app);
    }

    // Where the skin a build wears comes from, locked down. The engine used to answer with one machine's skins
    // folder whenever nobody had named one, which left the front end standing in two skins at once: the compiled
    // markup and every test dressed in the repository's files while the screen wore that folder, so a style
    // change vanished on the television and stayed green in the tests - and nobody editing a style file had any
    // way of finding out they were editing a copy nothing reads. Reading a folder off disk is a thing a machine
    // asks for now, by naming it in MP2_SKINS_DIR.

    [Test]
    public void NobodyNamingAFolderWearsTheSkinFolderBesideThisBuild()
    {
      string beside = Path.Combine(Path.GetTempPath(), "mp2-skins-beside-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(beside);
      try
      {
        Assert.That(SkinEngine.ResolveSkinsFolder(null, beside), Is.EqualTo(beside),
            "with nothing said, the front end wears the skins its own build laid beside the executable - the "
            + "same files the screens were compiled from, so an edit cannot go missing");
        Assert.That(SkinEngine.SkinsFolderProblem, Is.Null,
            "and nothing went wrong, so there is nothing to complain about");
      }
      finally
      {
        Directory.Delete(beside, true);
      }
    }

    [Test]
    public void ANameSetToNothingWearsNoFolderOnDiskAtAll()
    {
      // The distinction the tests live by. MP2_SKINS_DIR set to an empty value is an order - wear the markup
      // compiled into the assembly, none off disk - and not "nobody said anything": a test binary carries a
      // copy of the shipped skins beside itself, and handing that to a test wearing skins of its own out of
      // its own resources would dress it in somebody else's clothes.
      string beside = Path.Combine(Path.GetTempPath(), "mp2-skins-beside-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(beside);
      try
      {
        Assert.That(SkinEngine.ResolveSkinsFolder("", beside), Is.Null,
            "an empty name means no skin folder, even with a folder sitting beside the build");
        Assert.That(SkinEngine.ResolveSkinsFolder("   ", beside), Is.Null,
            "spaces are the same order as nothing");
        Assert.That(SkinEngine.SkinsFolderProblem, Is.Null, "wearing no folder is not a problem to report");
      }
      finally
      {
        Directory.Delete(beside, true);
      }
    }

    [Test]
    public void ANamedFolderIsWornAheadOfTheOnesBesideTheBuild()
    {
      string beside = Path.Combine(Path.GetTempPath(), "mp2-skins-beside-" + Guid.NewGuid().ToString("N"));
      string named = Path.Combine(Path.GetTempPath(), "mp2-skins-named-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(beside);
      Directory.CreateDirectory(named);
      try
      {
        Assert.That(SkinEngine.ResolveSkinsFolder(named, beside), Is.EqualTo(named),
            "a machine that names a skins folder wears that folder - that is how an installed skin, or one "
            + "somebody is editing by hand, is put on");
        Assert.That(SkinEngine.SkinsFolderProblem, Is.Null, "a folder that is there is no problem");
      }
      finally
      {
        Directory.Delete(beside, true);
        Directory.Delete(named, true);
      }
    }

    [Test]
    public void AFolderThatIsNotThereIsSaidRatherThanSilentlyIgnored()
    {
      string beside = Path.Combine(Path.GetTempPath(), "mp2-skins-beside-" + Guid.NewGuid().ToString("N"));
      string missing = Path.Combine(Path.GetTempPath(), "mp2-skins-missing-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(beside);
      try
      {
        Assert.That(SkinEngine.ResolveSkinsFolder(missing, beside), Is.EqualTo(beside),
            "a name leading nowhere cannot be worn, but the front end still has to wear something");
        Assert.That(SkinEngine.SkinsFolderProblem, Is.Not.Null.And.Contains(missing),
            "and it says so, with the name in the sentence: 'the folder you named is missing' and 'nobody named "
            + "one' look identical on the screen and are different answers to whoever edited a style file");
      }
      finally
      {
        Directory.Delete(beside, true);
      }
    }

    [Test]
    public void TheSkinsBesideTheExecutableAreAWholeSkinAndNotOnlyItsPictures()
    {
      // The reason a machine's path was ever hard-coded: the build used to leave only the pictures outside the
      // assembly, so the folder beside the executable was half a skin and the whole one had to come from
      // somewhere else - which is how a build came to wear a folder nobody had asked it to wear.
      Assert.That(SkinEngine.BuiltInSkinsFolder, Is.EqualTo(Path.Combine(AppContext.BaseDirectory, "Skins")),
          "the built-in answer is beside the running executable, never a folder somebody remembered");
      Assert.That(Directory.Exists(SkinEngine.BuiltInSkinsFolder), Is.True,
          "and there really is one there, because the build lays it out");
      Assert.That(File.Exists(Path.Combine(SkinEngine.BuiltInSkinsFolder, "Nereus", "themes", "default",
          "styles.xaml")), Is.True,
          "the skin's markup rides beside the executable too - a folder with pictures but no styles is the half "
          + "skin that made a hard-coded path look necessary");
      Assert.That(Directory.EnumerateFiles(
              Path.Combine(SkinEngine.BuiltInSkinsFolder, "Nereus", "images"), "*.png").Any(), Is.True,
          "and so do its pictures, so wearing this folder is not a skin without any");
    }

    [Test]
    public void NoMachinesFolderPathIsWrittenIntoTheSkinEngine()
    {
      // The gate that keeps the whole business from coming back. A folder path written into the engine's own
      // source is a skin being chosen by the machine that compiled it, and it outranks the build silently.
      string source = Path.Combine(MetadataPath("FrontendSource"), "Theming", "SkinEngine.cs");
      Assert.That(File.Exists(source), Is.True,
          "the engine's own file has to be readable for this to be checkable");

      string[] offenders = File.ReadAllLines(source)
          .Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal))
          .Where(l => System.Text.RegularExpressions.Regex.IsMatch(l, "\"[A-Za-z]:[\\\\/]"))
          .ToArray();

      Assert.That(offenders, Is.Empty,
          "the skin folder comes from MP2_SKINS_DIR or from nothing at all; a path baked in here puts the "
          + "screen between this build's skin and somebody's folder ("
          + string.Join(" | ", offenders.Select(l => l.Trim())) + ")");
    }

    /// <summary>
    /// A directory the test project was told about at build time; see TestTheme's helper of the same name.
    /// </summary>
    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestSkinFromDisk).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));
      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");
      return Path.GetFullPath(value!.Value!);
    }
 }
}
