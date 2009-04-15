using System;
using System.IO;
using System.Runtime.InteropServices;
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
    protected bool _isValid = false;

    #endregion

    /// <summary>
    /// Creates a new instance of <see cref="MediaInfoWrapper"/>. To get metadata, the media resource has to be
    /// opened by the use of one of the methods <see cref="Open(Stream)"/> or <see cref="Open(string)"/>.
    /// </summary>
    public MediaInfoWrapper()
    {
      _mediaInfo = new MediaInfo();
    }

    /// <summary>
    /// Returns the information if the underlaying MediaInfo instance is valid, i.e. it can provide metadata
    /// for the opened media resource.
    /// </summary>
    public bool IsValid
    {
      get { return _isValid; }
    }

    /// <summary>
    /// Disposes all resources. This method will call the <see cref="Close"/> method.
    /// </summary>
    public void Dispose()
    {
      if (_isValid)
        Close();
    }

    #region Protected methods

    /// <summary>
    /// Extracts an int value from the specified <paramref name="strValue"/> parameter, if it has the correct
    /// format.
    /// </summary>
    /// <param name="strValue">String to extract the value from.</param>
    /// <returns>Extracted value or <c>null</c>, if the <paramref name="strValue"/> doesn't have the correct
    /// format.</returns>
    protected static int? GetIntOrNull(string strValue)
    {
      if (string.IsNullOrEmpty(strValue))
        return null;
      int result;
      return int.TryParse(strValue, out result) ? result : new int?();
    }

    /// <summary>
    /// Extracts a long value from the specified <paramref name="strValue"/> parameter, if it has the correct
    /// format.
    /// </summary>
    /// <param name="strValue">String to extract the value from.</param>
    /// <returns>Extracted value or <c>null</c>, if the <paramref name="strValue"/> doesn't have the correct
    /// format.</returns>
    protected static long? GetLongOrNull(string strValue)
    {
      if (string.IsNullOrEmpty(strValue))
        return null;
      long result;
      return long.TryParse(strValue, out result) ? result : new long?();
    }

    /// <summary>
    /// Extracts a float value from the specified <paramref name="strValue"/> parameter, if it has the correct
    /// format.
    /// </summary>
    /// <param name="strValue">String to extract the value from.</param>
    /// <returns>Extracted value or <c>null</c>, if the <paramref name="strValue"/> doesn't have the correct
    /// format.</returns>
    protected static float? GetFloatOrNull(string strValue)
    {
      if (string.IsNullOrEmpty(strValue))
        return null;
      float result;
      return float.TryParse(strValue, out result) ? result : new float?();
    }

    #endregion

    /// <summary>
    /// Opens the media resource at the specified file path.
    /// </summary>
    /// <param name="filePath">Path of the file where the metadata should be extracted.</param>
    /// <returns><c>true</c>, if the open operation was successful. In this case, <see cref="IsValid"/> will
    /// be <c>true</c> also. <c>false</c>, if the open operation was not successful. This means,
    /// <see cref="IsValid"/> will also be <c>false</c>.</returns>
    public bool Open(string filePath)
    {
      _isValid = _mediaInfo.Open(filePath) == MEDIAINFO_FILE_OPENED;
      return _isValid;
    }

    /// <summary>
    /// Opens the media resource given by the specified <paramref name="stream"/>
    /// </summary>
    /// <param name="stream">Stream containing the contents of the file where the metadata should be
    /// extracted.</param>
    /// <returns><c>true</c>, if the open operation was successful. In this case, <see cref="IsValid"/> will
    /// be <c>true</c> also. <c>false</c>, if the open operation was not successful. This means,
    /// <see cref="IsValid"/> will also be <c>false</c>.</returns>
    public bool Open(Stream stream)
    {
      const int buffer_size = 64 * 1024;
      int bytes_read = 0;
      byte[] buffer = new byte[buffer_size];  // init the buffer to communicate with MediaInfo

      _isValid = (_mediaInfo.Open_Buffer_Init(stream.Length, 0) == MEDIAINFO_FILE_OPENED);
      if (!_isValid)
      {
        return false;
      }

      // Now we need to run the parsing loop, as long as MediaInfo requests information from the stream
      do
      {
        bytes_read = stream.Read(buffer, 0, buffer_size);
        GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        IntPtr buffer_ptr = gcHandle.AddrOfPinnedObject();

        
        if (_mediaInfo.Open_Buffer_Continue(buffer_ptr, (IntPtr)bytes_read) == 0)
        {
          // MediaInfo doesn't need more information from us
          gcHandle.Free();
          break;
        }
        gcHandle.Free();
 
        // Now we need to test, if MediaInfo wants data from a different place of the stream
        if (_mediaInfo.Open_Buffer_Continue_GoTo_Get() != -1)
        {
          Int64 pos = stream.Seek(_mediaInfo.Open_Buffer_Continue_GoTo_Get(), SeekOrigin.Begin);  // Position the stream
          _mediaInfo.Open_Buffer_Init(stream.Length, pos);  // Inform MediaInfo that we are at the new position
        }
      } while (bytes_read > 0);
 
      // Finalising MediaInfo procesing
      _mediaInfo.Open_Buffer_Finalize();

      return _isValid;
    }

    /// <summary>
    /// Closes the underlaying MediaInfo instance and releases all resources.
    /// </summary>
    public void Close()
    {
      _mediaInfo.Close();
      _isValid = false;
    }

    /// <summary>
    /// Returns the number of video streams in the media resource.
    /// </summary>
    /// <returns>Number of video streams. If the media resource isn't a video, <c>0</c> will be
    /// returned.</returns>
    public int GetVideoCount()
    {
      int result;
      return int.TryParse(_mediaInfo.Get(StreamKind.Video, 0, "StreamCount"), out result) ? result : 0;
    }

    /// <summary>
    /// Returns the name of the video codec used in the specified video <paramref name="stream"/> media resource.
    /// </summary>
    /// <param name="stream">Number of video stream to examine.</param>
    /// <returns>Name of the video codec or <c>null</c>, if the specified video
    /// stream doesn't exist.</returns>
    public string GetVidCodec(int stream)
    {
      return StringUtils.TrimToNull(_mediaInfo.Get(StreamKind.Video, stream, "Codec"));
    }

    /// <summary>
    /// Returns the bitrate of the video specified video <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of video stream to examine.</param>
    /// <returns>Bitrate in bits per second or <c>null</c>, if the specified video
    /// stream doesn't exist.</returns>
    public long? GetVidBitrate(int stream)
    {
      return GetLongOrNull(_mediaInfo.Get(StreamKind.Video, stream, "BitRate"));
    }

    /// <summary>
    /// Returns the width of the video in the specified video <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of video stream to examine.</param>
    /// <returns>Width of the video in pixels or <c>null</c>, if the specified video
    /// stream doesn't exist.</returns>
    public int? GetWidth(int stream)
    {
      return GetIntOrNull(_mediaInfo.Get(StreamKind.Video, stream, "Width"));
    }

    /// <summary>
    /// Returns the height of the video in the specified video <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of video stream to examine.</param>
    /// <returns>Height of the video in pixels or <c>null</c>, if the specified video
    /// stream doesn't exist.</returns>
    public int? GetHeight(int stream)
    {
      return GetIntOrNull(_mediaInfo.Get(StreamKind.Video, stream, "Height"));
    }

    /// <summary>
    /// Returns the aspect ratio of the video in the specified video <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of video stream to examine.</param>
    /// <returns>Aspect ratio as a floating point quotient of width/height or <c>null</c>,
    /// if the specified video stream doesn't exist.</returns>
    public float? GetAR(int stream)
    {
      return GetFloatOrNull(_mediaInfo.Get(StreamKind.Video, stream, "AspectRatio"));
    }

    /// <summary>
    /// Returns the duration of the video in the specified video <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of video stream to examine.</param>
    /// <returns>Duration of the video in seconds or <c>null</c>, if the specified video
    /// stream doesn't exist.</returns>
    public long? GetPlaytime(int stream)
    {
      return GetLongOrNull(_mediaInfo.Get(StreamKind.Video, stream, "PlayTime"));
    }

    /// <summary>
    /// Returns the framerate of the video in the specified video <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of video stream to examine.</param>
    /// <returns>Framerate in frames per second of the video or <c>null</c>, if the specified video
    /// stream doesn't exist.</returns>
    public int? GetFramerate(int stream)
    {
      return GetIntOrNull(_mediaInfo.Get(StreamKind.Video, stream, "FrameRate"));
    }

    /// <summary>
    /// Returns the number of audio streams in the media resource.
    /// </summary>
    /// <returns>Number of audio streams. If the media resource doesn't have an audio stream, <c>0</c> will
    /// be returned.</returns>
    public int GetAudioCount()
    {
      int result;
      return int.TryParse(_mediaInfo.Get(StreamKind.Audio, 0, "StreamCount"), out result) ? result : 0;
    }

    /// <summary>
    /// Returns the name of the audio codec in the specified audio <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of audio stream to examine.</param>
    /// <returns>Name of the audio codec or <c>null</c>, if the specified audio stream doesn't exist.</returns>
    public string GetAudioCodec(int stream)
    {
      return StringUtils.TrimToNull(_mediaInfo.Get(StreamKind.Audio, stream, "Codec"));
    }

    /// <summary>
    /// Returns the bitrate of the specified audio <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of audio stream to examine.</param>
    /// <returns>Bitrate in bits per second or <c>null</c>, if the specified audio stream doesn't exist.</returns>
    public long? GetAudioBitrate(int stream)
    {
      return GetLongOrNull(_mediaInfo.Get(StreamKind.Audio, stream, "BitRate"));
    }

    // TODO: (cover art, ....)
  }
}