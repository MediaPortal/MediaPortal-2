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
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The dialog a trailer speaks in: the wait while its film is looked up or brought over, and the reason when it
  /// is not coming.
  /// </summary>
  /// <remarks>
  /// <para>
  /// It is a dialog of its own and not a line of the shelf's question, because the two answer different things. The
  /// shelf asks <i>which</i> - which title, which trailer of it, which picture size - and its lines are one word
  /// long by build. What comes back from a trailer is a sentence, sometimes minutes after the press: a sentence
  /// written into the question is read as one more line of it, and a sentence thrown at the playback nudge is read
  /// as its first three words.
  /// </para>
  /// <para>
  /// The dialog stands from the moment the errand begins, so that a viewer who pressed a line knows the machine
  /// heard them, and it goes away on its own when the film arrives - the playback screen is the answer, and a box
  /// saying "being looked up" behind a playing film is a lie by then.
  /// </para>
  /// </remarks>
  public partial class TrailerNoticeView : UserControl, IWorkflowScreen, IDialogScreen
  {
    private readonly TrailerNoticeModel? _model;
    private readonly WorkflowManager? _workflow;

    /// <summary>For the XAML loader and the designer.</summary>
    public TrailerNoticeView()
        : this(null, null)
    {
    }

    public TrailerNoticeView(TrailerNoticeModel? model, WorkflowManager? workflow)
    {
      _model = model;
      _workflow = workflow;
      DataContext = model;
      InitializeComponent();

      // The remote's OK on a dialog: read it, and be done. Back is left to the shell, which pops this stop - the
      // same way out the frame's own answer button gives, since the frame draws one for this dialog.
      KeyDown += delegate (object? sender, KeyEventArgs args)
      {
        if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Back, args))
          return;

        if (Settings.KeyBindings.Current.Is(Settings.KeyAction.Select, args) && _model != null)
        {
          _model.Finish();
          args.Handled = true;
        }
      };

      if (model != null)
        model.Finished += delegate { Close(); };
    }

    /// <summary>
    /// Where the dialog's keys start: the frame's answer, which stands under the plate the sentence sits on.
    /// </summary>
    /// <remarks>
    /// Asked twice, because the button is not in the window when the view is built - it comes out of the skin's
    /// dialog frame, which arrives a pass later. A focus asked of a control that is not joined to a window yet
    /// answers nothing, so without the second ask the dialog would stand in front of the viewer with the keyboard
    /// still in the screen behind it.
    /// </remarks>
    public void OnScreenActivated()
    {
      void Ask()
      {
        Button? answer = this.GetVisualDescendants().OfType<Button>()
            .FirstOrDefault(b => b.Name == "dialogAnswerOk" && b.IsEffectivelyVisible);
        answer?.Focus();
      }

      Ask();
      Dispatcher.UIThread.Post(Ask, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Takes the dialog away. Pops only its own stop, because the application may have moved the viewer along in
    /// the same breath.
    /// </summary>
    private void Close()
    {
      WorkflowFrame? current = _workflow?.Current;
      if (current != null && current.StateId == "trailernotice")
        _workflow?.Pop();
    }
  }
}
