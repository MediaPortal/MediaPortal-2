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

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// Setting the sleep timer, hung as the dialog the reference hung it (<c>dialogSleepTimer.xaml</c>):
  /// pushed over the shutdown menu, closed by the back key, every press answered by the model. The wide
  /// button starts the wait and the dialog stays open to say "Stop" - the reference never dismissed it,
  /// because a viewer who has just set a wait wants to see that it stands.
  /// </summary>
  public partial class SleepTimerView : UserControl, IWorkflowScreen, IDialogScreen
  {
    private readonly SleepTimerViewModel? _model;
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>For the XAML loader and the designer.</summary>
    public SleepTimerView()
        : this(null)
    {
    }

    public SleepTimerView(SleepTimerViewModel? model)
    {
      _model = model;
      DataContext = model;
      InitializeComponent();

      _ring.Add(timeBox);
      _ring.Add(addButton);
      _ring.Add(subButton);
      _ring.Add(mediaItemButton);
      _ring.Add(actionButton);
      _ring.Add(activateButton);
      DpadNavigation.Attach(this, _ring);
    }

    private void OnAdd(object? sender, RoutedEventArgs args)
    {
      _model?.AddTime();
    }

    private void OnSub(object? sender, RoutedEventArgs args)
    {
      _model?.SubTime();
    }

    private void OnFromMediaItem(object? sender, RoutedEventArgs args)
    {
      _model?.FromMediaItem();
    }

    private void OnSelectAction(object? sender, RoutedEventArgs args)
    {
      _model?.SelectAction();
    }

    private void OnActivate(object? sender, RoutedEventArgs args)
    {
      _model?.Activate();
    }

    /// <summary>
    /// The number takes the focus every time this dialog is the screen: a viewer who came here to say how
    /// long is there to say a number, and the steppers and the wide button are one Tab away.
    /// </summary>
    public void OnScreenActivated()
    {
      timeBox.Focus();
      Dispatcher.UIThread.Post(delegate { timeBox.Focus(); }, DispatcherPriority.Loaded);
    }
  }
}
