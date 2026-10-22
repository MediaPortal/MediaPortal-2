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
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Skins.Nereus;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// The home content screen's buttons are clicked with a mouse, not called by hand.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The editor's model has been tested on its own and answers every one of its buttons, which is exactly why its
  /// own tests cannot catch what a viewer finds: a button that moves nothing still sits behind a face that looks
  /// right. What is walked here is the whole road a finger takes - the line the screen builds for one media list,
  /// the button inside that line, and the point of the screen that button covers - with the headless window's
  /// mouse, so hit testing, templates, bindings and commands all have to agree before the list moves.
  /// </para>
  /// <para>
  /// The screen is mounted the way the shell mounts it, in a window of the size the front end opens at, so a line
  /// whose row is narrower than its buttons or a face reaching outside its button is caught here as the
  /// unclickable thing a viewer would call broken rather than passing for looking right.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTheEditorButtonsAreAnswered
  {
    private static readonly Guid MOVIES = Guid.Parse("80d2e2cc-baaa-4750-807b-f37714153751");
    private static readonly Guid SERIES = Guid.Parse("30f57cba-459c-4202-a587-09fff5098251");

    private string _skinsFolderBefore = "";
    private readonly System.Collections.Generic.List<string> _files = new System.Collections.Generic.List<string>();

    [SetUp]
    public void RememberWhereSkinsComeFrom()
    {
      _skinsFolderBefore = SkinEngine.SkinsFolder;
    }

    [TearDown]
    public void PutEverythingBack()
    {
      foreach (string file in _files)
      {
        try
        {
          if (File.Exists(file))
            File.Delete(file);
        }
        catch (IOException)
        {
          // A leftover in the temp folder is untidy; a skin left on the application is not, and that is put back.
        }
      }
      _files.Clear();

      Application? app = Application.Current;
      if (app != null)
      {
        SkinEngine.Clear(app);
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
      }

      SkinEngine.SkinsFolder = _skinsFolderBefore;
    }

    private static void Pump()
    {
      for (int round = 0; round < 20; round++)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(5);
      }
    }

    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestTheEditorButtonsAreAnswered).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      string path = Path.GetFullPath(value!.Value!);
      Assert.That(Directory.Exists(path), Is.True, name + " points at '" + path + "', which is not a directory");
      return path;
    }

    private static void WearTheShippedSkin()
    {
      Application app = Application.Current!;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      FrontendTheme.Apply(app, "nereus", out _);
      Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");
    }

    /// <summary>
    /// The editor over the films tile, reading and writing a settings file of its own so that a test can never
    /// touch what the signed-in viewer arranged.
    /// </summary>
    private NereusMenuEditViewModel TheEditor()
    {
      string file = Path.Combine(Path.GetTempPath(),
          "mp2-clickedit-" + Guid.NewGuid().ToString("N") + ".json");
      _files.Add(file);

      return new NereusMenuEditViewModel(
          MainMenu.Build(Array.Empty<AspectViewModel>(), connected: false), null, null, file);
    }

    /// <summary>
    /// The editor opened on one tile's home content, mounted in a window the way the shell mounts a dialog. The
    /// tile is the films tile, which has four lists to arrange.
    /// </summary>
    private static NereusMenuEditView OpenHomeContent(NereusMenuEditViewModel editor, WorkflowManager workflow,
        NereusMenuEditScreen screen = NereusMenuEditScreen.Lists)
    {
      NereusMenuEditModel model = editor.Model;
      NereusMenuEditRow? tile = model.Rows.FirstOrDefault(row => row.ActionId == MOVIES);
      Assert.That(tile, Is.Not.Null, "the films tile is on the menu the editor was handed");

      Assert.That(model.Config(tile!), Is.True, "the films tile offers its lists to the editor");

      NereusMenuEditView view = new NereusMenuEditView(editor, screen, workflow,
          delegate (string key) { return new WorkflowFrame(key, key, editor); });

      Window window = new Window { Content = view, Width = 1280, Height = 720 };
      window.Show();
      Pump();
      return view;
    }

    private static ListBox ListShowing(NereusMenuEditView view, object source)
    {
      ListBox? list = view.GetLogicalDescendants().OfType<ListBox>()
          .FirstOrDefault(candidate => ReferenceEquals(candidate.ItemsSource, source));

      Assert.That(list, Is.Not.Null, "the screen shows the collection it was asked to");
      return list!;
    }

    private static ListBox RowsList(NereusMenuEditView view, NereusMenuEditModel model)
    {
      ListBox list = ListShowing(view, model.MediaListRows);
      Assert.That(list.Items.Count, Is.EqualTo(4), "the films tile has four lists to arrange");
      return list;
    }

    /// <summary>
    /// The point of the screen that one face of one line covers. The button and its place are asserted to be big
    /// enough to be aimed at, because a button the size of a hairline answers no click no matter how well its
    /// command is wired - which is the whole difference between a screen that works and one that only looks right.
    /// </summary>
    private static Point PlaceOf(ListBox list, int line, string face, Window window)
    {
      Control? row = list.ContainerFromIndex(line) as Control;
      Assert.That(row, Is.Not.Null, "line " + line + " of the list was built");
      Assert.That(row!.Bounds.Height, Is.GreaterThan(0.0), "and takes up room on the screen");

      Button? button = row.GetLogicalDescendants().OfType<Button>()
          .FirstOrDefault(candidate => candidate.Classes.Contains(face));
      Assert.That(button, Is.Not.Null, "the line offers a '" + face + "' button");
      Assert.That(button!.IsEnabled, Is.True, "and that button can be pressed");

      return PlaceOf(button, window);
    }

    /// <summary>The point one button covers, wherever on the screen it ended up.</summary>
    private static Point PlaceOf(Button button, Window window)
    {
      Assert.That(button.IsVisible, Is.True, "the button shows itself");
      Assert.That(button.Bounds.Width, Is.GreaterThanOrEqualTo(16.0),
          "the button is only " + button.Bounds.Width + " wide - nothing to click");
      Assert.That(button.Bounds.Height, Is.GreaterThanOrEqualTo(16.0),
          "the button is only " + button.Bounds.Height + " tall - nothing to click");

      Point? at = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window);
      Assert.That(at, Is.Not.Null, "the button has a place on the screen");
      return at!.Value;
    }

    /// <summary>
    /// The two clicks a hand on the desk makes, sent to a point of the screen rather than to a control, so that
    /// whatever is on the way - a scrim, a row clipped too short, a face reaching past its button - has its say.
    /// </summary>
    private static void Click(Window window, Point at)
    {
      window.MouseDown(at, MouseButton.Left);
      window.MouseUp(at, MouseButton.Left);
      Pump();
    }

    private static Button FaceOf(Control screen, string face)
    {
      Button? button = screen.GetLogicalDescendants().OfType<Button>()
          .FirstOrDefault(candidate => candidate.Classes.Contains(face));

      Assert.That(button, Is.Not.Null, "the screen offers a '" + face + "' button");
      return button!;
    }

    /// <summary>
    /// Pressing the arrow of a home content line with the mouse moves that line, which is what the arrows are for.
    /// Both arrows are tried, so a line whose commands do not reach the lists cannot pass.
    /// </summary>
    [AvaloniaTest]
    public void TheArrowsOfAHomeContentLineMoveItWithTheMouse()
    {
      WearTheShippedSkin();
      NereusMenuEditViewModel editor = TheEditor();
      WorkflowManager workflow = new WorkflowManager();
      NereusMenuEditView view = OpenHomeContent(editor, workflow);
      Window window = view.GetVisualAncestors().OfType<Window>().First();

      ListBox list = RowsList(view, editor.Model);
      Click(window, PlaceOf(list, 1, "nereus-editor-up", window));
      Assert.That(editor.Model.MediaListRows.Select(row => row.ListKey),
          Is.EqualTo(new[] { "ContinuePlayMovies", "LatestMovies", "FavoriteMovies", "UnplayedMovies" }),
          "the arrow was clicked where it sits on the screen, and the line did not move");

      list = RowsList(view, editor.Model);
      Click(window, PlaceOf(list, 0, "nereus-editor-down", window));
      Assert.That(editor.Model.MediaListRows.Select(row => row.ListKey),
          Is.EqualTo(new[] { "LatestMovies", "ContinuePlayMovies", "FavoriteMovies", "UnplayedMovies" }),
          "and the other arrow moved it back");
    }

    /// <summary>
    /// Pressing the bin of a home content line takes that list away from the tile and offers it again below the
    /// plus, which are the two answers the reference's media list dialog gave to one press.
    /// </summary>
    [AvaloniaTest]
    public void TheBinOfAHomeContentLineTakesTheListOutWithTheMouse()
    {
      WearTheShippedSkin();
      NereusMenuEditViewModel editor = TheEditor();
      WorkflowManager workflow = new WorkflowManager();
      NereusMenuEditView view = OpenHomeContent(editor, workflow);
      Window window = view.GetVisualAncestors().OfType<Window>().First();

      ListBox list = RowsList(view, editor.Model);
      Click(window, PlaceOf(list, 0, "nereus-editor-remove", window));

      Assert.That(editor.Model.MediaListRows.Select(row => row.ListKey),
          Is.EqualTo(new[] { "ContinuePlayMovies", "FavoriteMovies", "UnplayedMovies" }),
          "the bin was clicked where it sits on the screen, and the line did not leave");
      Assert.That(editor.Model.OtherMediaListRows.Select(row => row.ListKey), Is.EqualTo(new[] { "LatestMovies" }),
          "and the taken list is offered again");
    }

    /// <summary>
    /// The same clicks one layer up, where the front end really shows the editor: inside the dialog layer of the
    /// shell, over the screen it is a question about, with the scrim behind it and the trail above it. A button
    /// that answers in a bare window and not there answers nowhere, because nowhere is where the viewer sits.
    /// </summary>
    [AvaloniaTest]
    public void TheRowButtonsAnswerInsideTheShellsDialogLayer()
    {
      WearTheShippedSkin();
      NereusMenuEditViewModel editor = TheEditor();
      NereusMenuEditRow tile = editor.Model.Rows.First(row => row.ActionId == MOVIES);
      Assert.That(editor.Model.Config(tile), Is.True, "the films tile offers its lists to the editor");

      WorkflowManager workflow = new WorkflowManager();
      Dictionary<string, Control> built = new Dictionary<string, Control>();

      Control? Build(WorkflowFrame frame)
      {
        if (!built.TryGetValue(frame.StateId, out Control? view))
        {
          view = frame.StateId == "menulists"
              ? new NereusMenuEditView(editor, NereusMenuEditScreen.Lists, workflow,
                  delegate (string key) { return new WorkflowFrame(key, key, editor); })
              : new Label { Content = frame.StateId };
          built[frame.StateId] = view;
        }
        return view;
      }

      ShellWindow window = new ShellWindow(workflow, Build);
      window.Show();
      Pump();

      workflow.Push(new WorkflowFrame("home", "Home", new Label()));
      workflow.Push(new WorkflowFrame("menulists", "Edit home content", editor));
      Pump();

      ListBox list = RowsList((NereusMenuEditView)built["menulists"], editor.Model);
      Click(window, PlaceOf(list, 1, "nereus-editor-up", window));
      Assert.That(editor.Model.MediaListRows.Select(row => row.ListKey),
          Is.EqualTo(new[] { "ContinuePlayMovies", "LatestMovies", "FavoriteMovies", "UnplayedMovies" }),
          "the arrow answers in a bare window but not in the dialog the shell shows the editor in");

      editor.Model.Remove(editor.Model.MediaListRows.First());
      Pump();

      Button plus = FaceOf(built["menulists"], "nereus-editor-add");
      Click(window, PlaceOf(plus, window));
      Assert.That(workflow.Current!.StateId, Is.EqualTo("menulistadd"),
          "the plus above the lines does not open the offer from inside the dialog either");

      window.Close();
    }

    /// <summary>
    /// One press on an offered entry takes it and steps back, the way the reference's picker of buttons answered.
    /// An entry that only lights up under a press and waits for a second key afterwards is a screen that appears to
    /// the viewer to do nothing, however carefully the Enter key below it is wired.
    /// </summary>
    [AvaloniaTest]
    public void APressOnAnOfferedEntryTakesItAndStepsBack()
    {
      WearTheShippedSkin();
      NereusMenuEditViewModel editor = TheEditor();
      WorkflowManager workflow = new WorkflowManager();
      workflow.Push(new WorkflowFrame("menulists", "Edit home content", editor));

      NereusMenuEditView view = OpenHomeContent(editor, workflow, NereusMenuEditScreen.PickLists);
      workflow.Push(new WorkflowFrame("menulistadd", "Add home content", editor));
      Window window = view.GetVisualAncestors().OfType<Window>().First();

      // One list off the tile, which is what leaves something to be offered. Opening the tile for its lists puts
      // every list back on it, so this happens after the tile was opened rather than before.
      editor.Model.Remove(editor.Model.MediaListRows.First());
      Pump();

      ListBox picks = view.GetLogicalDescendants().OfType<ListBox>().First(candidate =>
          ReferenceEquals(candidate.ItemsSource, editor.Model.OtherMediaListRows));
      Assert.That(picks.IsVisible, Is.True, "the offer is what this stop shows");
      Assert.That(picks.Items.Count, Is.EqualTo(1), "one list was taken out, so one list is offered");

      Control? entry = picks.ContainerFromIndex(0) as Control;
      Assert.That(entry, Is.Not.Null, "the offered entry is on the screen");
      Point? at = entry!.TranslatePoint(new Point(entry.Bounds.Width / 2, entry.Bounds.Height / 2), window);
      Assert.That(at, Is.Not.Null);
      Click(window, at!.Value);

      Assert.That(editor.Model.MediaListRows.Select(row => row.ListKey),
          Is.EqualTo(new[] { "ContinuePlayMovies", "FavoriteMovies", "UnplayedMovies", "LatestMovies" }),
          "the press landed on the entry, and the list was not put back on the tile");
      Assert.That(workflow.Current!.StateId, Is.EqualTo("menulists"),
          "and taking an entry steps back off the offer, as pressing an entry in the reference's menu did");
    }

    /// <summary>
    /// The menu's own stop, under the same mouse: the arrows that order the tiles, the gear that opens one tile's
    /// home content, and the plus that offers the tiles missing from the menu. They are tested here rather than
    /// left to the model because the complaint was about a button that looked present and answered nothing, and
    /// only a press at the place a button covers says whether a viewer can reach it.
    /// </summary>
    [AvaloniaTest]
    public void TheMenuScreenAnswersItsButtonsUnderTheMouse()
    {
      WearTheShippedSkin();
      NereusMenuEditViewModel editor = TheEditor();
      WorkflowManager workflow = new WorkflowManager();

      NereusMenuEditView view = new NereusMenuEditView(editor, NereusMenuEditScreen.Tiles, workflow,
          delegate (string key) { return new WorkflowFrame(key, key, editor); });
      Window window = new Window { Content = view, Width = 1280, Height = 720 };
      window.Show();
      Pump();
      workflow.Push(new WorkflowFrame("menuedit", "Edit menu", editor));
      Pump();

      ListBox list = ListShowing(view, editor.Model.Rows);
      Click(window, PlaceOf(list, 1, "nereus-editor-up", window));
      Assert.That(editor.Model.Rows.Select(row => row.ActionId).First(), Is.EqualTo(MOVIES),
          "the arrow of the second line was pressed where it sits, and the tile did not come up");

      list = ListShowing(view, editor.Model.Rows);
      Click(window, PlaceOf(list, 0, "nereus-editor-config", window));
      Assert.That(workflow.Current!.StateId, Is.EqualTo("menulists"),
          "the gear of the films tile was pressed where it sits, and its home content did not open");

      workflow.Pop();
      Button plus = FaceOf(view, "nereus-editor-add");
      Click(window, PlaceOf(plus, window));
      Assert.That(workflow.Current!.StateId, Is.EqualTo("menuadd"),
          "the plus was pressed where it sits, and the offer of the missing tiles did not open");
    }

    /// <summary>
    /// The plus above the lines opens the screen that offers the lists the tile does not show yet, and OK on the
    /// home content screen keeps the choice in the editor without writing the file - the file is written once, by
    /// the OK of the menu itself, exactly as the reference's two dialogs divided the work.
    /// </summary>
    [AvaloniaTest]
    public void ThePlusOpensTheOfferAndOkKeepsTheChoice()
    {
      WearTheShippedSkin();
      NereusMenuEditViewModel editor = TheEditor();
      WorkflowManager workflow = new WorkflowManager();
      workflow.Push(new WorkflowFrame("menuedit", "Edit menu", editor));
      NereusMenuEditView view = OpenHomeContent(editor, workflow);
      workflow.Push(new WorkflowFrame("menulists", "Edit home content", editor));
      Window window = view.GetVisualAncestors().OfType<Window>().First();

      Button plus = FaceOf(view, "nereus-editor-add");
      Assert.That(plus.IsVisible, Is.False,
          "while every list the tile has is on the tile, there is nothing left to add");

      editor.Model.Remove(editor.Model.MediaListRows.First());
      Pump();
      Assert.That(plus.IsVisible, Is.True, "and once one is taken out, the plus offers itself");

      Click(window, PlaceOf(plus, window));
      Assert.That(workflow.Current!.StateId, Is.EqualTo("menulistadd"),
          "the plus was clicked where it sits on the screen, and the offer did not open");

      // The shell would now show the offer and, on a pick, step back here. The test steps back by hand; which
      // screen is on top is the shell's business, the push above is the plus's whole answer.
      workflow.Pop();

      Button ok = FaceOf(view, "nereus-primary");
      Click(window, PlaceOf(ok, window));

      Assert.That(workflow.Current!.StateId, Is.EqualTo("menuedit"), "and OK steps back off the home content");
      Assert.That(editor.Model.MediaListSettings(),
          Contains.Item(MOVIES + ":ContinuePlayMovies,FavoriteMovies,UnplayedMovies"),
          "OK keeps the choice in the editor, which is all the reference's list dialog did to the file");
    }

    /// <summary>One face of one line, as the screen built it.</summary>
    private static Button FaceIn(ListBox list, int line, string face)
    {
      Control? row = list.ContainerFromIndex(line) as Control;
      Assert.That(row, Is.Not.Null, "line " + line + " of the list was built");

      Button? button = row!.GetLogicalDescendants().OfType<Button>()
          .FirstOrDefault(candidate => candidate.Classes.Contains(face));
      Assert.That(button, Is.Not.Null, "the line offers a '" + face + "' button");
      return button!;
    }

    /// <summary>
    /// A line of home content carries no gear, and a line of the menu still does. The reference showed the gear on
    /// both and left it without a command on the first, which a viewer reads as a button that is broken; here the
    /// question is said where it is asked - a tile of the menu is opened for its content, a line of content has no
    /// content of its own to open.
    /// </summary>
    [AvaloniaTest]
    public void AHomeContentLineCarriesNoGearAMenuLineStillDoes()
    {
      WearTheShippedSkin();
      NereusMenuEditViewModel editor = TheEditor();
      WorkflowManager workflow = new WorkflowManager();

      NereusMenuEditView content = OpenHomeContent(editor, workflow);
      Button gear = FaceIn(ListShowing(content, editor.Model.MediaListRows), 0, "nereus-editor-config");
      Assert.That(gear.IsVisible, Is.False,
          "the home content screen still hands out a gear that has nothing to open");

      NereusMenuEditView menu = new NereusMenuEditView(editor, NereusMenuEditScreen.Tiles, workflow,
          delegate (string key) { return new WorkflowFrame(key, key, editor); });
      Window window = new Window { Content = menu, Width = 1280, Height = 720 };
      window.Show();
      Pump();

      gear = FaceIn(ListShowing(menu, editor.Model.Rows), 0, "nereus-editor-config");
      Assert.That(gear.IsVisible, Is.True, "the menu's lines keep the gear that opens their tile's content");
    }

    /// <summary>
    /// The gear of the series tile opens that tile's own home content, with the four lists the reference's
    /// <c>SeriesHomeContent</c> offers - latest episodes, continue playing, favorites, unplayed - and nothing
    /// belonging to another tile. This is the choice the screen exists for: which of a tile's lists the pane beside
    /// the menu draws, and in which order.
    /// </summary>
    [AvaloniaTest]
    public void TheGearOfTheSeriesTileOpensItsOwnLists()
    {
      WearTheShippedSkin();
      NereusMenuEditViewModel editor = TheEditor();
      WorkflowManager workflow = new WorkflowManager();

      NereusMenuEditView menu = new NereusMenuEditView(editor, NereusMenuEditScreen.Tiles, workflow,
          delegate (string key) { return new WorkflowFrame(key, key, editor); });
      Window window = new Window { Content = menu, Width = 1280, Height = 720 };
      window.Show();
      Pump();
      workflow.Push(new WorkflowFrame("menuedit", "Edit menu", editor));
      Pump();

      ListBox rows = ListShowing(menu, editor.Model.Rows);
      int line = editor.Model.Rows.ToList().FindIndex(row => row.ActionId == SERIES);
      Assert.That(line, Is.GreaterThanOrEqualTo(0), "the series tile is on the menu");

      Button gear = FaceIn(rows, line, "nereus-editor-config");
      Assert.That(gear.IsEnabled, Is.True, "the series tile has lists to choose from");
      Click(window, PlaceOf(gear, window));

      Assert.That(workflow.Current!.StateId, Is.EqualTo("menulists"),
          "the gear of the series tile was pressed where it sits, and nothing opened");
      Assert.That(editor.Model.SelectedActionId, Is.EqualTo(SERIES), "and what opened is the series tile's own");
      Assert.That(editor.Model.MediaListRows.Select(row => row.ListKey),
          Is.EqualTo(new[] { "LatestEpisodes", "ContinuePlaySeries", "FavoriteSeries", "UnplayedSeries" }),
          "with the lists the reference's SeriesHomeContent offers");
      Assert.That(editor.Model.MediaListRows.Select(row => row.Label),
          Is.EqualTo(new[] { "Latest added", "Continue playing", "Favorites", "Unplayed" }),
          "named the way the pane names them");
    }
  }
}
