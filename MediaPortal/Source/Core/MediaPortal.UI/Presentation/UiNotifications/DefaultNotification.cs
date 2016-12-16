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

namespace MediaPortal.UI.Presentation.UiNotifications
{
  public class DefaultNotification : INotification
  {
    #region Consts

    public static TimeSpan DEFAULT_NOTIFICATIONS_TIMEOUT = TimeSpan.FromMinutes(10);

    #endregion

    protected NotificationType _type;
    protected string _title;
    protected string _text;
    protected Guid? _handlerWorkflowState = null;
    protected string _customIconPath = null;
    protected DateTime? _timeout = null;

    public DefaultNotification(NotificationType type, string title, string text)
    {
      _type = type;
      _title = title;
      _text = text;
      _timeout = DateTime.Now + DEFAULT_NOTIFICATIONS_TIMEOUT;
    }

    public DefaultNotification(NotificationType type, string title, string text, Guid? handlerWorkflowState) : this(type, title, text)
    {
      _handlerWorkflowState = handlerWorkflowState;
    }

    public NotificationType Type
    {
      get { return _type; }
    }

    public string Title
    {
      get { return _title; }
    }

    public string Text
    {
      get { return _text; }
    }

    public DateTime? Timeout
    {
      get { return _timeout; }
      set { _timeout = value; }
    }

    public Guid? HandlerWorkflowState
    {
      get { return _handlerWorkflowState; }
    }

    public string CustomIconPath
    {
      get { return _customIconPath; }
      set { _customIconPath = value; }
    }

    public void Enqueued() { }

    public void Dequeued() { }

    public override string ToString()
    {
      return _title;
    }
  }
}