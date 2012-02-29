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
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Plugins.SlimTvClient.Helpers;
using MediaPortal.Plugins.SlimTvClient.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.Plugins.SlimTvClient
{
  /// <summary>
  /// <see cref="SlimTvGuideModelBase"/> acts as base class for all TvGuide models (single channel, multi channel).
  /// </summary>
  public abstract class SlimTvGuideModelBase : SlimTvModelBase
  {
    #region Protected fields

    protected AbstractProperty _groupNameProperty = null;
    protected AbstractProperty _currentProgramProperty = null;

    #endregion

    #region Variables

    protected ItemsList _programActions;
    protected string _programActionsDialogName = "DialogProgramActions";

    #endregion

    #region GUI properties and methods

    /// <summary>
    /// Exposes the current group name to the skin.
    /// </summary>
    public string GroupName
    {
      get { return (string) _groupNameProperty.GetValue(); }
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
      get { return (ProgramProperties) _currentProgramProperty.GetValue(); }
      set { _currentProgramProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current program of tuned channel to the skin.
    /// </summary>
    public AbstractProperty CurrentProgramProperty
    {
      get { return _currentProgramProperty; }
    }

    public void UpdateProgram(ListItem selectedItem)
    {
      if (selectedItem != null)
      {
        IProgram program = (IProgram) selectedItem.AdditionalProperties["PROGRAM"];
        UpdateSingleProgramInfo(program);
      }
    }

    protected void SetGroupName()
    {
      if (_webChannelGroupIndex < _channelGroups.Count)
      {
        IChannelGroup group = _channelGroups[_webChannelGroupIndex];
        GroupName = group.Name;
      }
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
                                      if (_tvHandler.ProgramInfo.GetChannel(program, out channel))
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

    protected override void InitModel()
    {
      if (!_isInitialized)
      {
        _groupNameProperty = new WProperty(typeof(string), string.Empty);
        _currentProgramProperty = new WProperty(typeof(ProgramProperties), new ProgramProperties());

        _isInitialized = true;
      }
      base.InitModel();
    }

    #endregion

    #region Channel, groups and programs

    private void UpdateSingleProgramInfo(IProgram program)
    {
      CurrentProgram.SetProgram(program);
    }

    #endregion

    #endregion
  }
}