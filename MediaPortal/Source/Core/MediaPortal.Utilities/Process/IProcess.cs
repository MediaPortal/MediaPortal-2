#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Utilities.Process
{
  /// <summary>
  /// Interface for the standard methods and properties of an external process.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is a thin wrapper for the standard methods and properties of the <see cref="System.Diagnostics.Process"/> class.
  /// It is needed to abstract away non-standard implementations, for example the ImpersonationService may return a class
  /// that derives from <see cref="System.Diagnostics.Process"/> but that reimplements and hides some of the base members
  /// and therefore may not work correctly if those members are called directly on the base implementation, e.g. by casting
  /// the derived type to the base type. By implementing this interface the derived type can ensure that it's new members
  /// are called whilst still hiding implementation details behind this interface and allowing methods and plugins to work with
  /// any implementation of this interface in a unified way.
  /// </para>
  /// <para>
  /// This interface currently only implements the <see cref="System.Diagnostics.Process"/> members needed for existing services
  /// and plugins, if access to further members are needed they should be added to this interface as required.
  /// </para>
  /// </remarks>
  public interface IProcess : IDisposable
  {
    /// <summary>
    /// Gets or sets whether the Exited event should be raised when the process terminates.
    /// </summary>
    bool EnableRaisingEvents { get; set; }

    /// <summary>
    /// Gets the value that the associated process specified when it terminated.
    /// </summary>
    int ExitCode { get; }

    /// <summary>
    /// Gets a value indicating whether the associated process has been terminated.
    /// </summary>
    bool HasExited { get; }

    /// <summary>
    /// Gets or sets the overall priority category for the associated process.
    /// </summary>
    ProcessPriorityClass PriorityClass { get; set; }

    /// <summary>
    /// Gets a stream used to read the error output of the application.
    /// </summary>
    StreamReader StandardError { get; }

    /// <summary>
    /// Gets a stream used to write the input of the application.
    /// </summary>
    StreamWriter StandardInput { get; }

    /// <summary>
    /// Gets a stream used to read the textual output of the application.
    /// </summary>
    StreamReader StandardOutput { get; }

    /// <summary>
    /// Occurs when an application writes to its redirected StandardError stream.
    /// </summary>
    event DataReceivedEventHandler ErrorDataReceived;

    /// <summary>
    /// Occurs when a process exits.
    /// </summary>
    event EventHandler Exited;

    /// <summary>
    /// Occurs each time an application writes a line to its redirected StandardOutput stream.
    /// </summary>
    event DataReceivedEventHandler OutputDataReceived;

    /// <summary>
    /// Begins asynchronous read operations on the redirected StandardError stream of the application.
    /// </summary>
    void BeginErrorReadLine();

    /// <summary>
    /// Begins asynchronous read operations on the redirected StandardOutput stream of the application.
    /// </summary>
    void BeginOutputReadLine();

    /// <summary>
    /// Cancels the asynchronous read operation on the redirected StandardError stream of an application.
    /// </summary>
    void CancelErrorRead();

    /// <summary>
    /// Cancels the asynchronous read operation on the redirected StandardOutput stream of an application.
    /// </summary>
    void CancelOutputRead();

    /// <summary>
    /// Frees all the resources that are associated with this component.
    /// </summary>
    void Close();

    /// <summary>
    /// Forces termination of the underlying process.
    /// </summary>
    void Kill();

    /// <summary>
    /// Starts a process resource and associates it with a Process component.
    /// </summary>
    /// <returns><c>true</c> if a process resource is started; <c>false</c> if no new process resource is started (for example, if an existing process is reused).</returns>
    bool Start();

    /// <summary>
    /// Instructs the Process component to wait indefinitely for the associated process to exit.
    /// </summary>
    void WaitForExit();

    /// <summary>
    /// Instructs the Process component to wait the specified number of milliseconds for the associated process to exit.
    /// </summary>
    /// <param name="milliseconds">The amount of time, in milliseconds, to wait for the associated process to exit. A value of 0 specifies an immediate return, and a value of -1 specifies an infinite wait.</param>
    /// <returns><c>true</c> if the associated process has exited; otherwise, <c>false</c>.</returns>
    bool WaitForExit(int milliseconds);

    /// <summary>
    /// Instructs the process component to wait for the associated process to exit, or for the cancellationToken to be cancelled.
    /// </summary>
    /// <param name="cancellationToken">An optional token to cancel the asynchronous operation.</param>
    /// <returns>A task that will complete when the process has exited, cancellation has been requested, or an error occurs.</returns>
    Task WaitForExitAsync(CancellationToken cancellationToken = default);
  }
}
