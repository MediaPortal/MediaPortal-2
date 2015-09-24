using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.Base
{
  public class TranscodeContext : IDisposable
  {
    StringBuilder _errorOutput = new StringBuilder();
    StringBuilder _standardOutput = new StringBuilder();
    public string TargetFile { get; internal set; }
    public string SegmentDir { get; internal set; }
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
    public Stream TranscodedStream { get; private set; }

    internal void FFMPEG_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
      _errorOutput.Append(e.Data);
    }

    internal void FFMPEG_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
      _standardOutput.Append(e.Data);
    }

    public void Start(Stream stream, bool running)
    {
      Running = running;
      Aborted = false;
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
