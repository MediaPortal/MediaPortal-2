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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using MediaPortal.Plugins.Transcoding.Interfaces.Transcoding;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg
{
  public class FFMpegTranscodeContext : TranscodeContext
  {
    StringBuilder _errorOutput = new StringBuilder();
    StringBuilder _standardOutput = new StringBuilder();
    Regex progressRegex = new Regex(@"frame=\s*(?<frame>[0-9]*)\s*fps=(?<fps>[0-9]*)\s*q=(?<quality>[0-9|\.]*)\s*size=\s*(?<size>\S*)\s*time=(?<time>.*)\s*bitrate=(?<bitrate>\S*)", RegexOptions.IgnoreCase);
    bool _useCache = true;
    string _cachePath = null;

    internal FFMpegTranscodeContext(bool useCache, string cachePath) :
      base()
    {
      _useCache = useCache;
      _cachePath = cachePath;
    }

    public new bool InUse
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

    internal void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
      try
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
                if (long.TryParse(match.Groups["size"].Value.Substring(0, match.Groups["size"].Value.Length - 2).Trim(), out _lastSize))
                {
                  _lastSize = _lastSize * 1024;
                }
              }
              else if (match.Groups["size"].Value.EndsWith("mB", StringComparison.InvariantCultureIgnoreCase))
              {
                if (long.TryParse(match.Groups["size"].Value.Substring(0, match.Groups["size"].Value.Length - 2).Trim(), out _lastSize))
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
      catch { }
    }

    internal void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
      try
      {
        if (string.IsNullOrEmpty(e.Data) == false)
        {
          _standardOutput.Append(e.Data);
        }
      }
      catch { }
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
  }
}
