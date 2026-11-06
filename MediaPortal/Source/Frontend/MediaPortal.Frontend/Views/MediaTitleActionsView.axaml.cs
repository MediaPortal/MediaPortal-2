/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
    General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

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
  /// The question a title can be asked - what shall be done with it - put up over the wall that asked it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference raised this as a dialog over whatever was showing, and it raised it from the home wall as well
  /// as from a media list: its tile template bound the action model to the tile's context command
  /// (<c>HomeContentStyles.xaml:743-758</c>, <c>MediaItemsActionModel.cs:212-226</c>). This is the same dialog for
  /// the wall, wearing the skin's dialog frame, with the wall lit behind it and Back landing the viewer on the tile
  /// they pressed.
  /// </para>
  /// <para>
  /// A line on the list <i>is</i> the answer; there is no OK button, exactly as there was none there. Every line
  /// goes through the model, which closes the dialog before it asks for anything - so that a rewritten tile or an
  /// opened detail stands over the wall and not over a question that has already been answered.
  /// </para>
  /// </remarks>
  public partial class MediaTitleActionsView : UserControl, IWorkflowScreen, IDialogScreen
  {
    private readonly List<Control> _ring = new List<Control>();
    private readonly MediaTitleActionsModel? _model;
    private readonly WorkflowManager? _workflow;

    /// <summary>For the XAML loader and the designer.</summary>
    public MediaTitleActionsView()
        : this(null, null)
    {
    }

    /// <summary>
    /// Builds the dialog over the model holding the answers, which the wall built when a tile was pressed - the
    /// dialog is a question about one title, and a second copy of the model could only disagree with the wall
    /// about which tile was pressed.
    /// </summary>
    public MediaTitleActionsView(MediaTitleActionsModel? model, WorkflowManager? workflow)
    {
      _model = model;
      _workflow = workflow;
      DataContext = model;
      InitializeComponent();

      actionList.KeyDown += OnListKey;
      actionList.DoubleTapped += delegate { Answer(); };

      backButton.Click += delegate { Close(); };

      _ring.Add(actionList);
      _ring.Add(backButton);
      DpadNavigation.Attach(this, _ring);
    }

    /// <summary>
    /// The keys that answer: the same ones every list here answers to, and the remote's OK foremost because
    /// the reference's dialog was reached by pressing a tile and finished by pressing one more time on an
    /// answer. Which keys mean OK is the table's business, not this list's.
    /// </summary>
    private void OnListKey(object? sender, KeyEventArgs args)
    {
      if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Select, args))
      {
        Answer();
        args.Handled = true;
      }
    }

    /// <summary>
    /// Gives the answer that is stood on. A line that only explains something is not an answer and is left alone,
    /// rather than being pressed and doing nothing.
    /// </summary>
    private void Answer()
    {
      MediaTitleActionsModel? model = _model;
      MediaTitleAction? action = actionList.SelectedItem as MediaTitleAction;
      if (model == null || action == null || !action.IsCommand)
        return;

      _ = model.PressAsync(action);
    }

    /// <summary>
    /// Takes the dialog away, which leaves the viewer standing on the tile they pressed. The wall is the one that
    /// pops it, since the wall is the screen that pushed it; telling the model the question is over is what makes
    /// that happen.
    /// </summary>
    private void Close()
    {
      if (_model != null)
        _model.Close();
      else
        _workflow?.Pop();
    }

    /// <summary>
    /// The list takes the focus every time this dialog is the screen, and the first answer is stood on: a viewer
    /// who came here by pressing a tile wants to press one more time, not to move first.
    /// </summary>
    public void OnScreenActivated()
    {
      if (actionList.SelectedItem == null && actionList.Items.Count > 0)
        actionList.SelectedIndex = 0;

      actionList.Focus();
      Dispatcher.UIThread.Post(delegate { actionList.Focus(); }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// The dialog holds nothing of its own, so there is nothing to hand back when it stops standing; the tile was
    /// rewritten by the answer itself, from the server's reply.
    /// </summary>
    public void OnScreenClosed()
    {
    }
  }
}
