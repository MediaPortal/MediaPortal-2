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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The screen that ends the application - this front end's answer to the reference's
  /// <c>dialogSystemState</c>, which was the way a viewer standing at the television left.
  /// </summary>
  /// <remarks>
  /// <para>
  /// It is a screen of the workflow and not a window: the reference learned the hard way that dialogs which
  /// are windows get back buttons that close the wrong thing. This one is pushed like any other stop, so the
  /// trail names it and the back key steps off it, and the two buttons say the two answers the dialog ever
  /// had - leave, or go back the way you came.
  /// </para>
  /// <para>
  /// "Leave" is the application's own way out and nothing else: the same lifetime the window was built on
  /// shuts down, which is how the disposal of the player and the reporter already wired there runs. Closing a
  /// window by hand would leave the desktop lifetime waiting for a window that has gone.
  /// </para>
  /// </remarks>
  public partial class CloseView : UserControl, IWorkflowScreen
  {
    private readonly WorkflowManager? _workflow;

    public CloseView()
        : this(new CloseViewModel(null), null)
    {
    }

    public CloseView(CloseViewModel model, WorkflowManager? workflow = null)
    {
      DataContext = model;
      _workflow = workflow;
      InitializeComponent();
    }

    /// <summary>
    /// Where the focus starts: on "Back". A screen whose default answer is the one that cannot be taken
    /// back is a screen that eventually closes an application nobody meant to close, and the reference's
    /// power dialogs are the one place this front end keeps that rule in its markup.
    /// </summary>
    public void OnScreenActivated()
    {
      backButton.Focus();
    }

    private void OnClose(object? sender, RoutedEventArgs args)
    {
      if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        desktop.Shutdown();
    }

    private void OnBack(object? sender, RoutedEventArgs args)
    {
      _workflow?.Pop();
    }
  }
}
