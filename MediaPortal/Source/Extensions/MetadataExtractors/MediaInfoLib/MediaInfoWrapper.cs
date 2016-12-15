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
using System.Globalization;
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
      _isValid = false;
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
      if (strValue.IndexOf("(") > 1)
        strValue = strValue.Substring(0, strValue.IndexOf("(")).Trim();
      return int.TryParse(strValue, NumberStyles.Number, CultureInfo.InvariantCulture, out result) ? result : new int?();
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
      return long.TryParse(strValue, NumberStyles.Number, CultureInfo.InvariantCulture, out result) ? result : new long?();
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
      return float.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ? result : new float?();
    }

    /// <summary>
    /// Extracts a bool value from the specified <paramref name="strValue"/> parameter, if it has the correct
    /// format.
    /// </summary>
    /// <param name="strValue">String to extract the value from.</param>
    /// <returns>Extracted value or <c>null</c>, if the <paramref name="strValue"/> doesn't have the correct
    /// format.</returns>
    protected static bool? GetBoolOrNull(string strValue)
    {
      if (string.IsNullOrEmpty(strValue))
        return null;
      bool result;
      if (bool.TryParse(strValue, out result))
        return result;
      if (strValue.Equals("No", StringComparison.InvariantCultureIgnoreCase))
        return false;
      if (strValue.Equals("Yes", StringComparison.InvariantCultureIgnoreCase))
        return true;
      return null;
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
      _isValid = (_mediaInfo.Open_Buffer_Init(stream.Length, 0) == MEDIAINFO_FILE_OPENED);
      if (!_isValid)
        return false;

      // Increased buffer size for some .mp4 that contain first audio, then video stream. If buffer is smaller (i.e. 64 kb),
      // MediaInfo only detects the audio stream. It works correctly in file mode.
      const int bufferSize = 512 * 1024;
      byte[] buffer = new byte[bufferSize]; // init the buffer to communicate with MediaInfo
      GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
      IntPtr bufferPtr = gcHandle.AddrOfPinnedObject();
      try
      {
        // Now we need to run the parsing loop, as long as MediaInfo requests information from the stream
        int bytesRead;
        do
        {
          bytesRead = stream.Read(buffer, 0, bufferSize);

          if ((_mediaInfo.Open_Buffer_Continue(bufferPtr, (IntPtr)bytesRead) & BufferResult.Finalized) == BufferResult.Finalized)
            // MediaInfo doesn't need more information from us
            break;

          long newPos = _mediaInfo.Open_Buffer_Continue_GoTo_Get();
          // Now we need to test, if MediaInfo wants data from a different place of the stream
          if (newPos == -1)
            break;
          Int64 pos = stream.Seek(newPos, SeekOrigin.Begin);  // Position the stream
          _mediaInfo.Open_Buffer_Init(stream.Length, pos);  // Inform MediaInfo that we are at the new position
        } while (bytesRead > 0);
      }
      finally
      {
        gcHandle.Free();

        // Finalising MediaInfo procesing
        _mediaInfo.Open_Buffer_Finalize();
      }
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
      return StringUtils.TrimToNull(_mediaInfo.Get(StreamKind.Video, stream, "CodecID/Hint")) ??
             StringUtils.TrimToNull(_mediaInfo.Get(StreamKind.Video, stream, "Codec/String"));
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
    /// <returns>Duration of the video in milliseconds or <c>null</c>, if the specified video
    /// stream doesn't exist.</returns>
    public long? GetPlaytime(int stream)
    {
      return GetLongOrNull(_mediaInfo.Get(StreamKind.Video, stream, "PlayTime"));
    }

    /// <summary>
    /// Returns the steam id of the specified video <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of video stream to examine.</param>
    /// <returns>ID of video stream or <c>null</c>, if the specified audio stream doesn't exist.</returns>
    public int? GetVideoStreamID(int stream)
    {
      string id = _mediaInfo.Get(StreamKind.Video, stream, "ID/String");
      if (string.IsNullOrEmpty(id))
        return null;
      return GetIntOrNull(id);
    }

    /// <summary>
    /// Returns the framerate of the video in the specified video <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of video stream to examine.</param>
    /// <returns>Framerate in frames per second of the video or <c>null</c>, if the specified video
    /// stream doesn't exist.</returns>
    public float? GetFramerate(int stream)
    {
      return GetFloatOrNull(_mediaInfo.Get(StreamKind.Video, stream, "FrameRate"));
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
      return StringUtils.TrimToNull(_mediaInfo.Get(StreamKind.Audio, stream, "CodecID/Hint")) ??
             StringUtils.TrimToNull(_mediaInfo.Get(StreamKind.Audio, stream, "Codec/String"));
    }

    /// <summary>
    /// Returns the language of the specified audio <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of audio stream to examine.</param>
    /// <returns><see cref="CultureInfo.TwoLetterISOLanguageName"/> if a valid language was matched, or <c>null</c>.</returns>
    public string GetAudioLanguage(int stream)
    {
      string lang2 = StringUtils.TrimToNull(_mediaInfo.Get(StreamKind.Audio, stream, "Language/String2"));
      if (lang2 == null)
        return null;
      try
      {
        CultureInfo cultureInfo = new CultureInfo(lang2);
        return cultureInfo.TwoLetterISOLanguageName;
      }
      catch (CultureNotFoundException)
      {
        try
        {
          if (lang2.Contains("/"))
            lang2 = lang2.Substring(0, lang2.IndexOf("/")).Trim();

          CultureInfo cultureInfo = new CultureInfo(lang2);
          return cultureInfo.TwoLetterISOLanguageName;
        }
        catch
        {
          return null;
        }
      }
      catch (ArgumentException)
      {
        return null;
      }
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

    /// <summary>
    /// Returns the number of channels of the specified audio <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of audio stream to examine.</param>
    /// <returns>Number of audio channels or <c>null</c>, if the specified audio stream doesn't exist.</returns>
    public int? GetAudioChannels(int stream)
    {
      return GetIntOrNull(_mediaInfo.Get(StreamKind.Audio, stream, "Channel(s)"));
    }

    /// <summary>
    /// Returns the sample rate of the specified audio <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of audio stream to examine.</param>
    /// <returns>Number of audio channels or <c>null</c>, if the specified audio stream doesn't exist.</returns>
    public long? GetAudioSampleRate(int stream)
    {
      return GetLongOrNull(_mediaInfo.Get(StreamKind.Audio, stream, "SamplingRate"));
    }

    /// <summary>
    /// Returns the steam id of the specified audio <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of audio stream to examine.</param>
    /// <returns>ID of audio stream or <c>null</c>, if the specified audio stream doesn't exist.</returns>
    public int? GetAudioStreamID(int stream)
    {
      string id = _mediaInfo.Get(StreamKind.Audio, stream, "ID/String");
      if (string.IsNullOrEmpty(id))
        return null;
      return GetIntOrNull(id);
    }

    /// <summary>
    /// Returns the number of subtitle streams in the media resource.
    /// </summary>
    /// <returns>Number of subtitle streams. If the media resource doesn't have a subtitle stream, <c>0</c> will
    /// be returned.</returns>
    public int GetSubtitleCount()
    {
      int result;
      return int.TryParse(_mediaInfo.Get(StreamKind.Text, 0, "StreamCount"), out result) ? result : 0;
    }

    /// <summary>
    /// Returns the name of the subtitle codec in the specified subtitle <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of subtitle stream to examine.</param>
    /// <returns>Name of the subtitle codec or <c>null</c>, if the specified subtitle stream doesn't exist.</returns>
    public string GetSubtitleCodec(int stream)
    {
      return StringUtils.TrimToNull(_mediaInfo.Get(StreamKind.Text, stream, "CodecID"));
    }

    /// <summary>
    /// Returns the name of the subtitle format in the specified subtitle <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of subtitle stream to examine.</param>
    /// <returns>Name of the subtitle format or <c>null</c>, if the specified subtitle stream doesn't exist.</returns>
    public string GetSubtitleFormat(int stream)
    {
      return StringUtils.TrimToNull(_mediaInfo.Get(StreamKind.Text, stream, "Format"));
    }

    /// <summary>
    /// Returns the language of the specified subtitle <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of subtitle stream to examine.</param>
    /// <returns><see cref="CultureInfo.TwoLetterISOLanguageName"/> if a valid language was matched, or <c>null</c>.</returns>
    public string GetSubtitleLanguage(int stream)
    {
      string lang2 = StringUtils.TrimToNull(_mediaInfo.Get(StreamKind.Text, stream, "Language/String2"));
      if (lang2 == null)
        return null;
      try
      {
        CultureInfo cultureInfo = new CultureInfo(lang2);
        return cultureInfo.TwoLetterISOLanguageName;
      }
      catch (ArgumentException)
      {
        return null;
      }
    }

    /// <summary>
    /// Returns the default parameter of the specified subtitle <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of subtitle stream to examine.</param>
    /// <returns>Is subtitle default or <c>null</c>, if the specified subtitle stream doesn't exist.</returns>
    public bool? GetSubtitleDefault(int stream)
    {
      return GetBoolOrNull(_mediaInfo.Get(StreamKind.Text, stream, "Default"));
    }

    /// <summary>
    /// Returns the forced parameter of the specified subtitle <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of subtitle stream to examine.</param>
    /// <returns>Is subtitle forced or <c>null</c>, if the specified subtitle stream doesn't exist.</returns>
    public bool? GetSubtitleForced(int stream)
    {
      return GetBoolOrNull(_mediaInfo.Get(StreamKind.Text, stream, "Forced"));
    }

    /// <summary>
    /// Returns the steam id of the specified subtitle <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of subtitle stream to examine.</param>
    /// <returns>ID of subtitle stream or <c>null</c>, if the specified subtitle stream doesn't exist.</returns>
    public int? GetSubtitleStreamID(int stream)
    {
      string id = _mediaInfo.Get(StreamKind.Text, stream, "ID/String");
      if (string.IsNullOrEmpty(id))
        return null;
      return GetIntOrNull(id);
    }

    // TODO: (cover art, ....)
  }
}
