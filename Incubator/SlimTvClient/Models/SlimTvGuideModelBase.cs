#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.Plugins.SlimTvClient.Helpers;
using MediaPortal.Plugins.SlimTvClient.Interfaces;
using MediaPortal.Plugins.SlimTvClient.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.Plugins.SlimTvClient
{
  /// <summary>
  /// Model which holds the GUI state for the GUI test state.
  /// </summary>
  public abstract class SlimTvGuideModelBase : IDisposable
  {
    #region Protected fields

    protected AbstractProperty _groupNameProperty = null;
    protected AbstractProperty _currentProgramProperty = null;

    #endregion

    #region Variables

    protected ITvHandler _tvHandler;
    protected IList<IChannelGroup> _channelGroups;
    protected IList<IChannel> _channels;
    protected int _webChannelGroupIndex;
    protected int _webChannelIndex;

    protected IList<IProgram> _programs;
    protected ItemsList _programActions;
    protected bool _isInitialized;
    protected string _programActionsDialogName = "DialogProgramActions";

    #endregion

    #region GUI properties and methods

    /// <summary>
    /// Exposes the current group name to the skin.
    /// </summary>
    public string GroupName
    {
      get { return (string)_groupNameProperty.GetValue(); }
      set { _groupNameProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current group name to the skin.
    /// </summary>
    public AbstractProperty GroupNameProperty
    {
      get { return _groupNameProperty; }
    }

    /// <summary>
    /// Exposes the list of channels in current group.
    /// </summary>
    public ItemsList ProgramActions
    {
      get { return _programActions; }
    }

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

    public void UpdateProgram(ListItem selectedItem)
    {
      if (selectedItem != null)
      {
        IProgram program = (IProgram)selectedItem.AdditionalProperties["PROGRAM"];
        UpdateSingleProgramInfo(program);
      }
    }

    public void ExecProgramAction(ListItem item)
    {
      if (item == null)
        return;
      if (item.Command != null)
        item.Command.Execute();
    }

    protected void ShowProgramActions(IProgram program)
    {
      if (program == null)
        return;
      
      ILocalization loc = ServiceRegistration.Get<ILocalization>();

      _programActions = new ItemsList();
      // if program is over already, there is nothing to do.
      if (program.EndTime < DateTime.Now)
      {
        _programActions.Add(new ListItem(Consts.KEY_NAME, loc.ToString("[SlimTvClient.ProgramOver]")));
      }
      else
      {
        // check if program is currently running.
        if (DateTime.Now >= program.StartTime && DateTime.Now <= program.EndTime)
        {
          _programActions.Add(new ListItem(Consts.KEY_NAME, loc.ToString("[SlimTvClient.WatchNow]"))
                                {
                                  Command =
                                    new MethodDelegateCommand(() =>
                                    {
                                      IChannel channel;
                                      if (_tvHandler.ChannelAndGroupInfo.GetChannel(program.ChannelId, out channel))
                                        _tvHandler.StartTimeshift(PlayerManagerConsts.PRIMARY_SLOT, channel);
                                    })
                                });
        }
        //TODO: define and implement recording info interfaces, add code again
        //if (program.IsRecording || program.IsRecordingOncePending || program.IsRecordingSeriesPending)
        //{
        //  _programActions.Add(
        //    new ListItem(Consts.KEY_NAME, loc.ToString("[SlimTvClient.DeleteSchedule]"))
        //      {
        //        Command = new MethodDelegateCommand(() => _tvServer.TvServer.CancelSchedule(programId))
        //      });
        //}
        //else
        //{
        //  _programActions.Add(
        //    new ListItem(Consts.KEY_NAME, loc.ToString("[SlimTvClient.CreateSchedule]"))
        //      {
        //        Command = new MethodDelegateCommand(() =>
        //                                            _tvServer.TvServer.AddSchedule(_channelID, program.Title,
        //                                                                           program.StartTime, program.EndTime, 0))
        //      });
        //}
      }
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog(_programActionsDialogName);
    }

    #endregion

    #region Members

    #region Inits and Updates

    protected virtual void InitModel()
    {
      if (!_isInitialized)
      {
        _groupNameProperty = new WProperty(typeof(string), string.Empty);
        _currentProgramProperty = new WProperty(typeof(ProgramProperties), new ProgramProperties());

        if (!ServiceRegistration.IsRegistered<ITvHandler>())
          ServiceRegistration.Set<ITvHandler>(new SlimTvHandler());

        _tvHandler = ServiceRegistration.Get<ITvHandler>();

        _tvHandler.ChannelAndGroupInfo.GetChannelGroups(out _channelGroups);

        _webChannelGroupIndex = 0;
        _isInitialized = true;

        UpdateChannels();
        UpdatePrograms();
      }
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
        GroupName = group.Name;

        _webChannelIndex = 0;
        UpdateCurrentChannel();
        UpdatePrograms();
      }
    }


    private void UpdateSingleProgramInfo(IProgram program)
    {
      CurrentProgram.SetProgram(program);
    }

    #endregion

    #endregion
    
    #region IWorkflowModel implementation

    public virtual Guid ModelId
    {
      get { return Guid.Empty; }
    }

    public virtual bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public virtual void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      InitModel();

      // We could initialize some data here when entering the media navigation state
    }

    public virtual void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // We could dispose some data here when exiting media navigation context
    }

    public virtual void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // We could initialize some data here when changing the media navigation state
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

    public void Dispose()
    {
      if (_tvHandler != null)
        _tvHandler.Dispose();
    }

    #endregion
  }
}