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

namespace MediaPortal.Common.TaskScheduler
{
  /// <summary>
  /// Public representation of the task scheduler service.
  /// The task scheduler simplifies generating events at a particular time schedule. It sends out a message
  /// as soon as a task is due on the message queue of name <see cref="TaskSchedulerMessaging.CHANNEL"/>. It sends out a
  /// message of message type <see cref="TaskSchedulerMessaging.MessageType.DUE"/> with the particular <see cref="Task"/>
  /// as a reference to the interested listeners on this message queue.
  /// Listeners then can act on the generated message to perform time or interval based tasks.
  /// </summary>
  /// <remarks>
  /// This is a high-level interface compared to the <see cref="Threading.IThreadPool"/> service, which provides the low-level threads.
  /// Use this service to manage <see cref="Task">Task instances</see> which are persistent, have a lifetime and other scheduling options.
  /// </remarks>
  public interface ITaskScheduler
  {
    /// <summary>
    /// Starts the task scheduler. This will install the worker at the thread pool and call all startup tasks.
    /// </summary>
    void Startup();

    /// <summary>
    /// Shuts the task scheduler down.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Adds a task to the task scheduler.
    /// </summary>
    /// <param name="newTask">Task to add to the scheduler.</param>
    /// <returns>ID assigned to the given task.</returns>
    Guid AddTask(Task newTask);

    /// <summary>
    /// Updates an already registered task.
    /// </summary>
    /// <param name="taskId">ID of the task to update.</param>
    /// <param name="updatedTask">The updated task.</param>
    void UpdateTask(Guid taskId, Task updatedTask);

    /// <summary>
    /// Removes a task from the task scheduler.
    /// </summary>
    /// <param name="taskId">ID of the task to remove.</param>
    void RemoveTask(Guid taskId);

    /// <summary>
    /// Gets a registered task from the task scheduler.
    /// </summary>
    /// <param name="taskId">ID of the task to get.</param>
    /// <returns>Task with given ID.</returns>
    Task GetTask(Guid taskId);

    /// <summary>
    /// Gets all registered tasks for the given owner from the task scheduler.
    /// </summary>
    /// <param name="ownerId">owner ID to get a task list for.</param>
    /// <returns>List of tasks for given owner.</returns>
    ICollection<Task> GetTasks(string ownerId);
  }
}
