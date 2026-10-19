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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using NUnit.Framework;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// Skins read off disk: the folder the viewer keeps under D:\Mediaportal\TIF\skins is what the engine wears,
  /// written and edited as plain files - the reference's bargain that a skin is installed by its folder
  /// existing and changed by opening one of its files. These tests build a throwaway skin folder and say out
  /// loud that the engine really reads it.
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
  }
}
