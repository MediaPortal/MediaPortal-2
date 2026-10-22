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
    /// <param name="iconName">
    /// The icon the shortcut is known by, spelled as the skin files it under an images directory: slashes kept,
    /// extension dropped. The model says which glyph the shortcut is and leaves the face to say what it is made
    /// of - the shortcuts are vectors now, so <c>MediaMenu/genre</c> is answered out of the skin's <c>.svg</c>
    /// files, and a face that ever wants a bitmap again answers the same name out of its pictures.
    /// </param>
    public NereusHomeShortcut(string label, string iconName, string? browseBy = null, string? screenKey = null)
    {
      Label = label;
      IconName = iconName;
      BrowseBy = browseBy;
      ScreenKey = screenKey;
    }

    public string Label { get; }
    public string IconName { get; }
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
            new NereusHomeShortcut("Genre", "MediaMenu/genre", "genre"),
            new NereusHomeShortcut("Year", "MediaMenu/year", "year"),
            new NereusHomeShortcut("Age", "MediaMenu/age"),
            new NereusHomeShortcut("Actor", "MediaMenu/actor", "actors"),
            new NereusHomeShortcut("Search", "MediaMenu/search", screenKey: "search")
          },
          [Guid.Parse("30f57cba-459c-4202-a587-09fff5098251")] = new[]
          {
            new NereusHomeShortcut("Genre", "MediaMenu/genre", "genre"),
            new NereusHomeShortcut("Year", "MediaMenu/year", "year"),
            new NereusHomeShortcut("Age", "MediaMenu/age"),
            new NereusHomeShortcut("Actor", "MediaMenu/actor", "actors"),
            new NereusHomeShortcut("Search", "MediaMenu/search", screenKey: "search")
          },
          [Guid.Parse("30715d73-4205-417f-80aa-e82f0834171f")] = new[]
          {
            new NereusHomeShortcut("Genre", "MediaMenu/audiogenre", "genre"),
            new NereusHomeShortcut("Track", "MediaMenu/track"),
            new NereusHomeShortcut("Album", "MediaMenu/album"),
            new NereusHomeShortcut("Artist", "MediaMenu/artist", "actors"),
            new NereusHomeShortcut("Year", "MediaMenu/year", "year")
          },
          [Guid.Parse("55556593-9fe9-436c-a3b6-a971e10c9d44")] = new[]
          {
            new NereusHomeShortcut("Year", "MediaMenu/year", "year"),
            new NereusHomeShortcut("Location", "MediaMenu/place"),
            new NereusHomeShortcut("System", "MediaMenu/system"),
            new NereusHomeShortcut("Size", "MediaMenu/size"),
            new NereusHomeShortcut("Search", "MediaMenu/search", screenKey: "search")
          },
          [Guid.Parse("a4df2df6-8d66-479a-9930-d7106525eb07")] = new[]
          {
            new NereusHomeShortcut("Year", "MediaMenu/year", "year"),
            new NereusHomeShortcut("Location", "MediaMenu/place"),
            new NereusHomeShortcut("System", "MediaMenu/system"),
            new NereusHomeShortcut("Size", "MediaMenu/size"),
            new NereusHomeShortcut("Search", "MediaMenu/search", screenKey: "search")
          },
          [Guid.Parse("b4a9199f-6dd4-4bda-a077-de9c081f7703")] = new[]
          {
            new NereusHomeShortcut("Live TV", "MediaMenu/LiveTV", screenKey: "tv"),
            new NereusHomeShortcut("EPG", "MediaMenu/EPG", screenKey: "tv"),
            new NereusHomeShortcut("Schedules", "MediaMenu/schedules", screenKey: "tv"),
            new NereusHomeShortcut("Recordings", "MediaMenu/Recordings", screenKey: "tv"),
            new NereusHomeShortcut("Search", "MediaMenu/SearchProgram", screenKey: "tv")
          },
          [Guid.Parse("e3bbc989-99db-40e9-a15f-ccb50b17a4c8")] = new[]
          {
            new NereusHomeShortcut("Live radio", "MediaMenu/LiveRadio", screenKey: "radio"),
            new NereusHomeShortcut("EPG", "MediaMenu/EPG", screenKey: "radio"),
            new NereusHomeShortcut("Schedules", "MediaMenu/schedules", screenKey: "radio"),
            new NereusHomeShortcut("Recordings", "MediaMenu/Recordings", screenKey: "radio"),
            new NereusHomeShortcut("Search", "MediaMenu/SearchProgram", screenKey: "radio")
          },
          [Guid.Parse("bb49a591-7705-408f-8177-45d633fdfad0")] = new[]
          {
            new NereusHomeShortcut("Setup", "MediaMenu/setup", screenKey: "news"),
            new NereusHomeShortcut("Refresh", "MediaMenu/refresh", screenKey: "news")
          },
          [Guid.Parse("e34fdb62-1f3e-4aa9-8a61-d143e0af77b5")] = new[]
          {
            new NereusHomeShortcut("Location", "MediaMenu/location", screenKey: "weather"),
            new NereusHomeShortcut("Setup", "MediaMenu/setup", screenKey: "weather"),
            new NereusHomeShortcut("Refresh", "MediaMenu/refresh", screenKey: "weather")
          }
        };

    private readonly MainMenuEntry _otherPluginsEntry = new MainMenuEntry(
        null, null, HomeViewModel.OTHER_PLUGINS_HEADING, Array.Empty<string>(), "All other plugins.", null,
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
