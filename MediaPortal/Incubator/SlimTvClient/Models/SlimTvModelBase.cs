#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// <see cref="SlimTvModelBase"/> provides basic features for all derived models, i.e. channel groupp and channel selection.
  /// </summary>
  public abstract class SlimTvModelBase : BaseTimerControlledModel, IWorkflowModel
  {
    #region Protected fields

    protected ITvHandler _tvHandler;
    protected IList<IChannelGroup> _channelGroups;
    protected IList<IChannel> _channels;
    protected int _webChannelGroupIndex;
    protected int _webChannelIndex;

    protected IList<IProgram> _programs;
    protected bool _isInitialized;

    #endregion

    #region Constructor

    protected SlimTvModelBase()
      : this(5000)
    { }

    protected SlimTvModelBase(long updateInterval)
      : base (updateInterval)
    { }

    #endregion

    #region GUI properties and methods

    /// <summary>
    /// Skips group index to next one.
    /// </summary>
    public void NextGroup()
    {
      if (_channelGroups == null)
        return;

      _webChannelGroupIndex++;
      if (_webChannelGroupIndex >= _channelGroups.Count)
        _webChannelGroupIndex = 0;

      UpdateChannels();
    }

    /// <summary>
    /// Skips group index to previous one.
    /// </summary>
    public void PrevGroup()
    {
      if (_channelGroups == null)
        return;

      _webChannelGroupIndex--;
      if (_webChannelGroupIndex < 0)
        _webChannelGroupIndex = _channelGroups.Count - 1;

      UpdateChannels();
    }

    /// <summary>
    /// Skips group index to next one.
    /// </summary>
    public void NextChannel()
    {
      if (_channels == null)
        return;

      _webChannelIndex++;
      if (_webChannelIndex >= _channels.Count)
        _webChannelIndex = 0;

      UpdateCurrentChannel();
      UpdatePrograms();
    }

    /// <summary>
    /// Skips group index to previous one.
    /// </summary>
    public void PrevChannel()
    {
      if (_channelGroups == null)
        return;

      _webChannelIndex--;
      if (_webChannelIndex < 0)
        _webChannelIndex = _channels.Count - 1;

      UpdateCurrentChannel();
      UpdatePrograms();
    }

    public void ExecProgramAction(ListItem item)
    {
      if (item == null)
        return;
      if (item.Command != null)
        item.Command.Execute();
    }

    #endregion

    #region Members

    #region Inits and Updates

    protected virtual void InitModel()
    {
      if (_tvHandler == null)
      {
        _tvHandler = ServiceRegistration.Get<ITvHandler>();
        _tvHandler.Initialize();
      }
      _tvHandler.ChannelAndGroupInfo.GetChannelGroups(out _channelGroups);

      GetCurrentChannelGroup();
      GetCurrentChannel();
      UpdateChannels();
      UpdatePrograms();
    }

    protected void GetCurrentChannelGroup()
    {
      _webChannelGroupIndex = 0;
      if (_channelGroups != null && _tvHandler.ChannelAndGroupInfo != null && _tvHandler.ChannelAndGroupInfo.SelectedChannelGroupId != 0)
        for (int idx = 0; idx < _channelGroups.Count; idx++)
          if (_channelGroups[idx].ChannelGroupId == _tvHandler.ChannelAndGroupInfo.SelectedChannelGroupId)
          {
            _webChannelGroupIndex = idx;
            break;
          }
    }

    protected void GetCurrentChannel()
    {
      _webChannelIndex = 0;
      if (_channels != null && _tvHandler.ChannelAndGroupInfo != null && _tvHandler.ChannelAndGroupInfo.SelectedChannelId != 0)
        for (int idx = 0; idx < _channels.Count; idx++)
          if (_channels[idx].ChannelId == _tvHandler.ChannelAndGroupInfo.SelectedChannelId)
          {
            _webChannelIndex = idx;
            break;
          }
    }

    protected void SetCurrentChannelGroup()
    {
      if (_tvHandler.ChannelAndGroupInfo != null)
        _tvHandler.ChannelAndGroupInfo.SelectedChannelGroupId = _channelGroups[_webChannelGroupIndex].ChannelGroupId;
    }

    protected void SetCurrentChannel()
    {
      if (_tvHandler.ChannelAndGroupInfo != null)
        _tvHandler.ChannelAndGroupInfo.SelectedChannelId = _channels[_webChannelIndex].ChannelId;
    }

    #endregion

    #region Channel, groups and programs

    protected abstract void UpdateCurrentChannel();
    protected abstract void UpdatePrograms();

    protected virtual void UpdateChannels()
    {
      if (_webChannelGroupIndex < _channelGroups.Count)
      {
        IChannelGroup group = _channelGroups[_webChannelGroupIndex];
        _tvHandler.ChannelAndGroupInfo.GetChannels(group, out _channels);

        _webChannelIndex = 0;
        SetCurrentChannelGroup();
        UpdateCurrentChannel();
        UpdatePrograms();
      }
    }

    #endregion

    #endregion

    #region IWorkflowModel implementation

    public abstract Guid ModelId { get; }

    public virtual bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public virtual void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      InitModel();
    }

    public virtual void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public virtual void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
    }

    public virtual void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public virtual void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public virtual void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion

    #region IDisposable Member

    public override void Dispose()
    {
      base.Dispose();

      if (_tvHandler != null)
        _tvHandler.Dispose();
    }

    #endregion
  }
}