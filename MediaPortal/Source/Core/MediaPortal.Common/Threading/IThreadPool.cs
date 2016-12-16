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
using System.Threading;

namespace MediaPortal.Common.Threading
{
  #region Enums

  public enum QueuePriority
  {
    Low,
    Normal,
    High
  }

  #endregion

  /// <summary>
  /// Container and management service for asynchronous execution threads and tasks.
  /// </summary>
  /// <remarks>
  /// Use this service to create simple (low-level) work tasks. Use the <see cref="TaskScheduler.ITaskScheduler"/> service to manage higher-level
  /// persistent tasks.
  /// </remarks>
  public interface IThreadPool
  {
    #region Methods to add work

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// </summary>
    /// <param name="work">Work handler which contains the work to be performed.</param>
    /// <returns><see cref="IWork"/> object or <c>null</c>, if the work could not be added.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="work"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">If the work's state is not <c>INIT</c>.</exception>
    IWork Add(DoWorkHandler work);
    
    /// <summary>
    /// Add work to be performed by the threadpool.
    /// </summary>
    /// <param name="work">Work handler which contains the work to be performed.</param>
    /// <param name="queuePriority">Queue priority for the work.</param>
    /// <returns><see cref="IWork"/> object or <c>null</c>, if the work could not be added.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="work"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">If the work's state is not <c>INIT</c>.</exception>
    IWork Add(DoWorkHandler work, QueuePriority queuePriority);

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// </summary>
    /// <param name="work">Work handler which contains the work to be performed.</param>
    /// <param name="description">Description for the work.</param>
    /// <returns><see cref="IWork"/> object or <c>null</c>, if the work could not be added.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="work"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">If the work's state is not <c>INIT</c>.</exception>
    IWork Add(DoWorkHandler work, string description);

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// </summary>
    /// <param name="work">Work handler which contains the work to be performed.</param>
    /// <param name="threadPriority">Thread priority for the work.</param>
    /// <returns><see cref="IWork"/> object or <c>null</c>, if the work could not be added.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="work"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">If the work's state is not <c>INIT</c>.</exception>
    IWork Add(DoWorkHandler work, ThreadPriority threadPriority);
    
    /// <summary>
    /// Add work to be performed by the threadpool.
    /// </summary>
    /// <param name="work">Work handler which contains the work to be performed.</param>
    /// <param name="workCompletedHandler">Event handler to be called on completion.</param>
    /// <returns><see cref="IWork"/> object or <c>null</c>, if the work could not be added.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="work"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">If the work's state is not <c>INIT</c>.</exception>
    IWork Add(DoWorkHandler work, WorkEventHandler workCompletedHandler);
    
    /// <summary>
    /// Add work to be performed by the threadpool.
    /// </summary>
    /// <param name="work">Work handler which contains the work to be performed.</param>
    /// <param name="description">Description for the work.</param>
    /// <param name="queuePriority">Queue priority for the work.</param>
    /// <returns><see cref="IWork"/> object or <c>null</c>, if the work could not be added.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="work"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">If the work's state is not <c>INIT</c>.</exception>
    IWork Add(DoWorkHandler work, string description, QueuePriority queuePriority);
    
    /// <summary>
    /// Add work to be performed by the threadpool.
    /// </summary>
    /// <param name="work">Work handler which contains the work to be performed.</param>
    /// <param name="description">Description for the work.</param>
    /// <param name="queuePriority">Queue priority for the work.</param>
    /// <param name="threadPriority">Thread priority for the work.</param>
    /// <returns><see cref="IWork"/> object or <c>null</c>, if the work could not be added.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="work"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">If the work's state is not <c>INIT</c>.</exception>
    IWork Add(DoWorkHandler work, string description, QueuePriority queuePriority, ThreadPriority threadPriority);

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// </summary>
    /// <param name="work">Work handler which contains the work to be performed.</param>
    /// <param name="description">Description for the work.</param>
    /// <param name="queuePriority">Queue priority for the work</param>
    /// <param name="threadPriority">System.Threading.ThreadPriority for the work</param>
    /// <param name="workCompletedHandler">WorkEventHandler to be called on completion.</param>
    /// <returns><see cref="IWork"/> object or <c>null</c>, if the work could not be added.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="work"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">If the work's state is not <c>INIT</c>.</exception>
    IWork Add(DoWorkHandler work, string description, QueuePriority queuePriority, ThreadPriority threadPriority, WorkEventHandler workCompletedHandler);

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// </summary>
    /// <param name="work">Work to be performed by the threadpool.</param>
    /// <returns><c>true</c>, if the work could successfully be added, else <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="work"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">If the work's state is not <c>INIT</c>.</exception>
    bool Add(IWork work);

    /// <summary>
    /// Add work to be performed by the threadpool.
    /// </summary>
    /// <returns><c>true</c>, if the work could successfully be added, else <c>false</c>.</returns>
    /// <param name="work">Add work to be performed by the threadpool</param>
    /// <param name="queuePriority">Queue priority for the work.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="work"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">If the work's state is not <c>INIT</c>.</exception>
    bool Add(IWork work, QueuePriority queuePriority);

    #endregion

    #region Methods to manage interval-based work

    void AddIntervalWork(IIntervalWork intervalWork, bool runNow);
    void RemoveIntervalWork(IIntervalWork intervalWork);

    #endregion

    #region Methods to control the threadpool

    /// <summary>
    /// Shuts down the ThreadPool. Active threads will eventually stop; idle threads
    /// will be shutdown and queue will not accept new work anymore.
    /// </summary>
    void Stop();
    void Shutdown();

    #endregion

    #region Threadpool status properties

    int ThreadCount { get; }
    int BusyThreadCount { get; }
    long WorkItemsProcessed { get; }
    int QueueLength { get; }
    int MinimumThreads { get; }
    int MaximumThreads { get; }

    #endregion
  }
}
