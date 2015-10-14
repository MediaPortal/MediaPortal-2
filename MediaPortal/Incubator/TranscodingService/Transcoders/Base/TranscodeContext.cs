using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.Base
{
  public class TranscodeContext : IDisposable
  {
    StringBuilder _errorOutput = new StringBuilder();
    StringBuilder _standardOutput = new StringBuilder();

    private Stream _transcodedStream;

    public string TargetFile { get; internal set; }
    public string SegmentDir { get; internal set; }
    public string HlsBaseUrl { get; internal set; }
    public bool Aborted { get; internal set; }
    public bool Failed { get; internal set; }
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
    public bool Running { get; internal set; }

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
      _errorOutput.Append(e.Data);
    }

    internal void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
      _standardOutput.Append(e.Data);
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
