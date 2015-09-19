#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using System.Diagnostics;
using System.Threading.Tasks;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.Process;

namespace MediaPortal.Extensions.MetadataExtractors.FFMpegLib
{
  /// <summary>
  /// <see cref="IFFMpegLib"/> provides access to the "ffmpeg.exe" and "ffprobe.exe" programs for processing video and its metadata.
  /// </summary>
  public interface IFFMpegLib
  {
    /// <summary>
    /// Executes FFMpeg and ensures that it has access to the respective resource
    /// </summary>
    /// <param name="lfsra"><see cref="ILocalFsResourceAccessor"/> to which FFMpeg needs access to</param>
    /// <param name="arguments">Arguments for FFMpeg</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns>A <see cref="Task"/> representing the result of executing FFMpeg</returns>
    /// <remarks>
    /// This is a convenience method that enables executing FFMpeg directly on the <see cref="ILocalFsResourceAccessor"/>
    /// interface to which FFMpeg needs access. The purpose of an <see cref="ILocalFsResourceAccessor"/> is providing
    /// access to a resource - not executing programs which is why this method is implemented as an extension method instead of
    /// a method directly on the interface.
    /// </remarks>
    Task<ProcessExecutionResult> FFMpegExecuteWithResourceAccessAsync(ILocalFsResourceAccessor lfsra, string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = ProcessUtils.DEFAULT_TIMEOUT);

    /// <summary>
    /// Executes FFProbe and ensures that it has access to the respective resource
    /// </summary>
    /// <param name="lfsra"><see cref="ILocalFsResourceAccessor"/> to which FFProbe needs access to</param>
    /// <param name="arguments">Arguments for FFProbe</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns>A <see cref="Task"/> representing the result of executing FFProbe</returns>
    /// <remarks>
    /// This is a convenience method that enables executing FFProbe directly on the <see cref="ILocalFsResourceAccessor"/>
    /// interface to which FFProbe needs access. The purpose of an <see cref="ILocalFsResourceAccessor"/> is providing
    /// access to a resource - not executing programs which is why this method is implemented as an extension method instead of
    /// a method directly on the interface.
    /// </remarks>
    Task<ProcessExecutionResult> FFProbeExecuteWithResourceAccessAsync(ILocalFsResourceAccessor lfsra, string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = ProcessUtils.DEFAULT_TIMEOUT);

    /// <summary>
    /// Executes FFMpeg. This function doesn't check if impersionation is necessary
    /// </summary>
    /// <param name="arguments">Arguments for FFMpeg</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns>A <see cref="Task"/> representing the result of executing FFMpeg</returns>
    /// <remarks>
    /// This is a convenience method that enables executing FFMpeg directly.
    /// </remarks>
    Task<ProcessExecutionResult> FFMpegExecuteAsync(string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = ProcessUtils.DEFAULT_TIMEOUT);

    /// <summary>
    /// Executes FFProbe. This function doesn't check if impersionation is necessary
    /// </summary>
    /// <param name="arguments">Arguments for FFProbe</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns>A <see cref="Task"/> representing the result of executing FFProbe</returns>
    /// <remarks>
    /// This is a convenience method that enables executing FFProbe directly.
    /// </remarks>
    Task<ProcessExecutionResult> FFProbeExecuteAsync(string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = ProcessUtils.DEFAULT_TIMEOUT);

    /// <summary>
    /// Returns the absolute path to FFMpeg binary.
    /// </summary>
    string FFMpegBinaryPath
    {
      get;
    }

    /// <summary>
    /// Returns the absolute path to FFProbe binary.
    /// </summary>
    string FFProbeBinaryPath
    {
      get;
    }
  }
}
