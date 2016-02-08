using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using System.Threading;
using System.Text.RegularExpressions;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.Base
{
  public class TranscodeContext : IDisposable
  {
    StringBuilder _errorOutput = new StringBuilder();
    StringBuilder _standardOutput = new StringBuilder();
    Regex progressRegex = new Regex(@"frame=\s*(?<frame>[0-9]*)\s*fps=(?<fps>[0-9]*)\s*q=(?<quality>[0-9|\.]*)\s*size=\s*(?<size>\S*)\s*time=(?<time>.*)\s*bitrate=(?<bitrate>\S*)", RegexOptions.IgnoreCase);

    Stream _transcodedStream;
    long _lastSize = 0;
    long _lastFrame = 0;
    long _lastFPS = 0;
    long _lastBitrate = 0;
    TimeSpan _lastTime = TimeSpan.FromTicks(0);
    object _lastSync = new object();
    bool _streamInUse = false;
    bool _useCache = true;
    string _cachePath = null;
    long _currentSegment = 0;
    ManualResetEvent _completeEvent = new ManualResetEvent(true);

    public TranscodeContext(bool useCache, string cachePath)
    {
      _useCache = useCache;
      _cachePath = cachePath;
    }

    internal ManualResetEvent CompleteEvent 
    {
      get { return _completeEvent; }
    }

    public string TargetFile { get; internal set; }
    public string TargetSubtitle { get; internal set; }
    public string SegmentDir { get; internal set; }
    public string HlsBaseUrl { get; internal set; }
    public bool Aborted { get; internal set; }
    public bool Failed { get; internal set; }
    public bool Partial { get; internal set; }
    public bool Segmented 
    { 
      get
      {
        return string.IsNullOrEmpty(SegmentDir) == false;
      }
    }
    public bool Live { get; internal set; }
    public bool InUse 
    {
      get { return _streamInUse; }
      set
      {
        if (_streamInUse == true && value == false && (Partial == true || _useCache == false || Live == true))
        {
          //Delete transcodes if no longer used
          Stop();
          DeleteFiles();
        }
        _streamInUse = value;
      }
    }
    public long LastSegment { get; internal set; }
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
    public TimeSpan TargetDuration { get; internal set; }
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
        else if (_transcodedStream != null && _transcodedStream.CanRead)
        {
          return _transcodedStream.Length;
        }
        else
        {
          return _lastSize;
        }
      }
    }
    public string ConsoleErrorOutput
    {
      get
      {
        return _errorOutput.ToString();
      }
    }
    public string ConsoleOutput
    {
      get
      {
        return _standardOutput.ToString();
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

    internal void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
      //frame= 4152 fps=115 q=30.0 size=   40610kB time=00:02:53.56 bitrate=1916.8kbits/s  
      if (string.IsNullOrEmpty(e.Data) == false)
      {
        Match match = progressRegex.Match(e.Data);
        if (match.Success)
        {
          lock (_lastSync)
          {
            if (match.Groups["size"].Value.EndsWith("kB", StringComparison.InvariantCultureIgnoreCase))
            {
              if(long.TryParse(match.Groups["size"].Value.Substring(0, match.Groups["size"].Value.Length - 2).Trim(), out _lastSize))
              {
                _lastSize = _lastSize * 1024;
              }
            }
            else if (match.Groups["size"].Value.EndsWith("mB", StringComparison.InvariantCultureIgnoreCase))
            {
              if(long.TryParse(match.Groups["size"].Value.Substring(0, match.Groups["size"].Value.Length - 2).Trim(), out _lastSize))
              {
                _lastSize = _lastSize * 1024 * 1024;
              }
            }
            TimeSpan.TryParse(match.Groups["time"].Value, out _lastTime);
            long.TryParse(match.Groups["frame"].Value, out _lastFrame); 
            long.TryParse(match.Groups["fps"].Value, out _lastFPS);
            long.TryParse(match.Groups["bitrate"].Value, out _lastBitrate);
          }
        }
        _errorOutput.Append(e.Data);
      }
    }

    internal void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
      if (string.IsNullOrEmpty(e.Data) == false)
      {
        _standardOutput.Append(e.Data);
      }
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

    internal void DeleteFiles()
    {
      if (TranscodedStream != null)
        TranscodedStream.Dispose();

      string deletePath = TargetFile;
      bool isFolder = false;
      if (string.IsNullOrEmpty(SegmentDir) == false)
      {
        deletePath = SegmentDir;
        isFolder = true;
      }
      if (Live && Segmented == false)
      {
        deletePath = "";
      }

      string subtitlePath = TargetSubtitle;
      if (Partial == false && Live == false)
      {
        subtitlePath = "";
      }

      DateTime waitStart = DateTime.Now;
      while (true)
      {
        if ((DateTime.Now - waitStart).TotalSeconds > 30.0)
        {
          break;
        }
        try
        {
          //Only delete subtitle if it is in the cache
          if (subtitlePath != null && subtitlePath.StartsWith(_cachePath) == true)
          {
            try
            {
              if (File.Exists(subtitlePath))
              {
                File.Delete(subtitlePath);
              }
            }
            catch { }
          }
          if (isFolder == false)
          {
            if (File.Exists(deletePath))
            {
              File.Delete(deletePath);
              break;
            }
            else
            {
              break;
            }
          }
          else
          {
            if (Directory.Exists(deletePath))
            {
              Directory.Delete(deletePath, true);
              break;
            }
            else
            {
              break;
            }
          }
        }
        catch { }
        Thread.Sleep(500);
      }
    }

    public void Dispose()
    {
      Stop();
      if (TranscodedStream != null)
        TranscodedStream.Dispose();
    }
  }
}
