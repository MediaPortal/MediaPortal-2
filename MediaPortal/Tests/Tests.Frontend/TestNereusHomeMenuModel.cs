using System;
using System.Linq;
using MediaPortal.Frontend.Skins.Nereus;
using MediaPortal.Frontend.ViewModels;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Tests.Frontend
{
  [TestFixture]
  public class TestNereusHomeMenuModel
  {
    private static MainMenuEntry FindEntry(string actionId)
    {
      foreach (MainMenuEntry entry in MainMenu.Build(Array.Empty<AspectViewModel>(), connected: false))
      {
        if (string.Equals(entry.HomeImageId, actionId, StringComparison.OrdinalIgnoreCase))
          return entry;
      }

      Assert.Fail("No home action has image id " + actionId);
      return null!;
    }

    [Test]
    public void ConfiguredActionsAreShownInSettingsOrderWithOtherPluginsLast()
    {
      MainMenuEntry audio = FindEntry("30715d73-4205-417f-80aa-e82f0834171f");
      MainMenuEntry movies = FindEntry("80d2e2cc-baaa-4750-807b-f37714153751");
      MainMenuEntry settings = FindEntry("f6255762-c52a-4231-9e67-14c28735216e");
      MainMenuEntry videos = FindEntry("a4df2df6-8d66-479a-9930-d7106525eb07");
      var model = new NereusHomeMenuModel();

      model.Rebuild(MainMenu.Build(Array.Empty<AspectViewModel>(), connected: false), new[]
      {
          Guid.Parse("80d2e2cc-baaa-4750-807b-f37714153751"),
          Guid.Parse("30715d73-4205-417f-80aa-e82f0834171f")
      });

      CollectionAssert.AreEqual(
          new[] { movies.HomeImageId, audio.HomeImageId },
          model.MainMenuItems.Take(2).Select(entry => entry.HomeImageId));
      Assert.That(model.IsOtherPluginsEntry(model.MainMenuItems[2]), Is.True);
      CollectionAssert.Contains(model.OtherMenuItems.Select(entry => entry.HomeImageId), videos.HomeImageId);
      CollectionAssert.Contains(model.OtherMenuItems.Select(entry => entry.HomeImageId), settings.HomeImageId);
    }

    [Test]
    public void MissingAndRepeatedConfiguredActionsDoNotDuplicateMenuItems()
    {
      MainMenuEntry audio = FindEntry("30715d73-4205-417f-80aa-e82f0834171f");
      var available = MainMenu.Build(Array.Empty<AspectViewModel>(), connected: false);
      var model = new NereusHomeMenuModel();

      model.Rebuild(available, new[]
      {
          Guid.Parse("30715d73-4205-417f-80aa-e82f0834171f"),
          Guid.Parse("30715d73-4205-417f-80aa-e82f0834171f"),
          Guid.NewGuid()
      });

      CollectionAssert.AreEqual(
          new[] { audio.HomeImageId },
          model.MainMenuItems.Take(1).Select(entry => entry.HomeImageId));
      Assert.That(model.MainMenuItems.Count, Is.EqualTo(2));
      Assert.That(model.OtherMenuItems, Does.Not.Contain(audio));
    }

    [Test]
    public void HomeContentListsUseNereusDefaultsAndHonorConfiguredOrder()
    {
      Guid moviesActionId = Guid.Parse("80d2e2cc-baaa-4750-807b-f37714153751");

      CollectionAssert.AreEqual(
          new[] { "Latest added", "Continue playing", "Favorites", "Unplayed" },
          HomeContentList.ForAction(moviesActionId).Select(list => list.Heading));

      CollectionAssert.AreEqual(
          new[] { "Unplayed", "Favorites", "Latest added" },
          HomeContentList.ForAction(moviesActionId, new[]
          {
              moviesActionId.ToString("D") + ":UnplayedMovies,FavoriteMovies,LatestMovies"
          }).Select(list => list.Heading));
    }

    [Test]
    public void MediaShortcutsKeepTheReferenceOrderAndSkinIcons()
    {
      MainMenuEntry movies = FindEntry("80d2e2cc-baaa-4750-807b-f37714153751");
      MainMenuEntry audio = FindEntry("30715d73-4205-417f-80aa-e82f0834171f");
      var model = new NereusHomeMenuModel();

      CollectionAssert.AreEqual(
          new[] { "Genre", "Year", "Age", "Actor", "Search" },
          model.ShortcutsFor(movies).Select(shortcut => shortcut.Label));
      CollectionAssert.AreEqual(
          new[] { "Genre", "Track", "Album", "Artist", "Year" },
          model.ShortcutsFor(audio).Select(shortcut => shortcut.Label));
      // The icon is named, not filed: the model says which glyph, the skin says what it is made of - and it is
      // made of vectors now, so no extension is written anywhere in the model.
      Assert.That(model.ShortcutsFor(movies)[0].IconName, Is.EqualTo("MediaMenu/genre"));
    }

    [Test]
    public void HomeContentMosaicsUseTheReferenceMediaLayouts()
    {
      Guid movies = Guid.Parse("80d2e2cc-baaa-4750-807b-f37714153751");
      Guid series = Guid.Parse("30f57cba-459c-4202-a587-09fff5098251");
      Guid audio = Guid.Parse("30715d73-4205-417f-80aa-e82f0834171f");
      Guid images = Guid.Parse("55556593-9fe9-436c-a3b6-a971e10c9d44");
      Guid videos = Guid.Parse("a4df2df6-8d66-479a-9930-d7106525eb07");

      Assert.That(HomeContentList.ForAction(movies)[0].Layout, Is.EqualTo("poster1"));
      Assert.That(HomeContentList.ForAction(series)[0].Layout, Is.EqualTo("thumbnail1"));
      Assert.That(HomeContentList.ForAction(series)[1].Layout, Is.EqualTo("poster3"));
      Assert.That(HomeContentList.ForAction(audio)[1].Layout, Is.EqualTo("square2"));
      Assert.That(HomeContentList.ForAction(images)[0].Layout, Is.EqualTo("thumbnail1"));
      Assert.That(HomeContentList.ForAction(images)[2].Layout, Is.EqualTo("thumbnail2"));
      Assert.That(HomeContentList.ForAction(videos)[1].Layout, Is.EqualTo("thumbnail2"));
      Assert.That(HomeContentList.ForAction(images)[0].Width, Is.EqualTo(900));
    }
  }
}
