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
  /// The plugin settings screen against a server of its own making. Which sections stand open, what fields
  /// they hold and which of the three kinds each of them is are all the server's answers - what these tests
  /// hold is the screen's side of the deal: it lists what it was told, draws each kind as the control that
  /// kind asks for, sends back only what the viewer actually moved, adopts what the server answers to a
  /// save, and says out loud when a call failed instead of showing a form that is not real.
  /// </summary>
  [TestFixture]
  public class TestPluginSettingsScreen
  {
    /// <summary>A server that answers what the test tells it to and remembers what it was asked to store.</summary>
    private sealed class FakeSectionsApi : IPluginSettingsApi
    {
      public SettingSectionsAnswer ListAnswer = new SettingSectionsAnswer();
      public SettingSectionState SectionAnswer = new SettingSectionState();

      /// <summary>What a save answers. Left null, the save answers with <see cref="SectionAnswer"/>.</summary>
      public SettingSectionState? PutAnswer;

      public SettingSectionWish? Sent;
      public string SentKey = "";
      public List<string> Calls { get; } = new List<string>();
      public Exception? ThrowOnList;
      public Exception? ThrowOnGet;
      public Exception? ThrowOnPut;

      public Task<SettingSectionsAnswer> GetSettingSectionsAsync(CancellationToken cancellation = default)
      {
        Calls.Add("sections");
        if (ThrowOnList != null)
          throw ThrowOnList;
        return Task.FromResult(ListAnswer);
      }

      public Task<SettingSectionState> GetSettingSectionAsync(string key,
          CancellationToken cancellation = default)
      {
        Calls.Add("get:" + key);
        if (ThrowOnGet != null)
          throw ThrowOnGet;
        return Task.FromResult(SectionAnswer);
      }

      public Task<SettingSectionState> PutSettingSectionAsync(string key, SettingSectionWish wish,
          CancellationToken cancellation = default)
      {
        Calls.Add("put:" + key);
        if (ThrowOnPut != null)
          throw ThrowOnPut;
        Sent = wish;
        SentKey = key;
        return Task.FromResult(PutAnswer ?? SectionAnswer);
      }
    }

    /// <summary>One row of the list, as the server's summary of a section.</summary>
    private static SettingSectionSummary Summary(string key = "fanart", string title = "FanArt",
        string owner = "Media library", int fields = 2)
    {
      return new SettingSectionSummary
      {
        Key = key,
        Title = title,
        Description = "How many pictures of each kind one title keeps.",
        Owner = owner,
        FieldCount = fields
      };
    }

    private static JsonElement Json(object value)
    {
      return JsonSerializer.SerializeToElement(value);
    }

    private static SettingFieldState NumberField(string key, string label, int value, int min = 1, int max = 20)
    {
      return new SettingFieldState
      {
        Key = key,
        Label = label,
        Help = "The wide strip with the title's name on it.",
        Kind = SettingSectionViewModelKinds.Number,
        Min = min,
        Max = max,
        Value = Json(value)
      };
    }

    private static SettingFieldState ToggleField(string key, string label, bool value)
    {
      return new SettingFieldState
      {
        Key = key,
        Label = label,
        Kind = SettingSectionViewModelKinds.Toggle,
        Value = Json(value)
      };
    }

    private static SettingFieldState TextField(string key, string label, string? value)
    {
      return new SettingFieldState
      {
        Key = key,
        Label = label,
        Help = "Two letters the library's importer writes for the country a rating comes from.",
        Kind = SettingSectionViewModelKinds.Text,
        Value = value == null ? default : Json(value)
      };
    }

    private static SettingFieldState ChoiceField(string key, string label, string value, bool restart = false,
        params string[] options)
    {
      return new SettingFieldState
      {
        Key = key,
        Label = label,
        Kind = SettingSectionViewModelKinds.Choice,
        Options = options,
        Value = Json(value),
        RestartNeeded = restart
      };
    }

    /// <summary>A section's answer, with the rows a test names standing in it.</summary>
    private static SettingSectionState State(params SettingFieldState[] fields)
    {
      return State("fanart", fields);
    }

    private static SettingSectionState State(string key, params SettingFieldState[] fields)
    {
      return new SettingSectionState
      {
        Key = key,
        Title = "How many pictures of each kind one title keeps",
        Description = "The library fetches pictures for a title and keeps the first few of every kind.",
        Owner = "Media library",
        Fields = fields.ToList()
      };
    }

    /// <summary>A screen over a server that offers one section with these rows, already opened on it.</summary>
    private static async Task<PluginSettingsViewModel> ScreenOn(FakeSectionsApi api)
    {
      PluginSettingsViewModel screen = new PluginSettingsViewModel(() => api);
      await screen.ReloadAsync();
      await screen.OpenAsync(screen.Sections.First());
      return screen;
    }

    [Test]
    public async Task OpeningTheRowListsWhatTheServerOffers()
    {
      FakeSectionsApi api = new FakeSectionsApi
      {
        ListAnswer = new SettingSectionsAnswer
        {
          Sections = new[]
          {
            Summary("fanart", "How many pictures of each kind one title keeps", "Media library", 7),
            Summary("logging", "How much the server writes down", "Server", 1)
          }
        }
      };
      PluginSettingsViewModel screen = new PluginSettingsViewModel(() => api);

      await screen.ReloadAsync();

      Assert.That(screen.Sections.Select(s => s.Key), Is.EqualTo(new[] { "fanart", "logging" }),
          "the list is the server's, in the server's order, with nothing added to it");
      Assert.That(screen.HasSections, Is.True);
      Assert.That(screen.HasSection, Is.False, "listing is not editing - no editor stands up by itself");
      Assert.That(api.Calls, Is.EqualTo(new[] { "sections" }), "one call reads the whole list");
    }

    [Test]
    public async Task WithoutAServerAddressTheSettingsScreenSaysWhereToGo()
    {
      PluginSettingsViewModel screen = new PluginSettingsViewModel(() => null);

      await screen.ReloadAsync();

      Assert.That(screen.Status, Does.Contain("Server address"), "the refusal names the row that fixes it");
      Assert.That(screen.HasSections, Is.False);
    }

    [Test]
    public async Task AServerWithoutTheEndpointIsNamedAsAnOldOne()
    {
      FakeSectionsApi api = new FakeSectionsApi
      {
        ThrowOnList = new ServerApiException(new Uri("http://server/api/media/settings/sections"),
            System.Net.HttpStatusCode.NotFound, "")
      };
      PluginSettingsViewModel screen = new PluginSettingsViewModel(() => api);

      await screen.ReloadAsync();

      Assert.That(screen.Status, Does.Contain("older build"),
          "a missing endpoint is an old server, not a broken screen");
      Assert.That(screen.HasSections, Is.False, "and no empty form is offered over the refusal");
    }

    [Test]
    public async Task EachKindDrawsTheControlThatKindAskedFor()
    {
      FakeSectionsApi api = new FakeSectionsApi
      {
        ListAnswer = new SettingSectionsAnswer { Sections = new[] { Summary() } },
        SectionAnswer = State(
            ToggleField("keep_thumbs", "Keep the small ones", true),
            NumberField("max_banners", "Banners", 4),
            ChoiceField("log_level", "Log level", "Warning", false, "None", "Warning", "Debug"),
            TextField("display_movie_certification_country", "Movies", "DE"))
      };

      PluginSettingsViewModel screen = await ScreenOn(api);
      SettingSectionViewModel section = screen.Section!;

      Assert.That(section.Fields[0].IsToggle, Is.True);
      Assert.That(section.Fields[1].IsNumber, Is.True);
      Assert.That(section.Fields[2].IsChoice, Is.True);
      Assert.That(section.Fields[3].IsText, Is.True,
          "a country the library cannot be asked to pick from a list is written into a box");
      foreach (SettingFieldViewModel field in section.Fields)
      {
        Assert.That(new[] { field.IsToggle, field.IsNumber, field.IsChoice, field.IsText }.Count(on => on),
            Is.EqualTo(1),
            "{0} wears exactly one control - a row that drew two would ask the viewer which one is meant",
            field.Label);
        Assert.That(field.IsUndrawable, Is.False);
      }

      Assert.That(section.Fields[0].Toggle, Is.True, "the switch stands as the server stored it");
      Assert.That(section.Fields[1].NumberText, Is.EqualTo("4"));
      Assert.That(section.Fields[1].RangeHint, Is.EqualTo("Between 1 and 20."),
          "the range the server named is said beside the box, not only when it is broken");
      Assert.That(section.Fields[2].Options.Select(o => o.Name),
          Is.EqualTo(new[] { "None", "Warning", "Debug" }), "the names come in the server's order");
      Assert.That(section.Fields[2].Options.Single(o => o.IsOn).Name, Is.EqualTo("Warning"));
    }

    [Test]
    public async Task APressOnANameMovesTheMarkAndReleasesTheOthers()
    {
      FakeSectionsApi api = new FakeSectionsApi
      {
        ListAnswer = new SettingSectionsAnswer { Sections = new[] { Summary() } },
        SectionAnswer = State(ChoiceField("log_level", "Log level", "Warning", false,
            "None", "Critical", "Error", "Warning", "Information", "Debug", "All"))
      };

      PluginSettingsViewModel screen = await ScreenOn(api);
      SettingFieldViewModel level = screen.Section!.Fields[0];

      level.Options.First(o => o.Name == "Debug").PickCommand.Execute(null);

      Assert.That(level.Chosen, Is.EqualTo("Debug"));
      Assert.That(level.Options.Where(o => o.IsOn).Select(o => o.Name), Is.EqualTo(new[] { "Debug" }),
          "one answer at a time - the row's promise is that pressing a name releases the rest");
      Assert.That(level.IsChanged, Is.True, "and the row knows it no longer holds what the server answered");
    }

    [Test]
    public async Task ASectionThatWasLeftAloneSendsNothingAtAll()
    {
      FakeSectionsApi api = new FakeSectionsApi
      {
        ListAnswer = new SettingSectionsAnswer { Sections = new[] { Summary() } },
        SectionAnswer = State(NumberField("max_banners", "Banners", 4),
            ToggleField("keep_thumbs", "Keep the small ones", false))
      };

      PluginSettingsViewModel screen = await ScreenOn(api);
      SettingSectionViewModel section = screen.Section!;

      await section.SaveAsync();

      Assert.That(api.Calls.Count(c => c.StartsWith("put")), Is.Zero,
          "a section nobody edited writes nothing - a save that resent the whole section could overwrite "
          + "what another television saved in between");
      Assert.That(section.Status, Does.Contain("Nothing was changed"));
    }

    [Test]
    public async Task AChangedNumberSendsThatFieldAndOnlyThatField()
    {
      FakeSectionsApi api = new FakeSectionsApi
      {
        ListAnswer = new SettingSectionsAnswer { Sections = new[] { Summary() } },
        SectionAnswer = State(NumberField("max_banners", "Banners", 4),
            NumberField("max_backdrops", "Backdrops", 3),
            ToggleField("keep_thumbs", "Keep the small ones", true))
      };

      PluginSettingsViewModel screen = await ScreenOn(api);
      SettingSectionViewModel section = screen.Section!;
      section.Fields[1].NumberText = "9";

      await section.SaveAsync();

      Assert.That(api.SentKey, Is.EqualTo("fanart"));
      Assert.That(api.Sent!.Values.Keys, Is.EqualTo(new[] { "max_backdrops" }),
          "the wish is the difference and nothing else");
      Assert.That(api.Sent.Values["max_backdrops"], Is.EqualTo(9), "typed text goes back as a whole number");
      Assert.That(section.Status, Is.EqualTo("Saved on the server."));
    }

    [Test]
    public async Task AFlippedSwitchSendsItselfAsOnOrOff()
    {
      FakeSectionsApi api = new FakeSectionsApi
      {
        ListAnswer = new SettingSectionsAnswer { Sections = new[] { Summary() } },
        SectionAnswer = State(ToggleField("keep_thumbs", "Keep the small ones", false),
            NumberField("max_banners", "Banners", 4))
      };

      PluginSettingsViewModel screen = await ScreenOn(api);
      SettingSectionViewModel section = screen.Section!;
      section.Fields[0].Toggle = true;

      await section.SaveAsync();

      Assert.That(api.Sent!.Values.Keys, Is.EqualTo(new[] { "keep_thumbs" }));
      Assert.That(api.Sent.Values["keep_thumbs"], Is.EqualTo(true));
    }

    /// <summary>
    /// The country rows are the screen's only free text, so they are where the box has to behave: what comes
    /// back is the word without the spaces somebody typed around it, and a box somebody emptied on purpose is
    /// an answer of its own - the ratings fall back to the importer's - not a row that was left alone.
    /// </summary>
    [Test]
    public async Task AWordGoesBackAsAWordAndAnEmptiedBoxGoesBackToo()
    {
      FakeSectionsApi api = new FakeSectionsApi
      {
        ListAnswer = new SettingSectionsAnswer { Sections = new[] { Summary("certifications", "Certificates") } },
        SectionAnswer = State("certifications",
            TextField("display_movie_certification_country", "Movies", "DE"),
            TextField("display_series_certification_country", "Series", "DE"))
      };

      PluginSettingsViewModel screen = await ScreenOn(api);
      SettingSectionViewModel section = screen.Section!;

      section.Fields[0].Words = "  US  ";
      await section.SaveAsync();

      Assert.That(api.Sent!.Values.Keys, Is.EqualTo(new[] { "display_movie_certification_country" }));
      Assert.That(api.Sent.Values["display_movie_certification_country"], Is.EqualTo("US"),
          "what the server wanted was a country, so the spaces around it are not sent along");

      api.Sent = null;
      section.Fields[1].Words = "";
      await section.SaveAsync();

      Assert.That(api.Sent!.Values.Keys, Is.EqualTo(new[] { "display_series_certification_country" }),
          "a box emptied on purpose is a change, not a row that was left alone");
      Assert.That(api.Sent.Values["display_series_certification_country"], Is.EqualTo(""));
    }

    [Test]
    public async Task ANumberOutsideItsRangeIsRefusedByItsRowAndNeverLeavesTheScreen()
    {
      FakeSectionsApi api = new FakeSectionsApi
      {
        ListAnswer = new SettingSectionsAnswer { Sections = new[] { Summary() } },
        SectionAnswer = State(NumberField("max_banners", "Banners", 4))
      };

      PluginSettingsViewModel screen = await ScreenOn(api);
      SettingSectionViewModel section = screen.Section!;
      section.Fields[0].NumberText = "900";

      Assert.That(section.Fields[0].Error, Does.Contain("Between 1 and 20"),
          "the row says the range it refused the number against, where the number was typed");

      await section.SaveAsync();

      Assert.That(api.Calls.Count(c => c.StartsWith("put")), Is.Zero,
          "one bad row stops the whole change before the wire, which is what the server would do anyway");
      Assert.That(section.Status, Does.Contain("Nothing was sent"));
      Assert.That(section.Status, Does.Contain("Banners"), "and the sentence says which row stopped it");
    }

    [Test]
    public async Task HalfTypedTextIsRefusedByItsRowToo()
    {
      FakeSectionsApi api = new FakeSectionsApi
      {
        ListAnswer = new SettingSectionsAnswer { Sections = new[] { Summary() } },
        SectionAnswer = State(NumberField("max_banners", "Banners", 4))
      };

      PluginSettingsViewModel screen = await ScreenOn(api);
      SettingFieldViewModel banners = screen.Section!.Fields[0];
      banners.NumberText = "fo";

      Assert.That(banners.Error, Does.Contain("whole number"));

      await screen.Section!.SaveAsync();
      Assert.That(api.Calls.Count(c => c.StartsWith("put")), Is.Zero);
    }

    [Test]
    public async Task ASaveAdoptsWhatTheServerAnswersRatherThanWhatWasTyped()
    {
      // A server with a clamp, or with another editor in the house, answers a save with the value it stored
      // - which is not always the one that was asked for. The screen shows the stored one.
      FakeSectionsApi api = new FakeSectionsApi
      {
        ListAnswer = new SettingSectionsAnswer { Sections = new[] { Summary() } },
        SectionAnswer = State(NumberField("max_banners", "Banners", 4)),
        PutAnswer = State(NumberField("max_banners", "Banners", 20))
      };

      PluginSettingsViewModel screen = await ScreenOn(api);
      SettingSectionViewModel section = screen.Section!;
      section.Fields[0].NumberText = "20";

      await section.SaveAsync();

      Assert.That(section.Fields[0].NumberText, Is.EqualTo("20"));
      Assert.That(section.Fields[0].IsChanged, Is.False,
          "what the server stored is now what the screen holds, so nothing stands changed any more");
      Assert.That(section.HasChanges, Is.False);
    }

    [Test]
    public async Task AStoredFieldThatWaitsForARestartSaysSoAndNamesItself()
    {
      FakeSectionsApi api = new FakeSectionsApi
      {
        ListAnswer = new SettingSectionsAnswer { Sections = new[] { Summary("logging", "How much the server writes down", "Server", 1) } },
        SectionAnswer = State("logging",
            ChoiceField("log_level", "Log level", "Warning", true, "None", "Warning", "Debug")),
        PutAnswer = new SettingSectionState
        {
          Key = "logging",
          Title = "How much the server writes down",
          Owner = "Server",
          Fields = new[] { ChoiceField("log_level", "Log level", "Debug", true, "None", "Warning", "Debug") },
          RestartNeededFor = new List<string> { "log_level" }
        }
      };

      PluginSettingsViewModel screen = new PluginSettingsViewModel(() => api);
      await screen.ReloadAsync();
      await screen.OpenAsync(screen.Sections.First());
      SettingSectionViewModel section = screen.Section!;

      Assert.That(section.Fields[0].RestartNeeded, Is.True,
          "a read says where it is too: the note stands under the row, not only in the line at the bottom");
      Assert.That(section.Fields[0].RestartNote, Does.Contain("start again"));

      section.Fields[0].Options.First(o => o.Name == "Debug").PickCommand.Execute(null);
      await section.SaveAsync();

      Assert.That(section.RestartNote, Does.Contain("Log level"),
          "a stored-but-inert change is admitted, and named by the word the row was showing");
      Assert.That(section.RestartNote, Does.Not.Contain("log_level"),
          "the key underneath is what the two machines agree on, not what a person should read off a television");
      Assert.That(section.Status, Does.Contain("restart"));
      Assert.That(section.Fields[0].Chosen, Is.EqualTo("Debug"), "and the new value stands picked");
    }

    [Test]
    public async Task ARefusedSaveKeepsWhatWasTypedAndSaysWhoseNoItWas()
    {
      FakeSectionsApi api = new FakeSectionsApi
      {
        ListAnswer = new SettingSectionsAnswer { Sections = new[] { Summary() } },
        SectionAnswer = State(NumberField("max_banners", "Banners", 4)),
        ThrowOnPut = new ServerApiException(new Uri("http://server/api/media/settings/sections/fanart"),
            System.Net.HttpStatusCode.BadRequest, "{\"error\":\"no\"}")
      };

      PluginSettingsViewModel screen = await ScreenOn(api);
      SettingSectionViewModel section = screen.Section!;
      section.Fields[0].NumberText = "7";

      await section.SaveAsync();

      Assert.That(section.Status, Does.Contain("rejected"), "400 is the server's refusal, in its words");
      Assert.That(section.Fields[0].NumberText, Is.EqualTo("7"), "and the form is left as the viewer left it");
      Assert.That(section.Fields[0].IsChanged, Is.True,
          "still changed, so the next save still means what the viewer meant");
    }

    [Test]
    public async Task ASectionThatWillNotReadIsNotShownAsAnEmptyForm()
    {
      FakeSectionsApi api = new FakeSectionsApi
      {
        ListAnswer = new SettingSectionsAnswer { Sections = new[] { Summary() } },
        ThrowOnGet = new ServerApiException(new Uri("http://server/api/media/settings/sections/fanart"),
            System.Net.HttpStatusCode.ServiceUnavailable, "{\"error\":\"no settings manager\"}")
      };
      PluginSettingsViewModel screen = new PluginSettingsViewModel(() => api);
      await screen.ReloadAsync();

      await screen.OpenAsync(screen.Sections.First());

      Assert.That(screen.HasSection, Is.False, "the list stays up rather than drawing an editor of nothing");
      Assert.That(screen.Status, Does.Contain("could not be reached").Or.Contain("refused"));
    }

    [Test]
    public async Task AKindThisScreenCannotDrawIsSaidAndNeverSent()
    {
      FakeSectionsApi api = new FakeSectionsApi
      {
        ListAnswer = new SettingSectionsAnswer { Sections = new[] { Summary() } },
        SectionAnswer = State(
            NumberField("max_banners", "Banners", 4),
            new SettingFieldState
            {
              Key = "colour_wash",
              Label = "Backdrop wash",
              Kind = "slider",
              Value = Json(0.5)
            })
      };

      PluginSettingsViewModel screen = await ScreenOn(api);
      SettingSectionViewModel section = screen.Section!;

      Assert.That(section.Status, Does.Contain("cannot draw"),
          "a server speaking a word the client has never heard is reported, not crashed on");
      Assert.That(section.Fields[1].IsUndrawable, Is.True);
      Assert.That(section.Fields[1].IsChanged, Is.False, "and an undrawable row is never a change");

      section.Fields[0].NumberText = "6";
      await section.SaveAsync();

      Assert.That(api.Sent!.Values.Keys, Is.EqualTo(new[] { "max_banners" }),
          "the row the screen does understand still saves, on its own");
    }

    [Test]
    public async Task GoingBackReadsTheSummariesAgainSoTheirCountsAreUpToDate()
    {
      FakeSectionsApi api = new FakeSectionsApi
      {
        ListAnswer = new SettingSectionsAnswer { Sections = new[] { Summary("fanart", "FanArt", "Media library", 7) } },
        SectionAnswer = State(NumberField("max_banners", "Banners", 4))
      };

      PluginSettingsViewModel screen = await ScreenOn(api);
      Assert.That(api.Calls, Is.EqualTo(new[] { "sections", "get:fanart" }));

      await screen.BackAsync();

      Assert.That(screen.HasSection, Is.False, "Back puts the list back");
      Assert.That(api.Calls, Is.EqualTo(new[] { "sections", "get:fanart", "sections" }),
          "and reads the summaries again, so a count beside a section is the server's count now");
      Assert.That(screen.IsBusy, Is.False, "and the screen is done talking when it lands");
    }
  }
}
