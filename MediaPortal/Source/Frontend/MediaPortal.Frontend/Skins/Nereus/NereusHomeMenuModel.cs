using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Skins.Nereus
{
  /// <summary>A focusable shortcut shown before the selected Nereus shelf's media groups.</summary>
  public sealed class NereusHomeShortcut
  {
    public NereusHomeShortcut(string label, string imagePath, string? browseBy = null, string? screenKey = null)
    {
      Label = label;
      ImagePath = imagePath;
      BrowseBy = browseBy;
      ScreenKey = screenKey;
    }

    public string Label { get; }
    public string ImagePath { get; }
    public string? BrowseBy { get; }
    public string? ScreenKey { get; }
  }

  /// <summary>
  /// Nereus' home menu: configured actions in configured order, followed by its entry for the remaining actions.
  /// </summary>
  public sealed class NereusHomeMenuModel
  {
    public static readonly Guid OTHER_PLUGINS_ACTION_ID = new Guid("1DC6BE8F-2321-4274-9789-0FF60E1BB19C");

    private static readonly IReadOnlyDictionary<Guid, NereusHomeShortcut[]> HOME_SHORTCUTS =
        new Dictionary<Guid, NereusHomeShortcut[]>
        {
          [Guid.Parse("80d2e2cc-baaa-4750-807b-f37714153751")] = new[]
          {
            new NereusHomeShortcut("Genre", "MediaMenu/genre.png", "genre"),
            new NereusHomeShortcut("Year", "MediaMenu/year.png", "year"),
            new NereusHomeShortcut("Age", "MediaMenu/age.png"),
            new NereusHomeShortcut("Actor", "MediaMenu/actor.png", "actors"),
            new NereusHomeShortcut("Search", "MediaMenu/search.png", screenKey: "search")
          },
          [Guid.Parse("30f57cba-459c-4202-a587-09fff5098251")] = new[]
          {
            new NereusHomeShortcut("Genre", "MediaMenu/genre.png", "genre"),
            new NereusHomeShortcut("Year", "MediaMenu/year.png", "year"),
            new NereusHomeShortcut("Age", "MediaMenu/age.png"),
            new NereusHomeShortcut("Actor", "MediaMenu/actor.png", "actors"),
            new NereusHomeShortcut("Search", "MediaMenu/search.png", screenKey: "search")
          },
          [Guid.Parse("30715d73-4205-417f-80aa-e82f0834171f")] = new[]
          {
            new NereusHomeShortcut("Genre", "MediaMenu/audiogenre.png", "genre"),
            new NereusHomeShortcut("Track", "MediaMenu/track.png"),
            new NereusHomeShortcut("Album", "MediaMenu/album.png"),
            new NereusHomeShortcut("Artist", "MediaMenu/artist.png", "actors"),
            new NereusHomeShortcut("Year", "MediaMenu/year.png", "year")
          },
          [Guid.Parse("55556593-9fe9-436c-a3b6-a971e10c9d44")] = new[]
          {
            new NereusHomeShortcut("Year", "MediaMenu/year.png", "year"),
            new NereusHomeShortcut("Location", "MediaMenu/place.png"),
            new NereusHomeShortcut("System", "MediaMenu/system.png"),
            new NereusHomeShortcut("Size", "MediaMenu/size.png"),
            new NereusHomeShortcut("Search", "MediaMenu/search.png", screenKey: "search")
          },
          [Guid.Parse("a4df2df6-8d66-479a-9930-d7106525eb07")] = new[]
          {
            new NereusHomeShortcut("Year", "MediaMenu/year.png", "year"),
            new NereusHomeShortcut("Location", "MediaMenu/place.png"),
            new NereusHomeShortcut("System", "MediaMenu/system.png"),
            new NereusHomeShortcut("Size", "MediaMenu/size.png"),
            new NereusHomeShortcut("Search", "MediaMenu/search.png", screenKey: "search")
          },
          [Guid.Parse("b4a9199f-6dd4-4bda-a077-de9c081f7703")] = new[]
          {
            new NereusHomeShortcut("Live TV", "MediaMenu/LiveTV.png", screenKey: "tv"),
            new NereusHomeShortcut("EPG", "MediaMenu/EPG.png", screenKey: "tv"),
            new NereusHomeShortcut("Schedules", "MediaMenu/schedules.png", screenKey: "tv"),
            new NereusHomeShortcut("Recordings", "MediaMenu/Recordings.png", screenKey: "tv"),
            new NereusHomeShortcut("Search", "MediaMenu/SearchProgram.png", screenKey: "tv")
          },
          [Guid.Parse("e3bbc989-99db-40e9-a15f-ccb50b17a4c8")] = new[]
          {
            new NereusHomeShortcut("Live radio", "MediaMenu/LiveRadio.png", screenKey: "radio"),
            new NereusHomeShortcut("EPG", "MediaMenu/EPG.png", screenKey: "radio"),
            new NereusHomeShortcut("Schedules", "MediaMenu/schedules.png", screenKey: "radio"),
            new NereusHomeShortcut("Recordings", "MediaMenu/Recordings.png", screenKey: "radio"),
            new NereusHomeShortcut("Search", "MediaMenu/SearchProgram.png", screenKey: "radio")
          },
          [Guid.Parse("bb49a591-7705-408f-8177-45d633fdfad0")] = new[]
          {
            new NereusHomeShortcut("Setup", "MediaMenu/setup.png", screenKey: "news"),
            new NereusHomeShortcut("Refresh", "MediaMenu/7AEB11DE-BA40-40a2-933A-B00BBD151B08.png", screenKey: "news")
          },
          [Guid.Parse("e34fdb62-1f3e-4aa9-8a61-d143e0af77b5")] = new[]
          {
            new NereusHomeShortcut("Location", "MediaMenu/65A9A5F6-CA50-4459-8E49-0DC614B7A2E8.png", screenKey: "weather"),
            new NereusHomeShortcut("Setup", "MediaMenu/setup.png", screenKey: "weather"),
            new NereusHomeShortcut("Refresh", "MediaMenu/7AEB11DE-BA40-40a2-933A-B00BBD151B08.png", screenKey: "weather")
          }
        };

    private readonly MainMenuEntry _otherPluginsEntry = new MainMenuEntry(
        null, null, "Other Plugins", Array.Empty<string>(), "other available actions", null,
        homeImageId: OTHER_PLUGINS_ACTION_ID.ToString("D"));

    public ObservableCollection<MainMenuEntry> MainMenuItems { get; } =
        new ObservableCollection<MainMenuEntry>();

    public ObservableCollection<MainMenuEntry> OtherMenuItems { get; } =
        new ObservableCollection<MainMenuEntry>();

    public bool IsOtherPluginsEntry(MainMenuEntry? entry)
    {
      return ReferenceEquals(entry, _otherPluginsEntry);
    }

    public bool ContainsMainEntry(MainMenuEntry? entry)
    {
      return entry != null && MainMenuItems.Contains(entry);
    }

    public IReadOnlyList<NereusHomeShortcut> ShortcutsFor(MainMenuEntry entry)
    {
      if (!Guid.TryParse(entry.HomeImageId, out Guid actionId)
          || !HOME_SHORTCUTS.TryGetValue(actionId, out NereusHomeShortcut[]? shortcuts))
        return Array.Empty<NereusHomeShortcut>();

      return shortcuts;
    }

    public void Rebuild(IEnumerable<MainMenuEntry> availableItems, IEnumerable<Guid> configuredActionIds)
    {
      List<MainMenuEntry> available = availableItems.ToList();
      HashSet<MainMenuEntry> included = new HashSet<MainMenuEntry>();
      HashSet<Guid> visitedActionIds = new HashSet<Guid>();

      MainMenuItems.Clear();
      OtherMenuItems.Clear();

      foreach (Guid actionId in configuredActionIds)
      {
        if (!visitedActionIds.Add(actionId))
          continue;

        MainMenuEntry? match = available.FirstOrDefault(item =>
            Guid.TryParse(item.HomeImageId, out Guid itemActionId) && itemActionId == actionId);
        if (match != null && included.Add(match))
          MainMenuItems.Add(match);
      }

      foreach (MainMenuEntry item in available)
      {
        if (!included.Contains(item))
          OtherMenuItems.Add(item);
      }

      MainMenuItems.Add(_otherPluginsEntry);
    }
  }
}
