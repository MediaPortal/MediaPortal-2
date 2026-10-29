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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Services;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// What a dialog body is shown WITH. A skin's dialog frame is a template drawn over the dialog's own control,
  /// and a control that a <c>ContentPresenter</c> takes as its content is drawn under the <i>presenter's</i>
  /// data, which is the body itself. Left to itself, every plain <c>{Binding}</c> inside a dialog asks the body
  /// for the screen's words, finds none, and the dialog opens as an empty panel: a frame, a glow, and no
  /// question standing in it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// That is why the heading of a headed frame is read off <c>$parent[ContentControl]</c> instead of from the
  /// body, and why each dialog body hands its screen's data back to itself: <c>CloseView</c>, the notification
  /// dialog, the television's question about a programme, the menu editor and the wall of faces on the wake.
  /// The wall walked straight into it - the frame measured its own padding, drew its glow around nothing, and
  /// the three people the machine had really read never appeared.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTheDialogFrameKeepsTheScreensData
  {
    /// <summary>
    /// The way out shows its menu inside the frame that carries the words. A dialog nobody can read is a
    /// dialog nobody can answer, and an empty frame is exactly that: the shutdown menu's rows and its heading
    /// both have to survive the dialog frame's hand of the content, or the viewer stands in front of a glow.
    /// </summary>
    [AvaloniaTest]
    public void TheWordsOnTheWayOutStandInsideTheFrameThatCarriesThem()
    {
      Application app = Application.Current;
      Assert.That(FrontendTheme.Apply(app, "nereus", out string themeProblem), Is.EqualTo("nereus"),
          themeProblem ?? "");
      Assert.That(SkinEngine.Apply(app, "nereus", out string skinProblem), Is.True, skinProblem ?? "");
      SkinEngine.SkinsFolder = Path.Combine(Meta("FrontendSource"), "Skins");

      CloseViewModel close = new CloseViewModel(null);
      CloseView view = new CloseView();
      view.DataContext = close;
      Window window = new ShellWindow(view);
      window.Width = 1920;
      window.Height = 1080;
      window.Show();
      Pump();
      try
      {
        ShutdownChoice leaving = close.Choices.First(c => c.Action == ShutdownAction.CloseMP);
        TextBlock words = view.GetVisualDescendants().OfType<TextBlock>()
            .FirstOrDefault(t => t.Text == leaving.Name);
        Assert.That(words, Is.Not.Null, "the way out opened without its words");

        Border frame = words.GetVisualAncestors().OfType<Border>()
            .FirstOrDefault(b => b.BoxShadow.ToString().Contains("120"));
        Assert.That(frame, Is.Not.Null, "the words stood outside any glowing frame");
        Assert.That(frame.Bounds.Height, Is.GreaterThan(150),
            "the frame stood " + frame.Bounds.Height + " high around its words");
        Assert.That(words.GetSelfAndVisualAncestors().Contains(frame), Is.True,
            "the words were shown somewhere else than by the frame");

        // The heading belongs to the frame, not to the body, and it only stands when the body hands its
        // data back through the overlay - the very law this screen once broke.
        TextBlock heading = view.GetVisualDescendants().OfType<TextBlock>()
            .FirstOrDefault(t => t.Text == close.Heading);
        Assert.That(heading, Is.Not.Null, "the way out opened without its heading");
      }
      finally
      {
        window.Close();
      }
    }

    private static void Pump()
    {
      for (int i = 0; i < 40; i++)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(20);
      }
    }

    private static string Meta(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestTheDialogFrameKeepsTheScreensData).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));
      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty);
      return Path.GetFullPath(value!.Value!);
    }
  }
}

