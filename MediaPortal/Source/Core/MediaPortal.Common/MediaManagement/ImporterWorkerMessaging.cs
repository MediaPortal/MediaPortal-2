#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Messaging;

namespace MediaPortal.Common.MediaManagement
{
  public class ImporterWorkerMessaging
  {
    // Message channel name
    public const string CHANNEL = "ImporterWorker";

    /// <summary>
    /// Messages of this type are sent by the <see cref="MediaPortal.Common.Services.MediaManagement.ImporterWorker"/>.
    /// </summary>
    public enum MessageType
    {
      ImportStarted,
      ImportStatus,
      ImportCompleted,
    }

    // Message data
    public const string RESOURCE_PATH = "ResourcePath"; // Type: ResourcePath

    internal static void SendImportMessage(MessageType messageType, ResourcePath path)
    {
      SystemMessage msg = new SystemMessage(messageType);
      msg.MessageData[RESOURCE_PATH] = path;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}