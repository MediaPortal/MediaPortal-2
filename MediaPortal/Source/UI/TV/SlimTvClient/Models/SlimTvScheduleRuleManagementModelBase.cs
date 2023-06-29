#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Client.Messaging;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  public abstract class SlimTvScheduleRuleManagementModelBase : SlimTvScheduleManagementBase
  {
    #region Fields

    protected IScheduleRule _selectedScheduleRule;

    protected AbstractProperty _channelNameProperty = null;
    protected AbstractProperty _channelGroupNameProperty = null;
    protected AbstractProperty _channelNumberProperty = null;
    protected AbstractProperty _channelLogoTypeProperty = null;
    protected AbstractProperty _scheduleNameProperty = null;
    protected AbstractProperty _scheduleTypeProperty = null;
    protected AbstractProperty _currentProgramProperty = null;
    protected readonly ItemsList _matchingProgramsList = new ItemsList();
    protected readonly ItemsList _scheduleRulesList = new ItemsList();

    #endregion

    #region GUI properties and methods

    /// <summary>
    /// Exposes the current program of tuned channel to the skin.
    /// </summary>
    public ProgramProperties CurrentProgram
    {
      get { return (ProgramProperties)_currentProgramProperty.GetValue(); }
      set { _currentProgramProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current program of tuned channel to the skin.
    /// </summary>
    public AbstractProperty CurrentProgramProperty
    {
      get { return _currentProgramProperty; }
    }

    /// <summary>
    /// Exposes the current channel name to the skin.
    /// </summary>
    public string ChannelName
    {
      get { return (string)_channelNameProperty.GetValue(); }
      set { _channelNameProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current channel name to the skin.
    /// </summary>
    public AbstractProperty ChannelNameProperty
    {
      get { return _channelNameProperty; }
    }

    /// <summary>
    /// Exposes the current channel logo type to the skin.
    /// </summary>
    public string ChannelLogoType
    {
      get { return (string)_channelLogoTypeProperty.GetValue(); }
      set { _channelLogoTypeProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current channel logo type to the skin.
    /// </summary>
    public AbstractProperty ChannelLogoTypeProperty
    {
      get { return _channelLogoTypeProperty; }
    }

    /// <summary>
    /// Exposes the current channel number to the skin.
    /// </summary>
    public int ChannelNumber
    {
      get { return (int)_channelNumberProperty.GetValue(); }
      set { _channelNumberProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current channel number to the skin.
    /// </summary>
    public AbstractProperty ChannelNumberProperty
    {
      get { return _channelNumberProperty; }
    }

    /// <summary>
    /// Exposes the current channel group name to the skin.
    /// </summary>
    public string ChannelGroupName
    {
      get { return (string)_channelGroupNameProperty.GetValue(); }
      set { _channelGroupNameProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current channel group name to the skin.
    /// </summary>
    public AbstractProperty ChannelGroupNameProperty
    {
      get { return _channelGroupNameProperty; }
    }

    /// <summary>
    /// Exposes the schedule type of current schedule to the skin.
    /// </summary>
    public string ScheduleName
    {
      get { return (string)_scheduleNameProperty.GetValue(); }
      set { _scheduleNameProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the schedule type of current schedule to the skin.
    /// </summary>
    public AbstractProperty ScheduleNameProperty
    {
      get { return _scheduleNameProperty; }
    }

    /// <summary>
    /// Exposes the schedule type of current schedule to the skin.
    /// </summary>
    public string ScheduleType
    {
      get { return (string)_scheduleTypeProperty.GetValue(); }
      set { _scheduleTypeProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the schedule type of current schedule to the skin.
    /// </summary>
    public AbstractProperty ScheduleTypeProperty
    {
      get { return _scheduleTypeProperty; }
    }

    /// <summary>
    /// Exposes the list of programs matching the rule.
    /// </summary>
    public ItemsList MatchingProgramList
    {
      get { return _matchingProgramsList; }
    }

    /// <summary>
    /// Exposes the list of schedule rules.
    /// </summary>
    public ItemsList ScheduleRulesList
    {
      get { return _scheduleRulesList; }
    }

    public void UpdateScheduleRule(object sender, SelectionChangedEventArgs e)
    {
      var selectedItem = e.FirstAddedItem as ListItem;
      if (selectedItem == null)
        return;
      IScheduleRule scheduleRule = (IScheduleRule)selectedItem.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_RULE];
      UpdateScheduleRuleDetails(scheduleRule).Wait();
      if (selectedItem.AdditionalProperties.ContainsKey(SlimTvClientModelBase.KEY_PROP_PROGRAM))
      {
        IProgram program = (IProgram)selectedItem.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_PROGRAM];
        CurrentProgram.SetProgram(program);
      }
      else
      {
        CurrentProgram.SetProgram(null);
      }
    }

    private Task UpdateScheduleRuleDetails(IScheduleRule scheduleRule)
    {
      // Clear properties if no schedule is given
      if (scheduleRule == null)
      {
        ChannelName = ChannelLogoType = ChannelGroupName = ScheduleName = ScheduleType = string.Empty;
        ChannelNumber = 0;
        return Task.CompletedTask;
      }

      IChannel channel = null;
      if (scheduleRule.ChannelId.HasValue && _channels.ContainsKey(scheduleRule.ChannelId.Value))
        channel = _channels[scheduleRule.ChannelId.Value];

      IChannelGroup channelGroup = null;
      if (scheduleRule.ChannelGroupId.HasValue && _channelGroups.ContainsKey(scheduleRule.ChannelGroupId.Value))
        channelGroup = _channelGroups[scheduleRule.ChannelGroupId.Value];

      ChannelName = channel?.Name ?? String.Empty;
      ChannelNumber = channel?.ChannelNumber ?? 0;
      ChannelLogoType = channel?.GetFanArtMediaType() ?? String.Empty;
      ChannelGroupName = channelGroup?.Name ?? String.Empty;
      ScheduleName = scheduleRule.Name;
      ScheduleType = string.Format("[SlimTvClient.ScheduleRecordingType_{0}]", scheduleRule.RecordingType);

      return Task.CompletedTask;
    }
    #endregion

    protected async Task LoadScheduleRules()
    {
      await UpdateScheduleRuleDetails(null);
      await LoadChannels();

      if (_tvHandler.ScheduleRuleControl == null)
        return;

      var result = await _tvHandler.ScheduleRuleControl.GetScheduleRulesAsync();
      if (!result.Success)
        return;

      IList<IScheduleRule> scheduleRules = result.Result;
      if (_mediaMode == MediaMode.Tv)
      {
        scheduleRules = result.Result
          .Where(s => (!s.ChannelId.HasValue && !s.ChannelGroupId.HasValue) ||
                      (s.ChannelId.HasValue && _channels.ContainsKey(s.ChannelId.Value) && _channels[s.ChannelId.Value].MediaType == MediaType.TV) ||
                      (s.ChannelGroupId.HasValue && _channelGroups.ContainsKey(s.ChannelGroupId.Value) && _channelGroups[s.ChannelGroupId.Value].MediaType == MediaType.TV))
          .ToList();
      }
      else if (_mediaMode == MediaMode.Radio)
      {
        scheduleRules = result.Result
          .Where(s => (!s.ChannelId.HasValue && !s.ChannelGroupId.HasValue) ||
                      (s.ChannelId.HasValue && _channels.ContainsKey(s.ChannelId.Value) && _channels[s.ChannelId.Value].MediaType == MediaType.Radio) ||
                      (s.ChannelGroupId.HasValue && _channelGroups.ContainsKey(s.ChannelGroupId.Value) && _channelGroups[s.ChannelGroupId.Value].MediaType == MediaType.Radio))
          .ToList();
      }

      _scheduleRulesList.Clear();

      // Temporary list for sorting
      List<ListItem> sortList = new List<ListItem>();
      Comparison<ListItem> sortMode;

      foreach (IScheduleRule scheduleRule in scheduleRules)
      {
        var item = CreateScheduleRuleItem(scheduleRule);
        sortList.Add(item);
      }

      sortMode = RuleNameAndChannelComparison;
      sortList.Sort(sortMode);

      CollectionUtils.AddAll(_scheduleRulesList, sortList);
      _scheduleRulesList.FireChange();
    }

    /// <summary>
    /// Default sorting for rules: first by <see cref="IScheduleRule.RecordingType"/>=="Once", then <see cref="IScheduleRule.Name"/>, then by <see cref="IChannelGroup.Name"/>, then by <see cref="IChannel.Name"/>.
    /// </summary>
    private int RuleNameAndChannelComparison(ListItem p1, ListItem p2)
    {
      var scheduleRule1 = (IScheduleRule)p1.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_RULE];
      var scheduleRule2 = (IScheduleRule)p2.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_RULE];

      // The "Once" schedule should appear first
      if (scheduleRule1.RecordingType == RuleRecordingType.Once && scheduleRule2.RecordingType != RuleRecordingType.Once)
        return -1;
      if (scheduleRule1.RecordingType != RuleRecordingType.Once && scheduleRule2.RecordingType == RuleRecordingType.Once)
        return +1;

      int res = String.Compare(scheduleRule1.Name, scheduleRule2.Name, StringComparison.InvariantCultureIgnoreCase);
      if (res != 0)
        return res;

      var g1 = scheduleRule1.ChannelGroupId.HasValue && _channelGroups.ContainsKey(scheduleRule1.ChannelGroupId.Value) ? _channelGroups[scheduleRule1.ChannelGroupId.Value] : null;
      var g2 = scheduleRule2.ChannelGroupId.HasValue && _channelGroups.ContainsKey(scheduleRule2.ChannelGroupId.Value) ? _channelGroups[scheduleRule2.ChannelGroupId.Value] : null;
      if (g1 != null && g2 != null)
        res = String.Compare(g1.Name, g2.Name, StringComparison.InvariantCultureIgnoreCase);
      if (res != 0)
        return res;

      var ch1 = scheduleRule1.ChannelId.HasValue && _channels.ContainsKey(scheduleRule1.ChannelId.Value) ? _channels[scheduleRule1.ChannelId.Value] : null;
      var ch2 = scheduleRule2.ChannelId.HasValue && _channels.ContainsKey(scheduleRule2.ChannelId.Value) ? _channels[scheduleRule2.ChannelId.Value] : null;
      if (ch1 != null && ch2 != null)
        return String.Compare(ch1.Name, ch2.Name, StringComparison.InvariantCultureIgnoreCase);

      return 0;
    }

    private ListItem CreateScheduleRuleItem(IScheduleRule scheduleRule)
    {
      IScheduleRule currentSchedule = scheduleRule;
      ListItem item = new ListItem("Name", currentSchedule.Name);
      if (currentSchedule.ChannelId.HasValue && _channels.ContainsKey(currentSchedule.ChannelId.Value))
        item.SetLabel("ChannelName", _channels[currentSchedule.ChannelId.Value].Name);
      if (currentSchedule.ChannelGroupId.HasValue && _channelGroups.ContainsKey(currentSchedule.ChannelGroupId.Value))
        item.SetLabel("ChannelGroupName", _channelGroups[currentSchedule.ChannelGroupId.Value].Name);
      item.SetLabel("ScheduleType", string.Format("[SlimTvClient.ScheduleRecordingType_{0}]", currentSchedule.RecordingType));
      item.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_SCHEDULE] = currentSchedule;
      item.Command = new MethodDelegateCommand(() => ShowActions(item));
      return item;
    }

    protected override void ShowActions(ListItem item)
    {
      var currentScheduleRule = item.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_RULE] as IScheduleRule;
      var program = item.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_PROGRAM] as IProgram;

      DialogHeader = currentScheduleRule?.Name;
      _dialogActionsList.Clear();

      _dialogActionsList.Add(new ListItem(Consts.KEY_NAME, "[SlimTvClient.EditScheduleRule]")
      {
        Command = new AsyncMethodDelegateCommand(() => EditScheduleRule(currentScheduleRule))
      });

      _dialogActionsList.Add(new ListItem(Consts.KEY_NAME, "[SlimTvClient.DeleteScheduleRule]")
      {
        Command = new AsyncMethodDelegateCommand(() => DeleteScheduleRule(currentScheduleRule))
      });

      _dialogActionsList.Add(new ListItem(Consts.KEY_NAME, "[SlimTvClient.ShowRuleMatchingPrograms]")
      {
        Command = new AsyncMethodDelegateCommand(() => ShowMatchingPrograms(currentScheduleRule))
      });
      _dialogActionsList.FireChange();

      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      if (_mediaMode == MediaMode.Radio)
        screenManager.ShowDialog("DialogScheduleManagementRadio");
      else
        screenManager.ShowDialog("DialogScheduleManagement");
    }

    private async Task ShowMatchingPrograms(IScheduleRule scheduleRule)
    {

    }

    private async Task EditScheduleRule(IScheduleRule scheduleRule)
    {

    }

    private async Task DeleteScheduleRule(IScheduleRule scheduleRule)
    {
      if (_tvHandler.ScheduleControl != null && await _tvHandler.ScheduleRuleControl.RemoveScheduleRuleAsync(scheduleRule))
      {
        await LoadScheduleRules();
      }
    }

    protected virtual async Task<bool> CreateScheduleRule(IProgram program, RuleRecordingType recordingType = RuleRecordingType.Once)
    {
      IScheduleRuleControlAsync scheduleControl = _tvHandler.ScheduleRuleControl;
      if (scheduleControl != null)
      {
        string title = program.Title;
        ScheduleRuleTarget target = new ScheduleRuleTarget()
        {
          SearchMatch = RuleSearchMatch.Exact,
          SearchTarget = RuleSearchTarget.Title,
          SearchText = program.Title
        };
        var targets = new List<IScheduleRuleTarget>
        {
          target
        };
        var result = await scheduleControl.CreateScheduleRuleAsync(title, targets, null, _channels[program.ChannelId], null, null, null, 
          recordingType, 0, 0, 0, KeepMethodType.Always, null);
        if (result.Success)
        {
          await LoadScheduleRules();
          return true;
        }
      }

      return false;
    }

    protected override void InitModel()
    {
      if (!_isInitialized)
      {
        _channelNameProperty = new WProperty(typeof(string), string.Empty);
        _channelNumberProperty = new WProperty(typeof(int), 0);
        _channelLogoTypeProperty = new WProperty(typeof(string), string.Empty);
        _scheduleNameProperty = new WProperty(typeof(string), string.Empty);
        _channelGroupNameProperty = new WProperty(typeof(string), string.Empty);
        _scheduleTypeProperty = new WProperty(typeof(string), string.Empty);
        _currentProgramProperty = new WProperty(typeof(ProgramProperties), new ProgramProperties());
        _isInitialized = true;
      }
      base.InitModel();
    }

    protected override void Update()
    {
    }

    #region IWorkflowModel implementation

    public abstract override Guid ModelId { get; }

    public override void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      base.Reactivate(oldContext, newContext);
      _loadChannels = true;
      _ = LoadScheduleRules();
    }

    public override void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      base.EnterModelContext(oldContext, newContext);
      _loadChannels = true;
      _ = LoadScheduleRules();
    }

    #endregion
  }
}
