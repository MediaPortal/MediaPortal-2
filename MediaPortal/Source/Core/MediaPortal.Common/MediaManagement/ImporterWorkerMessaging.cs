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
      /// <summary>
      /// This message is sent when the importer worker was told to schedule an import.
      /// This message has two parameters <see cref="RESOURCE_PATH"/> and <see cref="IMPORT_JOB_TYPE"/>.
      /// </summary>
      ImportScheduled,

      /// <summary>
      /// This message is sent when the importer worker has canceled an import.
      /// This also happens during shutdown after the pending ImportJobs have been persisted to disk.
      /// This message has one parameter <see cref="RESOURCE_PATH"/>.
      /// </summary>
      ImportScheduleCanceled,

      /// <summary>
      /// This message is sent when the importer worker started the import of the given path.
      /// When the importer worker is reactivated after suspension, this message is resent.
      /// This message has one parameter <see cref="RESOURCE_PATH"/>.
      /// </summary>
      ImportStarted,

      /// <summary>
      /// This message is sent during an import when the importer worker processes the given resource path.
      /// This message has one parameter <see cref="RESOURCE_PATH"/>.
      /// ToDo: Remove this message once the old ImporterWorker is removed.
      /// </summary>
      ImportStatus,

      /// <summary>
      /// This message is sent when the importer worker finishes the import of the given path (successfully or erroneous).
      /// This message has one parameter <see cref="RESOURCE_PATH"/>.
      /// </summary>
      ImportCompleted,

      /// <summary>
      /// This message is sent if the importer's scheduler executes a full refresh of all local shares.
      /// </summary>
      RefreshLocalShares,

      /// <summary>
      /// This message is sent to report the progress of all currently running imports.
      /// This message has one parameter <see cref="IMPORT_PROGRESS"/>
      /// </summary>
      /// <remarks>
      /// The parameter consists of a Dictionary that maps the <see cref="ImportJobInformation"/>s of all currently
      /// running ImportJobs to a Tuple of two integers. The first integer is the number of resources that have been
      /// identiied so far in the given ImportJob, the second integer is the numnber of resources that have been completed.
      /// The message is sent
      ///  - when a new ImportJob has started (the progress parameter including this ImportJob with a Tuple(0, 0)
      ///  - when an ImportJob has finished (the progress parameter not including the respective ImportJob anymore)
      ///  - irrespective of the above after every 100 import resources that have been completed
      /// </remarks>
      ImportProgress,
    }

    // Message data
    public const string RESOURCE_PATH = "ResourcePath"; // Type: ResourcePath
    public const string IMPORT_JOB_TYPE = "ImportJobType"; // Type: ImportJobType
    public const string IMPORT_PROGRESS = "ImportProgress"; // Type: Dictionary<ImportJobInformation, Tuple<int, int>>

    internal static void SendImportMessage(MessageType messageType)
    {
      SystemMessage msg = new SystemMessage(messageType);
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    internal static void SendImportMessage(MessageType messageType, ResourcePath path)
    {
      SystemMessage msg = new SystemMessage(messageType);
      msg.MessageData[RESOURCE_PATH] = path;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    internal static void SendImportMessage(MessageType messageType, ResourcePath path, ImportJobType importJobType)
    {
      SystemMessage msg = new SystemMessage(messageType);
      msg.MessageData[RESOURCE_PATH] = path;
      msg.MessageData[IMPORT_JOB_TYPE] = importJobType;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    internal static void SendImportMessage(MessageType messageType, Dictionary<ImportJobInformation, Tuple<int, int>> progress)
    {
      SystemMessage msg = new SystemMessage(messageType);
      msg.MessageData[IMPORT_PROGRESS] = progress;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}