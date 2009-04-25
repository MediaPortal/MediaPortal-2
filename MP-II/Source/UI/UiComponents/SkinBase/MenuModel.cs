#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.Messaging;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Workflow;

namespace UiComponents.SkinBase
{
  public class MenuModel : IDisposable
  {
    #region Consts

    protected const string ITEM_ACTION_KEY = "MenuModel: Item-Action";

    #endregion

    #region Protected fields

    protected ItemsList _currentMenuItems = new ItemsList();
    protected object _syncObj = new object();

    #endregion

    #region Ctor

    public MenuModel()
    {
      SubscribeToMessages();
      UpdateMenu();
    }

    #endregion

    #region Protected methods

    protected void SubscribeToMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(WorkflowManagerMessaging.QUEUE).MessageReceived += OnWorkflowManagerMessageReceived;
    }

    protected void UnsubscribeFromMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(WorkflowManagerMessaging.QUEUE).MessageReceived -= OnWorkflowManagerMessageReceived;
    }

    protected void OnWorkflowManagerMessageReceived(QueueMessage message)
    {
      WorkflowManagerMessaging.MessageType messageType = (WorkflowManagerMessaging.MessageType) message.MessageData[WorkflowManagerMessaging.MESSAGE_TYPE];
      switch (messageType)
      {
        case WorkflowManagerMessaging.MessageType.StatePushed:
        case WorkflowManagerMessaging.MessageType.StatesPopped:
          UpdateMenu();
          break;
      }
    }

    protected void OnMenuActionStateChanged(WorkflowAction action)
    {
      // TODO: Can we optimize this? If multiple actions change their state simultaneously, we only need one update
      UpdateMenu();
    }

    protected void UpdateMenu()
    {
      lock (_syncObj)
      {
        foreach (ListItem item in _currentMenuItems)
          ((WorkflowAction) item.AdditionalProperties[ITEM_ACTION_KEY]).StateChanged -= OnMenuActionStateChanged;
        _currentMenuItems.Clear();

        NavigationContext context = ServiceScope.Get<IWorkflowManager>().CurrentNavigationContext;
        foreach (WorkflowAction action in context.MenuActions.Values)
        {
          action.StateChanged += OnMenuActionStateChanged;
          if (!action.IsVisible)
            continue;
          ListItem item = new ListItem("Name", action.DisplayTitle)
            {
                Command = new MethodDelegateCommand(action.Execute),
                Enabled = action.IsEnabled
            };
          item.AdditionalProperties[ITEM_ACTION_KEY] = action;
          _currentMenuItems.Add(item);
        }
        _currentMenuItems.FireChange();
      }
    }

    #endregion

    #region Public properties

    public ItemsList MenuItems
    {
      get { return _currentMenuItems; }
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      UnsubscribeFromMessages();
    }

    #endregion
  }
}
