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
using MediaPortal.Core.Commands;
using MediaPortal.Core.General;
using MediaPortal.Plugins.SlimTvClient.Helpers;
using MediaPortal.Plugins.SlimTvClient.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;

namespace MediaPortal.Plugins.SlimTvClient
{
  /// <summary>
  /// Model which holds the GUI state for the GUI test state.
  /// </summary>
  public class SlimTvMultiChannelGuideModel : SlimTvGuideModelBase, IWorkflowModel
  {
    public const string MODEL_ID_STR = "5054408D-C2A9-451f-A702-E84AFCD29C10";

    #region Constructor

    public SlimTvMultiChannelGuideModel()
    {
      _programActionsDialogName = "DialogProgramActionsFull"; // for MultiChannelGuide we need another dialog
    }

    #endregion

    #region Protected fields

    protected AbstractProperty _channelNameProperty = null;

    #endregion

    #region Variables

    private ItemsList _channelList = new ItemsList();
    private ItemsList _programsList = new ItemsList();

    #endregion

    #region GUI properties and methods

    /// <summary>
    /// Exposes the list of channels in current group.
    /// </summary>
    public ItemsList ChannelList
    {
      get { return _channelList; }
    }

    /// <summary>
    /// Exposes the list of channels in current group.
    /// </summary>
    public ItemsList ProgramsList
    {
      get { return _programsList; }
    }

    #endregion

    #region Members

    #region Inits and Updates

    protected override void InitModel()
    {
      if (!_isInitialized)
      {
        base.InitModel();
      }
    }

    #endregion

    #region Channel, groups and programs

    protected override void UpdateChannels()
    {
      base.UpdateChannels();
      _channelList.Clear();
      if (_channels != null)
      {
        foreach (IChannel channel in _channels)
        {
          _channelList.Add(new ChannelProgramListItem(channel, GetProgramsList(channel)));
        }
      }
      _channelList.FireChange();
    }

    protected ItemsList GetProgramsList(IChannel channel)
    {
      ItemsList channelPrograms = new ItemsList();
      if (_tvHandler.ProgramInfo.GetPrograms(channel, DateTime.Now, DateTime.Now.AddHours(6), out _programs))
      {
        foreach (IProgram program in _programs)
        {
          // Use local variable, otherwise delegate argument is not fixed
          ProgramProperties programProperties = new ProgramProperties();
          IProgram currentProgram = program;
          programProperties.SetProgram(currentProgram);

          ProgramListItem item = new ProgramListItem(programProperties)
          {
            Command = new MethodDelegateCommand(() => ShowProgramActions(currentProgram))
          };
          item.AdditionalProperties["PROGRAM"] = currentProgram;

          channelPrograms.Add(item);
        }
      }
      return channelPrograms;
    }
    protected override void UpdateCurrentChannel()
    {
    }

    protected override void UpdatePrograms()
    {
      if (_channels != null && _channels.Count > 0)
      {
        IChannel channel = _channels[0];
        _programsList.Clear();
        if (_tvHandler.ProgramInfo.GetPrograms(channel, DateTime.Now.AddHours(-2), DateTime.Now.AddHours(24), out _programs))
        {
          foreach (IProgram program in _programs)
          {
            // Use local variable, otherwise delegate argument is not fixed
            ProgramProperties programProperties = new ProgramProperties();
            IProgram currentProgram = program;
            programProperties.SetProgram(currentProgram);

            ProgramListItem item = new ProgramListItem(programProperties)
            {
              Command = new MethodDelegateCommand(() => ShowProgramActions(currentProgram))
            };
            item.AdditionalProperties["PROGRAM"] = currentProgram;

            _programsList.Add(item);
          }
        }
        ProgramsList.FireChange();
      }
      else
      {
        _programs = null;
        _programsList.Clear();
      }

    }

    #endregion

    #endregion

    #region IWorkflowModel implementation

    public override Guid ModelId
    {
      get { return new Guid(MODEL_ID_STR); }
    }

    #endregion

  }
}