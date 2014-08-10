#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion
// #define DEBUG_LAYOUT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.General;
#if DEBUG_LAYOUT
using MediaPortal.Common.Logging;
#endif
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Client.Messaging;
using MediaPortal.Plugins.SlimTv.Client.Models;
using MediaPortal.Plugins.SlimTv.Client.Settings;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.Plugins.SlimTv.Client.Controls
{
  public class EpgGrid : Grid
  {
    #region Fields

    protected readonly double _visibleHours;
    protected readonly int _numberOfRows;
    protected int _numberOfColumns = 75; // Used to align programs in Grid. For example: 2.5h == 150 min. 150 min / 75 = 2 min per column.
    protected double _perCellTime; // Time in minutes per grid cell.

    protected AsynchronousMessageQueue _messageQueue = null;
    protected AbstractProperty _headerWidthProperty;
    protected AbstractProperty _programTemplateProperty;
    protected AbstractProperty _headerTemplateProperty;
    protected AbstractProperty _timeIndicatorTemplateProperty;
    protected bool _childrenCreated = false;
    protected int _channelViewOffset;
    protected double _actualWidth = 0.0d;
    protected double _actualHeight = 0.0d;
    protected int _groupIndex = -1;
    protected readonly object _syncObj = new object();
    protected Control _timeIndicatorControl;
    protected Timer _timer = null;
    protected long _updateInterval = 10000; // Update every 10 seconds

    #endregion

    #region Constructor / Dispose

    public EpgGrid()
    {
      // User defined layout settings.
      var settings = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvClientSettings>();
      _visibleHours = settings.EpgVisibleHours;
      _numberOfRows = settings.EpgNumberOfRows;

      _perCellTime = _visibleHours * 60 / _numberOfColumns; // Time in minutes per grid cell.

      _headerWidthProperty = new SProperty(typeof(Double), 200d);
      _programTemplateProperty = new SProperty(typeof(ControlTemplate), null);
      _headerTemplateProperty = new SProperty(typeof(ControlTemplate), null);
      _timeIndicatorTemplateProperty = new SProperty(typeof(ControlTemplate), null);
      Attach();
      SubscribeToMessages();
      StartTimer();
    }

    private void Attach()
    {
    }

    private void Detach()
    {
    }

    /// <summary>
    /// Sets the timer up to be called periodically.
    /// </summary>
    protected void StartTimer()
    {
      lock (_syncObj)
      {
        if (_timer != null)
          return;
        _timer = new Timer(OnTimerElapsed);
        ChangeInterval(_updateInterval);
      }
    }

    /// <summary>
    /// Changes the timer interval.
    /// </summary>
    /// <param name="updateInterval">Interval in ms</param>
    protected void ChangeInterval(long updateInterval)
    {
      lock (_syncObj)
      {
        if (_timer == null)
          return;
        _updateInterval = updateInterval;
        _timer.Change(updateInterval, updateInterval);
      }
    }

    /// <summary>
    /// Disables the timer and blocks until the last timer event has executed.
    /// </summary>
    protected void StopTimer()
    {
      WaitHandle notifyObject;
      lock (_syncObj)
      {
        if (_timer == null)
          return;
        notifyObject = new ManualResetEvent(false);
        _timer.Dispose(notifyObject);
        _timer = null;
      }
      notifyObject.WaitOne();
      notifyObject.Close();
    }

    void SubscribeToMessages()
    {
      AsynchronousMessageQueue messageQueue;
      lock (_syncObj)
      {
        if (_messageQueue != null)
          return;
        _messageQueue = new AsynchronousMessageQueue(this, new[] { SlimTvClientMessaging.CHANNEL });
        _messageQueue.MessageReceived += OnMessageReceived;
        messageQueue = _messageQueue;
      }
      messageQueue.Start();
    }

    void UnsubscribeFromMessages()
    {
      AsynchronousMessageQueue messageQueue;
      lock (_syncObj)
      {
        if (_messageQueue == null)
          return;
        messageQueue = _messageQueue;
        _messageQueue = null;
      }
      messageQueue.Shutdown();
    }

    protected void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      // Do not handle messages if control is not running. This is a workaround to avoid updating controls that are not used on screen.
      // The EpgGrid is instantiated twice: via ScreenManager.LoadScreen and Control.OnTemplateChanged as copy!?
      if (ElementState != ElementState.Running)
        return;

      if (message.ChannelName == SlimTvClientMessaging.CHANNEL)
      {
        SlimTvClientMessaging.MessageType messageType = (SlimTvClientMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SlimTvClientMessaging.MessageType.GroupChanged:
            OnGroupChanged();
            break;
          case SlimTvClientMessaging.MessageType.ProgramsChanged:
            OnProgramsChanged();
            break;
          case SlimTvClientMessaging.MessageType.ProgramStatusChanged:
            IProgram program = (IProgram)message.MessageData[SlimTvClientMessaging.KEY_PROGRAM];
            UpdateProgramStatus(program);
            break;
        }
      }
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();

      base.DeepCopy(source, copyManager);

      EpgGrid c = (EpgGrid)source;
      HeaderTemplate = copyManager.GetCopy(c.HeaderTemplate);
      ProgramTemplate = copyManager.GetCopy(c.ProgramTemplate);
      TimeIndicatorTemplate = copyManager.GetCopy(c.TimeIndicatorTemplate);

      Attach();
    }

    public override void Dispose()
    {
      Detach();
      StopTimer();
      UnsubscribeFromMessages();
      MPF.TryCleanupAndDispose(HeaderTemplate);
      MPF.TryCleanupAndDispose(ProgramTemplate);
      base.Dispose();
    }

    private void OnGroupChanged()
    {
      // Group changed, recreate all.
      RecreateAndArrangeChildren();
      ArrangeOverride();
    }

    private void OnProgramsChanged()
    {
      // Programs changed, only update.
      CreateVisibleChildren(true);
    }

    #endregion

    #region Public Properties

    public ItemsList ChannelsPrograms
    {
      get
      {
        return SlimTvMultiChannelGuideModel.ChannelList;
      }
    }

    public AbstractProperty ProgramTemplateProperty
    {
      get { return _programTemplateProperty; }
    }

    public ControlTemplate ProgramTemplate
    {
      get { return (ControlTemplate)_programTemplateProperty.GetValue(); }
      set { _programTemplateProperty.SetValue(value); }
    }

    public AbstractProperty HeaderTemplateProperty
    {
      get { return _headerTemplateProperty; }
    }

    public ControlTemplate HeaderTemplate
    {
      get { return (ControlTemplate)_headerTemplateProperty.GetValue(); }
      set { _headerTemplateProperty.SetValue(value); }
    }

    public AbstractProperty TimeIndicatorTemplateProperty
    {
      get { return _timeIndicatorTemplateProperty; }
    }

    public ControlTemplate TimeIndicatorTemplate
    {
      get { return (ControlTemplate)_timeIndicatorTemplateProperty.GetValue(); }
      set { _timeIndicatorTemplateProperty.SetValue(value); }
    }

    public AbstractProperty HeaderWidthProperty
    {
      get { return _headerWidthProperty; }
    }

    public double HeaderWidth
    {
      get { return (double)_headerWidthProperty.GetValue(); }
      set { _headerWidthProperty.SetValue(value); }
    }

    #endregion

    #region Layout overrides

    protected override void ArrangeOverride()
    {
      PrepareColumnAndRowLayout();
      base.ArrangeOverride();
    }

    public override void RenderOverride(RenderContext localRenderContext)
    {
      // Lock access to Children during render pass to avoid controls to be disposed during rendering.
      lock (Children.SyncRoot)
        base.RenderOverride(localRenderContext);
    }

    protected override void RenderChildren(RenderContext localRenderContext)
    {
      // Lock access to Children during render pass to avoid controls to be disposed during rendering.
      lock (Children.SyncRoot)
        base.RenderChildren(localRenderContext);
    }

    /// <summary>
    /// Updates the EpgGrid's state and sets the new time indicator position.
    /// </summary>
    /// <param name="state"></param>
    private void OnTimerElapsed(object state)
    {
      // Do not handle messages if control is not running. This is a workaround to avoid updating controls that are not used on screen.
      // The EpgGrid is instantiated twice: via ScreenManager.LoadScreen and Control.OnTemplateChanged as copy!?
      if (ElementState != ElementState.Running)
        return;

      SetTimeIndicator();
    }

    private void PrepareColumnAndRowLayout()
    {
      // Recreate columns and rows only after dimensions changed.
      if (_actualWidth == ActualWidth && _actualHeight == ActualHeight)
        return;
      _actualWidth = ActualWidth;
      _actualHeight = ActualHeight;

      ColumnDefinitions.Clear();
      RowDefinitions.Clear();

      double headerWidth = HeaderWidth;
      ColumnDefinition rowHeaderColumn = new ColumnDefinition { Width = new GridLength(GridUnitType.Pixel, headerWidth) };
      ColumnDefinitions.Add(rowHeaderColumn);

      double rowHeight = ActualHeight / _numberOfRows;
      double colWidth = (ActualWidth - headerWidth) / _numberOfColumns;
      for (int c = 0; c < _numberOfColumns; c++)
      {
        ColumnDefinition cd = new ColumnDefinition { Width = new GridLength(GridUnitType.Pixel, colWidth) };
        ColumnDefinitions.Add(cd);
      }
      for (int r = 0; r < _numberOfRows; r++)
      {
        RowDefinition cd = new RowDefinition { Height = new GridLength(GridUnitType.Pixel, rowHeight) };
        RowDefinitions.Add(cd);
      }

      RecreateAndArrangeChildren();
    }

    private void RecreateAndArrangeChildren(bool keepViewOffset = false)
    {
      if (!keepViewOffset)
        _channelViewOffset = 0;
      _childrenCreated = false;
      CreateVisibleChildren(false);
    }

    private void CreateVisibleChildren(bool updateOnly)
    {
      lock (Children.SyncRoot)
      {
        if (!updateOnly && _childrenCreated)
          return;

        _childrenCreated = true;

        if (!updateOnly)
        {
          _timeIndicatorControl = null;
          Children.Clear();
        }

        SetTimeIndicator();

        if (ChannelsPrograms == null)
          return;

        int rowIndex = 0;
        int channelIndex = _channelViewOffset;
        while (channelIndex < ChannelsPrograms.Count && rowIndex < _numberOfRows)
        {
          if (!CreateOrUpdateRow(updateOnly, ref channelIndex, rowIndex++))
            break;
        }
      }
    }

    private bool CreateOrUpdateRow(bool updateOnly, ref int channelIndex, int rowIndex)
    {
      if (channelIndex >= ChannelsPrograms.Count)
        return false;
      ChannelProgramListItem channel = ChannelsPrograms[channelIndex] as ChannelProgramListItem;
      if (channel == null)
        return false;

      // Default: take viewport from model
      DateTime viewportStart = SlimTvMultiChannelGuideModel.GuideStartTime;
      DateTime viewportEnd = SlimTvMultiChannelGuideModel.GuideEndTime;

      int colIndex = 0;
      if (!updateOnly)
      {
        Control btnHeader = CreateControl(channel);
        SetGrid(btnHeader, colIndex, rowIndex, 1);

        // Deep copy the styles to each program button.
        btnHeader.Template = MpfCopyManager.DeepCopyCutLVPs(HeaderTemplate);
        Children.Add(btnHeader);
      }

      int colSpan = 0;
      DateTime? lastStartTime = null;
      DateTime? lastEndTime = viewportStart;

#if DEBUG_LAYOUT
      // Debug layouting:
      if (rowIndex == 0)
        ServiceRegistration.Get<ILogger>().Debug("EPG: Viewport: {0}-{1} PerCell: {2} min", viewportStart.ToShortTimeString(), viewportEnd.ToShortTimeString(), _perCellTime);
#endif
      if (updateOnly)
      {
        // Remove all programs outside of viewport.
        DateTime start = viewportStart;
        DateTime end = viewportEnd;
        var removeList = GetRowItems(rowIndex).Where(el =>
        {
          ProgramListItem p = (ProgramListItem)el.Context;
          return p.Program.EndTime <= start || p.Program.StartTime >= end || channel.Channel.ChannelId != ((IProgram)p.AdditionalProperties["PROGRAM"]).ChannelId;
        }).ToList();
        removeList.ForEach(Children.Remove);
      }

      colIndex = 1; // After header
      int programIndex = 0;
      while (programIndex < channel.Programs.Count && colIndex < _numberOfColumns)
      {
        ProgramListItem program = channel.Programs[programIndex] as ProgramListItem;
        if (program == null || program.Program.StartTime > viewportEnd)
          break;

        // Ignore programs outside viewport and programs that start at same time (duplicates)
        if (program.Program.EndTime <= viewportStart || (lastStartTime.HasValue && lastStartTime.Value == program.Program.StartTime))
        {
          programIndex++;
          continue;
        }

        lastStartTime = program.Program.StartTime;

        CalculateProgamPosition(program, viewportStart, viewportEnd, ref colIndex, ref colSpan, ref lastEndTime);

        Control btnEpg = GetOrCreateControl(program, rowIndex);
        SetGrid(btnEpg, colIndex, rowIndex, colSpan);

        programIndex++;
        colIndex += colSpan; // Skip spanned columns.
      }

      channelIndex++;
      return true;
    }

    /// <summary>
    /// Calculates to position (Column) and size (ColumnSpan) for the given <paramref name="program"/> by considering the avaiable viewport (<paramref name="viewportStart"/>, <paramref name="viewportEnd"/>).
    /// </summary>
    /// <param name="program">Program.</param>
    /// <param name="viewportStart">Viewport from.</param>
    /// <param name="viewportEnd">Viewport to.</param>
    /// <param name="colIndex">Returns Column.</param>
    /// <param name="colSpan">Returns ColumnSpan.</param>
    /// <param name="lastEndTime">Last program's end time.</param>
    private void CalculateProgamPosition(ProgramListItem program, DateTime viewportStart, DateTime viewportEnd, ref int colIndex, ref int colSpan, ref DateTime? lastEndTime)
    {
      if (program.Program.EndTime < viewportStart || program.Program.StartTime > viewportEnd)
        return;

      DateTime programViewStart = program.Program.StartTime < viewportStart ? viewportStart : program.Program.StartTime;

      double minutesSinceStart = (programViewStart - viewportStart).TotalMinutes;
      if (lastEndTime != null)
      {
        int newColIndex = (int)Math.Round(minutesSinceStart / _perCellTime) + 1; // Header offset
        if (lastEndTime != program.Program.StartTime)
          colIndex = Math.Max(colIndex, newColIndex); // colIndex is already set to new position. Calculation is only done to support gaps in programs.

        lastEndTime = program.Program.EndTime;
      }

      colSpan = (int)Math.Round((program.Program.EndTime - programViewStart).TotalMinutes / _perCellTime);

      if (colIndex + colSpan > _numberOfColumns + 1)
        colSpan = _numberOfColumns - colIndex + 1;

      if (colSpan == 0)
        colSpan = 1;

#if DEBUG_LAYOUT
        // Debug layouting:
      ServiceRegistration.Get<ILogger>().Debug("EPG: {0,2}-{1,2}: {3}-{4}: {2}", colIndex, colSpan, program.Program.Title, program.Program.StartTime.ToShortTimeString(), program.Program.EndTime.ToShortTimeString());
#endif
    }

    /// <summary>
    /// Tries to find an existing control for given <paramref name="program"/> in the Grid row with index <paramref name="rowIndex"/>.
    /// If no control was found, this method creates a new control and adds it to the Grid.
    /// </summary>
    /// <param name="program">Program.</param>
    /// <param name="rowIndex">RowIndex.</param>
    /// <returns>Control.</returns>
    private Control GetOrCreateControl(ProgramListItem program, int rowIndex)
    {
      Control control = GetRowItems(rowIndex).FirstOrDefault(el => ((ProgramListItem)el.Context).Program.ProgramId == program.Program.ProgramId);
      if (control != null)
        return control;

      control = CreateControl(program);
      // Deep copy the styles to each program button.
      control.Template = MpfCopyManager.DeepCopyCutLVPs(ProgramTemplate);
      Children.Add(control);
      return control;
    }

    /// <summary>
    /// Tries to update a program item, i.e. if recording status was changed.
    /// </summary>
    /// <param name="program">Program</param>
    private void UpdateProgramStatus(IProgram program)
    {
      if (program == null)
        return;
      FrameworkElement control = GetProgramItems().FirstOrDefault(el =>
      {
        ProgramListItem programListItem = (ProgramListItem)el.Context;
        if (programListItem == null)
          return false;
        ProgramProperties programProperties = programListItem.Program;
        return programProperties != null && programProperties.ProgramId == program.ProgramId;
      });
      if (control == null)
        return;

      // Update properties
      IProgramRecordingStatus recordingStatus = program as IProgramRecordingStatus;
      if (recordingStatus != null)
        ((ProgramListItem)control.Context).Program.UpdateState(recordingStatus.RecordingStatus);
    }

    /// <summary>
    /// Creates a control in the given <paramref name="context"/>.
    /// </summary>
    /// <param name="context">ListItem, can be either "Channel" or "Program"</param>
    /// <returns>Control.</returns>
    private Control CreateControl(ListItem context)
    {
      Control btnEpg = new Control
      {
        LogicalParent = this,
        Context = context,
      };
      return btnEpg;
    }

    private void SetTimeIndicator()
    {
      if (_timeIndicatorControl == null)
      {
        _timeIndicatorControl = new Control { LogicalParent = this };
        // Deep copy the styles to each program button.
        _timeIndicatorControl.Template = MpfCopyManager.DeepCopyCutLVPs(TimeIndicatorTemplate);
        SetRow(_timeIndicatorControl, 0);
        SetRowSpan(_timeIndicatorControl, _numberOfRows);
        Children.Add(_timeIndicatorControl);
      }
      DateTime viewportStart = SlimTvMultiChannelGuideModel.GuideStartTime;
      int currentTimeColumn = (int)Math.Round((DateTime.Now - viewportStart).TotalMinutes / _perCellTime) + 1; // Header offset
      if (currentTimeColumn <= 1 || currentTimeColumn > _numberOfColumns + 1) // Outside viewport
      {
        _timeIndicatorControl.IsVisible = false;
      }
      else
      {
        _timeIndicatorControl.IsVisible = true;
        SetZIndex(_timeIndicatorControl, 100);
        SetColumn(_timeIndicatorControl, currentTimeColumn);
        _timeIndicatorControl.InvalidateLayout(true, true); // Required to arrange control on new position
      }
    }

    /// <summary>
    /// Sets Grid positioning attached properties.
    /// </summary>
    /// <param name="gridControl">Control in Grid.</param>
    /// <param name="colIndex">"Grid.Column"</param>
    /// <param name="rowIndex">"Grid.Row"</param>
    /// <param name="colSpan">"Grid.ColumnSpan"</param>
    private static void SetGrid(Control gridControl, int colIndex, int rowIndex, int colSpan)
    {
      SetRow(gridControl, rowIndex);
      SetColumn(gridControl, colIndex);
      SetColumnSpan(gridControl, colSpan);
    }

    /// <summary>
    /// Returns all programs from Grid for row with index <paramref name="rowIndex"/>.
    /// </summary>
    /// <param name="rowIndex">RowIndex.</param>
    /// <returns>Controls.</returns>
    private IEnumerable<Control> GetRowItems(int rowIndex)
    {
      return GetProgramItems().Where(el => GetRow(el) == rowIndex);
    }

    /// <summary>
    /// Returns all programs from Grid.
    /// </summary>
    /// <returns>Controls.</returns>
    private IEnumerable<Control> GetProgramItems()
    {
      return Children.OfType<Control>().Where(el => el.Context is ProgramListItem);
    }

    #endregion

    #region Focus handling

    public override void OnKeyPressed(ref Key key)
    {
      base.OnKeyPressed(ref key);

      if (key == Key.None)
        // Key event was handeled by child
        return;

      if (!CheckFocusInScope())
        return;

      if (key == Key.Down && OnDown())
        key = Key.None;
      else if (key == Key.Up && OnUp())
        key = Key.None;
      else if (key == Key.Left && OnLeft())
        key = Key.None;
      else if (key == Key.Right && OnRight())
        key = Key.None;
      else if (key == Key.Home && OnHome())
        key = Key.None;
      else if (key == Key.End && OnEnd())
        key = Key.None;
      else if (key == Key.PageDown && OnPageDown())
        key = Key.None;
      else if (key == Key.PageUp && OnPageUp())
        key = Key.None;
    }


    public override void OnMouseWheel(int numDetents)
    {
      base.OnMouseWheel(numDetents);

      if (!IsMouseOver)
        return;

      int scrollByLines = System.Windows.Forms.SystemInformation.MouseWheelScrollLines; // Use the system setting as default.

      int numLines = numDetents * scrollByLines;

      if (numLines < 0)
        MoveDown(-1 * numLines);
      else if (numLines > 0)
        MoveUp(numLines);
    }

    private bool IsViewPortAtTop
    {
      get
      {
        return ChannelsPrograms == null || ChannelsPrograms.Count == 0 || _channelViewOffset == 0;
      }
    }

    private bool IsViewPortAtBottom
    {
      get
      {
        return ChannelsPrograms == null || ChannelsPrograms.Count == 0 || _channelViewOffset >= ChannelsPrograms.Count - 1 - _numberOfRows;
      }
    }

    private static SlimTvMultiChannelGuideModel SlimTvMultiChannelGuideModel
    {
      get
      {
        SlimTvMultiChannelGuideModel model = (SlimTvMultiChannelGuideModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(SlimTvMultiChannelGuideModel.MODEL_ID);
        return model;
      }
    }

    /// <summary>
    /// Checks if the currently focused control is contained in the EpgGrid. We only need to handle focus changes inside the EpgGrid, but not in any other control.
    /// </summary>
    bool CheckFocusInScope()
    {
      Screen screen = Screen;
      Visual focusPath = screen == null ? null : screen.FocusedElement;
      while (focusPath != null)
      {
        if (focusPath == this)
          // Focused control is located in our focus scope
          return true;
        focusPath = focusPath.VisualParent;
      }
      return false;
    }

    private bool OnDown()
    {
      if (!MoveFocus1(MoveFocusDirection.Down))
      {
        if (IsViewPortAtBottom)
          return false;

        _channelViewOffset++;
        UpdateViewportVertical(-1);
        return MoveFocus1(MoveFocusDirection.Down); // After we created a new row, try to set focus again
      }
      return true;
    }

    private bool OnUp()
    {
      if (!MoveFocus1(MoveFocusDirection.Up))
      {
        if (IsViewPortAtTop)
          return false;

        _channelViewOffset--;
        UpdateViewportVertical(+1);
        return MoveFocus1(MoveFocusDirection.Up); // After we created a new row, try to set focus again
      }
      return true;
    }

    private bool OnHome()
    {
      if (IsViewPortAtTop)
        return false;

      _channelViewOffset = 0;
      RecreateAndArrangeChildren();
      FocusFirstProgramInRow(_channelViewOffset);
      return true;
    }

    private bool OnEnd()
    {
      if (IsViewPortAtBottom)
        return false;

      _channelViewOffset = ChannelsPrograms.Count - _numberOfRows;
      RecreateAndArrangeChildren(true);
      FocusFirstProgramInRow(Math.Min(ChannelsPrograms.Count, _numberOfRows) - 1);
      return true;
    }

    private bool OnPageDown()
    {
      return MoveDown(_numberOfRows);
    }

    private bool OnPageUp()
    {
      return MoveUp(_numberOfRows);
    }

    private bool MoveDown(int moveRows)
    {
      for (int i = 0; i < moveRows - 1; i++)
        if (!OnDown())
          return false;
      return true;
    }

    private bool MoveUp(int moveRows)
    {
      for (int i = 0; i < moveRows - 1; i++)
        if (!OnUp())
          return false;
      return true;
    }

    private void FocusFirstProgramInRow(int rowIndex)
    {
      var firstItem = GetRowItems(rowIndex).FirstOrDefault();
      if (firstItem != null)
        firstItem.SetFocus = true;
    }

    private bool OnRight()
    {
      if (!MoveFocus1(MoveFocusDirection.Right))
      {
        SlimTvMultiChannelGuideModel.Scroll(TimeSpan.FromMinutes(30));
        UpdateViewportHorizontal();
      }
      return true;
    }

    private bool OnLeft()
    {
      if (!MoveFocus1(MoveFocusDirection.Left))
      {
        SlimTvMultiChannelGuideModel.Scroll(TimeSpan.FromMinutes(-30));
        UpdateViewportHorizontal();
      }
      return true;
    }

    private void UpdateViewportHorizontal()
    {
      CreateVisibleChildren(true);
      ArrangeOverride();
    }

    private void UpdateViewportVertical(int moveOffset)
    {
      lock (Children.SyncRoot)
      {
        List<FrameworkElement> removeList = new List<FrameworkElement>();
        foreach (FrameworkElement element in Children)
        {
          // Indicator must not be removed, its position is fixed
          if (element == _timeIndicatorControl)
            continue;
          int row = GetRow(element);
          int targetRow = row + moveOffset;
          if (targetRow >= 0 && targetRow < _numberOfRows)
            SetRow(element, targetRow);
          else
            removeList.Add(element);
        }
        removeList.ForEach(Children.Remove);

        int rowIndex = moveOffset > 0 ? 0 : _numberOfRows - 1;
        int channelIndex = _channelViewOffset + rowIndex;
        CreateOrUpdateRow(false, ref channelIndex, rowIndex);
      }
      ArrangeOverride();
    }

    #endregion
  }
}
