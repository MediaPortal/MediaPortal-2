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
  /// The media settings screen against a server that answers what it does with the library's pictures. The
  /// reference filed those answers under one node of its configuration tree, <c>/Configuration/Settings/Media</c>,
  /// beside the media itself and never under a plugin node, so this screen is where a household answers them -
  /// and the plugin settings screen, which asks for the other area, no longer shows them.
  /// </summary>
  /// <remarks>
  /// What these tests hold is the screen's side of that: it asks the server for its area, draws one row per
  /// field in the place the server's rows have, sends back only the answer that was worked, and wears what the
  /// server says instead of what it hoped. Whether the server stores the answer is
  /// <c>Tests.Server.TestPluginSettingsOverHttp</c>'s business.
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
        // not tell "the screen wears the server's answer" apart from "the screen forgot what it did".
        int at = Sections.FindIndex(s => s.Key == key);
        SettingSectionState stored = Sections[at];
        List<SettingFieldState> fields = stored.Fields.ToList();
        foreach (KeyValuePair<string, object> sent in wish.Values)
        {
          int field = fields.FindIndex(f => f.Key == sent.Key);
          Assert.That(field, Is.GreaterThanOrEqualTo(0), "the screen sent a field the section does not have");
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

    private static SettingSectionState Section(string key, string title, params SettingFieldState[] fields)
    {
      return new SettingSectionState
      {
        Key = key,
        Title = title,
        Description = "What the server does with its pictures.",
        Owner = "Media library",
        Area = SettingSectionAreas.Media,
        Fields = fields
      };
    }

    /// <summary>The screen with a server of the test's making, with its server rows read.</summary>
    private static async Task<MediaSettingsViewModel> Opened(FakePicturesApi? api, string file)
    {
      MediaSettingsViewModel screen = new MediaSettingsViewModel(null, file, null, api);
      await screen.ServerAnswersRead;
      return screen;
    }

    [Test]
    public async Task TheScreenAsksTheServerForItsOwnPictures()
    {
      FakePicturesApi api = new FakePicturesApi();
      api.Sections.Add(Section("fanart", "How many pictures of each kind one title keeps",
          Number("max_backdrops", "Backdrops kept per title", 3, 1, 20)));
      api.Sections.Add(Section("pictures_movie", "Pictures lying beside the films",
          Toggle("cache_local_fanart", "Copies of the pictures lying beside a film on a disk in this machine",
              false)));

      MediaSettingsViewModel screen = await Opened(api, _file);

      Assert.That(api.Areas, Is.EqualTo(new[] { SettingSectionAreas.Media }),
          "the media settings screen asks for the media half of the server's answers and nothing else");

      // The machine's own five rows first, then every section of the server under a heading of its own, then the
      // row that sends the server over its library - so a viewer walking the screen reads this box, then the
      // server's answers grouped as the server groups them, then the doing.
      Assert.That(screen.Options[5].IsHeader, Is.True, "a server's section is named before its rows are asked");
      Assert.That(screen.Options[5].Label, Is.EqualTo("How many pictures of each kind one title keeps"));
      Assert.That(screen.Options[6].Label, Is.EqualTo("Backdrops kept per title"));
      Assert.That(screen.Options[7].Label, Is.EqualTo("Pictures lying beside the films"));
      Assert.That(screen.Options[8].Label,
          Is.EqualTo("Copies of the pictures lying beside a film on a disk in this machine"));
      Assert.That(screen.Options[8].IsToggle, Is.True, "a switch with two answers wears a checkbox");
      Assert.That(screen.Options[9].Label, Is.EqualTo("On the server: pictures it has ready"),
          "the server's rows go above the row that presses the server, not after it");
    }

    [Test]
    public async Task WorkingAServerRowSendsThatOneAnswerAndSaysTheServerHasIt()
    {
      FakePicturesApi api = new FakePicturesApi();
      api.Sections.Add(Section("pictures_movie", "Pictures lying beside the films",
          Toggle("cache_local_fanart", "Copies of the pictures lying beside a film", false),
          Toggle("cache_network_fanart", "Copies of the pictures beside a film on a sleeping share", true)));

      MediaSettingsViewModel screen = await Opened(api, _file);
      SkinSettingOption local = Row(screen, "Copies of the pictures lying beside a film");
      SkinSettingOption network = Row(screen, "Copies of the pictures beside a film on a sleeping share");

      Assert.That(local.IsChecked, Is.False);
      Assert.That(network.IsChecked, Is.True, "the server's answer is shown as the server holds it");

      local.Press();

      Assert.That(api.Saves.Count, Is.EqualTo(1), "one answered row is one call");
      Assert.That(api.Saves[0].Key, Is.EqualTo("pictures_movie"));
      Assert.That(api.Saves[0].Value.Values.Keys, Is.EqualTo(new[] { "cache_local_fanart" }),
          "and it carries only the answer that was worked");
      Assert.That((bool)api.Saves[0].Value.Values["cache_local_fanart"], Is.True);
      Assert.That(screen.Status, Does.Contain("on the server"),
          "the status line says whose answer this now is");
      Assert.That(local.IsChecked, Is.True, "the row wears the answer the server was sent");
    }

    [Test]
    public async Task ARowThatWasRefusedGoesBackToWhatTheServerHolds()
    {
      FakePicturesApi api = new FakePicturesApi();
      api.Sections.Add(Section("pictures_movie", "Pictures lying beside the films",
          Toggle("cache_local_fanart", "Copies of the pictures lying beside a film", false)));

      MediaSettingsViewModel screen = await Opened(api, _file);
      SkinSettingOption row = Row(screen, "Copies of the pictures lying beside a film");

      api.ThrowOnPut = new ServerApiException(NO_SERVER, "the server does not take that");
      row.Press();

      Assert.That(screen.Status, Does.Contain("would not take"),
          "a refused answer is said, not swallowed");
      Assert.That(row.IsChecked, Is.False,
          "and the face goes back to what the server says rather than sitting there looking stored");
    }

    [Test]
    public async Task ANumberRowWalksTheBoundsTheServerGaveIt()
    {
      FakePicturesApi api = new FakePicturesApi();
      api.Sections.Add(Section("fanart", "How many pictures of each kind one title keeps",
          Number("max_backdrops", "Backdrops kept per title", 2, 1, 3)));

      MediaSettingsViewModel screen = await Opened(api, _file);
      SkinSettingOption backdrops = Row(screen, "Backdrops kept per title");

      Assert.That(backdrops.ValueText, Is.EqualTo("2"));
      backdrops.Press();
      Assert.That(backdrops.ValueText, Is.EqualTo("3"));
      backdrops.Press();
      Assert.That(backdrops.ValueText, Is.EqualTo("1"), "past the ceiling the row starts again at the floor");
      Assert.That(api.Saves.Count, Is.EqualTo(2), "and every step is an answer the server is sent");
      Assert.That((int)api.Saves[1].Value.Values["max_backdrops"], Is.EqualTo(1));
    }

    [Test]
    public async Task ARestartAnswerSaysSo()
    {
      FakePicturesApi api = new FakePicturesApi();
      api.Sections.Add(new SettingSectionState
      {
        Key = "fanart",
        Title = "How many pictures of each kind one title keeps",
        Owner = "Media library",
        Area = SettingSectionAreas.Media,
        Fields = new[] { Number("max_backdrops", "Backdrops kept per title", 3, 1, 20) },
        RestartNeededFor = new[] { "max_backdrops" }
      });

      MediaSettingsViewModel screen = await Opened(api, _file);
      Row(screen, "Backdrops kept per title").Press();

      Assert.That(screen.Status, Does.Contain("when it next starts"),
          "a stored answer the server will only act on later says so, because otherwise the viewer assumes "
          + "it lies");
    }

    [Test]
    public async Task AServerThatWillNotTalkAboutItsPicturesLeavesTheScreenWorking()
    {
      FakePicturesApi api = new FakePicturesApi
      {
        ThrowOnList = new ServerApiException(NO_SERVER, "this server does not offer its settings")
      };

      MediaSettingsViewModel screen = await Opened(api, _file);

      Assert.That(screen.Status, Does.Contain("would not say"));
      Assert.That(screen.Options.Count, Is.EqualTo(6), "the machine's own rows are still there and still true");
      Assert.That(screen.Options[0].Label, Is.EqualTo("Keep pictures on this machine"));
    }

    /// <summary>
    /// The row a screen wears for one named answer. Looked up by name rather than by place, because a heading is
    /// a row of the same list - a test that counted places would be counting the names of sections and would
    /// fail every time the server was asked about one more thing.
    /// </summary>
    private static SkinSettingOption Row(MediaSettingsViewModel screen, string label)
    {
      return screen.Options.Single(option => option.Label == label);
    }

    [Test]
    public async Task AHeadingNamesTheSectionAndAnswersNothing()
    {
      FakePicturesApi api = new FakePicturesApi();
      api.Sections.Add(Section("fanart", "How many pictures of each kind one title keeps",
          Number("max_backdrops", "Backdrops kept per title", 3, 1, 20)));

      MediaSettingsViewModel screen = await Opened(api, _file);
      SkinSettingOption heading = Row(screen, "How many pictures of each kind one title keeps");

      Assert.That(heading.IsHeader, Is.True);
      Assert.That(heading.IsToggle, Is.False, "a heading wears no checkbox");
      Assert.That(heading.ShowsButton, Is.False, "and no button either");
      Assert.That(heading.ValueText, Is.Empty, "there is nothing on its face to read");

      heading.Press();

      Assert.That(api.Saves, Is.Empty, "pressing a heading sends nothing to the server");
      Assert.That(screen.Status, Is.Empty, "and it says nothing about having done something");

      // Read again - which is what opening the screen a second time does - and the section is still named once.
      await screen.ReadServerAnswersAsync();

      Assert.That(screen.Options.Count(o => o.Label == "How many pictures of each kind one title keeps"),
          Is.EqualTo(1), "a screen read twice does not fill up with headings");
    }

    [Test]
    public void WithoutAServerTheScreenAsksNobody()
    {
      MediaSettingsViewModel screen = new MediaSettingsViewModel(null, _file, null, null);

      Assert.That(screen.Options.Count, Is.EqualTo(6), "the five answers of this box and the doing");
      Assert.That(screen.Options[5].Label, Is.EqualTo("On the server: pictures it has ready"));
    }
  }
}
