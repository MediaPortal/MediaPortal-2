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
using MediaPortal.Common.Services.ResourceAccess.ImpersonationService;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Utilities.Process;

namespace MediaPortal.Extensions.MetadataExtractors.FFMpegLib
{
  public class FFMpegLib : IFFMpegLib
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

    private readonly string _ffMpegBinPath;
    private readonly string _ffProbeBinPath;

    #endregion

    public FFMpegLib()
    {
      _ffMpegBinPath = FileUtils.BuildAssemblyRelativePath(FFMPEG_EXECUTABLE);
      _ffProbeBinPath = FileUtils.BuildAssemblyRelativePath(FFPROBE_EXECUTABLE);
    }

    #region IFFMpegLib implementation

    Task<ProcessExecutionResult> IFFMpegLib.FFMpegExecuteWithResourceAccessAsync(ILocalFsResourceAccessor lfsra, string arguments, ProcessPriorityClass priorityClass, int maxWaitMs)
    {
      return lfsra.ExecuteWithResourceAccessAsync(_ffMpegBinPath, arguments, priorityClass, maxWaitMs);
    }

    Task<ProcessExecutionResult> IFFMpegLib.FFProbeExecuteWithResourceAccessAsync(ILocalFsResourceAccessor lfsra, string arguments, ProcessPriorityClass priorityClass, int maxWaitMs)
    {
      return lfsra.ExecuteWithResourceAccessAsync(_ffProbeBinPath, arguments, priorityClass, maxWaitMs);
    }

    Task<ProcessExecutionResult> IFFMpegLib.FFMpegExecuteAsync(string arguments, ProcessPriorityClass priorityClass, int maxWaitMs)
    {
      return ProcessUtils.ExecuteAsync(_ffMpegBinPath, arguments, priorityClass, maxWaitMs);
    }

    Task<ProcessExecutionResult> IFFMpegLib.FFProbeExecuteAsync(string arguments, ProcessPriorityClass priorityClass, int maxWaitMs)
    {
      return ProcessUtils.ExecuteAsync(_ffProbeBinPath, arguments, priorityClass, maxWaitMs);
    }

    public string FFMpegBinaryPath
    {
      get
      {
        return _ffMpegBinPath;
      }
    }

    public string FFProbeBinaryPath
    {
      get
      {
        return _ffProbeBinPath;
      }
    }

    #endregion

  }
}
