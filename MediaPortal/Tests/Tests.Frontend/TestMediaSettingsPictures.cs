#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The media door of the settings hub against a server that answers what it does with the library's pictures.
  /// The reference filed those answers under <c>/Configuration/Settings/Media</c> as eight pages of their own
  /// (ServerSettings.Client/plugin.xml:93), so what a household meets here is a door that opens a wall of
  /// pages: the machine's own answers behind one plate, one page per section of the server's behind the rest.
  /// </summary>
  /// <remarks>
  /// What these tests hold is the assembling of that wall and what a page behind a door is: the server's media
  /// sections and their order, no plugin section among them, a page that opens its one section without listing
  /// anything, and a machine page that asks the server about nothing at all. The editing of one field of one
  /// section - what a change sends, what a refusal does - is <c>TestPluginSettingsScreen</c>'s business, and
  /// one page's worth of it is proved here so that a page wired to the wrong section cannot pass.
  /// </remarks>
  [TestFixture]
  public class TestMediaSettingsPictures
  {
    /// <summary>An address for the exceptions a real call would have carried with it.</summary>
    private static readonly Uri NO_SERVER = new Uri("http://127.0.0.1:59999/MediaPortal/Server");

    private string _folder = "";
    private string _file = "";

    [SetUp]
    public void MakeMachine()
    {
      _folder = Path.Combine(Path.GetTempPath(), "mp2-media-pictures-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_folder);
      _file = Path.Combine(_folder, "media.json");
    }

    [TearDown]
    public void TakeTheMachineAway()
    {
      try
      {
        Directory.Delete(_folder, true);
      }
      catch (Exception)
      {
        // A leftover folder in temp is nobody's problem but the test runner's disk.
      }
    }

    /// <summary>A server that answers with the sections it was handed and remembers every change it was sent.</summary>
    private sealed class FakePicturesApi : IPluginSettingsApi
    {
      public readonly List<SettingSectionState> Sections = new List<SettingSectionState>();

      /// <summary>The areas each list call asked for.</summary>
      public readonly List<string?> Areas = new List<string?>();

      /// <summary>The sections each read-by-name call asked for, in the order they were asked.</summary>
      public readonly List<string> Keys = new List<string>();

      public readonly List<KeyValuePair<string, SettingSectionWish>> Saves =
          new List<KeyValuePair<string, SettingSectionWish>>();

      public Exception? ThrowOnList;
      public Exception? ThrowOnPut;

      public Task<SettingSectionsAnswer> GetSettingSectionsAsync(string? area = null,
          CancellationToken cancellation = default)
      {
        Areas.Add(area);
        if (ThrowOnList != null)
          throw ThrowOnList;

        return Task.FromResult(new SettingSectionsAnswer
        {
          Sections = Sections
              .Where(s => area == null || string.Equals(s.Area, area, StringComparison.Ordinal))
              .Select(s => new SettingSectionSummary
              {
                Key = s.Key,
                Title = s.Title,
                Description = s.Description,
                Owner = s.Owner,
                Area = s.Area,
                FieldCount = s.Fields.Count
              })
              .ToList()
        });
      }

      public Task<SettingSectionState> GetSettingSectionAsync(string key,
          CancellationToken cancellation = default)
      {
        Keys.Add(key);
        return Task.FromResult(Sections.First(s => s.Key == key));
      }

      public Task<SettingSectionState> PutSettingSectionAsync(string key, SettingSectionWish wish,
          CancellationToken cancellation = default)
      {
        if (ThrowOnPut != null)
          throw ThrowOnPut;

        Saves.Add(new KeyValuePair<string, SettingSectionWish>(key, wish));

        // The real server answers a change with the whole section as it now stands, so the fake stores what it
        // was sent first - otherwise it would answer with the value from before the change, and a test could
        // not tell "the page wears the server's answer" apart from "the page forgot what it did".
        int at = Sections.FindIndex(s => s.Key == key);
        SettingSectionState stored = Sections[at];
        List<SettingFieldState> fields = stored.Fields.ToList();
        foreach (KeyValuePair<string, object> sent in wish.Values)
        {
          int field = fields.FindIndex(f => f.Key == sent.Key);
          Assert.That(field, Is.GreaterThanOrEqualTo(0), "the page sent a field the section does not have");
          fields[field] = fields[field] with { Value = JsonSerializer.SerializeToElement(sent.Value) };
        }

        Sections[at] = stored with { Fields = fields };
        return Task.FromResult(Sections[at]);
      }
    }

    private static JsonElement Json(object value)
    {
      return JsonSerializer.SerializeToElement(value);
    }

    private static SettingFieldState Toggle(string key, string label, bool value)
    {
      return new SettingFieldState
      {
        Key = key,
        Label = label,
        Help = "Off is what this runs with.",
        Kind = SettingSectionViewModelKinds.Toggle,
        Value = Json(value)
      };
    }

    private static SettingFieldState Number(string key, string label, int value, int min, int max)
    {
      return new SettingFieldState
      {
        Key = key,
        Label = label,
        Help = "The picture behind a title's own screen.",
        Kind = SettingSectionViewModelKinds.Number,
        Min = min,
        Max = max,
        Value = Json(value)
      };
    }

    private static SettingSectionState Section(string key, string title, string area,
        params SettingFieldState[] fields)
    {
      return new SettingSectionState
      {
        Key = key,
        Title = title,
        Description = "What the server does with its pictures.",
        Owner = "Media library",
        Area = area,
        Fields = fields
      };
    }

    /// <summary>The machine's own page, which is the first plate of the wall whatever the server says.</summary>
    private static SettingsSection MachinePage()
    {
      // The build is spelled out as a screen's build rather than left to be read off a bare null: a door and a
      // page differ only by which delegate the section was handed, and a test that handed the wrong one would be
      // testing a door it never meant to open.
      return new SettingsSection("Kept pictures", "Frontend", "what this box keeps",
          (Func<Avalonia.Controls.Control?>)delegate { return null; });
    }

    [Test]
    public async Task TheDoorOpensTheMachineAndEverySectionOfTheServersMediaHalf()
    {
      FakePicturesApi api = new FakePicturesApi();
      api.Sections.Add(Section("fanart", "The picture store", SettingSectionAreas.Media,
          Number("max_backdrops", "Backdrops kept per title", 3, 1, 20)));
      api.Sections.Add(Section("pictures_movie", "Pictures lying beside the films", SettingSectionAreas.Media,
          Toggle("cache_local_fanart", "Copies of the pictures lying beside a film", false)));
      api.Sections.Add(Section("logging", "What the server writes down", SettingSectionAreas.Plugin,
          Toggle("log_readers", "Write a film's file itself into the log", false)));

      IReadOnlyList<SettingsSection> pages =
          await MediaSettingsPages.PagesAsync(() => api, MachinePage());

      Assert.That(api.Areas, Is.EqualTo(new[] { SettingSectionAreas.Media }),
          "the door asks for the media half of the server's answers and nothing else");
      Assert.That(pages.Select(p => p.Name),
          Is.EqualTo(new[] { "Kept pictures", "The picture store", "Pictures lying beside the films" }),
          "this machine first, then the server's pages in the server's own order - a wall is a wall because "
          + "one plate stands beside another, and the order the server lists them in is the order it means");
      Assert.That(pages.Select(p => p.Owner), Is.EqualTo(new[] { "Frontend", "Server", "Server" }),
          "a plate says whose answer its page holds");
      Assert.That(pages[0].IsFolder, Is.False, "the pages a door opens are pages, not doors");
      Assert.That(pages[1].IsFolder, Is.False);
    }

    [Test]
    public async Task AServerThatWouldNotSayAnythingLeavesTheMachineItsOnePlate()
    {
      FakePicturesApi api = new FakePicturesApi
      {
        ThrowOnList = new ServerApiException(NO_SERVER, "this server does not offer its settings")
      };

      Exception? thrown = null;
      try
      {
        await MediaSettingsPages.PagesAsync(() => api, MachinePage());
      }
      catch (Exception ex)
      {
        thrown = ex;
      }

      // The wall is not assembled from a half answer, and the hub is the place that says why a door did not
      // open - a wall of one plate taken for "the server has nothing" would be a lie about the server.
      Assert.That(thrown, Is.InstanceOf<ServerApiException>());
    }

    [Test]
    public async Task WithoutAServerTheDoorOpensOnlyThisMachine()
    {
      IReadOnlyList<SettingsSection> pages = await MediaSettingsPages.PagesAsync(
          delegate { return null; }, MachinePage());

      Assert.That(pages.Select(p => p.Name), Is.EqualTo(new[] { "Kept pictures" }),
          "a box with no server has its own answers and no other answers");
    }

    [Test]
    public void APageBehindADoorOpensItsOneSectionAndHasNoListOfItsOwn()
    {
      FakePicturesApi api = new FakePicturesApi();
      api.Sections.Add(Section("pictures_movie", "Pictures lying beside the films", SettingSectionAreas.Media,
          Toggle("cache_local_fanart", "Copies of the pictures lying beside a film", false)));

      SettingSectionSummary summary = new SettingSectionSummary
      {
        Key = "pictures_movie",
        Title = "Pictures lying beside the films",
        Description = "What the server does with its pictures.",
        Owner = "Media library",
        Area = SettingSectionAreas.Media,
        FieldCount = 1
      };

      PluginSettingsViewModel page = new PluginSettingsViewModel(() => api, summary);
      Assert.That(page.HasList, Is.False,
          "the wall outside a door already listed the sections; a page offering a list of one is a page asking "
          + "the viewer to choose what they have just chosen");

      page.ReloadAsync().Wait();

      Assert.That(api.Areas, Is.Empty, "a page behind a door never asks what the server offers");
      Assert.That(api.Keys, Is.EqualTo(new[] { "pictures_movie" }), "it reads its own section and nothing else");
      Assert.That(page.HasSection, Is.True);
      Assert.That(page.ShowsList, Is.False);
      Assert.That(page.Title, Is.EqualTo("Pictures lying beside the films"),
          "and it is called what its section is called");
      Assert.That(page.Intro, Does.Contain("Pictures lying beside the films"));
      Assert.That(page.Section!.Fields.Select(f => f.Label),
          Is.EqualTo(new[] { "Copies of the pictures lying beside a film" }));
    }

    [Test]
    public async Task APageReadsItsSectionAgainEveryTimeTheDoorOpens()
    {
      FakePicturesApi api = new FakePicturesApi();
      api.Sections.Add(Section("fanart", "The picture store", SettingSectionAreas.Media,
          Number("max_backdrops", "Backdrops kept per title", 3, 1, 20)));

      SettingSectionSummary summary = new SettingSectionSummary
      {
        Key = "fanart",
        Title = "The picture store",
        Owner = "Media library",
        Area = SettingSectionAreas.Media,
        FieldCount = 1
      };

      PluginSettingsViewModel page = new PluginSettingsViewModel(() => api, summary);
      await page.ReloadAsync();
      await page.ReloadAsync();

      Assert.That(api.Keys, Is.EqualTo(new[] { "fanart", "fanart" }),
          "the answer is the server's, and another television may have answered differently while the page "
          + "was closed");
    }

    [Test]
    public async Task APageOpensTheServerItWasGivenRatherThanTheOneItHopesFor()
    {
      FakePicturesApi api = new FakePicturesApi();
      api.Sections.Add(Section("fanart", "The picture store", SettingSectionAreas.Media,
          Number("max_backdrops", "Backdrops kept per title", 3, 1, 20)));

      SettingSectionSummary summary = new SettingSectionSummary
      {
        Key = "fanart",
        Title = "The picture store",
        Owner = "Media library",
        Area = SettingSectionAreas.Media,
        FieldCount = 1
      };

      bool asked = false;
      PluginSettingsViewModel page = new PluginSettingsViewModel(
          delegate
          {
            asked = true;
            return api;
          }, summary);

      await page.ReloadAsync();

      Assert.That(asked, Is.True,
          "the page asks where the server is every time, because the door may have been opened against a "
          + "different server than the last time");
    }

    [Test]
    public async Task WorkingAnAnswerOnAMediaPageSendsThatOneFieldToItsSection()
    {
      FakePicturesApi api = new FakePicturesApi();
      api.Sections.Add(Section("pictures_movie", "Pictures lying beside the films", SettingSectionAreas.Media,
          Toggle("cache_local_fanart", "Copies of the pictures lying beside a film", false),
          Toggle("cache_network_fanart", "Copies of the pictures beside a film on a sleeping share", true)));

      SettingSectionSummary summary = new SettingSectionSummary
      {
        Key = "pictures_movie",
        Title = "Pictures lying beside the films",
        Owner = "Media library",
        Area = SettingSectionAreas.Media,
        FieldCount = 2
      };

      PluginSettingsViewModel page = new PluginSettingsViewModel(() => api, summary);
      await page.ReloadAsync();

      SettingSectionViewModel section = page.Section!;
      Assert.That(section.Fields[0].Toggle, Is.False);
      Assert.That(section.Fields[1].Toggle, Is.True, "the server's answer is shown as the server holds it");

      section.Fields[0].Toggle = true;
      await section.SaveAsync();

      Assert.That(api.Saves.Count, Is.EqualTo(1), "one answered field is one call");
      Assert.That(api.Saves[0].Key, Is.EqualTo("pictures_movie"), "into the section it belongs to");
      Assert.That(api.Saves[0].Value.Values.Keys, Is.EqualTo(new[] { "cache_local_fanart" }),
          "carrying only the answer that was worked");
      Assert.That(section.Status, Is.EqualTo("Saved on the server."));
    }

    [Test]
    public void TheMachinePageAsksTheServerAboutNothing()
    {
      FakePicturesApi api = new FakePicturesApi();
      api.Sections.Add(Section("fanart", "The picture store", SettingSectionAreas.Media,
          Number("max_backdrops", "Backdrops kept per title", 3, 1, 20)));

      // Given a server and asking it nothing: the server's answers have their own pages now, and a machine page
      // that also drew them would be the long list the wall replaced.
      MediaSettingsViewModel screen = new MediaSettingsViewModel(null, _file, null);

      Assert.That(api.Areas, Is.Empty);
      Assert.That(api.Keys, Is.Empty);
      Assert.That(api.Saves, Is.Empty);

      Assert.That(screen.Options.Count, Is.EqualTo(6),
          "this machine's five answers, and the one doing that sends the server over its library");
      Assert.That(screen.Options[0].Label, Is.EqualTo("Keep pictures on this machine"));
      Assert.That(screen.Options[5].Label, Is.EqualTo("On the server: pictures it has ready"));
      Assert.That(screen.Heading, Does.Contain("this machine"),
          "and the page says whose answers it holds, because it stands beside the server's pages now");
    }
  }
}