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
    TimeSpan _lastTime = TimeSpan.FromTicks(0);
    object _lastSync = new object();
    bool _streamInUse = false;
    bool _useCache = true;
    ManualResetEvent _completeEvent = new ManualResetEvent(true);

    public TranscodeContext(bool useCache)
    {
      _useCache = useCache;
    }

    internal ManualResetEvent CompleteEvent 
    {
      get { return _completeEvent; }
    }

    public string TargetFile { get; internal set; }
    public string SegmentDir { get; internal set; }
    public string HlsBaseUrl { get; internal set; }
    public bool Aborted { get; internal set; }
    public bool Failed { get; internal set; }
    public bool Partial { get; internal set; }
    public bool InUse 
    {
      get { return _streamInUse; }
      set
      {
        if(_streamInUse == true && value == false && (Partial == true || _useCache == false))
        {
          //Delete transcodes if no longer used
          Stop();
          DeleteFiles();
        }
        _streamInUse = value;
      }
    }
    public TimeSpan TargetDuration { get; internal set; }
    public TimeSpan CurrentDuration 
    { 
      get
      {
        return _lastTime;
      }
    }
    public long TargetFileSize 
    { 
      get
      {
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
        if (_transcodedStream != null)
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
              _lastSize = long.Parse(match.Groups["size"].Value.Substring(0, match.Groups["size"].Value.Length - 2).Trim()) * 1024;
            }
            else if (match.Groups["size"].Value.EndsWith("mB", StringComparison.InvariantCultureIgnoreCase))
            {
              _lastSize = long.Parse(match.Groups["size"].Value.Substring(0, match.Groups["size"].Value.Length - 2).Trim()) * 1024 * 1024;
            }
            _lastTime = TimeSpan.Parse(match.Groups["time"].Value);
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
      string deletePath = TargetFile;
      bool isFolder = false;
      if (deletePath.EndsWith(".m3u8") == true)
      {
        deletePath = SegmentDir;
        isFolder = true;
      }

      _completeEvent.WaitOne(5000);
      DateTime waitStart = DateTime.Now;
      while (true)
      {
        if ((DateTime.Now - waitStart).TotalSeconds > 60.0)
        {
          break;
        }
        try
        {
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
      InUse = false;
      Stop();
      if (TranscodedStream != null)
        TranscodedStream.Dispose();
    }
  }
}
