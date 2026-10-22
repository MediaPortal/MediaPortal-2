using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Skins.Nereus
{
  /// <summary>
  /// What the home menu editor stands on: the model that holds the tiles and their lists, and the one moment the
  /// answer is written to disk.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Nereus wrote the editor's result in exactly one place - <c>HomeMenuModel.SaveMenuEdit</c>, called when the
  /// main dialog was closed with OK - and both settings went out together in that one write: the tiles in their
  /// order, and every tile's choice of media lists. Nothing was written while the viewer was still moving tiles
  /// about, which is why a home menu that was opened, played with and left produced no file at all. This class is
  /// that one moment.
  /// </para>
  /// <para>
  /// It stands in the skin's own folder because the editor is the skin's own: no other face has this menu, no
  /// other face's file is written here, and the words the dialogs are headed with came out of Nereus' language
  /// file. The catalogue of tiles it edits is handed in from outside, because the entries this front end can
  /// offer are the home screen's business.
  /// </para>
  /// </remarks>
  public sealed partial class NereusMenuEditViewModel : ObservableObject
  {
    /// <summary>
    /// The skin whose file the editor writes. The editor cannot be reached while another face is worn, and the
    /// reference wrote its menu into its own settings and nowhere else, so this says what it does rather than
    /// guessing from the television.
    /// </summary>
    private const string EDITORS_SKIN = "nereus";

    private readonly Action? _saved;
    private readonly string? _settingsPath;

    /// <param name="available">
    /// Every tile the front end can offer, which is the whole catalogue the viewer may pick from - not only the
    /// ones currently on the menu.
    /// </param>
    /// <param name="startActionId">The tile the viewer was standing on when the editor was asked for.</param>
    /// <param name="saved">
    /// Told once, after a write that worked, so the home screen can lay its menu out again. The reference needed no
    /// such notice: its settings watcher saw the change and relaid the menu by itself, and this port's watcher is
    /// the home screen's own method.
    /// </param>
    /// <param name="settingsPath">
    /// The file to read and write, or null for the one the signed-in viewer's Nereus answers live in. Only tests
    /// hand one over.
    /// </param>
    public NereusMenuEditViewModel(IEnumerable<MainMenuEntry> available, Guid? startActionId,
        Action? saved = null, string? settingsPath = null)
    {
      _saved = saved;
      _settingsPath = settingsPath;

      NereusSkinSettings? settings =
          SkinSettingsBase.LoadFor(EDITORS_SKIN, _settingsPath, out string? problem) as NereusSkinSettings;
      Model = new NereusMenuEditModel(available,
          settings?.HomeMenuActionIds ?? NereusSkinSettings.DefaultHomeMenuActionIds,
          startActionId,
          settings?.HomeMenuActionMediaLists);
      Status = problem ?? "";
    }

    /// <summary>The tiles, their order, their lists, and everything that can be done to them.</summary>
    public NereusMenuEditModel Model { get; }

    /// <summary>
    /// What the last write said, or the reason this viewer's Nereus answers could not be read at all. The dialogs
    /// show it as their small print, because a menu that silently did not move is worse than one that says it
    /// could not be written.
    /// </summary>
    [ObservableProperty]
    private string _status = "";

    /// <summary>
    /// The word the dialog's own head band carries: "Edit menu", "Add menu item", and the two lists' names.
    /// The reference spelled these on the dialog master (<c>Dialog_Header_Text</c>) rather than on the dialog,
    /// and the master drew them centred in white on the accent band - so the heading belongs to what the screen
    /// is on about, and the frame is the one that paints it.
    /// </summary>
    [ObservableProperty]
    private string _heading = "";

    /// <summary>
    /// Closes the editor with the answer kept: both settings go out in one write, the way the reference wrote
    /// them, and the home screen is told afterwards so it can lay the new menu out.
    /// </summary>
    public string Save()
    {
      if (SkinSettingsBase.LoadFor(EDITORS_SKIN, _settingsPath, out string? problem)
          is not NereusSkinSettings settings)
      {
        Status = problem ?? "the skin's own answers could not be read, so the menu has nowhere to be written";
        return Status;
      }

      settings.HomeMenuActionIds = Model.CurrentActionIds().ToArray();
      settings.HomeMenuActionMediaLists = Model.MediaListSettings();
      Status = settings.Save(_settingsPath, out bool saved);
      if (saved)
        _saved?.Invoke();
      return Status;
    }

    /// <summary>
    /// Closes one tile's list of media lists with the choice kept, in memory only. The reference did the same: the
    /// list dialog's OK stored the answer in the editor, and only the editor's own OK put it on disk.
    /// </summary>
    public void SaveMediaListEdit()
    {
      Model.SaveMediaListEdit();
    }
  }
}
