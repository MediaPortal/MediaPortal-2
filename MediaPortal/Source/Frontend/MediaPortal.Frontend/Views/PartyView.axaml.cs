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
using Avalonia.Interactivity;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.Workflow;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The party screen: the buttons that start and steer the music, the queue of songs coming, and the song
  /// the screen has asked the application to play.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Closing this window does not stop the party - the view model belongs to the application, the music was
  /// started through the same player the playback screen shows, and a screen that ended the evening because
  /// somebody pressed Back would be a screensaver with opinions.
  /// </para>
  /// <para>
  /// The keys are the plugin's gestures translated to a remote: N skips, because a party has one question,
  /// and Enter on a row puts that song now, because seeing a song in the queue and wanting it next is the
  /// same wish twice.
  /// </para>
  /// </remarks>
  public partial class PartyView : UserControl, IWorkflowScreen
  {
    private readonly PartyViewModel _party;
    private readonly WorkflowManager? _workflow;
    private readonly Func<WorkflowFrame>? _nowPlayingScreen;
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>
    /// For the XAML loader and the designer: the screen a front end with no server shows.
    /// </summary>
    public PartyView()
        : this(new PartyViewModel())
    {
    }

    /// <param name="workflow">
    /// The navigation this screen opens the playback screen through, or <c>null</c> in tests and the designer,
    /// where there is no workflow to push onto.
    /// </param>
    /// <param name="nowPlayingScreen">
    /// How to build the playback screen's frame, or null on a machine without a player - the same absence that
    /// hides the home screen's playback button hides this screen's, because a button that opens nothing is worse
    /// than no button.
    /// </param>
    public PartyView(PartyViewModel party, WorkflowManager? workflow = null,
        Func<WorkflowFrame>? nowPlayingScreen = null)
    {
      DataContext = party;
      _party = party;
      _workflow = workflow;
      _nowPlayingScreen = nowPlayingScreen;
      InitializeComponent();

      nowPlayingButton.IsVisible = nowPlayingScreen != null;

      // In the order they are drawn: the three buttons over the queue, then the queue itself. The panel on
      // the right is read, not walked.
      _ring.Add(startButton);
      _ring.Add(stopButton);
      _ring.Add(skipButton);
      _ring.Add(queueList);
      if (nowPlayingScreen != null)
        _ring.Add(nowPlayingButton);
      DpadNavigation.Attach(this, _ring);

      // A double-click on the queue means the Enter that plays that song; see HomeWindow for the general reason.
      MouseRemote.Attach(this, queueList);

      // And the screen opens with the start button under the thumb, because that is what the first press of a
      // party evening is for; see HomeWindow for why opening focused matters at all.

      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key == Key.N)
            {
              // N for next: the one press a party exists for.
              _party.SkipCommand.Execute(null);
              args.Handled = true;
              return;
            }

            if ((args.Key == Key.Enter || args.Key == Key.Return) && queueList.IsKeyboardFocusWithin)
            {
              // Enter on a row is the wish to hear that song now; the queue reorders and the application plays.
              _party.PlaySelectedCommand.Execute(null);
              args.Handled = true;
            }
          },
          RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Opens the playback screen over this one - the application's own frame, so the party's song and the
    /// transport belong to the same playback the library started, and a screen already standing on the
    /// playback does not push a second copy of it.
    /// </summary>
    private void OnNowPlaying(object? sender, RoutedEventArgs e)
    {
      if (_nowPlayingScreen == null || _workflow == null)
        return;

      WorkflowFrame? frame = _nowPlayingScreen();
      if (frame == null)
        return;

      if (_workflow.Current != null && _workflow.Current.StateId == frame.StateId)
        return;

      _workflow.Push(frame);
    }
    /// <summary>
    /// Runs every time the workflow shows this screen: the first showing loads, every showing takes
    /// the focus.  See IWorkflowScreen.
    /// </summary>
    public void OnScreenActivated()
    {
 startButton.Focus();
    }

  }
}
