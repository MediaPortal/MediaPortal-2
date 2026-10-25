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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using MediaPortal.Frontend;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The plates of the settings wall against the pictures the shipped skins carry. The reference hung a picture
  /// on every node of its configuration tree and named the file after the node
  /// (<c>Media_MovieMDESettings.jpg</c>, <c>Appearance_SkinSettings.jpg</c>, ...), and its tile asked the skin
  /// for <c>SettingsMenu\{path}.jpg</c> and <c>SettingsMenu\Icon_{path}.png</c>
  /// (OtherControlsOverrides.xaml:648 and :671). A plate whose name no skin carries is a bare rectangle on the
  /// television, which is a mistake nobody notices while the wall is being built and everybody notices when it
  /// hangs - so the names the model uses are checked here against the files the skins ship.
  /// </summary>
  [TestFixture]
  public class TestSettingsHubPictures
  {
    /// <summary>The two skins this front end ships, and therefore the two that have to answer a plate's picture.</summary>
    private static readonly string[] SHIPPED_SKINS = { "Nereus", "WMC" };

    /// <summary>The reference's eight branches, in the order its <c>/Configuration/Settings</c> lists them.</summary>
    private static readonly string[] BRANCHES =
        { "Appearance", "General", "Media", "Players", "Plugins", "Shares", "Television", "Users" };

    /// <summary>Where the front end's source - and with it its <c>Skins</c> folder - sits.</summary>
    private static string SkinsRoot()
    {
      AssemblyMetadataAttribute? value = typeof(TestSettingsHubPictures).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, "FrontendSource", StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand FrontendSource over for the skins' pictures to be checkable");

      string folder = Path.Combine(value!.Value!, "Skins");
      Assert.That(Directory.Exists(folder), Is.True, "there is no skins folder at " + folder);
      return folder;
    }

    /// <summary>
    /// Every plate of the wall: the doors, and the pages behind the doors that open without a server in front of
    /// them. A door whose pages are the server's answer is walked too, but a wall that cannot be reached here is
    /// noted and skipped rather than failed - the picture rule those plates use is
    /// <see cref="EveryNameTheMediaWallAsksForIsDrawn"/>'s business.
    /// </summary>
    private static async Task<List<SettingsSection>> WalkWall()
    {
      App app = (App)Application.Current!;
      SettingsViewModel hub = app.SettingsModel();

      var plates = new List<SettingsSection>();
      var unreachable = new List<string>();

      foreach (SettingsSection door in hub.Sections)
      {
        plates.Add(door);
        if (door.Pages == null)
          continue;

        try
        {
          foreach (SettingsSection page in await door.Pages())
            plates.Add(page);
        }
        catch (Exception)
        {
          // Nothing to talk to, which is the state a test run is in. The door itself was still checked.
          unreachable.Add(door.Name);
        }
      }

      TestContext.WriteLine("pages were not reachable for: " + string.Join(", ", unreachable));
      return plates;
    }

    [AvaloniaTest]
    public async Task EveryPlateOfTheWallWearsAPictureItsSkinShips()
    {
      string skins = SkinsRoot();
      List<SettingsSection> plates = await WalkWall();

      // The wall is worth checking only if the walk actually walked it: eight branches, a page behind most of
      // them, and the way out. Fewer plates than that means the wall was not built, not that it was bare.
      Assert.That(plates.Count, Is.GreaterThanOrEqualTo(20),
          "the walk found " + plates.Count + " plates; the wall is eight doors with pages behind them");

      foreach (SettingsSection plate in plates)
      {
        if (plate.Open != null)
        {
          // The close screen is a leaf, not a place in the tree - the reference gave it no plate of its own.
          continue;
        }

        Assert.That(plate.Picture, Is.Not.Empty, plate.Name + " names no picture");
        Assert.That(plate.Icon, Is.Not.Empty, plate.Name + " names no icon");

        foreach (string skin in SHIPPED_SKINS)
        {
          string folder = Path.Combine(skins, skin, "images", "SettingsMenu");
          Assert.That(File.Exists(Path.Combine(folder, plate.Picture + ".jpg")), Is.True,
              skin + " carries no plate picture named " + plate.Picture + ", which " + plate.Name +
              " asks for");
          Assert.That(File.Exists(Path.Combine(folder, "Icon_" + plate.Icon + ".png")), Is.True,
              skin + " carries no band icon named Icon_" + plate.Icon + ".png, which " + plate.Name +
              " asks for");
        }
      }
    }

    [AvaloniaTest]
    public void TheWallIsTheReferencesEightBranchesAndTheWayOut()
    {
      App app = (App)Application.Current!;
      List<string> names = app.SettingsModel().Sections.Select(s => s.Name).ToList();

      List<string> branches = names.Where(n => BRANCHES.Contains(n)).ToList();
      Assert.That(branches, Is.EqualTo(BRANCHES),
          "the wall is the reference's branches in the reference's own order");
      Assert.That(names[names.Count - 1], Is.EqualTo("Close"),
          "the way out is the last plate, where a household without a mouse looks for it");
      Assert.That(names.Count, Is.EqualTo(BRANCHES.Length + 1),
          "nothing else stands on the wall - a branch this front end cannot answer for is left off rather than "
          + "offered empty");

      // Every branch is a door: what a branch offers is a wall of pages, which is how the reference's own tree
      // was walked, and not one long list of every question at once.
      Assert.That(app.SettingsModel().Sections.Where(s => s.Name != "Close").All(s => s.IsFolder), Is.True);
    }

    [Test]
    public void EveryNameTheMediaWallAsksForIsDrawn()
    {
      // The media wall's plates are the server's sections, so their pictures are named by a rule rather than
      // stood in the model: every answer the server can list has to have been drawn by the reference, and every
      // name has to be a file the shipped skins carry.
      string[] keys =
      {
        "fanart", "certifications", "virtual_media", "nfo_movie", "nfo_series", "nfo_audio",
        "pictures_movie", "pictures_series", "pictures_music", "some_question_nobody_drew"
      };

      string skins = SkinsRoot();
      foreach (string key in keys)
      {
        string picture = PluginSettingsPages.PictureFor(key);
        Assert.That(picture, Is.Not.Empty, key + " was left without a picture");

        foreach (string skin in SHIPPED_SKINS)
        {
          string folder = Path.Combine(skins, skin, "images", "SettingsMenu");
          Assert.That(File.Exists(Path.Combine(folder, picture + ".jpg")), Is.True,
              skin + " carries no plate picture named " + picture + ", which the section " + key + " was given");
          Assert.That(File.Exists(Path.Combine(folder, "Icon_" + picture + ".png")), Is.True,
              skin + " carries no band icon named Icon_" + picture + ".png for the section " + key);
        }
      }

      Assert.That(PluginSettingsPages.PictureFor("some_question_nobody_drew"), Is.EqualTo("Media"),
          "a section the reference never drew a plate for wears the branch's own picture, which is a plate of "
          + "the same family rather than a hole in the wall");
    }

    [Test]
    public void EveryMediaQuestionHasAPictureOfItsOwn()
    {
      // Spelled out because a wall where three sections wear the door's own plate looks dressed and is not.
      Assert.That(PluginSettingsPages.PictureFor("fanart"), Is.EqualTo("Media_FanArtSettings"));
      Assert.That(PluginSettingsPages.PictureFor("certifications"), Is.EqualTo("Media_Certifications"));
      Assert.That(PluginSettingsPages.PictureFor("virtual_media"), Is.EqualTo("Media_General"));
      Assert.That(PluginSettingsPages.PictureFor("nfo_movie"), Is.EqualTo("Media_MovieMDESettings"));
      Assert.That(PluginSettingsPages.PictureFor("nfo_series"), Is.EqualTo("Media_SeriesMDESettings"));
      Assert.That(PluginSettingsPages.PictureFor("nfo_audio"), Is.EqualTo("Media_AudioMDESettings"));
      Assert.That(PluginSettingsPages.PictureFor("pictures_movie"), Is.EqualTo("Media_ImageMDESettings"));
    }

    [Test]
    public void APlateAsksForItsOwnIconUnlessSomebodyNamesAnotherOne()
    {
      // The reference named an icon after its plate's node and said otherwise only where it reused the parent's:
      // there is no Icon_General_System_Diagnostics.png, so that plate's band carries Icon_General_System.png.
      SettingsSection plate = new SettingsSection("Diagnostics", "Frontend", "what both machines are made of",
          (Func<Avalonia.Controls.Control?>)delegate { return null; }, "General_System_Diagnostics",
          "General_System");
      Assert.That(plate.Icon, Is.EqualTo("General_System"));

      SettingsSection itsOwn = new SettingsSection("Picture store", "Server", "what the server keeps",
          (Func<Avalonia.Controls.Control?>)delegate { return null; }, "Media_FanArtSettings");
      Assert.That(itsOwn.Icon, Is.EqualTo("Media_FanArtSettings"),
          "nobody named another icon, so the band asks for the plate's own name with Icon_ in front of it");
    }
  }
}
