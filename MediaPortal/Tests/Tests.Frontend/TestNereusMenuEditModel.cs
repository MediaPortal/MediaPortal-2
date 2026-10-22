using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Skins.Nereus;
using MediaPortal.Frontend.ViewModels;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Tests.Frontend
{
  /// <summary>
  /// The rules the home menu editor plays by, taken from Nereus' own <c>Models\MenuEditModel.cs</c>: which tile
  /// may move where, which button is holding the focus after a move, what happens to the tile that was taken out,
  /// which tile has lists to choose at all, and how a choice of lists comes back out of the settings file.
  /// </summary>
  [TestFixture]
  public class TestNereusMenuEditModel
  {
    private static readonly Guid MOVIES = Guid.Parse("80d2e2cc-baaa-4750-807b-f37714153751");
    private static readonly Guid AUDIO = Guid.Parse("30715d73-4205-417f-80aa-e82f0834171f");
    private static readonly Guid SERIES = Guid.Parse("30f57cba-459c-4202-a587-09fff5098251");

    private static List<MainMenuEntry> TheMenu()
    {
      return MainMenu.Build(Array.Empty<AspectViewModel>(), connected: false).ToList();
    }

    private static NereusMenuEditRow Row(NereusMenuEditModel editor, Guid actionId)
    {
      NereusMenuEditRow? row = editor.Rows.FirstOrDefault(candidate => candidate.ActionId == actionId);
      Assert.That(row, Is.Not.Null, "the editor was asked to edit a tile it does not hold");
      return row!;
    }

    private static NereusMenuEditModel Editor(IEnumerable<Guid> configured,
        IEnumerable<string>? mediaLists = null, Guid? start = null)
    {
      return new NereusMenuEditModel(TheMenu(), configured, start, mediaLists);
    }

    [Test]
    public void TheTilesStandInTheOrderTheViewerLeftThem()
    {
      NereusMenuEditModel editor = Editor(new[] { AUDIO, MOVIES, SERIES });

      CollectionAssert.AreEqual(new[] { AUDIO, MOVIES, SERIES }, editor.Rows.Select(row => row.ActionId));
      string audioLabel = TheMenu().First(entry => Guid.TryParse(entry.HomeImageId, out Guid id) && id == AUDIO).Label;
      Assert.That(Row(editor, AUDIO).Label, Is.EqualTo(audioLabel),
          "a tile is named the way the home menu names it, not by a word the editor invented");
    }

    [Test]
    public void NeitherEndOfTheMenuCanMoveFurtherThanItAlreadyStands()
    {
      NereusMenuEditModel editor = Editor(new[] { AUDIO, MOVIES, SERIES });

      Assert.That(Row(editor, AUDIO).IsUpEnabled, Is.False, "nothing is above the first tile");
      Assert.That(Row(editor, MOVIES).IsUpEnabled, Is.True);
      Assert.That(Row(editor, MOVIES).IsDownEnabled, Is.True);
      Assert.That(Row(editor, SERIES).IsDownEnabled, Is.False, "nothing is below the last tile");

      // And the ends move when the order moves underneath them.
      editor.MoveUp(Row(editor, MOVIES));
      CollectionAssert.AreEqual(new[] { MOVIES, AUDIO, SERIES }, editor.Rows.Select(row => row.ActionId));
      Assert.That(Row(editor, MOVIES).IsUpEnabled, Is.False, "the moved tile is the first tile now");
      Assert.That(Row(editor, AUDIO).IsUpEnabled, Is.True, "the tile it passed is in the middle now");
      Assert.That(Row(editor, AUDIO).IsDownEnabled, Is.True);
      Assert.That(Row(editor, SERIES).IsDownEnabled, Is.False, "and the last tile stays the last tile");
    }

    [Test]
    public void ATileStaysWhereItIsWhenThereIsNowhereToGo()
    {
      NereusMenuEditModel editor = Editor(new[] { AUDIO, MOVIES, SERIES });

      editor.MoveUp(Row(editor, AUDIO));
      editor.MoveDown(Row(editor, SERIES));
      CollectionAssert.AreEqual(new[] { AUDIO, MOVIES, SERIES }, editor.Rows.Select(row => row.ActionId));
    }

    [Test]
    public void TheFocusStaysOnTheButtonThatWasPressed()
    {
      // A viewer holding the up button presses it several times without looking for it again, so the focus follows
      // the tile - unless the tile arrived at an end, where that button no longer exists.
      NereusMenuEditModel editor = Editor(new[] { AUDIO, MOVIES, SERIES, Guid.Parse("55556593-9fe9-436c-a3b6-a971e10c9d44") });
      NereusMenuEditRow images = Row(editor, Guid.Parse("55556593-9fe9-436c-a3b6-a971e10c9d44"));

      NereusMenuEditFocusEventArgs? moved = null;
      editor.FocusRequested += (_, e) => moved = e;

      editor.MoveUp(images);
      Assert.That(images.IsUpFocused, Is.True, "the tile is still in the middle of the menu, so up is still a question");
      Assert.That(moved!.Button, Is.EqualTo(NereusMenuEditButton.Up));
      Assert.That(moved.IsMediaList, Is.False, "the notice says which of the editor's two lists to look in");

      editor.MoveUp(Row(editor, MOVIES));
      Assert.That(Row(editor, MOVIES).IsUpFocused, Is.False);
      Assert.That(Row(editor, MOVIES).IsDownFocused, Is.True,
          "the tile landed at the top, where the button that was pressed cannot exist");
    }

    [Test]
    public void MovingDownAtTheBottomEndsTheSameWay()
    {
      NereusMenuEditModel editor = Editor(new[] { AUDIO, MOVIES, SERIES });
      NereusMenuEditRow movies = Row(editor, MOVIES);

      editor.MoveDown(movies);
      Assert.That(movies.IsDownFocused, Is.False);
      Assert.That(movies.IsUpFocused, Is.True, "the tile landed last, so it can only go back up");
      CollectionAssert.AreEqual(new[] { AUDIO, SERIES, MOVIES }, editor.Rows.Select(row => row.ActionId));
    }

    [Test]
    public void ATileAlreadyOnTheMenuIsNotOfferedAgain()
    {
      NereusMenuEditModel editor = Editor(new[] { AUDIO, MOVIES });

      CollectionAssert.DoesNotContain(editor.OtherRows.Select(row => row.ActionId), AUDIO);
      CollectionAssert.DoesNotContain(editor.OtherRows.Select(row => row.ActionId), MOVIES);
      Assert.That(editor.HasOtherRows, Is.True, "this build has more tiles than the two on the menu");

      editor.Add(Row(editor, AUDIO));
      Assert.That(editor.Rows.Count, Is.EqualTo(2), "pressing add on a tile that stands there changes nothing");
    }

    [Test]
    public void AddingATilePutsItAtTheEndAndTakesItOutOfTheOffer()
    {
      NereusMenuEditModel editor = Editor(new[] { AUDIO });
      NereusMenuEditRow added = editor.OtherRows.First();

      editor.Add(added);

      Assert.That(editor.Rows.Last(), Is.SameAs(added), "the reference appended what was added");
      CollectionAssert.DoesNotContain(editor.OtherRows.Select(row => row.ActionId), added.ActionId);
      editor.Remove(added);
      CollectionAssert.Contains(editor.OtherRows.Select(row => row.ActionId), added.ActionId);
      Assert.That(editor.HasOtherRows, Is.True);
    }

    [Test]
    public void TakingATileOutHandsTheNeighboursRemoveButton()
    {
      NereusMenuEditModel editor = Editor(new[] { AUDIO, MOVIES, SERIES });

      editor.Remove(Row(editor, MOVIES));

      Assert.That(Row(editor, SERIES).IsRemoveFocused, Is.True,
          "the tile that took the place is where the finger already is");
      Assert.That(Row(editor, AUDIO).IsRemoveFocused, Is.False);
      CollectionAssert.AreEqual(new[] { AUDIO, SERIES }, editor.Rows.Select(row => row.ActionId));

      editor.Remove(Row(editor, SERIES));
      Assert.That(Row(editor, AUDIO).IsRemoveFocused, Is.True,
          "with the last tile gone, the one above it is the last tile");
      Assert.That(editor.HasOtherRows, Is.True, "everything taken out is offered again");
    }

    [Test]
    public void ATileIsOnlyOpenForListsWhereItHasLists()
    {
      NereusMenuEditModel editor = Editor(new[] { MOVIES, AUDIO, SERIES });

      Assert.That(Row(editor, MOVIES).IsConfigEnabled, Is.True);
      Assert.That(editor.Config(Row(editor, MOVIES)), Is.True);
      Assert.That(editor.SelectedLabel, Is.EqualTo(TheMenu().First(entry => entry.HomeImageId != null
          && Guid.TryParse(entry.HomeImageId, out Guid id) && id == MOVIES).Label),
          "the dialog is headed by the tile's own name");

      foreach (NereusMenuEditRow row in editor.Rows)
      {
        Assert.That(row.IsConfigEnabled, Is.EqualTo(HomeContentList.AvailableFor(row.ActionId).Count > 0),
          "the reference asked the tile's home content how many lists it has, and disabled the button when it had none");
      }
    }

    [Test]
    public void AFileThatNeverBeenChosenShowsEveryListTheTileHas()
    {
      NereusMenuEditModel editor = Editor(new[] { MOVIES });

      editor.Config(Row(editor, MOVIES));

      CollectionAssert.AreEqual(
          HomeContentList.AvailableFor(MOVIES).Select(list => list.Heading),
          editor.MediaListRows.Select(row => row.Label));
      Assert.That(editor.HasOtherMediaListRows, Is.False, "everything on offer is on the list");
      CollectionAssert.AreEqual(new[] { "LatestMovies", "ContinuePlayMovies", "FavoriteMovies", "UnplayedMovies" },
          editor.MediaListRows.Select(row => row.ListKey));
    }

    [Test]
    public void AChosenSetOfListsComesBackOnTheNextOpening()
    {
      NereusMenuEditModel editor = Editor(new[] { MOVIES, AUDIO });
      editor.Config(Row(editor, MOVIES));

      editor.Remove(editor.MediaListRows.First());
      editor.Remove(editor.MediaListRows.First());
      CollectionAssert.AreEqual(new[] { "FavoriteMovies", "UnplayedMovies" },
          editor.MediaListRows.Select(row => row.ListKey));
      Assert.That(editor.HasOtherMediaListRows, Is.True);
      CollectionAssert.AreEqual(new[] { "LatestMovies", "ContinuePlayMovies" },
          editor.OtherMediaListRows.Select(row => row.ListKey));

      // Leaving this dialog without saving throws the unsaved lines away, so opening another tile and coming back
      // shows what the file says - which is nothing chosen, which is everything.
      editor.Config(Row(editor, AUDIO));
      editor.Config(Row(editor, MOVIES));
      CollectionAssert.AreEqual(new[] { "LatestMovies", "ContinuePlayMovies", "FavoriteMovies", "UnplayedMovies" },
          editor.MediaListRows.Select(row => row.ListKey));

      editor.Remove(editor.MediaListRows.First());
      editor.SaveMediaListEdit();
      CollectionAssert.Contains(editor.MediaListSettings(), MOVIES + ":ContinuePlayMovies,FavoriteMovies,UnplayedMovies");

      NereusMenuEditModel reopened = Editor(new[] { MOVIES }, editor.MediaListSettings());
      reopened.Config(Row(reopened, MOVIES));
      CollectionAssert.AreEqual(new[] { "ContinuePlayMovies", "FavoriteMovies", "UnplayedMovies" },
          reopened.MediaListRows.Select(row => row.ListKey));
    }

    [Test]
    public void PuttingAListBackPutsItAtTheEndOfTheList()
    {
      NereusMenuEditModel editor = Editor(new[] { MOVIES });
      editor.Config(Row(editor, MOVIES));
      NereusMenuEditRow taken = editor.MediaListRows.First();
      editor.Remove(taken);

      editor.Add(editor.OtherMediaListRows.First());

      Assert.That(editor.MediaListRows.Last().ListKey, Is.EqualTo("LatestMovies"),
          "the reference appended what was added, and the viewer moves it from there");
      Assert.That(editor.MediaListRows, Does.Contain(taken));
    }

    [Test]
    public void ATileLeftWithoutASingleListIsWrittenWithAColonAndShowsNothing()
    {
      // The reference wrote "guid:" for that case, and a colon with nothing behind it is the one answer that means
      // "nothing here" - leaving the tile out of the file would mean "everything here", which is the opposite.
      NereusMenuEditModel editor = Editor(new[] { MOVIES });
      editor.Config(Row(editor, MOVIES));
      while (editor.MediaListRows.Count > 0)
        editor.Remove(editor.MediaListRows.First());
      editor.SaveMediaListEdit();

      CollectionAssert.AreEqual(new[] { MOVIES + ":" }, editor.MediaListSettings());
      Assert.That(HomeContentList.ForAction(MOVIES, editor.MediaListSettings()), Is.Empty);
    }

    [Test]
    public void AStoredTileThisFrontEndCannotOfferIsSkippedAndATileNamedTwiceIsNamedOnce()
    {
      NereusMenuEditModel editor = Editor(new[] { MOVIES, Guid.NewGuid(), MOVIES, AUDIO });

      CollectionAssert.AreEqual(new[] { MOVIES, AUDIO }, editor.Rows.Select(row => row.ActionId));
    }

    [Test]
    public void TheEditorOpensOnTheTileTheViewerWasStandingOn()
    {
      NereusMenuEditModel editor = Editor(new[] { AUDIO, MOVIES, SERIES }, start: MOVIES);

      Assert.That(editor.StartActionId, Is.EqualTo(MOVIES));
      Assert.That(Row(editor, MOVIES).IsUpFocused, Is.True,
          "the reference selected that line and gave its up button the focus, so the viewer could start at once");
      Assert.That(Row(editor, AUDIO).IsUpFocused, Is.False);
    }

    [Test]
    public void WhatTheEditorLeavesBehindIsWhatTheSettingsFileHolds()
    {
      string file = Path.Combine(Path.GetTempPath(),
          "mp2-menuedit-" + Guid.NewGuid().ToString("N") + ".json");
      try
      {
        NereusMenuEditModel editor = Editor(new[] { AUDIO, MOVIES, SERIES });
        editor.Config(Row(editor, MOVIES));
        editor.Remove(editor.MediaListRows.First());
        editor.SaveMediaListEdit();
        editor.MoveUp(Row(editor, SERIES));

        NereusSkinSettings writing = (NereusSkinSettings)(SkinSettingsBase.LoadFor("nereus", file, out _)
            ?? new NereusSkinSettings());
        writing.HomeMenuActionIds = editor.CurrentActionIds().ToArray();
        writing.HomeMenuActionMediaLists = editor.MediaListSettings();
        writing.Save(file, out bool saved);
        Assert.That(saved, Is.True);

        NereusSkinSettings read = (NereusSkinSettings)(SkinSettingsBase.LoadFor("nereus", file, out string? problem)
            ?? new NereusSkinSettings());
        Assert.That(problem, Is.Null);
        CollectionAssert.AreEqual(new[] { AUDIO, SERIES, MOVIES }, read.HomeMenuActionIds);
        CollectionAssert.AreEqual(new[] { MOVIES + ":ContinuePlayMovies,FavoriteMovies,UnplayedMovies" },
            read.HomeMenuActionMediaLists);

        // And the next opening of the editor reads the same answer back out of it.
        NereusMenuEditModel next = Editor(read.HomeMenuActionIds, read.HomeMenuActionMediaLists);
        CollectionAssert.AreEqual(new[] { AUDIO, SERIES, MOVIES }, next.Rows.Select(row => row.ActionId));
        next.Config(Row(next, MOVIES));
        CollectionAssert.AreEqual(new[] { "Continue playing", "Favorites", "Unplayed" },
            next.MediaListRows.Select(row => row.Label), "the pane's headings, not the keys it stores them under");
        CollectionAssert.AreEqual(new[] { "ContinuePlayMovies", "FavoriteMovies", "UnplayedMovies" },
            next.MediaListRows.Select(row => row.ListKey));
      }
      finally
      {
        try { File.Delete(file); } catch (IOException) { }
      }
    }

    [Test]
    public void EveryListTheEditorOffersIsAListThePaneCanRead()
    {
      // The editor and the content pane must speak one key. If a heading were offered under a key the pane's own
      // lookup did not answer, a viewer could choose a list that silently never appeared.
      foreach (Guid actionId in new NereusMenuEditModel(TheMenu(), Array.Empty<Guid>(), null, null)
               .OtherRows.Select(row => row.ActionId).Distinct())
      {
        foreach (HomeContentList list in HomeContentList.AvailableFor(actionId))
        {
          IReadOnlyList<HomeContentList> read = HomeContentList.ForAction(actionId,
              new[] { actionId + ":" + list.Key });
          CollectionAssert.Contains(read.Select(readList => readList.Key), list.Key,
              "the pane must show the very list the editor offered for " + list.Heading);
        }
      }
    }
  }
}
