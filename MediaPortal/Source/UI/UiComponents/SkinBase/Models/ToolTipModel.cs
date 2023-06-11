#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.Common.General;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using System;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  /// <summary>
  /// Model that can be used by screens to set and display a tooltip text to
  /// describe the action of the currently focused control.
  /// </summary>
  public class ToolTipModel : BaseMessageControlledModel
  {
    const string STR_TOOLTIP_MODEL_ID = "CA1BA301-6DA4-41C5-997A-0D4C8A8E66D9";
    public static readonly Guid TOOLTIP_MODEL_ID = new Guid(STR_TOOLTIP_MODEL_ID);

    protected AbstractProperty _textProperty = new WProperty(typeof(string), null);

    public ToolTipModel()
    {
      SubscribeToMessages();
    }

    public AbstractProperty TextProperty
    {
      get { return _textProperty; }
    }

    /// <summary>
    /// Help text to display to describe the action of the currently focused control. Should be set by the skin,
    /// either directly of with calls to <see cref="SetText(string)"/> and <see cref="ClearText"/>.
    /// </summary>
    public string Text
    {
      get { return (string)_textProperty.GetValue(); }
      set { _textProperty.SetValue(value); }
    }

    /// <summary>
    /// Sets the <see cref="Text"/> property to the specified value.
    /// </summary>
    /// <param name="value"></param>
    public void SetText(string value)
    {
      Text = value;
    }

    /// <summary>
    /// Clears the <see cref="Text"/> property to <c>null</c>. 
    /// </summary>
    public void ClearText()
    {
      Text = null;
    }

    void SubscribeToMessages()
    {
      _messageQueue.PreviewMessage += OnMessageReceived;
      _messageQueue.SubscribeToMessageChannel(WorkflowManagerMessaging.CHANNEL);
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WorkflowManagerMessaging.CHANNEL)
      {
        WorkflowManagerMessaging.MessageType messageType = (WorkflowManagerMessaging.MessageType)message.MessageType;
        // Reset the tooltip text when a new screen is being shown
        if (messageType == WorkflowManagerMessaging.MessageType.StatePushed || messageType == WorkflowManagerMessaging.MessageType.StatesPopped)
          ClearText();
      }
    }
  }
}
