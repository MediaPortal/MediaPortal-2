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

using System.Diagnostics;
using System.Threading.Tasks;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.ImpersonationService;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Utilities.Process;

namespace MediaPortal.Extensions.MetadataExtractors.FFMpegLib
{
  public static class FFMpegBinary
  {
    #region Constants

    /// <summary>
    /// Executable name of FFMpeg.
    /// </summary>
    private const string FFMPEG_EXECUTABLE = "ffmpeg.exe";

    /// <summary>
    /// Executable name of FFProbe.
    /// </summary>
    private const string FFPROBE_EXECUTABLE = "ffprobe.exe";

    #endregion

    #region Variables

    private static readonly string _ffMpegBinPath;
    private static readonly string _ffProbeBinPath;

    #endregion

    /// <summary>
    /// <see cref="FFMpegLib"/> provides access to the "ffmpeg.exe" and "ffprobe.exe" programs for processing video and its metadata.
    /// </summary>
    static FFMpegBinary()
    {
      _ffMpegBinPath = FileUtils.BuildAssemblyRelativePath(FFMPEG_EXECUTABLE);
      _ffProbeBinPath = FileUtils.BuildAssemblyRelativePath(FFPROBE_EXECUTABLE);
    }

    /// <summary>
    /// Returns the absolute path to FFMpeg binary.
    /// </summary>
    public static string FFMpegPath
    {
      get
      {
        return _ffMpegBinPath;
      }
    }

    /// <summary>
    /// Returns the absolute path to FFProbe binary.
    /// </summary>
    public static string FFProbePath
    {
      get
      {
        return _ffProbeBinPath;
      }
    }

    #region Async methods

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
    public static Task<ProcessExecutionResult> FFMpegExecuteWithResourceAccessAsync(ILocalFsResourceAccessor lfsra, string arguments, ProcessPriorityClass priorityClass, int maxWaitMs)
    {
      return lfsra.ExecuteWithResourceAccessAsync(_ffMpegBinPath, arguments, priorityClass, maxWaitMs);
    }

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
    public static Task<ProcessExecutionResult> FFProbeExecuteWithResourceAccessAsync(ILocalFsResourceAccessor lfsra, string arguments, ProcessPriorityClass priorityClass, int maxWaitMs)
    {
      return lfsra.ExecuteWithResourceAccessAsync(_ffProbeBinPath, arguments, priorityClass, maxWaitMs);
    }

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
    public static Task<ProcessExecutionResult> FFMpegExecuteAsync(string arguments, ProcessPriorityClass priorityClass, int maxWaitMs)
    {
      return ProcessUtils.ExecuteAsync(_ffMpegBinPath, arguments, priorityClass, maxWaitMs);
    }

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
    public static Task<ProcessExecutionResult> FFProbeExecuteAsync(string arguments, ProcessPriorityClass priorityClass, int maxWaitMs)
    {
      return ProcessUtils.ExecuteAsync(_ffProbeBinPath, arguments, priorityClass, maxWaitMs);
    }

    #endregion
  }
}
