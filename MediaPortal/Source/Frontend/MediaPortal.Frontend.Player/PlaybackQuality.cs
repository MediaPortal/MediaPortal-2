/*
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

namespace MediaPortal.Frontend.Player
{
  /// <summary>
  /// What the engine found out about the video track of the media it was given: how big the picture is and what
  /// it is coded with. Nothing here is guessed; a field is null when the engine did not say it.
  /// </summary>
  /// <remarks>
  /// This is the answer to "which of the three copies is playing", and it arrives late - an engine reads a
  /// container's tracks while the media starts, so a screen that wants it asks for it and is told when it comes,
  /// rather than waiting before the first picture. A screen that has nothing to say about a medium it was not
  /// told about says nothing, which is why the fields are nullable and the whole thing is.
  /// </remarks>
  public sealed class PlaybackQuality
  {
    public PlaybackQuality(int width, int height, string? codecFourcc, long bitrateSeconds = 0)
    {
      Width = width;
      Height = height;
      CodecFourcc = string.IsNullOrWhiteSpace(codecFourcc) ? null : codecFourcc!.Trim();
      BitrateSeconds = bitrateSeconds;
    }

    /// <summary>Width of the coded picture in pixels, or 0 when the engine has not said.</summary>
    public int Width { get; }

    /// <summary>Height of the coded picture in pixels, or 0 when the engine has not said.</summary>
    public int Height { get; }

    /// <summary>
    /// The four characters the container writes the codec with, as the container writes them - "hvc1", "avc1",
    /// "dvhe". Null when the engine has not said. Compared case-insensitively by whoever reads it, because
    /// containers disagree about capitalisation.
    /// </summary>
    public string? CodecFourcc { get; }

    /// <summary>Bits per second the container declares for the video, or 0 when it declares none.</summary>
    public long BitrateSeconds { get; }

    /// <summary>Whether the engine said anything at all about the picture.</summary>
    public bool HasSize
    {
      get { return Width > 0 && Height > 0; }
    }

    /// <summary>
    /// Whether the medium is coded as Dolby Vision, which a container says by naming the codec "dvhe" or "dvh1" -
    /// one of the few things about a picture that a container states rather than implies.
    /// </summary>
    public bool IsDolbyVision
    {
      get
      {
        string fourcc = (CodecFourcc ?? string.Empty).ToLowerInvariant();
        return fourcc == "dvhe" || fourcc == "dvh1" || fourcc == "dvav" || fourcc == "dva1";
      }
    }

    /// <summary>Whether the medium is coded as HEVC, which is the container's own statement, not an inference.</summary>
    public bool IsHevc
    {
      get
      {
        string fourcc = (CodecFourcc ?? string.Empty).ToLowerInvariant();
        return fourcc == "hvc1" || fourcc == "hev1" || fourcc == "hevc" || fourcc == "h265";
      }
    }

    public override string ToString()
    {
      return Width + "x" + Height + " " + (CodecFourcc ?? "unknown codec");
    }
  }
}
