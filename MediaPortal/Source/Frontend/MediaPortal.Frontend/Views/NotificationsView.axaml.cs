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
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The dialog that reads the queue: one notification at a time, with the reference's picture beside the words.
  /// </summary>
  /// <remarks>
  /// <para>
  /// It is a dialog rather than a screen for the reference's reason: a notification is a word spoken to somebody who
  /// is doing something (<c>Skin/Nereus/screens/WatchSystemNotifications.xaml</c> was raised over the master that was
  /// already up, and the household's shelf stayed behind it).
  /// </para>
  /// <para>
  /// Every button goes through the model, and the model says when it has run out of things to say - which is the only
  /// way an empty box ever gets taken away. Back does the same as the reference's Back did: closes the dialog and
  /// leaves the viewer on the screen behind, with whatever is still waiting still waiting.
  /// </para>
  /// </remarks>
  public partial class NotificationsView : UserControl, IWorkflowScreen, IDialogScreen
  {
    private readonly List<Control> _ring = new List<Control>();
    private readonly NotificationsViewModel? _model;
    private readonly WorkflowManager? _workflow;

    /// <summary>For the XAML loader and the designer.</summary>
    public NotificationsView()
        : this(null, null)
    {
    }

    public NotificationsView(NotificationsViewModel? model, WorkflowManager? workflow)
    {
      _model = model;
      _workflow = workflow;
      DataContext = model;
      InitializeComponent();

      okButton.Click += delegate
      {
        _model?.ReadThat();
      };
      handleButton.Click += delegate
      {
        _model?.Handle();
      };
      noMoreButton.Click += delegate
      {
        _model?.ReadNoMore();
      };

      // Enter is the reference's answer to a dialog: it opened this one from a press and finished it with another.
      KeyDown += delegate (object? sender, KeyEventArgs args)
      {
        if (args.Key == Key.Escape)
          return;

        if (args.Key == Key.Enter || args.Key == Key.Space || args.Key == Key.Select)
        {
          _model?.ReadThat();
          args.Handled = true;
        }
      };

      _ring.Add(okButton);
      _ring.Add(handleButton);
      _ring.Add(noMoreButton);
      DpadNavigation.Attach(this, _ring);

      if (model != null)
        model.Finished += delegate { Close(); };
    }

    /// <summary>
    /// Where the dialog's keys start: the first of its buttons that can take them.
    /// </summary>
    /// <remarks>
    /// A focus asked for in the constructor is asked of a control that is not joined to a window yet, and that
    /// answers nothing - which left this dialog standing over the screen behind it with the keyboard still in that
    /// screen's hand: the arrows walked a shelf the viewer could only see blurred, and Enter pressed a tile nobody
    /// meant. So the ask belongs to activation, and it is asked again once the layout has settled, because the
    /// buttons come out of the skin's dialog frame, which arrives a pass after the call.
    /// </remarks>
    public void OnScreenActivated()
    {
      void Ask()
      {
        foreach (Control button in _ring)
        {
          if (button.IsVisible && button.Focus())
            return;
        }
      }

      // Asked twice for two different reasons. The first ask is the one that usually works and costs nothing; the
      // second runs once the layout has settled, which is when the frame's template - and with it the buttons - has
      // really arrived, and when a button has an IsVisible the dialog can trust.
      Ask();
      Dispatcher.UIThread.Post(Ask, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Takes the dialog away. Pops only its own stop, because the model may have sent the viewer somewhere else in
    /// the same breath as reading the last notification, and popping then would walk them back out of the place they
    /// were sent to.
    /// </summary>
    private void Close()
    {
      WorkflowFrame? current = _workflow?.Current;
      if (current != null && current.StateId == "notifications")
        _workflow?.Pop();
    }
  }
}
