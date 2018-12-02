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

using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Plugins.SlimTv.Client.Notifications;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using System;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// Model for displaying SlimTv popup notifications in a superlayer
  /// </summary>
  public class SlimTvNotificationModel
  {
    public const string MODEL_ID_STR = "7F283468-27E9-46B9-8B3B-17E3143AFEEB";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    protected AbstractProperty _notificationProperty = new WProperty(typeof(ISlimTvNotification), null);

    public static SlimTvNotificationModel Instance
    {
      get { return (SlimTvNotificationModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(MODEL_ID); }
    }

    public AbstractProperty NotificationProperty
    {
      get { return _notificationProperty; }
    }

    public ISlimTvNotification Notification
    {
      get { return (ISlimTvNotification)_notificationProperty.GetValue(); }
      set { _notificationProperty.SetValue(value); }
    }

    /// <summary>
    /// Shows the notification using the super layer specified in the <paramref name="notification"/>. 
    /// </summary>
    /// <param name="notification">The notification to show.</param>
    /// <param name="duration">Duration to show the given notification.</param>
    public void ShowNotification(ISlimTvNotification notification, TimeSpan duration)
    {
      Notification = notification;
      ServiceRegistration.Get<ISuperLayerManager>().ShowSuperLayer(notification.SuperLayerScreenName, duration);
    }
  }
}
