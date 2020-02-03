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
using System.IO;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding
{
  public class TranscodeContext : StreamContext
  {
    protected Stream _transcodedStream;
    protected long _lastSize = 0;
    protected long _lastFrame = 0;
    protected long _lastFPS = 0;
    protected long _lastBitrate = 0;
    protected TimeSpan _lastTime = TimeSpan.FromTicks(0);
    protected object _lastSync = new object();
    protected bool _streamInUse = false;
    protected TaskCompletionSource<bool> _completeTask = null;

    private object _syncLock = new object();

    public string TargetFile { get; set; }
    public ICollection<string> TargetSubtitles { get; set; } = new List<string>();
    public string SegmentDir { get; set; }
    public string HlsBaseUrl { get; set; }
    public bool Aborted { get; set; }
    public bool Failed { get; private set; }
    public bool Partial { get; set; }
    public bool Segmented => !string.IsNullOrEmpty(SegmentDir);
    public bool Live { get; set; }
    public bool InUse { get; }

    public long LastSegment { get; set; }
    public long CurrentSegment { get; set; }
    public TimeSpan TargetDuration { get; set; }
    public TimeSpan CurrentDuration 
    { 
      get
      {
        if (Running)
        {
          if (_lastSize > 0 && Partial == false)
          {
            return _lastTime;
          }
          return TimeSpan.FromTicks(0);
        }
        else
        {
          return TargetDuration;
        }
      }
    }
    public long CurrentFrames
    {
      get
      {
        if (Running)
        {
          return _lastFrame;
        }
        else
        {
          return 0;
        }
      }
    }
    public long CurrentFPS
    {
      get
      {
        if (Running)
        {
          return _lastFPS;
        }
        else
        {
          return 0;
        }
      }
    }
    public long CurrentBitrate
    {
      get
      {
        if (Running)
        {
          return _lastBitrate;
        }
        else
        {
          return 0;
        }
      }
    }
    public long CurrentThroughput
    {
      get
      {
        if (Running)
        {
          return _lastSize;
        }
        else
        {
          return 0;
        }
      }
    }
    public long TargetFileSize 
    { 
      get
      {
        if (Live) return 0;

        if (Running)
        {
          if (_lastSize > 0 && Partial == false)
          {
            lock (_lastSync)
            {
              double secondSize = Convert.ToDouble(_lastSize) / _lastTime.TotalSeconds;
              return Convert.ToInt64(secondSize * TargetDuration.TotalSeconds);
            }
          }
          return 0;
        }
        else
        {
          if (_transcodedStream != null)
          {
            return _transcodedStream.Length;
          }
        }
        return 0;
      }
    }
    public long CurrentFileSize
    {
      get
      {
        if (Live) return 0;

        if (Segmented)
        {
          if (_lastSize > 0)
          {
            lock (_lastSync)
            {
              double secondSize = Convert.ToDouble(_lastSize) / _lastTime.TotalSeconds;
              return Convert.ToInt64(secondSize * TargetDuration.TotalSeconds);
            }
          }
          else
          {
            long totalSize = 0;
            string[] segmentFiles = Directory.GetFiles(SegmentDir, "*.ts");
            foreach (string file in segmentFiles)
            {
              totalSize += new FileInfo(file).Length;
            }
            return totalSize;
          }
        }
        else if (_transcodedStream != null && _transcodedStream.CanSeek)
        {
          return _transcodedStream.Length;
        }
        else
        {
          return _lastSize;
        }
      }
    }

    public bool Running { get; private set; }

    /// <summary>
    /// Returns a stream to the transcoded file or playlist file in case of HLS.
    /// Using HLS:
    /// FFMPeg creates a tmp file and replaces the playlist file for each new segment,
    /// because of this one has to close the stream after reading the playlist file.
    /// Here we try to recreate the stream for convenience.
    /// </summary>
    public override Stream Stream
    {
      get
      {
        if (_streamInUse && _transcodedStream is FileStream stream)
        {
          _transcodedStream.Dispose();
          if (!stream.CanRead)
            _transcodedStream = new FileStream(stream.Name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        return _transcodedStream;
      }
      set
      {
        _transcodedStream = value;
      }
    }

    public Task WaitForCompleteAsync() { return _completeTask?.Task ?? Task.CompletedTask; }

    public void WaitForComplete() { (_completeTask?.Task ?? Task.CompletedTask).Wait(); }

    public void AssignStream(Stream stream)
    {
      Stream?.Dispose();
      Stream = stream;
    }

    public virtual void UpdateStreamUse(bool inUse)
    {
      if (_streamInUse == true && inUse == false)
      {
        Stop();
      }
      _streamInUse = inUse;
    }

    public void Start()
    {
      lock (_syncLock)
      {
        if (!Running)
        {
          Running = true;
          Aborted = false;
          Failed = false;
          if (_completeTask?.Task.IsCompleted ?? true)
            _completeTask = new TaskCompletionSource<bool>();
        }
      }
    }

    public void Abort()
    {
      if (Running)
      {
        Aborted = true;
        Stop();
      }
    }

    public void Fail()
    {
      if (Running)
      {
        Failed = true;
        Stop();
      }
    }

    public void Stop()
    {
      lock (_syncLock)
      {
        if (Running)
        {
          base.Dispose();
          Running = false;
          _completeTask?.TrySetResult(true);
        }
      }
    }

    public override void Dispose()
    {
      Abort();
      _completeTask?.TrySetResult(true);
      UpdateStreamUse(false);
      Stream?.Dispose();
      base.Dispose();
    }
  }
}
