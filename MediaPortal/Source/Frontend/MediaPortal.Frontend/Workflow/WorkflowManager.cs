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

namespace MediaPortal.Frontend.Workflow
{
  /// <summary>
  /// One stop of the viewer's way through the front end: which screen, which model drives it, and what the trail
  /// above it should say about it. The frame keeps the screen's view once it has been built, so popping back to a
  /// stop finds the screen as the viewer left it - the scroll position and the typed search included, not a fresh
  /// copy that forgot everything.
  /// </summary>
  public sealed class WorkflowFrame
  {
    public WorkflowFrame(string stateId, string label, object viewModel)
    {
      if (string.IsNullOrEmpty(stateId))
        throw new ArgumentException("A state the navigation cannot name is a state the navigation cannot return to.",
            nameof(stateId));
      StateId = stateId;
      Label = label ?? "";
      ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    /// <summary>What this stop is called in the code, e.g. <c>"browser"</c>. Stable, never shown.</summary>
    public string StateId { get; }

    /// <summary>What this stop is called in the breadcrumb trail, e.g. the title of the films list.</summary>
    public string Label { get; set; }

    /// <summary>The model the screen was built on: a pop back shows the same reading, not a new one.</summary>
    public object ViewModel { get; }

    /// <summary>
    /// The control the shell shows for this stop, built the first time the stop became the top of the stack and
    /// kept afterwards. <c>null</c> until then; the shell owns building it.
    /// </summary>
    public object? View { get; set; }

    public override string ToString()
    {
      return StateId + " '" + Label + "'";
    }
  }

  /// <summary>
  /// Where the viewer is: a stack of <see cref="WorkflowFrame"/>s, bottomed on the home screen, topped by the
  /// screen the viewer is looking at.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The old client navigated like this and not by opening windows: the workflow manager kept a stack of
  /// navigation contexts, an action pushed a state on it and the back key popped one, so "back" always led to the
  /// exact place the viewer came from and never to a stray window behind a window. The front end had lost that -
  /// every screen was a new window shown over the last one, back was a hardcoded close, and nothing knew where
  /// the viewer actually was. This is that stack again, in the few pieces this front end needs: push, pop, pop to
  /// a named stop for the breadcrumb, and one event so the shell repaints when the way changes.
  /// </para>
  /// <para>
  /// It deliberately knows nothing about Avalonia controls: frames carry their view as <c>object</c> and the
  /// shell decides what to build and show. That keeps "what does back do here" testable without a display, which
  /// is the same reason the reference split the manager out of the window.
  /// </para>
  /// </remarks>
  public sealed class WorkflowManager
  {
    private readonly List<WorkflowFrame> _stack = new List<WorkflowFrame>();

    /// <summary>
    /// Raised after the stack changed - a push, a pop, or a replace - so the shell shows the new top and redraws
    /// the trail. Handlers run on whatever thread moved the stack, which in this front end is always the one the
    /// viewer pressed a key on.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Raised for each frame that left the stack for good - popped back over, or replaced. The frame is gone and
    /// the viewer cannot reach it again, which is the moment its screen puts down whatever it was holding: the
    /// old client disposed the model of a state the workflow left, and a screen watching a server should do no
    /// less. A screen merely pushed past is not removed and hears nothing here.
    /// </summary>
    public event Action<WorkflowFrame>? Removed;

    /// <summary>The stops from home to where the viewer stands, bottom first.</summary>
    public IReadOnlyList<WorkflowFrame> Stack
    {
      get { return _stack; }
    }

    /// <summary>The stop the viewer is looking at, or <c>null</c> before the first one was pushed.</summary>
    public WorkflowFrame? Current
    {
      get { return _stack.Count == 0 ? null : _stack[_stack.Count - 1]; }
    }

    /// <summary>Whether popping would leave nothing on screen - which is where the back key stops asking.</summary>
    public bool IsAtRoot
    {
      get { return _stack.Count <= 1; }
    }

    /// <summary>
    /// Opens one more screen over the current one. The viewer can be back here in one key press, so pushing is
    /// only ever the answer to something the viewer asked for; opening on a guess buries the way back.
    /// </summary>
    public void Push(WorkflowFrame frame)
    {
      if (frame == null)
        throw new ArgumentNullException(nameof(frame));
      _stack.Add(frame);
      Changed?.Invoke();
    }

    /// <summary>
    /// Leaves the current screen. <c>false</c> at the root: back from the home screen is not the workflow's
    /// decision, it is the question of whether the application quits, and only the shell answers that.
    /// </summary>
    public bool Pop()
    {
      if (_stack.Count <= 1)
        return false;
      WorkflowFrame gone = _stack[_stack.Count - 1];
      _stack.RemoveAt(_stack.Count - 1);
      Changed?.Invoke();
      Removed?.Invoke(gone);
      return true;
    }

    /// <summary>
    /// Pops whole stops at once - what a click on the breadcrumb trail does, or what "back to home" means. The
    /// named stop stays on the stack and becomes the screen the viewer sees again. <c>false</c> when no stop of
    /// that name is open, because then the trail lied and the caller has to decide what that means.
    /// </summary>
    public bool PopToState(string stateId)
    {
      for (int i = _stack.Count - 1; i >= 0; i--)
      {
        if (_stack[i].StateId != stateId)
          continue;
        if (i == _stack.Count - 1)
          return true; // Already standing on it; the trail clicked its own last crumb.
        List<WorkflowFrame> closed = new List<WorkflowFrame>();
        for (int above = _stack.Count - 1; above > i; above--)
          closed.Add(_stack[above]);
        _stack.RemoveRange(i + 1, _stack.Count - i - 1);
        Changed?.Invoke();
        foreach (WorkflowFrame gone in closed)
          Removed?.Invoke(gone);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Swaps the top of the stack rather than pushing over it - for the screens that replace where the viewer is
    /// rather than opening from it, like the connection screen handing the application over to the home screen.
    /// The replaced screen is gone, not hidden, because its job is done.
    /// </summary>
    public void ReplaceCurrent(WorkflowFrame frame)
    {
      if (frame == null)
        throw new ArgumentNullException(nameof(frame));
      if (_stack.Count == 0)
        Push(frame);
      else
      {
        WorkflowFrame gone = _stack[_stack.Count - 1];
        _stack[_stack.Count - 1] = frame;
        Changed?.Invoke();
        Removed?.Invoke(gone);
      }
    }
  }
}
