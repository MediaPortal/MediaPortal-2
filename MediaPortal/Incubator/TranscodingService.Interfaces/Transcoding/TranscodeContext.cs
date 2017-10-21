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

using System;
using System.IO;
using System.Threading;

namespace MediaPortal.Plugins.Transcoding.Interfaces.Transcoding
{
  public class TranscodeContext : IDisposable
  {
    protected Stream _transcodedStream;
    protected long _lastSize = 0;
    protected long _lastFrame = 0;
    protected long _lastFPS = 0;
    protected long _lastBitrate = 0;
    protected TimeSpan _lastTime = TimeSpan.FromTicks(0);
    protected object _lastSync = new object();
    protected bool _streamInUse = false;
    protected long _currentSegment = 0;
    protected ManualResetEvent _completeEvent = new ManualResetEvent(true);

    public ManualResetEvent CompleteEvent 
    {
      get { return _completeEvent; }
    }

    public string TargetFile { get; set; }
    public string TargetSubtitle { get; set; }
    public string SegmentDir { get; set; }
    public string HlsBaseUrl { get; set; }
    public bool Aborted { get; set; }
    public bool Failed { get; set; }
    public bool Partial { get; set; }
    public bool Segmented 
    { 
      get
      {
        return string.IsNullOrEmpty(SegmentDir) == false;
      }
    }
    public bool Live { get; set; }
    public bool InUse 
    {
      get { return _streamInUse; }
      set
      {
        if (_streamInUse == true && value == false)
        {
          Stop();
        }
        _streamInUse = value;
      }
    }
    public long LastSegment { get; set; }
    public long CurrentSegment
    {
      set
      {
        _currentSegment = value;
      }
      get
      {
        return _currentSegment;
      }
    }
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
    /// Returns a Stream to the transcoded file or also to a playlist file in case of HLS.
    /// Using HLS:
    /// FFMPeg creates a tmp file and replaces the playlist file for each new segment,
    /// because of this one has to close the Stream after reading the playlist file.
    /// Here we try to recreate the Stream for convenience.
    /// </summary>
    public Stream TranscodedStream
    {
      get
      {
        var stream = _transcodedStream as FileStream;
        if (stream != null)
        {
          if (!stream.CanRead)
            _transcodedStream = new FileStream(stream.Name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        return _transcodedStream;
      }
      private set { _transcodedStream = value; }
    }

    public void Start()
    {
      Running = true;
      Aborted = false;
    }

    public void AssignStream(Stream stream)
    {
      if (TranscodedStream != null)
        TranscodedStream.Dispose();
      TranscodedStream = stream;
    }

    public void Stop()
    {
      Running = false;
    }

    public void Dispose()
    {
      Stop();
      if (TranscodedStream != null)
        TranscodedStream.Dispose();
    }
  }
}
