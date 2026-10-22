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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;

namespace Tests.Frontend
{
  /// <summary>
  /// What a dialog is asked over is thrown out of focus for the length of the question.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A dialog stays over the screen it is a question about, and that screen keeps living behind it - covers
  /// arrive, the rail lights up, a shelf goes on scrolling. Detail moving under the letters of a question is the
  /// one thing a dialog must not put up with, and dimming alone does not cure it: a dimmed cover is still a
  /// readable cover. Throwing the ground out of focus says what the reference's black wash said - this is the
  /// front of the stage - and takes the readability away instead of the brightness, so the viewer still
  /// recognises where they were standing and nothing there competes with what they are being asked.
  /// </para>
  /// <para>
  /// Being out of focus here means a photograph of the ground, taken small and stretched back, and these tests
  /// are where the two things that can go wrong with that are pinned down. The photograph has to be taken at the
  /// pixels the screen is really drawn in and not at its dependent-pixel size, or on a television that draws one
  /// and a half pixels per dot the whole home comes out magnified and out of step with itself - which is exactly
  /// what a blur the framework draws as a layer of its own does there. And it has to be one photograph for the
  /// length of the question rather than a new one every time something behind it moves.
  /// </para>
  /// <para>
  /// How far the ground is shrunk belongs to the skin, so what the shipped face asks for is walked here off the
  /// real skin and what a silent face leaves standing is walked next to it - because a blur the shell invented,
  /// applied to every skin alike, is a face no skin was asked about.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTheDialogBlursTheScreenBehindIt
  {
    private readonly System.Collections.Generic.Dictionary<string, Control> _views =
        new System.Collections.Generic.Dictionary<string, Control>();

    private string _skinsFolderBefore = "";

    [SetUp]
    public void ForgetTheScreensOfThePreviousWindow()
    {
      // NUnit builds one fixture for all its tests, so a control that already stood in a window that has closed
      // would be a control with a parent the next window cannot take from.
      _views.Clear();
      _skinsFolderBefore = SkinEngine.SkinsFolder;
    }

    [TearDown]
    public void PutEverythingBack()
    {
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
      AssemblyMetadataAttribute? value = typeof(TestTheDialogBlursTheScreenBehindIt).Assembly
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

    /// <summary>A screen of the workflow, built once per stop so that a test can point at it.</summary>
    private Control? BuildScreen(WorkflowFrame frame)
    {
      if (!_views.TryGetValue(frame.StateId, out Control? built))
      {
        if (frame.StateId.StartsWith("ask", StringComparison.Ordinal))
          built = new DialogMarker();
        else if (frame.StateId.StartsWith("film", StringComparison.Ordinal))
          built = new FilmMarker();
        else
          built = new ScreenMarker();

        _views[frame.StateId] = built;
      }

      return built;
    }

    /// <summary>
    /// The layer the thrown-away ground is shown on, which the shell has to name for a probe - and a viewer with
    /// a screen reader - to ask about.
    /// </summary>
    private static Border BackdropLayer(Window window)
    {
      Border? layer = window.GetVisualDescendants().OfType<Border>()
          .FirstOrDefault(b => b.Name == "ChromeBackdrop");

      Assert.That(layer, Is.Not.Null, "the shell hands the layer behind its dialogs a name a probe can ask about");
      return layer!;
    }

    /// <summary>
    /// The brush the photograph is stretched to the window with. How the picture is laid over the window is part
    /// of the deal and not an implementation detail: an image control paints a bitmap at the picture's own
    /// pixels, so a ground photographed a fifth as big as the screen would land as a small blurred square in the
    /// corner with the rest of the television black.
    /// </summary>
    private static IImageBrush? PhotographBrushOn(Window window)
    {
      Control? picture = BackdropLayer(window).Child as Control;
      Assert.That(picture, Is.Not.Null, "the backdrop layer carries the photograph of the ground");

      IImageBrush? brush = (picture as Avalonia.Controls.Shapes.Rectangle)?.Fill as IImageBrush;
      Assert.That(brush, Is.Not.Null, "the photograph is worn as a brush, which fills the surface it is on");
      return brush;
    }

    /// <summary>
    /// The photograph of the ground that layer is showing, or null while it is showing none. A layer that is
    /// away is not throwing anything out of focus, however full its bitmap still is.
    /// </summary>
    private static Bitmap? PhotographOn(Window window)
    {
      return BackdropLayer(window).IsVisible ? PhotographBrushOn(window)?.Source as Bitmap : null;
    }
    /// <summary>
    /// A dialog is asked over a screen that has been thrown out of focus: the question is the only thing on the
    /// television with edges on it.
    /// </summary>
    [AvaloniaTest]
    public void ADialogThrowsTheScreenBehindItOutOfFocus()
    {
      WearTheShippedSkin();

      ShellWindow window = new ShellWindow(new WorkflowManager(), BuildScreen);
      window.Show();
      Pump();

      window.Workflow.Push(new WorkflowFrame("home", "Home", new Label()));
      Pump();

      Assert.That(BackdropLayer(window).IsVisible, Is.False, "standing on the home, the ground is itself");

      window.Workflow.Push(new WorkflowFrame("ask", "Ask", new Label()));
      Pump();

      Assert.That(BackdropLayer(window).IsVisible, Is.True,
          "the home a dialog is asked over is thrown out of focus, not merely dimmed - a dimmed cover is still readable");
      Assert.That(_views["home"].IsVisible, Is.True,
          "and the ground itself is left where it was - it is its photograph the viewer sees, not a screen put away");

      window.Close();
    }

    /// <summary>
    /// How far out of focus is the skin's answer: the ground is photographed at one number-th of its size, and
    /// the number the shipped skin names is the number the shell shrinks by.
    /// </summary>
    [AvaloniaTest]
    public void TheSkinSaysHowFarTheGroundIsShrunk()
    {
      WearTheShippedSkin();

      ShellWindow window = new ShellWindow(new WorkflowManager(), BuildScreen);
      window.Show();
      Pump();

      window.Workflow.Push(new WorkflowFrame("home", "Home", new Label()));
      window.Workflow.Push(new WorkflowFrame("ask", "Ask", new Label()));
      Pump();

      double shrink = SkinEngine.WornNumber("DialogBackdropBlur") ?? 0.0;
      Assert.That(shrink, Is.GreaterThanOrEqualTo(2.0).Within(0.001),
          "the shipped face has to name a distance the shell can shrink by - below two the home behind the "
          + "question stays readable; how far past two it goes is a dial the face turns by eye, and the skin "
          + "carries " + shrink);

      Bitmap? shot = PhotographOn(window);
      Assert.That(shot, Is.Not.Null, "the ground behind the question is a photograph of the screen");

      int wanted = (int) Math.Round(BackdropLayer(window).Bounds.Width * window.RenderScaling / shrink);
      Assert.That(shot!.PixelSize.Width, Is.EqualTo(wanted).Within(1),
          "the photograph is a fifth of the ground wide, which is what shrinking by a fifth is: " + shot.PixelSize);

      window.Close();
    }

    /// <summary>
    /// The photograph covers the whole television. A picture that lands at the size of its own pixels leaves the
    /// home sharp in three quarters of the window and black in the rest, which is worse than not throwing it out
    /// of focus at all - so the stretch is what the viewer sees and is checked here, not left to the control.
    /// </summary>
    [AvaloniaTest]
    public void ThePhotographIsStretchedOverTheWholeWindow()
    {
      WearTheShippedSkin();

      ShellWindow window = new ShellWindow(new WorkflowManager(), BuildScreen);
      window.Show();
      Pump();

      window.Workflow.Push(new WorkflowFrame("home", "Home", new Label()));
      window.Workflow.Push(new WorkflowFrame("ask", "Ask", new Label()));
      Pump();

      IImageBrush? brush = PhotographBrushOn(window);
      Assert.That(brush!.Stretch, Is.EqualTo(Stretch.Fill),
          "the photograph is stretched to the window rather than painted at its own pixels");

      Control picture = (Control) BackdropLayer(window).Child!;
      Assert.That(picture.Bounds.Width, Is.EqualTo(window.Bounds.Width).Within(1.0),
          "the photograph stands over the whole width of the window");
      Assert.That(picture.Bounds.Height, Is.EqualTo(window.Bounds.Height).Within(1.0),
          "and over the whole height of it, so no corner of the home is left sharp or left black");

      window.Close();
    }

    /// <summary>
    /// The photograph is taken at the pixels the screen is really drawn in. On a television that draws one and a
    /// half pixels per dot, a photograph taken at the screen's own size is stretched half as much again when it
    /// goes back up - the home comes out magnified, blocky and out of step with itself, which is the whole
    /// complaint about the blur the framework draws as a layer of its own. Taken at the device's pixels, the
    /// stretch back is the skin's number and nothing else, so the blur is the same blur at every scaling.
    /// </summary>
    [AvaloniaTest]
    public void ThePhotographIsTakenAtThePixelsTheScreenIsDrawnIn()
    {
      WearTheShippedSkin();

      ShellWindow window = new ShellWindow(new WorkflowManager(), BuildScreen);
      window.SetRenderScaling(1.45);
      window.Show();
      Pump();

      window.Workflow.Push(new WorkflowFrame("home", "Home", new Label()));
      window.Workflow.Push(new WorkflowFrame("ask", "Ask", new Label()));
      Pump();

      Bitmap? shot = PhotographOn(window);
      Assert.That(shot, Is.Not.Null);

      double ground = BackdropLayer(window).Bounds.Width;
      double shrink = SkinEngine.WornNumber("DialogBackdropBlur") ?? 1.0;
      Assert.That(window.RenderScaling, Is.EqualTo(1.45).Within(0.01), "the television was asked to draw bigger");
      Assert.That(shot!.PixelSize.Width, Is.EqualTo((int) Math.Round(ground * 1.45 / shrink)).Within(2),
          "the photograph is as much bigger than the screen's own numbers as the television is, so stretching it "
          + "back blurs instead of magnifying: " + shot.PixelSize + " over a " + ground + " wide ground");

      window.Close();
    }

    /// <summary>
    /// What hangs over the ground is a picture the shell softened and not the raw photograph of it. A photograph
    /// taken a fifth as big is a mosaic of the home's edges until somebody blurs the pixels it landed on, and
    /// that softening is the whole answer to "everything is pixelated" - so the shell having gone through it is
    /// held here. Which of the two pictures is being shown cannot be read out of a size, both being the same, so
    /// it is read out of the picture's kind: the softened one is one the shell painted pixel by pixel.
    /// </summary>
    [AvaloniaTest]
    public void ThePhotographIsSoftenedAndNotMerelyShrunk()
    {
      WearTheShippedSkin();

      ShellWindow window = new ShellWindow(new WorkflowManager(), BuildScreen);
      window.Show();
      Pump();

      window.Workflow.Push(new WorkflowFrame("home", "Home", new Label()));
      window.Workflow.Push(new WorkflowFrame("ask", "Ask", new Label()));
      Pump();

      Bitmap? shot = PhotographOn(window);
      Assert.That(shot, Is.Not.Null, "the question is up and the ground under it photographed");
      Assert.That(shot, Is.InstanceOf<WriteableBitmap>(),
          "and the photograph was put out of focus by hand before it was stretched back - shrinking alone is "
          + "what the eye reads as blocks");

      window.Close();
    }
    /// <summary>
    /// Answering takes the ground back: the screen the viewer returns to is the one they have to read, and a
    /// photograph left hanging over it would keep the home out of focus - and out of date - for nothing.
    /// </summary>
    [AvaloniaTest]
    public void AnsweringADialogHandsTheScreenBackSharp()
    {
      WearTheShippedSkin();

      ShellWindow window = new ShellWindow(new WorkflowManager(), BuildScreen);
      window.Show();
      Pump();

      window.Workflow.Push(new WorkflowFrame("home", "Home", new Label()));
      window.Workflow.Push(new WorkflowFrame("ask", "Ask", new Label()));
      Pump();
      Assert.That(PhotographOn(window), Is.Not.Null, "the question is up and the ground under it is soft");

      window.Workflow.Pop();
      Pump();

      Assert.That(BackdropLayer(window).IsVisible, Is.False, "the question is answered and the home is itself again");
      Assert.That(PhotographOn(window), Is.Null, "and the photograph of it is put away, not left behind the home");
      Assert.That(window.GetVisualDescendants().Contains(_views["ask"]), Is.False,
          "and the dialog itself is gone, not left behind out of focus");

      window.Close();
    }

    /// <summary>
    /// A second question is still asked over the same thrown-away ground: the photograph belongs to the ground
    /// and not to one dialog, so stepping from a question into a question of that question changes nothing under
    /// it - and does not take a second picture of a screen that has not moved.
    /// </summary>
    [AvaloniaTest]
    public void ASecondDialogStandsOnTheSamePhotograph()
    {
      WearTheShippedSkin();

      ShellWindow window = new ShellWindow(new WorkflowManager(), BuildScreen);
      window.Show();
      Pump();

      window.Workflow.Push(new WorkflowFrame("home", "Home", new Label()));
      window.Workflow.Push(new WorkflowFrame("ask", "Ask", new Label()));
      Pump();

      Bitmap? first = PhotographOn(window);
      Assert.That(first, Is.Not.Null);

      window.Workflow.Push(new WorkflowFrame("askagain", "Ask again", new Label()));
      Pump();

      Assert.That(PhotographOn(window), Is.SameAs(first),
          "the deeper question stands on the same soft ground, with the same photograph - not a second one under it");

      window.Close();
    }

    /// <summary>
    /// A skin that names no number keeps its dialogs over a sharp background. Nothing here is the shell's taste:
    /// throwing the ground away is the skin's answer, and a skin that does not answer is left exactly as it was.
    /// </summary>
    [AvaloniaTest]
    public void ASkinThatNamesNoBlurLeavesTheScreenBehindSharp()
    {
      ShellWindow window = new ShellWindow(new WorkflowManager(), BuildScreen);
      window.Show();
      Pump();

      window.Workflow.Push(new WorkflowFrame("home", "Home", new Label()));
      window.Workflow.Push(new WorkflowFrame("ask", "Ask", new Label()));
      Pump();

      Assert.That(SkinEngine.WornNumber("DialogBackdropBlur"), Is.Null,
          "the palette worn here says nothing about throwing a dialog's ground out of focus");
      Assert.That(BackdropLayer(window).IsVisible, Is.False, "and so its screen behind stays as it is");

      window.Close();
    }

    /// <summary>
    /// A film is left alone behind a question. It is the one screen whose every frame is new, so a photograph of
    /// it is stale before it is drawn, and a dialog asked over a film is a question about the film rather than a
    /// question the film has to step back for.
    /// </summary>
    [AvaloniaTest]
    public void AFilmIsNotThrownOutOfFocusBehindAQuestion()
    {
      WearTheShippedSkin();

      ShellWindow window = new ShellWindow(new WorkflowManager(), BuildScreen);
      window.Show();
      Pump();

      window.Workflow.Push(new WorkflowFrame("film", "Film", new Label()));
      window.Workflow.Push(new WorkflowFrame("ask", "Ask", new Label()));
      Pump();

      Assert.That(SkinEngine.WornNumber("DialogBackdropBlur"), Is.Not.Null,
          "the worn skin does ask for a thrown-away ground behind its dialogs");
      Assert.That(BackdropLayer(window).IsVisible, Is.False,
          "and a film is still left as it is, because a dialog over a film is a question about that film");

      window.Close();
    }

    /// <summary>The marker a screen answers with: an ordinary stop of the workflow.</summary>
    private sealed class ScreenMarker : ContentControl
    {
    }

    /// <summary>The marker a film answers with: a screen that wants the whole television to itself.</summary>
    private sealed class FilmMarker : ContentControl, IFullScreenScreen
    {
    }

    /// <summary>The marker a dialog answers with: the same control, asking to be raised over what is behind.</summary>
    private sealed class DialogMarker : ContentControl, IDialogScreen
    {
    }
  }
}