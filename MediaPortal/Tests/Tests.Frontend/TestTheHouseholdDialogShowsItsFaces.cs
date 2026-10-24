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
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The wall of faces the wake opens when somebody presses the round picture: every face of the household
  /// stands on the worn skin's dialog frame, a face is nothing but a round picture and its name, and the frame
  /// is a frame - ground, outline and glow - rather than a bar.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A body that stands inside <c>DialogFrameTemplate</c> is shown <i>by</i> the frame, and the frame hands its
  /// content to its own <c>ContentPresenter</c> as that content's data context: inside the frame
  /// <c>{Binding}</c> is the dialog's body and not the screen, which is exactly why the heading of a headed
  /// frame reads itself off <c>$parent[ContentControl]</c>. The first wall of faces bound its list to
  /// <c>Household</c> plainly, so the list asked the list for a household, found none, and the frame measured
  /// its padding and nothing else - a sixty-pixel band with a glow around an empty room, on a screen that
  /// really did hold three people.
  /// </para>
  /// <para>
  /// So these are measurements on a shown screen: the data has to reach the containers, the containers have to
  /// be laid out, and the frame around them has to have grown big enough to be the dialog the skin drew.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTheHouseholdDialogShowsItsFaces
  {
    private const string ONE_PIXEL_PNG =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFAAH/q842iQAAAABJRU5ErkJggg==";

    /// <summary>
    /// Every face the server keeps stands on the wall with its name under it - the one standing at the wake
    /// included, since a wall one could not answer by staying would be no wall - and a person whose picture
    /// the household uploaded wears that picture, while a person without one wears their initial.
    /// </summary>
    [AvaloniaTest]
    public void EveryFaceOfTheHouseholdStandsOnTheWallWithItsNameUnderIt()
    {
      OnTheWall(delegate (StartupViewModel wake, ListBox faces)
      {
        Assert.That(faces.Items.Count, Is.EqualTo(3),
            "the frame showed " + faces.Items.Count + " of the household's 3 faces");
        Assert.That(faces.Bounds.Height, Is.GreaterThan(150),
            "the wall of faces measured " + faces.Bounds.Height + " of a height");
        string[] names = new[] { "Gerrit", "Anne", "Martina mit langem Namen" };
        for (int i = 0; i < names.Length; i++)
        {
          Control container = faces.ContainerFromIndex(i) as Control;
          Assert.That(container, Is.Not.Null, "face " + i + " was never realized");
          Assert.That(container.Bounds.Height, Is.GreaterThan(150),
              "the face of " + names[i] + " measured nothing");
          Assert.That(container.GetVisualDescendants().OfType<TextBlock>()
              .Any(t => t.Text == names[i]), Is.True, "no name stood under the face of " + names[i]);
        }

        // The first two brought a picture, the third did not and so wears an initial.
        Assert.That(HasPicture(faces, 0), Is.True, "a face whose household uploaded a picture showed none");
        Assert.That(HasPicture(faces, 1), Is.True, "a face whose household uploaded a picture showed none");
        Assert.That(HasPicture(faces, 2), Is.False, "a face without a picture showed one");
        Assert.That(Initials(faces, 2), Is.EqualTo("M"));
      });
    }

    /// <summary>
     /// The dialog stands on the skin's frame, and a frame is only a frame while it holds its body: the ground,
     /// the outline and the glow are drawn around the wall of faces, so what a viewer sees is the slab the close
     /// screen and the notification shelf wear - not a collapsed band around nothing.
     /// </summary>
    [AvaloniaTest]
    public void TheDialogStandsOnTheSkinsFrameRatherThanOnACollapsedBar()
    {
      OnTheWall(delegate (StartupViewModel wake, ListBox faces)
      {
        Border frame = faces.GetSelfAndVisualAncestors().OfType<Border>().First();
        Assert.That(frame.Bounds.Height, Is.GreaterThan(200),
            "the dialog frame was " + frame.Bounds.Height + " high, which is padding around nothing");
        Assert.That(frame.BoxShadow.Count, Is.GreaterThanOrEqualTo(2), "the frame wore no glow");
        TestContext.Out.WriteLine("shadow " + frame.BoxShadow + " | ground " + frame.Background + " | outline "
            + frame.BorderBrush + " | corner " + frame.CornerRadius);
        Assert.That(frame.BoxShadow.ToString(), Does.Contain("120"), "the glow does not reach out");
        Assert.That(frame.CornerRadius, Is.EqualTo(new CornerRadius(25)), "the frame did not wear the skin\u0027s corner");
        ContentControl host = faces.GetSelfAndVisualAncestors().OfType<ContentControl>().First();
        Assert.That(host.Bounds.Width, Is.LessThanOrEqualTo(760),
            "the frame grew past the width the skin gives a dialog");
      });
    }

    /// <summary>
     /// What the wall of faces stands IN FRONT OF, which is the half of a dialog the frame cannot draw itself:
     /// the shell photographs the screen the question is asked over and throws it out of focus, and the dialog
     /// ground washes that photograph dark with the skin's own wash. Asked from inside the wake, neither half
     /// happened - the wake went on being sharp, its ring went on lighting the room, and the panel read as
     /// pasted on rather than as lit.
     /// </summary>
     [AvaloniaTest]
     public void TheWallIsAskedOverAWakeThatHasBeenThrownOutOfFocus()
     {
       OnTheWall(delegate (StartupViewModel wake, ListBox faces, ShellWindow window)
       {
         Border backdrop = BackdropLayer(window);
         Assert.That(backdrop, Is.Not.Null, "the shell keeps the layer behind its dialogs where a probe can find it");
         Assert.That(backdrop.IsVisible, Is.True,
             "the wake a question is asked over went on standing sharp behind the faces");

         Control picture = backdrop.Child as Control;
         Avalonia.Media.IImageBrush photograph = (picture as Avalonia.Controls.Shapes.Rectangle)?.Fill
             as Avalonia.Media.IImageBrush;
         Assert.That(photograph?.Source, Is.Not.Null, "the ground behind the faces is not a photograph of the screen");

         // The wash belongs to the dialog rather than to the frame, and it covers the whole television: a
         // rectangle of scrim around the panel would leave the room's corners as bright as the question.
         UserControl dialog = faces.GetSelfAndVisualAncestors().OfType<UserControl>().First();
         TestContext.Out.WriteLine("dialog " + dialog.GetType().Name + " classes=["
             + string.Join(",", dialog.Classes) + "] background=" + dialog.Background);
         Assert.That(dialog.Classes.Contains("nereus-dialog-screen"), Is.True,
             "the wall of faces did not wear the dialog ground the skin washes its questions with");
         Assert.That(dialog.Background, Is.InstanceOf<ISolidColorBrush>(),
             "the dialog ground is not one colour, so it washes nothing");
         ISolidColorBrush? wash = dialog.Background as ISolidColorBrush;
         Assert.That(wash.Color.ToUInt32(), Is.EqualTo((Application.Current.FindResource("DialogWashBrush")
             as ISolidColorBrush).Color.ToUInt32()), "the dialog wore a wash the skin never named");
         Assert.That(dialog.Bounds.Width, Is.EqualTo(window.Bounds.Width).Within(1),
             "the wash stopped short of the window's width");
         Assert.That(dialog.Bounds.Height, Is.EqualTo(window.Bounds.Height).Within(1),
             "the wash stopped short of the window's height");

         TestContext.Out.WriteLine("wash " + wash.Color + " | photograph " + photograph.Source);
       });
     }

     /// <summary>
     /// A glow is drawn outside the bounds of the panel that carries it, so a dialog whose halo is cut into a
     /// straight edge is a dialog somebody clipped. Nothing between the frame and the window may cut it: every
     /// surface the dialog stands on has to leave the glow the room its blur reaches, on all four sides - which
     /// is what raising the question on the shell's dialog layer, rather than on a panel inside a screen, is for.
     /// </summary>
     [AvaloniaTest]
     public void TheGlowOfTheWallIsNotCutByAnythingBetweenItAndTheWindow()
     {
       OnTheWall(delegate (StartupViewModel wake, ListBox faces, ShellWindow window)
       {
         // The panel that carries the glow, which is the frame and not the ListBox's own chrome.
         Border frame = faces.GetSelfAndVisualAncestors().OfType<Border>().First(b => b.BoxShadow.Count >= 2);
         const double GLOW = 120;
         Rect inside = OnTheWindow(frame, window);

         // What the frame itself is made of, up to and including the panel that carries the glow.
         for (Visual? within = faces; within != null && within != frame; within = within.GetVisualParent())
         {
           Control pieceControl = within as Control;
           TestContext.Out.WriteLine(((pieceControl != null && pieceControl.ClipToBounds) || within.Clip != null ? "CUTS  " : "open  ")
               + Describe(within) + " " + OnTheWindow(within, window));
         }

         // The surfaces the dialog stands ON, and only those of them that cut what they hold: a surface that
         // clips at its own bounds clips the halo with it, because a shadow is drawn outside the bounds of the
         // thing that carries it.
         Visual? visual = frame.GetVisualParent();
         while (visual != null && !(visual is Window))
         {
           Control surfaceControl = visual as Control;
           bool cuts = (surfaceControl != null && surfaceControl.ClipToBounds) || visual.Clip != null;
           Rect surface = OnTheWindow(visual, window);
           TestContext.Out.WriteLine((cuts ? "CUTS  " : "open  ") + Describe(visual) + " " + surface);
           if (cuts)
           {
             Assert.That(surface.X, Is.LessThanOrEqualTo(inside.X - GLOW),
                 Describe(visual) + " cuts the glow at the left");
             Assert.That(surface.Y, Is.LessThanOrEqualTo(inside.Y - GLOW),
                 Describe(visual) + " cuts the glow at the top");
             Assert.That(surface.Right, Is.GreaterThanOrEqualTo(inside.Right + GLOW),
                 Describe(visual) + " cuts the glow at the right");
             Assert.That(surface.Bottom, Is.GreaterThanOrEqualTo(inside.Bottom + GLOW),
                 Describe(visual) + " cuts the glow below");
           }

           visual = visual.GetVisualParent();
         }

         Assert.That(visual, Is.Not.Null, "the dialog was not standing in a window at all");
         TestContext.Out.WriteLine("frame " + inside + " in " + window.Bounds);
         Assert.That(inside.X, Is.GreaterThanOrEqualTo(GLOW), "the window cuts the glow at the left");
         Assert.That(window.Bounds.Width - inside.Right, Is.GreaterThanOrEqualTo(GLOW), "the glow was cut at the right");
         Assert.That(inside.Y, Is.GreaterThanOrEqualTo(GLOW), "the window cuts the glow at the top");
         Assert.That(window.Bounds.Height - inside.Bottom, Is.GreaterThanOrEqualTo(GLOW), "the glow was cut below");
       });
     }

     /// <summary>
     /// Where a visual really stands: its bounds are written in its parent's coordinates, so a surface higher up
     /// the tree cannot be compared with the frame until both are said in the window's.
     /// </summary>
     private static Rect OnTheWindow(Visual visual, Visual window)
     {
       Point? topLeft = visual.TranslatePoint(default, window);
       Assert.That(topLeft, Is.Not.Null, Describe(visual) + " has no place on the window to be measured at");
       return new Rect(topLeft!.Value, visual.Bounds.Size);
     }

     private static string Describe(Visual visual)
     {
       Control control = visual as Control;
       return visual.GetType().Name + (string.IsNullOrEmpty(control?.Name) ? "" : " '" + control.Name + "'");
     }

     /// <summary>
     /// A face the keyboard stands on is lit around the circle, the way the wake's own picture is lit one row
     /// above. It is not a selected row: the orange block a list paints behind a standing row and the square
     /// frame the platform draws around a focused control are both furniture a wall of pictures never wore.
     /// </summary>
    [AvaloniaTest]
    public void AStandingFaceLightsItsCircleAndWearsNoRowsOrangeBlock()
    {
      OnTheWall(delegate (StartupViewModel wake, ListBox faces)
      {
        TestContext.Out.WriteLine("padding " + (faces.ContainerFromIndex(1) as ListBoxItem).Padding
            + " adorner " + ((object)(faces.ContainerFromIndex(1) as ListBoxItem).FocusAdorner == null));
        Assert.That(faces.SelectedIndex, Is.EqualTo(0),
            "the wall opened with nothing stood on, so an answer had nothing to take");
        ListBoxItem second = faces.ContainerFromIndex(1) as ListBoxItem;
        Assert.That(second, Is.Not.Null);
        Assert.That(second.FocusAdorner, Is.Null, "a square frame stood around the face");
        Assert.That(second.Background, Is.Not.SameAs(Application.Current.FindResource("AccentBrush")),
            "the row's orange block stood behind the face");
        Assert.That(second.Background, Is.InstanceOf<ISolidColorBrush>(),
            "a standing face wore " + second.Background + " behind it");
        Assert.That(((ISolidColorBrush)second.Background).Color, Is.EqualTo(Colors.Transparent),
            "a standing face wore " + second.Background + " behind it");

        Border circle = Circle(second);
        Assert.That(circle, Is.Not.Null, "the face had no circle of its own");
        Assert.That(((ISolidColorBrush)circle.BorderBrush).Color, Is.EqualTo(Color.Parse("#55E2621B")),
            "a face at rest has no ring waiting to light");

        Assert.That(second.Focus(), Is.True);
        for (int i = 0; i < 10; i++)
          Dispatcher.UIThread.RunJobs();

        Border lit = Circle(second);
        Assert.That(((ISolidColorBrush)lit.BorderBrush).Color, Is.EqualTo(Color.Parse("#FFFFB066")),
            "the face the keyboard stood on stayed dark");
        Assert.That(lit.BorderThickness.Top, Is.EqualTo(4));
      });
    }

    private static bool HasPicture(ListBox faces, int index)
    {
      Control container = faces.ContainerFromIndex(index) as Control;
      Assert.That(container, Is.Not.Null);
      Image picture = container.GetVisualDescendants().OfType<Image>().FirstOrDefault();
      return picture != null && picture.IsEffectivelyVisible && picture.Source != null;
    }

    private static string Initials(ListBox faces, int index)
    {
      Control container = faces.ContainerFromIndex(index) as Control;
      Assert.That(container, Is.Not.Null);
      TextBlock initial = container.GetVisualDescendants().OfType<TextBlock>()
          .FirstOrDefault(t => t.IsEffectivelyVisible && t.FontSize > 40);
      Assert.That(initial, Is.Not.Null, "a face without a picture wore no initial");
      return initial.Text;
    }

    private static Border Circle(Visual container)
    {
      return container.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Classes.Contains("faceCircle"));
    }

    /// <summary>
    /// The wall as the machine raises it: the wake at 1920x1080 with the household read, the round picture
    /// pressed, and the shell putting the dialog of faces over the screen it was asked on. Everything these
    /// tests measure is therefore measured on a live shell, because whether the ground behind a question is
    /// thrown out of focus and whether a glow has room to fall off are both things the shell decides.
    /// </summary>
    private static void OnTheWall(Action<StartupViewModel, ListBox> body)
    {
      OnTheWall(delegate (StartupViewModel wake, ListBox faces, ShellWindow window)
      {
        body(wake, faces);
      });
    }

    private static void OnTheWall(Action<StartupViewModel, ListBox, ShellWindow> body)
    {
      Application app = Application.Current;
      Assert.That(FrontendTheme.Apply(app, "nereus", out string themeProblem), Is.EqualTo("nereus"),
          themeProblem ?? "");
      Assert.That(SkinEngine.Apply(app, "nereus", out string skinProblem), Is.True, skinProblem ?? "");
      SkinEngine.SkinsFolder = Path.Combine(Meta("FrontendSource"), "Skins");

      FakeWakeServer server = new FakeWakeServer();
      server.People = new[]
      {
        new UserInfo { ProfileId = Guid.NewGuid(), Name = "Gerrit", Kind = "User", HasPassword = true,
            Image = ONE_PIXEL_PNG },
        new UserInfo { ProfileId = Guid.NewGuid(), Name = "Anne", Kind = "User", Image = ONE_PIXEL_PNG },
        new UserInfo { ProfileId = Guid.NewGuid(), Name = "Martina mit langem Namen", Kind = "Admin" },
      };

      StartupViewModel wake = new StartupViewModel(() => server,
          delegate (string name, string pin) { return System.Threading.Tasks.Task.FromResult(true); },
          null, TimeSpan.FromMilliseconds(10));
      wake.ForSwitching = true;

      WorkflowManager workflow = new WorkflowManager();
      StartupView wakeView = new StartupView(wake, workflow);
      ShellWindow window = new ShellWindow(workflow, delegate (WorkflowFrame frame)
      {
        if (frame.StateId == "startup")
          return wakeView;
        if (frame.StateId == "household")
          return new UserPickerView((StartupViewModel)frame.ViewModel, workflow);
        return null;
      });
      window.Width = 1920;
      window.Height = 1080;
      window.Show();
      window.Workflow.Push(new WorkflowFrame("startup", "Welcome", wake));
      Pump();

      try
      {
        wake.SwitchUser();
        Pump();
        Assert.That(wake.Switching, Is.True, "the wake refused to open its wall of faces");
        Assert.That(window.Workflow.Current?.StateId, Is.EqualTo("household"),
            "the household was not raised as a dialog of the workflow, but the wake is what the question is asked over");
        ListBox faces = window.GetVisualDescendants().OfType<ListBox>().Single(l => l.Name == "othersList");
        body(wake, faces, window);
      }
      finally
      {
        window.Close();
      }
    }

    /// <summary>The layer the shell shows the thrown-away ground on, or null while it shows none.</summary>
    private static Border BackdropLayer(Window window)
    {
      return window.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "ChromeBackdrop");
    }

    /// <summary>The household as the machine keeps it: three people, one of them standing at the wake.</summary>
    private sealed class FakeWakeServer : IUsersApi
    {
      public UserInfo[] People = Array.Empty<UserInfo>();

      public System.Threading.Tasks.Task<UsersAnswer> GetUsersAsync(CancellationToken cancellation = default)
      {
        return System.Threading.Tasks.Task.FromResult(new UsersAnswer { Users = People });
      }

      public System.Threading.Tasks.Task<UserAnswer> CreateUserAsync(CreateUserWish wish,
          CancellationToken cancellation = default) => throw new NotSupportedException();

      public System.Threading.Tasks.Task<UserAnswer> UpdateUserAsync(Guid profileId, UpdateUserWish wish,
          CancellationToken cancellation = default) => throw new NotSupportedException();

      public System.Threading.Tasks.Task<UserAnswer> DeleteUserAsync(Guid profileId,
          CancellationToken cancellation = default) => throw new NotSupportedException();
    }

    private static void Pump()
    {
      for (int i = 0; i < 60; i++)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(20);
      }
    }

    private static string Meta(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestTheHouseholdDialogShowsItsFaces).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));
      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty);
      return Path.GetFullPath(value!.Value!);
    }
  }
}





