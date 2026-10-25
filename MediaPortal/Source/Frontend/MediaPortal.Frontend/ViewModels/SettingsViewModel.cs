using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One stop in the settings screen: a name, who owns the question it answers, and what opens when it is
  /// picked.
  /// </summary>
  /// <remarks>
  /// A section is one of two things, exactly as the reference's configuration tree was. Either it is a
  /// <b>leaf</b> - a screen of its own inside the settings window, or a hand-over to somebody else - or it is a
  /// <b>folder</b>, a place with pages underneath it, which is what <see cref="Pages"/> answers. The reference
  /// knew both: <c>/Configuration/Settings</c> stood over <c>Shares</c> and <c>Media</c>, and <c>Media</c> stood
  /// over its eight reader and picture pages (ServerSettings.Client/plugin.xml:93). Picking a leaf opens a
  /// screen; picking a folder opens the wall of its pages, which is the difference a viewer notices between a
  /// settings screen with thirty rows in it and one with eight doors.
  ///
  /// What still hands the pick away is leaving the application: the trail and the back key belong to the close
  /// screen, not to the hub. The server address used to be carried away the same way, to the connection
  /// screen; it stands inside the hub now like every other section.
  /// </remarks>
  public sealed class SettingsSection
  {
    /// <param name="open">
    /// What the pick hands over to somebody else instead of a pane - today only the close screen. Last, and
    /// named where it is used, because a plate's picture is said far more often than a hand-over is.
    /// </param>
    public SettingsSection(string name, string owner, string description, Func<Control?>? build,
        string picture = "", string icon = "", Action? open = null)
    {
      Name = name;
      Owner = owner;
      Description = description;
      Build = build;
      Open = open;
      Picture = picture;
      Icon = icon.Length > 0 ? icon : picture;
    }

    /// <summary>
    /// A place with pages underneath. The pages are asked for when the door is opened rather than carried in a
    /// list, because a folder's pages are frequently somebody else's answer - the media folder's are the
    /// server's sections - and a table here would only ever be that answer as it used to be.
    /// </summary>
    public SettingsSection(string name, string owner, string description,
        Func<Task<IReadOnlyList<SettingsSection>>> pages, string picture = "", string icon = "")
    {
      Name = name;
      Owner = owner;
      Description = description;
      Pages = pages;
      Picture = picture;
      Icon = icon.Length > 0 ? icon : picture;
    }

    /// <summary>The line on the list.</summary>
    public string Name { get; }

    /// <summary>Whose question this is: the front end's own, or the server's.</summary>
    public string Owner { get; }

    /// <summary>The one sentence under the name.</summary>
    public string Description { get; }

    /// <summary>
    /// The picture this section's plate asks the worn skin for, spelled the way the reference spelled it: the
    /// section's place in the configuration tree, as its pictures were named after it. The reference's
    /// <c>SettingsMenuItemDataTemplate</c> (OtherControlsOverrides.xaml:648) asked
    /// <c>SettingsMenu\{path}.jpg</c> of the skin and the skin shipped the answer under exactly that name
    /// (<c>Media_MovieMDESettings.jpg</c>, <c>Appearance_SkinSettings.jpg</c>, ...). Empty for a section the
    /// reference never had a plate for, which leaves the plate bare.
    /// </summary>
    public string Picture { get; }

    /// <summary>
    /// The small picture inside the plate's name band, the reference's <c>Icon_{path}.png</c>
    /// (OtherControlsOverrides.xaml:671). Where the tree's own picture has no icon of its own - the reference
    /// shipped <c>Icon_General_System.png</c> but none for its <c>General_System_Diagnostics</c> child - a
    /// section says so by naming the parent's icon here; naming none asks for the plate's own name, and a
    /// section with no picture at all asks for nothing, which leaves the band carrying only its name.
    /// </summary>
    public string Icon { get; }

    /// <summary>The screen this section opens inside the hub, or <c>null</c> when <see cref="Open"/> does the work.</summary>
    public Func<Control?>? Build { get; }

    /// <summary>What the pick does when it is not an embedded screen - pushes the close screen, today.</summary>
    public Action? Open { get; }

    /// <summary>The pages underneath this place, or <c>null</c> for a section that is a screen itself.</summary>
    public Func<Task<IReadOnlyList<SettingsSection>>>? Pages { get; }

    /// <summary>Whether picking this opens a wall of pages rather than a screen.</summary>
    public bool IsFolder => Pages != null;
  }

  /// <summary>
  /// The front end's settings as the reference had them: not one question about colours, but a list of the
  /// places a household configures - its own machine and the server behind it - each opening its own screen.
  /// </summary>
  /// <remarks>
  /// The hub deliberately owns none of the answers. Every section carries a screen that was already a screen
  /// on its own - the theme picker, the folders of the library, the connection - so the settings list is the
  /// door to them rather than a copy of them, and what a viewer changes there is the same screen with the
  /// same checks that changed it before.
  /// </remarks>
  /// <summary>
  /// The front end's settings as the reference had them: not one question about colours, but a list of the
  /// places a household configures - its own machine and the server behind it - each opening its own screen,
  /// and a place with pages underneath opening the wall of those pages first.
  /// </summary>
  /// <remarks>
  /// The hub deliberately owns none of the answers. Every section carries a screen that was already a screen
  /// on its own - the theme picker, the folders of the library, the connection - so the settings list is the
  /// door to them rather than a copy of them, and what a viewer changes there is the same screen with the
  /// same checks that changed it before.
  ///
  /// <b>The two shapes of a level.</b> A level with nothing open is a wall: the pages of it standing as tiles,
  /// the way the reference's <c>master_configuration_menu.xaml</c> switched its one list between
  /// <c>SettingsMenuListViewStyle</c> (a wrap of tiles) and <c>MainMenuListViewStyle</c> (the narrowed list) on
  /// the single question of whether contents were showing (:143-147). A level with a leaf open is the narrowed
  /// list beside that leaf's screen, which is what this hub looked like before it had walls at all.
  /// <see cref="ShowingContents"/> is this front end's <c>Show_Contents</c>.
  /// </remarks>
  public partial class SettingsViewModel : ObservableObject
  {
    /// <summary>The places of the first level, kept so that walking back out of a folder ends somewhere.</summary>
    private readonly List<SettingsSection> _topLevel = new List<SettingsSection>();

    /// <summary>
    /// The way back: for every folder the viewer walked into, the level they were standing on before walking
    /// in. The entries are kept rather than asked for again - a level the viewer is looking at is a wall of
    /// doors that already opened once, and coming back to a wall that has to be rebuilt is a wall that is blank
    /// for as long as the answer takes.
    /// </summary>
    private readonly List<(SettingsSection Folder, IReadOnlyList<SettingsSection> Level)> _trail =
        new List<(SettingsSection, IReadOnlyList<SettingsSection>)>();

    /// <summary>The level's own entries, remembered so that coming back does not ask anybody again.</summary>
    private IReadOnlyList<SettingsSection> _level;

    /// <summary>
    /// Counts the openings, because a folder answers its pages later and a viewer who picked something else in
    /// the meantime is not interested in the answer that arrives after their newer pick.
    /// </summary>
    private int _openToken;

    /// <summary>
    /// @param level The entries of the level to start on. Left out, the hub starts empty and fills up from
    /// <see cref="Add"/> and <see cref="AddFolder"/>, which is how the application builds it.
    /// </summary>
    public SettingsViewModel()
    {
      // The first level is the application's own table, kept apart from the bound list so that walking into a
      // door and back out again refills the list from it rather than from what the list happened to hold last.
      _level = _topLevel;
    }

    /// <summary>
    /// The places of the level the viewer is looking at. Named <c>Sections</c> because that is what the list
    /// showed before there were levels and what its template still binds; a folder's pages are sections of
    /// their own, so a level is spelled with the same list and the hub can be walked without a second kind of
    /// entry.
    /// </summary>
    /// <remarks>
    /// <b>A new list for a new level.</b> Walking into a door hands over a fresh collection rather than emptying
    /// the one on screen and refilling it. A list on screen is not only its items: the control holding it has
    /// containers out of the old level and - worse - a <i>number</i>, the place it last pressed, and Avalonia's
    /// selection model keeps that number across a cleared source. The next press then walks back to a number the
    /// level it is showing never had, and the answer is not a wrong selection but
    /// <c>ArgumentOutOfRangeException</c> inside <c>SelectedItems</c>, unhandled, which took the whole client
    /// down when a viewer walked from nine doors onto a door's seven pages. Replacing the collection leaves the
    /// old level's containers and numbers with the old level; <see cref="LevelChanged"/> is what tells the lists
    /// so.
    /// </remarks>
    public ObservableCollection<SettingsSection> Sections { get; private set; } =
        new ObservableCollection<SettingsSection>();

    /// <summary>
    /// Raised when the level on screen is another collection than it was, which is the one moment a list has a
    /// number of its own to forget. The view listens, because the number is a control's business, not the
    /// hub's.
    /// </summary>
    public event Action? LevelChanged;

    [ObservableProperty]
    private SettingsSection? _selected;

    [ObservableProperty]
    private Control? _currentView;

    [ObservableProperty]
    private string _status = string.Empty;

    /// <summary>Whether one screen stands beside the list: the hub's answer to the reference's <c>Show_Contents</c>.</summary>
    [ObservableProperty]
    private bool _showingContents;

    /// <summary>Where the viewer is, spelled the way the reference spelled its <c>CurrentLocation</c>.</summary>
    [ObservableProperty]
    private string _location = "Settings";

    /// <summary>Whether the level is standing as a wall of tiles - which is every time nothing is open.</summary>
    public bool IsWall => !ShowingContents;

    /// <summary>
    /// Whether the back key has something to do here: a screen to fold away, or a folder to walk out of. The
    /// reference let the back key leave the configuration screen altogether; here the same key stops at the
    /// first level and the shell takes it from there, which is what <see cref="GoBack"/> answering <c>false</c>
    /// is for.
    /// </summary>
    public bool CanGoBack => ShowingContents || _trail.Count > 0;

    partial void OnShowingContentsChanged(bool value)
    {
      OnPropertyChanged(nameof(IsWall));
      OnPropertyChanged(nameof(CanGoBack));
    }

    /// <summary>
    /// Add one place to the first level. The application builds the list, because only it knows what a section's
    /// screen needs: the theme picker needs the application to change colours in, the folders need the client
    /// the library was read with.
    /// </summary>
    public void Add(string name, string owner, string description, Func<Control?>? build,
        string picture = "", string icon = "", Action? open = null)
    {
      AddLevel(new SettingsSection(name, owner, description, build, picture, icon, open));
    }

    /// <summary>
    /// Add one <b>place with pages</b> to the first level. Picking it shows the wall of its pages instead of a
    /// screen, so the pages are asked for when the door is opened - which is also when the server is asked what
    /// it has to be configured about.
    /// </summary>
    public void AddFolder(string name, string owner, string description,
        Func<Task<IReadOnlyList<SettingsSection>>> pages, string picture = "", string icon = "")
    {
      AddLevel(new SettingsSection(name, owner, description, pages, picture, icon));
    }

    /// <summary>
    /// The pages of a door whose pages this front end already knows - a door standing over its own screens
    /// rather than over somebody else's answer - spelled without anybody having to write a task by hand. A door
    /// over the server's sections answers with a real ask instead, so that both doors look the same from the
    /// outside and only one of them waits for anybody.
    /// </summary>
    public static Task<IReadOnlyList<SettingsSection>> PagesHere(params SettingsSection[] pages) =>
        Task.FromResult<IReadOnlyList<SettingsSection>>(pages);

    /// <summary>Putting one entry on the first level, which is the only level anybody adds to.</summary>
    private void AddLevel(SettingsSection section)
    {
      _topLevel.Add(section);
      if (!ReferenceEquals(_level, _topLevel))
        return;
      Sections.Add(section);
    }

    partial void OnSelectedChanged(SettingsSection? value)
    {
      if (value == null)
        return;

      // The pick is answered a step later, not on the spot. A press arrives while the list is still inside its
      // own selection work (SelectingItemsControl.UpdateSelectionFromEvent -> SelectionModel.CommitOperation),
      // and answering it can hand over another level - a door's pages - which replaces the very collection the
      // list is enumerating to finish its press. Avalonia's answer to that is an ArgumentOutOfRangeException
      // out of SelectedItems on the UI thread, which closed the client the moment the "Media library" door was
      // pressed - a door that answers at once, because two of its pages are this front end's own. Posting the
      // pick lets the list finish pressing before the hub changes what it is standing on.
      //
      // A headless window does not feel the difference - only the real backend enumerates a selection while
      // the press is still being committed - so the crash was measured on the television and only there can its
      // absence be measured again. What the tests of this hub hold is what the step later has to keep: the
      // door's pages on screen, the old level's numbers forgotten, and the way back where it was.
      Dispatcher.UIThread.Post(delegate
      {
        _ = OpenAsync(value);
      }, DispatcherPriority.Input);
    }

    /// <summary>
    /// Working out what a picked place means: a hand-over, a folder's wall of pages, or a screen beside the
    /// list. Awaited by the view's own opening and by every pick; public because a screen with no pointer behind
    /// it opens a place by calling this.
    /// </summary>
    public async Task OpenAsync(SettingsSection section)
    {
      // Every pick counts, not only a pick of a door: a plate pressed beside a door whose pages are still on
      // their way is the viewer saying what they actually came for.
      int token = ++_openToken;

      if (section.Open != null)
      {
        // Somebody else's screen, in the workflow proper - the trail and the back key belong to it, not to the
        // hub. The hub keeps showing the section it showed before, because that is where the back key will
        // land the viewer again.
        Status = string.IsNullOrEmpty(section.Description)
            ? section.Name
            : section.Name + ": " + section.Description;
        section.Open();
        return;
      }

      if (section.Pages != null)
      {
        await OpenFolderAsync(section, token).ConfigureAwait(true);
        return;
      }

      Control? view = section.Build == null ? null : section.Build();
      if (view == null)
      {
        Status = section.Name + " is not available on this machine";
        return;
      }

      CurrentView = view;
      ShowingContents = true;
      Status = string.Empty;
    }

    /// <summary>
    /// Walking into a place: its pages become the level, the walked doors remember the way back, and the wall
    /// of the pages is what the viewer sees. Nothing is open, because a door has no screen of its own - that
    /// is the whole reason it is a door.
    /// </summary>
    /// <remarks>
    /// <b>The one page is opened, not stood in front of.</b> A door that holds exactly one page shows no wall:
    /// the page is opened and the door is walked through in the same press. That is the reference's
    /// <c>SkipForSingleItem</c> saying what a screen with a single item on it is worth
    /// (<c>AbstractScreenData.cs:94</c>, worked by <c>MediaNavigationModel.cs:470</c>, which hands the
    /// workflow the single item's own command through <c>KEY_NAVIGATION_SKIP_ACTION</c> instead of showing the
    /// screen) - a wall of one plate asks the viewer to count to one. The skipped level is never entered, so
    /// the back key lands on the level the door stood on, which is where the viewer came from.
    /// </remarks>
    /// <param name="token">The pick this answer belongs to; anything newer on screen outranks it.</param>
    private async Task OpenFolderAsync(SettingsSection folder, int token)
    {
      IReadOnlyList<SettingsSection> pages;

      try
      {
        pages = await folder.Pages().ConfigureAwait(true) ?? new List<SettingsSection>();
      }
      catch (Exception ex)
      {
        if (token != _openToken)
          return;

        // The pages are somebody else's answer, and an answer that does not come is said rather than
        // pretended: the viewer stays on the level they were on, where everything still works.
        Status = folder.Name + " could not be opened: " + ex.Message;
        return;
      }

      if (token != _openToken)
        return; // something newer was picked while this was on its way

      if (pages.Count == 1)
      {
        // The door has one page, so the door is not a level: what the page is - another door, a screen, or a
        // hand-over - decides what happens, and this level stays on screen behind it.
        await OpenAsync(pages[0]).ConfigureAwait(true);
        return;
      }

      _trail.Add((folder, _level));
      ShowLevel(pages);
      Status = pages.Count == 0
          ? folder.Name + " has nothing to open at the moment."
          : string.Format("{0} opens {1} page(s).", folder.Name, pages.Count);
    }

    /// <summary>
    /// Putting a level's entries on the screen. The old selection goes first, because the list is bound to it
    /// twice over and a level whose first entry is still "selected" would open a screen of the level the viewer
    /// just left - and the entries come as a collection of their own, for what <see cref="Sections"/> says about
    /// the numbers a list keeps.
    /// </summary>
    private void ShowLevel(IReadOnlyList<SettingsSection> level)
    {
      Selected = null;
      CurrentView = null;
      ShowingContents = false;

      Sections = new ObservableCollection<SettingsSection>(level);
      OnPropertyChanged(nameof(Sections)); // the lists bind to the property, not to the collection it held
      _level = level;
      LevelChanged?.Invoke();

      var path = new System.Text.StringBuilder("Settings");
      foreach (var step in _trail)
        path.Append(" › ").Append(step.Folder.Name);
      Location = path.ToString();
      OnPropertyChanged(nameof(CanGoBack));
    }

    /// <summary>
    /// One press of the back key: out of a screen into the wall it came from, or out of a folder into the level
    /// above it. <c>false</c> is the hub saying it has nowhere left to go, which is the shell's cue to leave the
    /// settings altogether - the same rule the reference's workflow applied to its configuration states.
    /// </summary>
    public bool GoBack()
    {
      _openToken++; // an answer on its way belongs to a level the viewer has left

      if (ShowingContents)
      {
        ShowingContents = false;
        Selected = null;
        Status = string.Empty;
        return true;
      }

      if (_trail.Count == 0)
        return false;

      var step = _trail[_trail.Count - 1];
      _trail.RemoveAt(_trail.Count - 1);
      ShowLevel(step.Level);
      Status = string.Empty;
      return true;
    }
  }
}