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
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using Avalonia.Threading;
using NUnit.Framework;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// The window wearing a skin: the header over the screens, the footer under them, and the superlayer over
  /// everything - the master chrome the reference wrapped around every screen, now drawn from the skin files
  /// rather than coded into the window.
  /// </summary>
  [TestFixture]
  public class TestShellChrome
  {
    [TearDown]
    public void PutTheDefaultBack()
    {
      Application? app = Application.Current;
      if (app != null)
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
    }

    private static void Pump()
    {
      for (int round = 0; round < 20; round++)
        Dispatcher.UIThread.RunJobs();
    }

    private static ContentControl? Part(Window window, string name)
    {
      return window.GetVisualDescendants().OfType<ContentControl>().FirstOrDefault(c => c.Name == name);
    }

    [AvaloniaTest]
    public void AWornSkinPutsItsHeaderAndFooterAroundTheScreen()
    {
      Application app = Application.Current!;
      FrontendTheme.Apply(app, "nereus", out _);

      ShellWindow window = new ShellWindow(new Label { Content = "somewhere" });
      window.Show();
      Pump();

      ContentControl? header = Part(window, "ChromeHeader");
      ContentControl? footer = Part(window, "ChromeFooter");
      Assert.That(header, Is.Not.Null, "the window draws a header slot whether or not a skin fills it");
      Assert.That(footer, Is.Not.Null);
      Assert.That(header!.Template, Is.Not.Null, "the skin's chrome file filled the header slot");
      Assert.That(header.IsVisible, Is.True);
      Assert.That(footer!.Template, Is.Not.Null, "the skin's chrome file filled the footer slot too");

      window.Close();
    }

    [AvaloniaTest]
    public void AWindowUnderNoSkinKeepsItsChromeToItself()
    {
      Application app = Application.Current!;
      FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);

      ShellWindow window = new ShellWindow(new Label { Content = "plain" });
      window.Show();
      Pump();

      ContentControl? header = Part(window, "ChromeHeader");
      Assert.That(header!.Template, Is.Null, "the default look is the front end's own, not a skin's");
      Assert.That(header.IsVisible, Is.False);

      window.Close();
    }

    [AvaloniaTest]
    public void SwitchingSkinsOnTheCouchSwapsTheWindowsChrome()
    {
      Application app = Application.Current!;
      FrontendTheme.Apply(app, "nereus", out _);

      ShellWindow window = new ShellWindow(new Label { Content = "tonight" });
      window.Show();
      Pump();
      object? nereusTemplate = Part(window, "ChromeHeader")!.Template;

      FrontendTheme.Apply(app, "wmc", out _);
      Pump();

      ContentControl header = Part(window, "ChromeHeader")!;
      Assert.That(header.Template, Is.Not.Null.And.Not.SameAs(nereusTemplate),
          "the new skin's header arrived in the same window - nobody had to open another one");
      Assert.That(header.IsVisible, Is.True);

      window.Close();
    }

    [AvaloniaTest]
    public void TheHeaderNamesTheScreenTheViewerReached()
    {
      ShellWindow window = new ShellWindow(new WorkflowManager(), frame => new Label { Content = frame.Label });
      window.Show();
      Pump();

      window.Workflow.Push(new WorkflowFrame("home", "Home", new Label()));
      window.Workflow.Push(new WorkflowFrame("photos", "Photos", new Label()));
      Pump();
      Assert.That(window.Chrome.ScreenTitle, Is.EqualTo("Photos"),
          "the reference's master header named the screen it was showing");

      window.Workflow.Pop();
      Pump();
      Assert.That(window.Chrome.ScreenTitle, Is.EqualTo(string.Empty),
          "back at home the header says nothing - master_home.xaml docks an empty HeaderPanel, because at home "
          + "the fanart is the head line and no word of the application's own belongs over it");

      window.Close();
    }

    [AvaloniaTest]
    public void TheFooterSaysWhatIsPlaying()
    {
      Application app = Application.Current!;
      FrontendTheme.Apply(app, "nereus", out _);

      ShellWindow window = new ShellWindow(new Label { Content = "watching" });
      window.Show();
      Pump();

      window.Chrome.ReportPlaying("Big Buck Bunny", 42, "Playing");
      Pump();

      TextBlock? title = window.GetVisualDescendants().OfType<TextBlock>()
          .FirstOrDefault(t => t.Text == "Big Buck Bunny");
      Assert.That(title, Is.Not.Null,
          "the footer drew the film's name out of the chrome file's own template");

      window.Chrome.ReportStopped();
      Pump();
      Assert.That(window.Chrome.HasMedia, Is.False, "nothing playing means the footer has nothing to say");

      window.Close();
    }

    [AvaloniaTest]
    public void TheSuperlayerSaysOneThingOverTheWholeWindow()
    {
      Application app = Application.Current!;
      FrontendTheme.Apply(app, "nereus", out _);

      ShellWindow window = new ShellWindow(new Label { Content = "quiet" });
      window.Show();
      Pump();

      Assert.That(window.ShowSuperlayer("Superlayer_Volume", "Volume 50 %"), Is.True,
          "the skin knows the volume layer");
      Pump();
      ContentControl overlay = Part(window, "ChromeSuperlayer")!;
      Assert.That(overlay.IsVisible, Is.True);
      Assert.That(overlay.Content, Is.EqualTo("Volume 50 %"));

      window.HideSuperlayer();
      Assert.That(overlay.IsVisible, Is.False, "and it goes away when the moment is over");

      window.Close();
    }
  }
}
