#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

namespace MediaPortal.UI.Presentation.UiNotifications
{
  /// <summary>
  /// Type of the notification. Can be used to choose the icon to show.
  /// </summary>
  public enum NotificationType
  {
    Info,
    Warning,
    Error,
    UserInteractionRequired
  }

  /// <summary>
  /// Notification data class. Contains data and lifetime callback methods of a notification.
  /// </summary>
  public interface INotification
  {
    NotificationType Type { get; }
    string Title { get; }
    string Text { get; }
    DateTime? Timeout { get; set; }
    Guid? HandlerWorkflowState { get; }
    string CustomIconPath { get; }

    /// <summary>
    /// Called when this notification is enqueued to the notification queue.
    /// </summary>
    void Enqueued();

    /// <summary>
    /// Called when this notification is dequeued to the notification queue.
    /// </summary>
    void Dequeued();
  }

  /// <summary>
  /// Service which maintains a queue of notifications to be shown to the user.
  /// </summary>
  /// <remarks>
  /// This service is thread-safe.
  /// </remarks>
  public interface INotificationService
  {
    /// <summary>
    /// Gets the list of queued notifications. The next entry to be dequeued is at the end of the returned list.
    /// </summary>
    IList<INotification> Notifications { get; }

    /// <summary>
    /// Gets or sets a flag indicating whether this service automatically removes timed-out notifications.
    /// </summary>
    bool CheckForTimeouts { get; set; }

    void Startup();
    void Shutdown();

    /// <summary>
    /// Enqueues a new basic notification which is simply shown to the user and discarded.
    /// </summary>
    /// <remarks>
    /// The <see cref="INotification.Timeout"/> of the returned <see cref="INotification"/> object may be modified by the caller.
    /// </remarks>
    /// <param name="type">Type of the notification to enqueue. The type only determines how the notification is
    /// shown to the user.</param>
    /// <param name="title">The short title of the notification which is presented to the user.</param>
    /// <param name="text">The detailed text of the notification.</param>
    /// <param name="urgent">If this flag is set, the notification is put in front of the notification queue.</param>
    /// <returns>Notification instance which was enqueued.</returns>
    INotification EnqueueNotification(NotificationType type, string title, string text, bool urgent);

    /// <summary>
    /// Enqueues a new extended notification. The user gets the option to navigate to a "handler" workflow state 
    /// which gives the user more information and maybe presents a
    /// sub workflow.
    /// </summary>
    /// <remarks>
    /// The <see cref="INotification.Timeout"/> of the returned <see cref="INotification"/> object may be modified by the caller.
    /// </remarks>
    /// <param name="type">Type of the notification to enqueue. The type only determines how the notification is
    /// shown to the user.</param>
    /// <param name="title">The short title of the notification which is presented to the user.</param>
    /// <param name="text">The detailed text of the notification.</param>
    /// <param name="handlerWorkflowState">Workflow state which is provided to be navigated to in the context of
    /// the notification.</param>
    /// <param name="urgent">If this flag is set, the notification is put in front of the notification queue.</param>
    /// <returns>Notification instance which was enqueued.</returns>
    INotification EnqueueNotification(NotificationType type, string title, string text,
        Guid handlerWorkflowState, bool urgent);

    /// <summary>
    /// Enqueues a new extended custom notification. A custom notification gets notified when it is enqueued
    /// and dequeued.
    /// </summary>
    /// <param name="notification">The custom notification object to enqueue.</param>
    /// <param name="urgent">If this flag is set, the notification is put in front of the notification queue.</param>
    void EnqueueNotification(INotification notification, bool urgent);

    /// <summary>
    /// Removes a notification from the notification queue, if the notification is not valid any more.
    /// </summary>
    /// <param name="notification">The notification to be removed from the queue.</param>
    void RemoveNotification(INotification notification);

    /// <summary>
    /// Returns but not removes the next notification of the notification queue.
    /// </summary>
    /// <returns>
    /// Next notification from the queue or <c>null</c>, if no notification is available.
    /// </returns>
    INotification PeekNotification();

    /// <summary>
    /// Returns and removes the next notification of the notification queue.
    /// </summary>
    /// <returns>
    /// Next notification from the queue or <c>null</c>, if no notification is available.
    /// </returns>
    INotification DequeueNotification();
  }
}