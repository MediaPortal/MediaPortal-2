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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Common.Logging;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Common.Async
{
  public static class AsyncExtensions
  {
    /// <summary>
    /// Adds a try/catch block around the given <paramref name="task"/> and logs possible exceptions as warnings.
    /// </summary>
    /// <param name="task">Task to execute</param>
    public static async Task Try(this Task task)
    {
      try
      {
        await task;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Exception in async taks: ", e);
      }
    }
    /// <summary>
    /// Adds a try/catch block around the given <paramref name="task"/> and logs possible exceptions as warnings.
    /// </summary>
    /// <param name="task">Task to execute</param>
    public static async Task<T> Try<T>(this Task<T> task)
    {
      try
      {
        return await task;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Exception in async taks: ", e);
        return default(T);
      }
    }

    /// <summary>
    /// Adds a try/catch block around the given <paramref name="task"/>, logs possible exceptions as warnings and synchronously waits for completion.
    /// </summary>
    /// <param name="task">Task to execute</param>
    public static void TryWait(this Task task)
    {
      task.Try().Wait();
    }

    /// <summary>
    /// Adds a try/catch block around the given <paramref name="task"/>, logs possible exceptions as warnings and synchronously waits for completion.
    /// </summary>
    /// <param name="task">Task to execute</param>
    public static T TryWait<T>(this Task<T> task)
    {
      return task.Try().Result;
    }

    // Code taken from https://github.com/StephenCleary/AsyncEx/blob/master/src/Nito.AsyncEx.Tasks/CancellationTokenTaskSource.cs
    /// <summary>
    /// Asynchronously waits for the task to complete, or for the cancellation token to be canceled.
    /// </summary>
    /// <param name="this">The task to wait for. May not be <c>null</c>.</param>
    /// <param name="cancellationToken">The cancellation token that cancels the wait.</param>
    public static Task WaitAsync(this Task @this, CancellationToken cancellationToken)
    {
      if (@this == null)
        throw new ArgumentNullException(nameof(@this));

      if (!cancellationToken.CanBeCanceled)
        return @this;
      if (cancellationToken.IsCancellationRequested)
        return Task.FromCanceled(cancellationToken);
      return DoWaitAsync(@this, cancellationToken);
    }

    private static async Task DoWaitAsync(Task task, CancellationToken cancellationToken)
    {
      using (var cancelTaskSource = new CancellationTokenTaskSource<object>(cancellationToken))
        await await Task.WhenAny(task, cancelTaskSource.Task).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously waits for the task to complete, or for the cancellation token to be canceled.
    /// </summary>
    /// <typeparam name="TResult">The type of the task result.</typeparam>
    /// <param name="this">The task to wait for. May not be <c>null</c>.</param>
    /// <param name="cancellationToken">The cancellation token that cancels the wait.</param>
    public static Task<TResult> WaitAsync<TResult>(this Task<TResult> @this, CancellationToken cancellationToken)
    {
      if (@this == null)
        throw new ArgumentNullException(nameof(@this));

      if (!cancellationToken.CanBeCanceled)
        return @this;
      if (cancellationToken.IsCancellationRequested)
        return Task.FromCanceled<TResult>(cancellationToken);
      return DoWaitAsync(@this, cancellationToken);
    }

    private static async Task<TResult> DoWaitAsync<TResult>(Task<TResult> task, CancellationToken cancellationToken)
    {
      using (var cancelTaskSource = new CancellationTokenTaskSource<TResult>(cancellationToken))
        return await await Task.WhenAny(task, cancelTaskSource.Task).ConfigureAwait(false);
    }

    public static async Task<IList<object>> InvokeAsyncTask(this CpAction action, IList<object> inParameters)
    {
      return await Task.Factory.FromAsync((callback, stateObject) => action.BeginInvokeAction(inParameters, callback, stateObject), action.EndInvokeAction, null).ConfigureAwait(false);
    }

    public static ConfiguredTaskAwaitable<IList<object>> InvokeAsync(this CpAction action, IList<object> inParameters)
    {
      return Task.Factory.FromAsync((callback, stateObject) => action.BeginInvokeAction(inParameters, callback, stateObject), action.EndInvokeAction, null).ConfigureAwait(false);
    }
  }

  // Code taken from https://github.com/StephenCleary/AsyncEx/blob/master/src/Nito.AsyncEx.Tasks/CancellationTokenTaskSource.cs
  /// <summary>
  /// Holds the task for a cancellation token, as well as the token registration. The registration is disposed when this instance is disposed.
  /// </summary>
  public sealed class CancellationTokenTaskSource<T> : IDisposable
  {
    /// <summary>
    /// The cancellation token registration, if any. This is <c>null</c> if the registration was not necessary.
    /// </summary>
    private readonly IDisposable _registration;

    /// <summary>
    /// Creates a task for the specified cancellation token, registering with the token if necessary.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    public CancellationTokenTaskSource(CancellationToken cancellationToken)
    {
      if (cancellationToken.IsCancellationRequested)
      {
        Task = System.Threading.Tasks.Task.FromCanceled<T>(cancellationToken);
        return;
      }
      var tcs = new TaskCompletionSource<T>();
      _registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false);
      Task = tcs.Task;
    }

    /// <summary>
    /// Gets the task for the source cancellation token.
    /// </summary>
    public Task<T> Task { get; private set; }

    /// <summary>
    /// Disposes the cancellation token registration, if any. Note that this may cause <see cref="Task"/> to never complete.
    /// </summary>
    public void Dispose()
    {
      _registration?.Dispose();
    }
  }
}
