#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UiComponents.WMCSkin.Messaging;
using MediaPortal.Utilities.Events;
using System;

namespace MediaPortal.UiComponents.WMCSkin.Models
{
  public class HomeContentModel
  {
    //Intermediate value for focused content index to ensure that
    //a changed event is always fired when toggling focus
    protected const int NO_FOCUS = -2;
    //Focused content index that indicates that the content
    //does not have focus
    protected const int UNFOCUSED = -1;

    public const string STR_MODEL_ID = "24BB1BBE-A3B3-474A-8D55-C37EBE182F6C";
    public static readonly Guid MODEL_ID = new Guid(STR_MODEL_ID);

    protected const int UPDATE_DELAY_MS = 500;
    
    protected AbstractProperty _currentContentIndexProperty;
    protected AbstractProperty _isContentFocusedProperty;
    protected AbstractProperty _focusedContentIndexProperty;
    protected AbstractProperty _content0ActionIdProperty;
    protected AbstractProperty _content1ActionIdProperty;
    protected WorkflowAction _nextAction;
    protected DelayedEvent _updateEvent;
    protected AsynchronousMessageQueue _messageQueue;

    public HomeContentModel()
    {
      Init();
      Attach();
    }

    protected void Init()
    {
      _currentContentIndexProperty = new WProperty(typeof(int), 0);
      _isContentFocusedProperty = new WProperty(typeof(bool), false);
      _focusedContentIndexProperty = new WProperty(typeof(int), -1);
      _content0ActionIdProperty = new WProperty(typeof(string), null);
      _content1ActionIdProperty = new WProperty(typeof(string), null);
      _updateEvent = new DelayedEvent(UPDATE_DELAY_MS);
      _updateEvent.OnEventHandler += OnUpdate;

      SubscribeToMessages();
    }

    protected void Attach()
    {
      _isContentFocusedProperty.Attach(OnIsContentFocusedChanged);
    }

    #region Message Handling

    private void SubscribeToMessages()
    {
      if (_messageQueue != null)
        return;
      _messageQueue = new AsynchronousMessageQueue(this, new[] { HomeMenuMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == HomeMenuMessaging.CHANNEL)
      {
        HomeMenuMessaging.MessageType messageType = (HomeMenuMessaging.MessageType)message.MessageType;
        if (messageType == HomeMenuMessaging.MessageType.CurrentItemChanged)
          OnCurrentHomeItemChanged(message.MessageData[HomeMenuMessaging.NEW_ITEM] as ListItem);
      }
    }

    #endregion

    private void OnIsContentFocusedChanged(AbstractProperty property, object oldValue)
    {
      //Reset focused content index when the focus changes
      //so a changed event is always fired when ToggleFocus is called
      FocusedContentIndex = NO_FOCUS;
    }

    public AbstractProperty CurrentContentIndexProperty
    {
      get { return _currentContentIndexProperty; }
    }

    public int CurrentContentIndex
    {
      get { return (int)_currentContentIndexProperty.GetValue(); }
      set { _currentContentIndexProperty.SetValue(value); }
    }

    public AbstractProperty IsContentFocusedProperty
    {
      get { return _isContentFocusedProperty; }
    }

    public bool IsContentFocused
    {
      get { return (bool)_isContentFocusedProperty.GetValue(); }
      set { _isContentFocusedProperty.SetValue(value); }
    }

    public AbstractProperty FocusedContentIndexProperty
    {
      get { return _focusedContentIndexProperty; }
    }

    public int FocusedContentIndex
    {
      get { return (int)_focusedContentIndexProperty.GetValue(); }
      set { _focusedContentIndexProperty.SetValue(value); }
    }

    public AbstractProperty Content0ActionIdProperty
    {
      get { return _content0ActionIdProperty; }
    }

    public string Content0ActionId
    {
      get { return (string)_content0ActionIdProperty.GetValue(); }
      set { _content0ActionIdProperty.SetValue(value); }
    }

    public AbstractProperty Content1ActionIdProperty
    {
      get { return _content1ActionIdProperty; }
    }

    public string Content1ActionId
    {
      get { return (string)_content1ActionIdProperty.GetValue(); }
      set { _content1ActionIdProperty.SetValue(value); }
    }

    /// <summary>
    /// Toggles the focus on the home content.
    /// </summary>
    public void ToggleFocus()
    {
      if (IsContentFocused)
      {
        IsContentFocused = false;
        FocusedContentIndex = UNFOCUSED;
      }
      else
        FocusedContentIndex = CurrentContentIndex;
    }

    private void EnqueueUpdate(WorkflowAction action)
    {
      _nextAction = action;
      _updateEvent.EnqueueEvent(this, EventArgs.Empty);
    }

    private void OnUpdate(object sender, EventArgs e)
    {
      UpdateContent(_nextAction);
    }

    private void OnCurrentHomeItemChanged(ListItem newItem)
    {
      WorkflowAction action;
      if (!TryGetAction(newItem, out action))
        action = null;
      EnqueueUpdate(action);
    }

    protected void UpdateContent(WorkflowAction action)
    {
      string nextContentActionId = action?.ActionId.ToString();
      AbstractProperty nextContentActionIdProperty;
      int nextContentIndex;
      int currentContentIndex = CurrentContentIndex;
      if (currentContentIndex == 0)
      {
        if (Content0ActionId == nextContentActionId)
          return;
        nextContentIndex = 1;
        nextContentActionIdProperty = _content1ActionIdProperty;
      }
      else
      {
        if (Content1ActionId == nextContentActionId)
          return;
        nextContentIndex = 0;
        nextContentActionIdProperty = _content0ActionIdProperty;
      }
      FocusedContentIndex = NO_FOCUS;
      nextContentActionIdProperty.SetValue(nextContentActionId);
      CurrentContentIndex = nextContentIndex;
    }

    protected static bool TryGetAction(ListItem item, out WorkflowAction action)
    {
      if (item != null)
      {
        action = item.AdditionalProperties[Consts.KEY_ITEM_ACTION] as WorkflowAction;
        return action != null;
      }
      action = null;
      return false;
    }
  }
}
