#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.MpfElements.Input;
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
    protected AbstractProperty _groupButtonWidthProperty;
    protected AbstractProperty _programTemplateProperty;
    protected AbstractProperty _headerTemplateProperty;
    protected AbstractProperty _groupButtonTemplateProperty;
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
    protected int? _lastFocusedRow;

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
      _groupButtonWidthProperty = new SProperty(typeof(Double), 60d);
      _programTemplateProperty = new SProperty(typeof(ControlTemplate), null);
      _headerTemplateProperty = new SProperty(typeof(ControlTemplate), null);
      _groupButtonTemplateProperty = new SProperty(typeof(ControlTemplate), null);
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
      HeaderWidth = c.HeaderWidth;
      GroupButtonWidth = c.GroupButtonWidth;
      HeaderTemplate = copyManager.GetCopy(c.HeaderTemplate);
      GroupButtonTemplate = copyManager.GetCopy(c.GroupButtonTemplate);
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
      MPF.TryCleanupAndDispose(GroupButtonTemplate);
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

    public AbstractProperty GroupButtonTemplateProperty
    {
      get { return _groupButtonTemplateProperty; }
    }

    public ControlTemplate GroupButtonTemplate
    {
      get { return (ControlTemplate)_groupButtonTemplateProperty.GetValue(); }
      set { _groupButtonTemplateProperty.SetValue(value); }
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

    public AbstractProperty GroupButtonWidthProperty
    {
      get { return _groupButtonWidthProperty; }
    }

    public double GroupButtonWidth
    {
      get { return (double)_groupButtonWidthProperty.GetValue(); }
      set { _groupButtonWidthProperty.SetValue(value); }
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

      // Only reserve space if a template is set
      double headersOffset = 0d;
      if (GroupButtonEnabled)
      {
        headersOffset = GroupButtonWidth;
        ColumnDefinition groupButtonColumn = new ColumnDefinition { Width = new GridLength(GridUnitType.Pixel, headersOffset) };
        ColumnDefinitions.Add(groupButtonColumn);
      }

      double headerWidth = HeaderWidth;
      headersOffset += headerWidth;
      ColumnDefinition rowHeaderColumn = new ColumnDefinition { Width = new GridLength(GridUnitType.Pixel, headerWidth) };
      ColumnDefinitions.Add(rowHeaderColumn);

      double rowHeight = ActualHeight / _numberOfRows;
      double colWidth = (ActualWidth - headersOffset) / _numberOfColumns;
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

      SetInitialViewOffset();
      RecreateAndArrangeChildren(true);
    }

    private bool GroupButtonEnabled
    {
      get { return GroupButtonTemplate != null; }
    }

    private void RecreateAndArrangeChildren(bool keepViewOffset = false)
    {
      if (!keepViewOffset)
        _channelViewOffset = 0;
      _childrenCreated = false;
      CreateVisibleChildren(false);
    }

    /// <summary>
    /// Tries to find the current channel in the current group and makes sure it will be inside the visible area.
    /// </summary>
    private void SetInitialViewOffset()
    {
      int currentChannelIndex = 0;
      foreach (var channelsProgram in ChannelsPrograms.OfType<ChannelProgramListItem>())
      {
        if (ChannelContext.IsSameChannel(channelsProgram.Channel, ChannelContext.Instance.Channels.Current))
        {
          if (currentChannelIndex >= _numberOfRows)
            _channelViewOffset = currentChannelIndex - _numberOfRows + 1;
          break;
        }
        currentChannelIndex++;
      }
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

          if (GroupButtonEnabled)
            CreateGroupButton();
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

    private void CreateGroupButton()
    {
      Control btnGroup = CreateControl(null);
      SetGrid(btnGroup, 0, 0, 1, _numberOfRows);

      // Deep copy the styles to each program button.
      btnGroup.Template = MpfCopyManager.DeepCopyCutLVPs(GroupButtonTemplate);
      Children.Add(btnGroup);
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

      int colIndex = GroupButtonEnabled ? 1 : 0;
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
          return p.Program.EndTime <= start || p.Program.StartTime >= end || channel.Channel.ChannelId != ((IProgram)p.AdditionalProperties["PROGRAM"]).ChannelId
            || p is PlaceholderListItem;
        }).ToList();
        removeList.ForEach(Children.Remove);
      }

      colIndex++; // After header (and optional GroupButton)
      int programIndex = 0;
      while (programIndex < channel.Programs.Count && colIndex <= _numberOfColumns)
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

        if (ChannelContext.IsSameChannel(channel.Channel, ChannelContext.Instance.Channels.Current) && program.IsRunning && !updateOnly)
        {
          btnEpg.SetFocusPrio = SetFocusPriority.Highest;
        }

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
      int headersOffset = GroupButtonEnabled ? 2 : 1;
      if (lastEndTime != null)
      {
        int newColIndex = (int)Math.Round(minutesSinceStart / _perCellTime) + headersOffset; // Header offset
        if (lastEndTime != program.Program.StartTime)
          colIndex = Math.Max(colIndex, newColIndex); // colIndex is already set to new position. Calculation is only done to support gaps in programs.

        lastEndTime = program.Program.EndTime;
      }

      colSpan = (int)Math.Round((program.Program.EndTime - programViewStart).TotalMinutes / _perCellTime);

      if (colIndex + colSpan > _numberOfColumns + headersOffset)
        colSpan = _numberOfColumns - colIndex + headersOffset;

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
      var timeIndicatorControl = _timeIndicatorControl;
      if (timeIndicatorControl == null)
      {
        timeIndicatorControl = _timeIndicatorControl = new Control { LogicalParent = this };
        // Deep copy the styles to each program button.
        timeIndicatorControl.Template = MpfCopyManager.DeepCopyCutLVPs(TimeIndicatorTemplate);
        SetRow(timeIndicatorControl, 0);
        SetRowSpan(timeIndicatorControl, _numberOfRows);
        Children.Add(timeIndicatorControl);
      }
      DateTime viewportStart = SlimTvMultiChannelGuideModel.GuideStartTime;
      var headerOffset = GroupButtonEnabled ? 2 : 1;
      int currentTimeColumn = (int)Math.Round((DateTime.Now - viewportStart).TotalMinutes / _perCellTime) + headerOffset; // Header offset
      if (currentTimeColumn <= headerOffset || currentTimeColumn > _numberOfColumns + headerOffset) // Outside viewport
      {
        timeIndicatorControl.IsVisible = false;
      }
      else
      {
        timeIndicatorControl.IsVisible = true;
        SetZIndex(timeIndicatorControl, 100);
        SetColumn(timeIndicatorControl, currentTimeColumn);
        timeIndicatorControl.InvalidateLayout(true, true); // Required to arrange control on new position
      }
    }

    /// <summary>
    /// Sets Grid positioning attached properties.
    /// </summary>
    /// <param name="gridControl">Control in Grid.</param>
    /// <param name="colIndex">"Grid.Column"</param>
    /// <param name="rowIndex">"Grid.Row"</param>
    /// <param name="colSpan">"Grid.ColumnSpan"</param>
    /// <param name="rowSpan">Grid.RowSpan</param>
    private static void SetGrid(Control gridControl, int colIndex, int rowIndex, int colSpan, int rowSpan = 1)
    {
      SetRow(gridControl, rowIndex);
      SetColumn(gridControl, colIndex);
      SetColumnSpan(gridControl, colSpan);
      SetRowSpan(gridControl, rowSpan);
    }

    /// <summary>
    /// Returns the header control from Grid for row with index <paramref name="rowIndex"/>.
    /// </summary>
    /// <param name="rowIndex">RowIndex.</param>
    /// <returns>Header Control.</returns>
    private Control GetRowHeader(int rowIndex)
    {
      return Children.OfType<Control>().FirstOrDefault(el => GetRow(el) == rowIndex && el.Context is ChannelProgramListItem);
    }

    /// <summary>
    /// Returns all programs from Grid for row with index <paramref name="rowIndex"/>.
    /// </summary>
    /// <param name="rowIndex">RowIndex.</param>
    /// <returns>Controls.</returns>
    private IEnumerable<Control> GetRowItems(int rowIndex)
    {
      return GetProgramItems().Where(el => GetRow(el) == rowIndex).OrderBy(GetColumn);
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

    protected override void OnKeyPress(KeyEventArgs e)
    {
      // migration from OnKeyPressed(ref Key key)
      // - no need the check if already handled, b/c this is done by the invoker
      // - no need to check if any child has focus, since event was originally invoked on focused element, 
      //   and the bubbles up the visual tree
      // - instead of setting key to None, we set e.Handled = true

      if (e.Key == Key.Down && OnDown())
        e.Handled = true;
      else if (e.Key == Key.Up && OnUp())
        e.Handled = true;
      else if (e.Key == Key.Left && OnLeft())
        e.Handled = true;
      else if (e.Key == Key.Right && OnRight())
        e.Handled = true;
      else if (e.Key == Key.Home && OnHome())
        e.Handled = true;
      else if (e.Key == Key.End && OnEnd())
        e.Handled = true;
      else if (e.Key == Key.PageDown && OnPageDown())
        e.Handled = true;
      else if (e.Key == Key.PageUp && OnPageUp())
        e.Handled = true;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
      // migration from OnMouseWheel(int numDetents)
      // - no need to check if mouse is over
      // - no need to call base class

      int scrollByLines = System.Windows.Forms.SystemInformation.MouseWheelScrollLines; // Use the system setting as default.

      int numLines = e.NumDetents * scrollByLines;

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
        return ChannelsPrograms == null || ChannelsPrograms.Count == 0 || _channelViewOffset >= ChannelsPrograms.Count - _numberOfRows;
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

    private bool OnDown()
    {
      return ScrollVertical(-1);
    }

    private bool OnUp()
    {
      return ScrollVertical(+1);
    }

    private bool OnHome()
    {
      bool pageIndexChanged = false;
      if (_channelViewOffset != 0)
      {
        pageIndexChanged = true;
        _channelViewOffset = 0;
        RecreateAndArrangeChildren();
      }
      FocusFirstProgramInRow(_channelViewOffset, pageIndexChanged);
      return true;
    }

    private bool OnEnd()
    {
      bool pageIndexChanged = false;
      var lastDataIndex = Math.Max(ChannelsPrograms.Count - _numberOfRows, 0);
      if (_channelViewOffset != lastDataIndex)
      {
        pageIndexChanged = true;
        _channelViewOffset = lastDataIndex;
        RecreateAndArrangeChildren(true);
      }
      var lastViewIndex = Math.Min(ChannelsPrograms.Count, _numberOfRows) - 1;
      FocusFirstProgramInRow(lastViewIndex, pageIndexChanged);
      return true;
    }

    private bool OnPageDown()
    {
      MoveDown(_numberOfRows);
      FocusFirstProgramInRow(Math.Min(ChannelsPrograms.Count, _numberOfRows) - 1, false);
      return true;
    }

    private bool OnPageUp()
    {
      MoveUp(_numberOfRows);
      FocusFirstProgramInRow(0, false);
      return true;
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

    private void FocusFirstProgramInRow(int rowIndex, bool pageIndexChanged)
    {
      var firstItem = GetRowItems(rowIndex).FirstOrDefault();
      FocusControl(pageIndexChanged, firstItem);
    }

    private void FocusHeaderInRow(int rowIndex, bool pageIndexChanged)
    {
      var firstItem = GetRowHeader(rowIndex);
      FocusControl(pageIndexChanged, firstItem);
    }

    private void FocusControl(bool pageIndexChanged, Control firstItem)
    {
      if (firstItem == null)
        return;
      if (pageIndexChanged)
        firstItem.SetFocus = true;
      else
        firstItem.TrySetFocus(true);
    }

    private bool OnRight()
    {
      if (MoveFocus1(MoveFocusDirection.Right))
      {
        if (_lastFocusedRow.HasValue)
        {
          FocusHeaderInRow(_lastFocusedRow.Value, false);
          _lastFocusedRow = null;
        }
      }
      else
      {
        SlimTvMultiChannelGuideModel.Scroll(TimeSpan.FromMinutes(30));
        UpdateViewportHorizontal();
      }
      return true;
    }

    private bool OnLeft()
    {
      // As the group button spans all rows, we remember the last row to restore it when moving back right
      if (GroupButtonEnabled)
      {
        int row;
        ProgramListItem program;
        FrameworkElement header;
        if (GetFocusedRowAndStartTime(out program, out header, out row) && header != null)
        {
          _lastFocusedRow = row;
        }
      }
      if (!MoveFocus1(MoveFocusDirection.Left))
      {
        SlimTvMultiChannelGuideModel.Scroll(TimeSpan.FromMinutes(-30));
        UpdateViewportHorizontal();
      }
      return true;
    }

    private bool ScrollVertical(int scrollDirection)
    {
      int row;
      ProgramListItem program;
      FrameworkElement header;
      if (!GetFocusedRowAndStartTime(out program, out header, out row))
        return false;

      if (scrollDirection < 0)
      {
        if (row == _numberOfRows - 1)
        {
          if (IsViewPortAtBottom)
            return false;
          // Scroll down
          UpdateViewportVertical(scrollDirection);
        }
        else
          row++;
      }
      else
      {
        if (row == 0)
        {
          if (IsViewPortAtTop)
            return false;
          // Scroll up
          UpdateViewportVertical(scrollDirection);
        }
        else
          row--;
      }
      // Focus was on channel header
      if (header != null)
      {
        int colIndex = GroupButtonEnabled ? 1 : 0;
        var nextHeader = Children.FirstOrDefault(c => GetRow(c) == row && GetColumn(c) == colIndex);
        if (nextHeader != null)
        {
          return nextHeader.TrySetFocus(true);
        }
        return false;
      }

      // Focus was on program, first check if the program is the currently running, in this case we will also prefer currently running of next row
      if (program.IsRunning)
      {
        return FocusNextRunningProgram(row);
      }

      // Then try to find "nearest" program in new row.
      FrameworkElement control;
      var startTime = program.Program.StartTime;
      // If program is running already, compare with viewport start
      if (startTime < SlimTvMultiChannelGuideModel.GuideStartTime)
        startTime = SlimTvMultiChannelGuideModel.GuideStartTime;

      if (FindNearestProgram(startTime, row, out control))
      {
        return control.TrySetFocus(true);
      }
      return false;
    }

    /// <summary>
    /// Gets the currently focused program or header control and its Grid.Row.
    /// </summary>
    /// <param name="program">Focused program</param>
    /// <param name="headerControl">Focused header</param>
    /// <param name="row">The focused row</param>
    /// <returns><c>true</c> if matching program could be found</returns>
    private bool GetFocusedRowAndStartTime(out ProgramListItem program, out FrameworkElement headerControl, out int row)
    {
      program = null;
      headerControl = null;
      row = 0;
      FrameworkElement currentElement = GetFocusedElementOrChild();

      while (currentElement != null)
      {
        if (currentElement.DataContext != null)
        {
          // Check for program
          var item = currentElement.DataContext.Source as ProgramListItem;
          if (item != null)
          {
            program = item;
            row = GetRow(currentElement);
            return true;
          }
          // Check for channel header
          var channel = currentElement.DataContext.Source as ChannelProgramListItem;
          if (channel != null)
          {
            headerControl = currentElement;
            row = GetRow(currentElement);
            return true;
          }
        }
        currentElement = currentElement.LogicalParent as Control;
      }
      return false;
    }

    /// <summary>
    /// Perfers the running item of the next row.
    /// </summary>
    /// <param name="row">Row</param>
    /// <returns><c>true</c> if matching program could be found</returns>
    private bool FocusNextRunningProgram(int row)
    {
      var nextRowRunningItem = Children.FirstOrDefault(c => GetRow(c) == row && c.DataContext != null && c.DataContext.Source is ProgramListItem && ((ProgramListItem)c.DataContext.Source).IsRunning);
      if (nextRowRunningItem != null)
        return nextRowRunningItem.TrySetFocus(true);
      return false;
    }

    /// <summary>
    /// Finds the nearest program relative to given <paramref name="startTime"/>. The "nearest" one is checked by comparing program start times.
    /// If the start time is before the current view port, the SlimTvMultiChannelGuideModel.GuideStartTime will be used.
    /// </summary>
    /// <param name="startTime">Start time of currently focused program</param>
    /// <param name="row">New row to focus</param>
    /// <param name="programControl">Returns the next program's control to focus</param>
    /// <returns><c>true</c> if matching program could be found</returns>
    private bool FindNearestProgram(DateTime startTime, int row, out FrameworkElement programControl)
    {
      var rowItems = Children.Where(c => GetRow(c) == row && c.DataContext != null).ToList();
      double minDiff = Double.MaxValue;
      FrameworkElement nearestStartItem = null;
      foreach (var program in rowItems)
      {
        var pi = program.DataContext.Source as ProgramListItem;
        if (pi == null)
          continue;

        var programStartTime = pi.Program.StartTime;
        if (programStartTime < SlimTvMultiChannelGuideModel.GuideStartTime)
          programStartTime = SlimTvMultiChannelGuideModel.GuideStartTime;

        var diff = Math.Abs((startTime - programStartTime).TotalMinutes);
        if (nearestStartItem == null || diff < minDiff)
        {
          minDiff = diff;
          nearestStartItem = program;
        }
      }
      programControl = nearestStartItem;
      return programControl != null;
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
        _channelViewOffset -= moveOffset;
        List<FrameworkElement> removeList = new List<FrameworkElement>();
        foreach (FrameworkElement element in Children)
        {
          // Indicator must not be removed, its position is fixed
          if (ShouldKeepControl(element))
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

    /// <summary>
    /// Indicates if a control should be kept during partial updates (scrolling through channels)
    /// </summary>
    private bool ShouldKeepControl(FrameworkElement element)
    {
      return element == _timeIndicatorControl || GroupButtonEnabled && GetColumn(element) == 0;
    }

    #endregion
  }
}
