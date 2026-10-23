using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One stop in the settings screen: a name, who owns the question it answers, and what opens when it is
  /// picked.
  /// </summary>
  /// <remarks>
  /// A section either shows a screen of its own inside the settings window or hands the pick to someone else.
  /// What still hands the pick away is leaving the application: the trail and the back key belong to the close
  /// screen, not to the hub. The server address used to be carried away the same way, to the connection
  /// screen; it stands inside the hub now like every other section.
  /// </remarks>
  public sealed class SettingsSection
  {
    public SettingsSection(string name, string owner, string description, Func<Control?>? build, Action? open = null)
    {
      Name = name;
      Owner = owner;
      Description = description;
      Build = build;
      Open = open;
    }

    /// <summary>The line on the list.</summary>
    public string Name { get; }

    /// <summary>Whose question this is: the front end's own, or the server's.</summary>
    public string Owner { get; }

    /// <summary>The one sentence under the name.</summary>
    public string Description { get; }

    /// <summary>The screen this section opens inside the hub, or <c>null</c> when <see cref="Open"/> does the work.</summary>
    public Func<Control?>? Build { get; }

    /// <summary>What the pick does when it is not an embedded screen - pushes the close screen, today.</summary>
    public Action? Open { get; }
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
  public partial class SettingsViewModel : ObservableObject
  {
    public ObservableCollection<SettingsSection> Sections { get; } = new ObservableCollection<SettingsSection>();

    [ObservableProperty]
    private SettingsSection? _selected;

    [ObservableProperty]
    private Control? _currentView;

    [ObservableProperty]
    private string _status = string.Empty;

    /// <summary>
    /// Add one place to the list. The application builds the list, because only it knows what a section's
    /// screen needs: the theme picker needs the application to change colours in, the folders need the client
    /// the library was read with.
    /// </summary>
    public void Add(string name, string owner, string description, Func<Control?>? build, Action? open = null)
    {
      Sections.Add(new SettingsSection(name, owner, description, build, open));
    }

    partial void OnSelectedChanged(SettingsSection? value)
    {
      if (value == null)
        return;

      if (value.Open != null)
      {
        // Somebody else's screen, in the workflow proper - the trail and the back key belong to it, not to the
        // hub. The hub keeps showing the section it showed before, because that is where the back key will
        // land the viewer again.
        Status = value.Name + ": " + value.Description;
        value.Open();
        return;
      }

      Control? view = value.Build == null ? null : value.Build();
      if (view == null)
      {
        Status = value.Name + " is not available on this machine";
        return;
      }

      CurrentView = view;
      Status = string.Empty;
    }
  }
}