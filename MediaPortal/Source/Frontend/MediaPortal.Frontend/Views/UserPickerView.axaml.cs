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
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The household asked who is watching: the wall of faces, worn on the skin's own dialog frame over the wake
  /// that asked. It is a dialog and not a screen, so the wake stays behind it - out of focus and washed dark, the
  /// way the shell treats every question - and the answer is handed back to that wake: a face with four numbers
  /// waiting behind it takes the boxes, a person without any simply arrives, and the back key leaves the room
  /// exactly as it was.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The wall used to be a panel inside the wake, which cost the viewer three things at once: nothing behind the
  /// faces was blurred (only a dialog frame gets the shell's photograph of the ground), the wake's own ring went
  /// on lighting the room around the panel so that the frame's white glow read as orange, and the glow was drawn
  /// inside the screen area rather than on the dialog layer the shell raises questions on. Nothing about the faces
  /// themselves changed on the way here - a face is a circle and a name - but a dialog is a dialog.
  /// </para>
  /// <para>
  /// The keyboard belongs to the wall while it stands, which is why <see cref="OnScreenActivated"/> stands on the
  /// first face: a list that opens with nothing stood on answers no Enter. The walk between faces is the list's
  /// own, and the back key is answered here rather than left to the shell so that the wake hears the wall went
  /// and stops refusing the digits it will be asked for next.
  /// </para>
  /// </remarks>
  public partial class UserPickerView : UserControl, IWorkflowScreen, IDialogScreen
  {
    private readonly StartupViewModel? _model;
    private readonly WorkflowManager? _workflow;

    /// <summary>For the XAML loader and the designer.</summary>
    public UserPickerView()
        : this(null, null)
    {
    }

    public UserPickerView(StartupViewModel? model, WorkflowManager? workflow)
    {
      _model = model;
      _workflow = workflow;
      DataContext = model;
      InitializeComponent();

      // Enter takes the face stood on, like the server list on the connection screen; a double tap is the
      // same answer for the pointer.
      othersList.AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key != Key.Enter && args.Key != Key.Return)
              return;
            if (othersList.SelectedItem is StartupViewModel.HouseholdFace face)
            {
              args.Handled = true;
              Take(face);
            }
          },
          RoutingStrategies.Tunnel);
      othersList.DoubleTapped += delegate
      {
        if (othersList.SelectedItem is StartupViewModel.HouseholdFace face)
          Take(face);
      };

      // The back key has an honest meaning over a wall of faces: nobody was picked, and the wake takes its
      // question back. It is answered on the way in, before the list - which would only walk a selection
      // about - and before the shell, which would pop the dialog without ever telling the wake.
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (!Settings.KeyBindings.Current.Is(Settings.KeyAction.Back, args.Key)
                && args.Key != Key.Escape && args.Key != Key.Back)
              return;

            args.Handled = true;
            Leave();
          },
          RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Where the wall's keys start: the first face of the household, stood on and looked at. Asked again once
    /// the layout has settled, because the faces come out of the skin's dialog frame, which arrives a pass
    /// after the call.
    /// </summary>
    public void OnScreenActivated()
    {
      void Ask()
      {
        if (othersList.SelectedIndex < 0 && othersList.ItemCount > 0)
          othersList.SelectedIndex = 0;
        othersList.Focus();
      }

      Ask();
      Dispatcher.UIThread.Post(Ask, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// A face was picked. The dialog goes first and the model hears second: the answer a face gives can
    /// replace the whole trail - a person without numbers simply arrives - and a question still on the stack
    /// while that happens is popped out of a workflow that has already moved on.
    /// </summary>
    private void Take(StartupViewModel.HouseholdFace face)
    {
      Close();
      _model?.PickFace(face);
    }

    /// <summary>The back key: the wall steps aside and the wake is asked to go on asking - or, when it was the
    /// home's person button that opened the door, to let the room stay as it was.</summary>
    private void Leave()
    {
      StartupViewModel? model = _model;
      Close();
      model?.StayAsWas();
    }

    /// <summary>
    /// Takes the dialog away. Pops only its own stop, because picking a face sends the viewer off the wake in
    /// the same breath, and popping then would walk them back out of the place they were sent to.
    /// </summary>
    private void Close()
    {
      WorkflowFrame? current = _workflow?.Current;
      if (current != null && current.StateId == "household")
        _workflow?.Pop();
    }
  }
}
