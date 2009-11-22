#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;

namespace MediaPortal.UI.Presentation.Workflow
{
  /// <summary>
  /// This class provides an interface for the messages sent by the workflow manager.
  /// This class is part of the workflow manager API.
  /// </summary>
  public class WorkflowManagerMessaging
  {
    // Message channel name
    public const string CHANNEL = "WorkflowManager";

    /// <summary>
    /// Messages of this type are sent by the <see cref="IWorkflowManager"/>.
    /// </summary>
    /// <remarks>
    /// After calls of type <see cref="StatePushed"/> and <see cref="StatesPopped"/>,
    /// the workflow manager will trigger a call to <see cref="NavigationComplete"/>.
    /// These messages are sent synchronous - this means it is not allowed to request any lock which could
    /// interfere with the lock of the workflow manager, which is helt during the message sending.
    /// </remarks>
    public enum MessageType
    {
      /// <summary>
      /// A new workflow state was pushed onto the workflow navigation context stack.
      /// The parameter <see cref="CONTEXT"/> will contain the navigation context of the new state.
      /// </summary>
      StatePushed,

      /// <summary>
      /// States were popped from the workflow navigation context stack.
      /// The parameter <see cref="CONTEXTS"/> will contain a mapping of state guids to navigation contexts of those states
      /// which were popped.
      /// </summary>
      StatesPopped,

      /// <summary>
      /// A navigation is completed.
      /// </summary>
      NavigationComplete,
    }

    // Message data
    public const string CONTEXTS = "Contexts"; // Type: IDictionary<Guid, NavigationContext>
    public const string CONTEXT = "Context"; // Type: NavigationContext

    /// <summary>
    /// Sends a <see cref="MessageType.StatePushed"/> message.
    /// </summary>
    /// <param name="context">Navigation context of the workflow state which was pushed onto the navigation context stack.</param>
    public static void SendStatePushedMessage(NavigationContext context)
    {
      QueueMessage msg = new QueueMessage(MessageType.StatePushed);
      msg.MessageData[CONTEXT] = context;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    /// <summary>
    /// Sends a <see cref="MessageType.StatesPopped"/> message.
    /// </summary>
    /// <param name="contexts">Dictionary of workflow state ids to navigation contexts which were popped off the
    /// navigation context stack.</param>
    public static void SendStatesPoppedMessage(IDictionary<Guid, NavigationContext> contexts)
    {
      QueueMessage msg = new QueueMessage(MessageType.StatesPopped);
      msg.MessageData[CONTEXTS] = contexts;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    public static void SendNavigationCompleteMessage()
    {
      QueueMessage msg = new QueueMessage(MessageType.NavigationComplete);
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
