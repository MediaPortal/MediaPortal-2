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

using Avalonia.Controls;

namespace MediaPortal.Frontend.Workflow
{
  /// <summary>
  /// What a screen promises the shell that hosts it: to say where the focus belongs the moment it becomes the
  /// screen the viewer is looking at.
  /// </summary>
  /// <remarks>
  /// A window used to learn this from its <c>Opened</c> event, which happened exactly once. Inside the shell a
  /// screen appears more than once in its own lifetime - pushed, left, popped back to with its state kept - and
  /// the focus has to be taken again on every reappearance, which is the one thing the shell knows and the screen
  /// cannot. Everything else about appearing - what to read, what to highlight - the screens already do for
  /// themselves.
  /// </remarks>
  public interface IWorkflowScreen
  {
    /// <summary>
    /// Called every time this control becomes the top of the workflow, on first appearance and on every return.
    /// Implementations take the focus to where their keys start; a screen that does nothing here answers nothing
    /// until the viewer presses an arrow at random.  The default does nothing, for screens - like the playback
    /// screen - that are all buttons and want the focus nowhere in particular.
    /// </summary>
    void OnScreenActivated()
    {
    }

    /// <summary>
    /// Where the shell's net - not the screen's own wish - starts looking for the keyboard, for the moment the
    /// window notices it stands on none. The default says nothing, and the shell then takes the first control
    /// in tree order that takes the focus; a screen whose first such control stands off-screen - a menu rail
    /// slid behind the edge - says itself what the answer is, or the net drops the keyboard into the hidden
    /// rail and every arrow the viewer presses is spent on a list nobody can see.
    /// </summary>
    Control? KeysStartAt()
    {
      return null;
    }

    /// <summary>
    /// Called once when the frame this screen was built for left the workflow for good - popped past on the way
    /// back, or replaced - so the screen can put down what it was holding: an open reading, a subscription, a
    /// player. A screen that was merely covered by the next one is not closed and hears nothing; its state stays
    /// exactly as the viewer left it for the way back.  The default does nothing, for screens that hold nothing.
    /// </summary>
    void OnScreenClosed()
    {
    }
  }
}
