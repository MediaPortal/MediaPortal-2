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
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using NUnit.Framework;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// The television settings screen as the viewer sees it: every switch and every box holding the answer that
  /// stands in the file, both as the screen opens and when the file is read again while the screen stands there.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The model behind this screen has been right about these answers for a long time, and every test that asked the
  /// model agreed with it. What no test ever asked was what the box on the window shows, and that is a different
  /// thing: the screen builds its controls and binds them first and reads the file afterwards, so an answer that
  /// arrives after the binding was made only reaches the control if a "this property changed" notice comes with it.
  /// </para>
  /// <para>
  /// The notice the screen first had named five of its twenty answers and left out every switch and most number
  /// boxes, so a household could turn the demo data on, watch the guide fill itself with invented channels, open
  /// the settings again and be shown a switch that had stayed off - a screen that contradicts what the machine is
  /// doing, which is worse than a screen that is merely missing.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTvSettingsScreen
  {
    private readonly List<Window> _windows = new List<Window>();

    [TearDown]
    public void TearDown()
    {
      foreach (Window window in _windows)
        window.Close();
      _windows.Clear();
      Dispatcher.UIThread.RunJobs();
    }

    #region The screen and what it shows

    /// <summary>
    /// One answer flipped away from its default per answer, so a switch that never heard the answer reads the same
    /// as a switch that heard it wrong. The demo data is what this was written for: it is the one answer that is
    /// impossible to miss, because the whole guide changes behind it.
    /// </summary>
    private static TvBackendSettings WrittenDown()
    {
      TvBackendSettings settings = new TvBackendSettings();
      settings.Host = "tvhall";
      settings.Port = 9998;
      settings.GuideDays = 11;
      settings.VisibleHours = 6.5;
      settings.VisibleRows = 4;
      settings.ZapTimeoutSeconds = 12;
      settings.RecordingNotificationSeconds = 9;
      settings.DemoData = true;
      settings.ShowChannelNames = false;
      settings.ShowChannelNumbers = true;
      settings.ShowChannelLogos = false;
      settings.ShowGenreColors = false;
      settings.ShowSeriesInfo = false;
      settings.HideAllChannelsGroup = true;
      settings.ZapFromGuide = false;
      settings.ZapByChannelIndex = false;
      settings.AutoStartTV = true;
      settings.ShowRecordingStartedNotifications = false;
      settings.ShowRecordingEndedNotifications = true;
      return settings;
    }

    private static readonly Dictionary<string, bool> WRITTEN_SWITCHES = new Dictionary<string, bool>
    {
      { "demoCheck", true },
      { "namesCheck", false },
      { "numbersCheck", true },
      { "logosCheck", false },
      { "genreCheck", false },
      { "seriesCheck", false },
      { "hideAllCheck", true },
      { "zapGuideCheck", false },
      { "zapIndexCheck", false },
      { "autoStartCheck", true },
      { "recStartCheck", false },
      { "recEndCheck", true }
    };

    private static readonly Dictionary<string, string> WRITTEN_BOXES = new Dictionary<string, string>
    {
      { "hostBox", "tvhall" },
      { "portBox", "9998" },
      { "daysBox", "11" },
      { "hoursBox", "6.5" },
      { "rowsBox", "4" },
      { "zapBox", "12" },
      { "noticeBox", "9" }
    };

    /// <summary>
    /// The whole point of the screen: it says what is written down. Asked per control and not per property of the
    /// model, because the model was never the part that was wrong.
    /// </summary>
    [Test]
    [AvaloniaTest]
    public void TheScreenShowsTheAnswersThatStandInTheFile()
    {
      Window window = ShowOnScreen(WrittenDown());

      foreach (KeyValuePair<string, bool> answer in WRITTEN_SWITCHES)
      {
        CheckBox check = Named<CheckBox>(window, answer.Key);
        Assert.That(check.IsChecked, Is.EqualTo(answer.Value),
            "the switch named " + answer.Key + " shows " + check.IsChecked + " although "
            + answer.Value + " was written down");
      }

      foreach (KeyValuePair<string, string> answer in WRITTEN_BOXES)
      {
        TextBox box = Named<TextBox>(window, answer.Key);
        Assert.That(box.Text, Is.EqualTo(answer.Value),
            "the box named " + answer.Key + " holds \"" + box.Text + "\" although \"" + answer.Value
            + "\" was written down");
      }
    }

    /// <summary>
    /// A screen that has already been built is not rebuilt when the file is read again, so every answer needs its
    /// notice a second time. Without it the screen keeps showing the answers as they were when it opened, and the
    /// household that just changed two of them stops believing the screen.
    /// </summary>
    [Test]
    [AvaloniaTest]
    public void ReadingTheFileAgainWhileTheScreenStandsThereMovesEveryBox()
    {
      Window window = ShowOnScreen(WrittenDown());
      TvSettingsViewModel model = ModelOf(Named<CheckBox>(window, "demoCheck"));

      TvBackendSettings changed = WrittenDown();
      changed.DemoData = false;
      changed.ShowChannelLogos = true;
      changed.VisibleHours = 3;
      changed.AutoStartTV = false;
      model.Show(changed, null);
      Pump();

      Assert.That(Named<CheckBox>(window, "demoCheck").IsChecked, Is.False,
          "the demo switch stayed on after the file said it was off");
      Assert.That(Named<CheckBox>(window, "logosCheck").IsChecked, Is.True,
          "the picture switch stayed off after the file said it was on");
      Assert.That(Named<CheckBox>(window, "autoStartCheck").IsChecked, Is.False,
          "the automatic start switch did not follow the file");
      Assert.That(Named<TextBox>(window, "hoursBox").Text, Is.EqualTo("3"),
          "the hours box did not follow the file");

      // ... and the boxes following the model must not answer it back: a two way binding that wrote the value it had
      // been built with into the model while being corrected would undo the read it was being corrected from.
      Assert.That(model.DemoData, Is.False, "the switch wrote its old answer back into the model");
      Assert.That(model.ShowChannelLogos, Is.True, "the picture switch wrote its old answer back into the model");
      Assert.That(model.VisibleHours, Is.EqualTo("3"), "the hours box wrote its old answer back into the model");
    }

    /// <summary>
    /// What the viewer's own typing must survive: pressing a switch or typing in a box writes the model, and a
    /// notice out of the model must not push the old answer back into the control being written in.
    /// </summary>
    [Test]
    [AvaloniaTest]
    public void ANoticeFromTheModelDoesNotUndoWhatWasTypedIntoABox()
    {
      Window window = ShowOnScreen(WrittenDown());
      TextBox hours = Named<TextBox>(window, "hoursBox");
      hours.Text = "8";
      Pump();

      TvSettingsViewModel model = ModelOf(hours);
      model.RaiseEverything();
      Pump();

      Assert.That(model.VisibleHours, Is.EqualTo("8"), "the typed number was pushed out of the model");
      Assert.That(hours.Text, Is.EqualTo("8"), "the typed number was pushed out of the box");
    }

    #endregion

    #region Helpers

    private Window ShowOnScreen(TvBackendSettings settings)
    {
      Window window = new Window
      {
        Content = new TvSettingsView(settings),
        Width = 1280,
        Height = 720
      };
      window.Show();
      _windows.Add(window);
      Pump();
      return window;
    }

    private static void Pump()
    {
      Dispatcher.UIThread.RunJobs();
      Thread.Sleep(5);
      Dispatcher.UIThread.RunJobs();
    }

    /// <summary>The model behind a control that stands on the screen.</summary>
    private static TvSettingsViewModel ModelOf(Control onScreen)
    {
      TvSettingsViewModel? model = onScreen.DataContext as TvSettingsViewModel;
      Assert.That(model, Is.Not.Null, "the screen carries no television settings model");
      return model!;
    }

    private static T Named<T>(Visual root, string name) where T : Control
    {
      T? found = root.GetLogicalDescendants().OfType<T>().FirstOrDefault(c => c.Name == name);
      Assert.That(found, Is.Not.Null, "no control named " + name + " on the screen");
      return found!;
    }

    #endregion
  }
}
