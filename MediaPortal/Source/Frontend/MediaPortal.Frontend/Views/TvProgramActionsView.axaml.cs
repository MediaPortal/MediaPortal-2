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
  /// The question a programme in the guide can be asked, put up over the guide that asked it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference raised this as a dialog and never as a screen (<c>SlimTvGuideModelBase.ShowProgramActions</c>
  /// ended in <c>SlimTvExtScheduleModel.ShowDialog</c>), so it is one here too: the guide stays lit behind it, the
  /// header and the trail step back, and Back lands the viewer on the block they were standing on. The list carries
  /// the answers the model worked out - which is the whole point of the split, since three of the reference's
  /// answers depended on a question it asked the backend first and a dialog that guessed would have been worse than
  /// no dialog at all.
  /// </para>
  /// <para>
  /// A line on the list <i>is</i> the answer; there is no OK button, exactly as there was none there. Every line
  /// goes through the model, which closes the dialog before it asks for anything - so that getting to the channel
  /// opens the picture over the guide rather than over a question that has already been answered.
  /// </para>
  /// </remarks>
  public partial class TvProgramActionsView : UserControl, IWorkflowScreen, IDialogScreen
  {
    private readonly List<Control> _ring = new List<Control>();
    private readonly TvGuideViewModel? _guide;
    private readonly WorkflowManager? _workflow;

    /// <summary>For the XAML loader and the designer.</summary>
    public TvProgramActionsView()
        : this(null, null)
    {
    }

    /// <summary>
    /// Builds the dialog over the guide's own model, which is the one holding the answers - the dialog is a question
    /// about that guide, and a second copy of the model could only disagree with it about which programme was picked.
    /// </summary>
    public TvProgramActionsView(TvGuideViewModel? guide, WorkflowManager? workflow)
    {
      _guide = guide;
      _workflow = workflow;
      DataContext = guide;
      InitializeComponent();

      actionList.KeyDown += OnListKey;
      actionList.DoubleTapped += delegate { Answer(); };

      cancelButton.Click += delegate { Close(); };

      _ring.Add(actionList);
      _ring.Add(cancelButton);
      DpadNavigation.Attach(this, _ring);
    }

    /// <summary>
    /// The keys that answer: the same ones every list here answers to, and Enter foremost because the reference's
    /// dialog was reached by pressing Enter on a programme and finished by pressing it once more on an answer.
    /// </summary>
    private void OnListKey(object? sender, KeyEventArgs args)
    {
      if (args.Key == Key.Enter || args.Key == Key.Space || args.Key == Key.Select)
      {
        Answer();
        args.Handled = true;
      }
    }

    /// <summary>
    /// Gives the answer that is stood on. Every answer goes through the model, including getting to the channel:
    /// the model closes the dialog first and only then asks to watch, so that the picture is opened over the guide
    /// and not over a question that has already been answered.
    /// </summary>
    private void Answer()
    {
      TvGuideViewModel? guide = _guide;
      TvProgramAction? action = actionList.SelectedItem as TvProgramAction;
      if (guide == null || action == null || !action.IsCommand)
        return;

      _ = guide.RunProgramActionAsync(action);
    }

    /// <summary>
    /// Takes the dialog away, which leaves the viewer where they stood in the grid. The guide is the one that pops
    /// it, since the guide is the screen that pushed it; telling the model the question is over is what makes that
    /// happen, and it is also what the guide needs to hear to ask the backend again what is being recorded.
    /// </summary>
    private void Close()
    {
      if (_guide != null)
        _guide.CloseProgramActions();
      else
        _workflow?.Pop();
    }

    /// <summary>
    /// The list takes the focus every time this dialog is the screen, and the first answer is stood on: a viewer who
    /// came here by pressing a programme wants to press one more time, not to move first.
    /// </summary>
    public void OnScreenActivated()
    {
      if (actionList.SelectedItem == null && actionList.Items.Count > 0)
        actionList.SelectedIndex = 0;

      actionList.Focus();
      Dispatcher.UIThread.Post(delegate { actionList.Focus(); }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// The dialog holds nothing of its own, but the guide is told when it stops standing, because the guide keeps the
    /// mark of what is being recorded on its blocks and that mark has to be asked again after an answer was given.
    /// </summary>
    public void OnScreenClosed()
    {
    }
  }
}
