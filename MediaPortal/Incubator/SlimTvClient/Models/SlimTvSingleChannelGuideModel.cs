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
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// Model which holds the GUI state for the GUI test state.
  /// </summary>
  public class SlimTvSingleChannelGuideModel : SlimTvGuideModelBase
  {
    public const string MODEL_ID_STR = "74F50A53-BEF7-415c-A240-2EC718DA8C0F";

    #region Protected fields

    protected AbstractProperty _channelNameProperty = null;

    #endregion

    #region Variables

    private readonly ItemsList _programsList = new ItemsList();
    private IChannel _channel;

    #endregion

    #region GUI properties and methods

    /// <summary>
    /// Exposes the current channel name to the skin.
    /// </summary>
    public string ChannelName
    {
      get { return (string) _channelNameProperty.GetValue(); }
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
        _channelNameProperty = new WProperty(typeof(string), string.Empty);

      base.InitModel();
    }

    #endregion

    #region Channel, groups and programs

    protected override void Update()
    { }

    protected override void UpdateCurrentChannel()
    {
      SetGroupName();

      if (_webChannelIndex < _channels.Count)
      {
        IChannel channel = _channels[_webChannelIndex];
        _channel = channel;
        ChannelName = channel.Name;
        SetCurrentChannel();
      }
      else
      {
        _channel = null;
        ChannelName = String.Empty;
      }
    }

    protected override void UpdatePrograms()
    {
      _programsList.Clear();
      if (_channel != null)
      {
        if (_tvHandler.ProgramInfo.GetPrograms(_channel, DateTime.Now.AddHours(-2), DateTime.Now.AddHours(24), out _programs))
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
        _programs = null;
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