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
*/

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The shutdown menu - this front end's <c>dialogSystemState</c>, the dialog a viewer standing at the
  /// television opens to leave. It is raised as a dialog (<see cref="IDialogScreen"/>) over the screen it
  /// was asked from, which stays lit behind it - the reference's power menu was a dialog on its dialog
  /// master, never a master replacing the master - and the back key steps off it like any other stop.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Pressing a row is the whole answer - the reference hung no OK button on its power menu, because
  /// asking a viewer to press a row and then press something else is two presses where one does. That is
  /// also why focus starts on the first row: a viewer who came here by pressing the power plate wants to
  /// press once more, not to move first.
  /// </para>
  /// <para>
  /// "Close MediaPortal" is the application's own way out and nothing else: the same lifetime the window
  /// was built on shuts down, which is how the disposal of the player and the reporter already wired
  /// there runs. Closing a window by hand would leave the desktop lifetime waiting for a window that has
  /// gone. That leaving is the application's answer, handed to the model as a delegate - the screen never
  /// decides by itself what "leave" means.
  /// </para>
  /// </remarks>
  public partial class CloseView : UserControl, IWorkflowScreen, IDialogScreen
  {
    private readonly CloseViewModel? _model;
    private readonly WorkflowManager? _workflow;
    private readonly System.Collections.Generic.List<Control> _ring =
        new System.Collections.Generic.List<Control>();

    /// <summary>For the XAML loader and the designer.</summary>
    public CloseView()
        : this(null, null)
    {
    }

    public CloseView(CloseViewModel? model, WorkflowManager? workflow)
    {
      _model = model;
      _workflow = workflow;
      DataContext = model;
      InitializeComponent();

      choiceList.KeyDown += OnListKey;
      choiceList.DoubleTapped += delegate { Answer(); };

      _ring.Add(choiceList);
      DpadNavigation.Attach(this, _ring);
    }

    /// <summary>
    /// The keys that answer: Enter foremost, because the reference's menu was reached by pressing the
    /// power plate and finished by pressing once more on a row.
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
    /// Giving the answer that is stood on. The model answers the sleep timer's row itself (it opens the
    /// timer's dialog while nothing runs and stops the running one when something does) and hands every
    /// other row to the application, which is the only one that knows what leaving costs.
    /// </summary>
    private void Answer()
    {
      CloseViewModel? model = _model;
      ShutdownChoice? choice = choiceList.SelectedItem as ShutdownChoice;
      if (model == null || choice == null)
        return;

      model.Choose(choice);
    }

    /// <summary>
    /// The list takes the focus every time this dialog is the screen, and the first row is stood on - the
    /// reference's dialog opened with its menu ready to be pressed, and a viewer who came here by pressing
    /// the power plate wants to press once more, not to move first.
    /// </summary>
    public void OnScreenActivated()
    {
      if (choiceList.SelectedItem == null && choiceList.Items.Count > 0)
        choiceList.SelectedIndex = 0;

      choiceList.Focus();
      Dispatcher.UIThread.Post(delegate { choiceList.Focus(); }, DispatcherPriority.Loaded);
    }
  }
}
