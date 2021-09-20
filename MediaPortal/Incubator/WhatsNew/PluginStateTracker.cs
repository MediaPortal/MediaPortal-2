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
using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using WhatsNew.Settings;

namespace WhatsNew
{
  public class PluginStateTracker : IPluginStateTracker
  {
    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue;
    protected object _syncObj = new object();


    #endregion

    public PluginStateTracker()
    {
    }

    protected void DropMessageQueue()
    {
      lock (_syncObj)
      {
        if (_messageQueue != null)
          _messageQueue.Terminate();
        _messageQueue = null;
      }
    }

    #region Implementation of IPluginStateTracker

    public void Activated(PluginRuntime pluginRuntime)
    {
      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      var settings = settingsManager.Load<WhatsNewSettings>();

      if (settings.NewsConfirmed)
        return;

      _messageQueue = new AsynchronousMessageQueue(this.GetType(), new string[]
         {
            WorkflowManagerMessaging.CHANNEL
         });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WorkflowManagerMessaging.CHANNEL)
      {
        if ((WorkflowManagerMessaging.MessageType)message.MessageType == WorkflowManagerMessaging.MessageType.StatePushed)
        {
          var workflowManager = ServiceRegistration.Get<IWorkflowManager>();
          var isHomeScreen = workflowManager.CurrentNavigationContext.WorkflowState.StateId.ToString().Equals("7F702D9C-F2DD-42da-9ED8-0BA92F07787F", StringComparison.OrdinalIgnoreCase);
          if (isHomeScreen)
          {
            // Show Dialog
            IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
            screenManager.ShowDialog("whats-new");
          }

        }
      }
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      DropMessageQueue();
    }

    public void Continue()
    {
      // Nothing to do
    }

    public void Shutdown()
    {
      // Nothing to do
    }

#endregion
  }
}
