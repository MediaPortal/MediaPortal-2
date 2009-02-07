using System;
using System.IO;
using MediaPortal.Utilities;

namespace MediaInfoLib
{
  /// <summary>
  /// MediaPortal wrapper class for the third-party MediaInfo library.
  /// </summary>
  public class MediaInfoWrapper : IDisposable
  {
    #region Proteted fields & constants

    protected const int MEDIAINFO_FILE_OPENED = 1;

    protected MediaInfo _mediaInfo;
    protected bool _isOpened = false;

    #endregion

    public MediaInfoWrapper()
    {
      _mediaInfo = new MediaInfo();
    }

    public bool IsOpened
    {
      get { return _isOpened; }
    }

    public void Dispose()
    {
      Close();
    }

    protected static int? GetIntOrNull(string strValue)
    {
      if (string.IsNullOrEmpty(strValue))
        return null;
      int result;
      return int.TryParse(strValue, out result) ? result : new int?();
    }

    protected static long? GetLongOrNull(string strValue)
    {
      if (string.IsNullOrEmpty(strValue))
        return null;
      long result;
      return long.TryParse(strValue, out result) ? result : new long?();
    }

    protected static float? GetFloatOrNull(string strValue)
    {
      if (string.IsNullOrEmpty(strValue))
        return null;
      float result;
      return float.TryParse(strValue, out result) ? result : new float?();
    }

    // TODO: Method docs for all methods

    public bool Open(string fileName)
    {
      _isOpened = _mediaInfo.Open(fileName) == MEDIAINFO_FILE_OPENED;
      return _isOpened;
    }

    public bool Open(Stream fileStream)
    {
      // TODO: Open the stream in the underlaying _mediaInfo instance
      return _isOpened;
    }

    public void Close()
    {
      _mediaInfo.Close();
    }

    public int GetVideoCount()
    {
      int result;
      return int.TryParse(_mediaInfo.Get(StreamKind.Video, 0, "StreamCount"), out result) ? result : 0;
    }

    public string GetVidCodec()
    {
      return StringUtils.TrimToNull(_mediaInfo.Get(StreamKind.Video, 0, "Codec"));
    }

    public long? GetVidBitrate()
    {
      return GetLongOrNull(_mediaInfo.Get(StreamKind.Video, 0, "BitRate"));
    }

    public int? GetWidth()
    {
      return GetIntOrNull(_mediaInfo.Get(StreamKind.Video, 0, "Width"));
    }

    public int? GetHeight()
    {
      return GetIntOrNull(_mediaInfo.Get(StreamKind.Video, 0, "Height"));
    }

    public float? GetAR()
    {
      return GetFloatOrNull(_mediaInfo.Get(StreamKind.Video, 0, "AspectRatio"));
    }

    public long? GetPlaytime()
    {
      return GetLongOrNull(_mediaInfo.Get(StreamKind.Video, 0, "PlayTime"));
    }

    public int? GetFPS()
    {
      return GetIntOrNull(_mediaInfo.Get(StreamKind.Video, 0, "FrameRate"));
    }

    public int GetAudioCount()
    {
      int result;
      return int.TryParse(_mediaInfo.Get(StreamKind.Audio, 0, "StreamCount"), out result) ? result : 0;
    }

    public string GetAudioCodec()
    {
      return StringUtils.TrimToNull(_mediaInfo.Get(StreamKind.Audio, 0, "Codec"));
    }

    public long? GetAudioBitrate()
    {
      return GetLongOrNull(_mediaInfo.Get(StreamKind.Audio, 0, "BitRate"));
    }

    public int GetNoChannels()
    {
      return GetNoChannels(0);
    }

    public int GetNoChannels(int stream)
    {
      return int.Parse(_mediaInfo.Get(StreamKind.Audio, stream, "Channel(s)"));
    }

    // TODO: provide methods for all needed info (parsing successful? cover art? ....)
  }
}