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
using MediaPortal.Frontend.Workflow;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The navigation stack on its own, without a window: what the back key does, where the breadcrumb leads, and
  /// when "back" has to stop. Tested apart from the shell for the same reason the old client kept the workflow
  /// manager out of the window - "where does the viewer land" is the behaviour worth pinning down, and a display
  /// adds nothing to answering it.
  /// </summary>
  [TestFixture]
  public class TestWorkflowManager
  {
    private static WorkflowFrame Frame(string stateId, string label = "")
    {
      return new WorkflowFrame(stateId, label, new object());
    }

    [Test]
    public void BackFromTheRootIsRefusedRatherThanLeavingNothingOnScreen()
    {
      WorkflowManager workflow = new WorkflowManager();
      workflow.Push(Frame("home"));

      Assert.That(workflow.IsAtRoot, Is.True);
      Assert.That(workflow.Pop(), Is.False,
          "popping the home screen would leave the shell showing nothing; quitting is the shell's decision, not the stack's");
      Assert.That(workflow.Current.StateId, Is.EqualTo("home"));
    }

    [Test]
    public void TheWayInIsTheWayOutInTheReverseOrder()
    {
      WorkflowManager workflow = new WorkflowManager();
      workflow.Push(Frame("home"));
      workflow.Push(Frame("browser", "Filmbibliothek"));
      workflow.Push(Frame("details", "The film"));

      Assert.That(workflow.IsAtRoot, Is.False);
      Assert.That(workflow.Pop(), Is.True);
      Assert.That(workflow.Current.StateId, Is.EqualTo("browser"),
          "back leaves exactly the screen the viewer came from");
      Assert.That(workflow.Pop(), Is.True);
      Assert.That(workflow.IsAtRoot, Is.True);
    }

    [Test]
    public void TheBreadcrumbJumpsStraightBackToAnOpenStopAndClosesEverythingAboveIt()
    {
      WorkflowManager workflow = new WorkflowManager();
      workflow.Push(Frame("home"));
      workflow.Push(Frame("browser", "Filmbibliothek"));
      workflow.Push(Frame("browse", "Genres"));
      workflow.Push(Frame("titles", "Comedy"));

      Assert.That(workflow.PopToState("browser"), Is.True);
      Assert.That(workflow.Current.StateId, Is.EqualTo("browser"));
      Assert.That(workflow.Stack.Count, Is.EqualTo(2),
          "the stops above the clicked crumb are done with - the viewer asked to be at the crumb, not one step away from it");
    }

    [Test]
    public void JumpingToACloseOfTheSameNameLandsOnTheNearestOne()
    {
      // Two lists of the same kind can be open under one name, and a trail that carries the same word twice must
      // jump to the one the viewer stands closest under rather than teleporting to the oldest.
      WorkflowManager workflow = new WorkflowManager();
      workflow.Push(Frame("home"));
      workflow.Push(Frame("browser", "first"));
      workflow.Push(Frame("browse"));
      workflow.Push(Frame("browser", "second"));
      workflow.Push(Frame("titles"));

      Assert.That(workflow.PopToState("browser"), Is.True);
      Assert.That(workflow.Current.Label, Is.EqualTo("second"));
      Assert.That(workflow.Stack.Count, Is.EqualTo(4), "the nearest open stop of that name, not the first one ever opened");

      Assert.That(workflow.PopToState("browse"), Is.True);
      Assert.That(workflow.Current.StateId, Is.EqualTo("browse"));

      Assert.That(workflow.PopToState("home"), Is.True);
      Assert.That(workflow.Stack.Count, Is.EqualTo(1), "and the crumb all the way down closes the rest, nearest included");
    }

    [Test]
    public void TheShellHearsAboutEveryMoveAndAboutNoMoveThatDidNotHappen()
    {
      WorkflowManager workflow = new WorkflowManager();
      int changes = 0;
      workflow.Changed += delegate { changes++; };

      workflow.Push(Frame("home"));
      Assert.That(changes, Is.EqualTo(1));

      workflow.Push(Frame("browser"));
      Assert.That(changes, Is.EqualTo(2));

      workflow.Pop();
      Assert.That(changes, Is.EqualTo(3));

      workflow.Pop(); // refused at the root
      Assert.That(changes, Is.EqualTo(3), "a refused pop is not a move, and the shell should not repaint for it");

      Assert.That(workflow.PopToState("nowhere"), Is.False);
      Assert.That(changes, Is.EqualTo(3), "nor is a jump to a stop that is not open");
    }

    [Test]
    public void TheFrameKeepsItsModelRatherThanAskingForANewOneOnTheWayBack()
    {
      object model = new object();
      WorkflowFrame frame = new WorkflowFrame("browser", "Filmbibliothek", model);
      Assert.That(frame.ViewModel, Is.SameAs(model),
          "a screen the viewer returns to shows the same reading it showed on the way in, not a fresh request");

      Assert.That(() => new WorkflowFrame("", "no name", model), Throws.TypeOf<ArgumentException>());
      Assert.That(() => new WorkflowFrame("browser", "no model", null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void ReplacingTheTopIsForTheScreensThatHandOverRatherThanOpen()
    {
      // The connection screen's whole job ends when the server is set; the viewer must not be able to back into
      // it, so it replaces the home screen under itself instead of opening over it.
      WorkflowManager workflow = new WorkflowManager();
      workflow.Push(Frame("home"));
      workflow.ReplaceCurrent(Frame("connection"));

      Assert.That(workflow.Stack.Count, Is.EqualTo(1));
      Assert.That(workflow.Current.StateId, Is.EqualTo("connection"));
      Assert.That(workflow.IsAtRoot, Is.True, "and back from there is the shell's question again, not a pop into a gone screen");

      workflow.ReplaceCurrent(Frame("home"));
      Assert.That(workflow.Pop(), Is.False);
    }
  }
}
