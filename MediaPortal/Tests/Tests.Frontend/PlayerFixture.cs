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

using System;
using System.IO;

namespace Tests.Frontend
{
  /// <summary>
  /// The media these tests play, built here rather than stored in the repository.
  /// </summary>
  /// <remarks>
  /// A WAV file is a 44-byte header and samples, which means a test can hold a real, decodable media file with no
  /// codec, no download and no licence question - and its length is exactly what the header says, so "did the
  /// engine report the right length?" has an answer that is not a guess. Anything with a video track would need a
  /// real clip, and a real clip in the repository is a maintenance and licensing decision that is not a test's to
  /// make.
  /// </remarks>
  internal static class PlayerFixture
  {
    private static readonly string Directory =
        Path.Combine(Path.GetTempPath(), "mp2-player-tests");

    /// <summary>
    /// Write a silent PCM WAV of <paramref name="seconds"/> and return its absolute path.
    /// </summary>
    public static string WriteWav(int seconds, string? name = null)
    {
      if (seconds <= 0)
        throw new ArgumentOutOfRangeException(nameof(seconds));

      System.IO.Directory.CreateDirectory(Directory);
      string path = Path.Combine(Directory, (name ?? ("silent-" + seconds + "s")) + ".wav");

      const int sampleRate = 8000;
      const int bitsPerSample = 16;
      int bytesPerSample = bitsPerSample / 8;
      int dataBytes = sampleRate * seconds * bytesPerSample;

      using (FileStream stream = File.Create(path))
      using (BinaryWriter writer = new BinaryWriter(stream))
      {
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + dataBytes);
        writer.Write(new[] { 'W', 'A', 'V', 'E' });

        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16);                            // PCM fmt chunk size
        writer.Write((ushort)1);                     // PCM
        writer.Write((ushort)1);                     // mono
        writer.Write(sampleRate);
        writer.Write(sampleRate * bytesPerSample);   // byte rate
        writer.Write((ushort)bytesPerSample);        // block align
        writer.Write((ushort)bitsPerSample);

        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(dataBytes);
        writer.Write(new byte[dataBytes]);           // silence
      }
      return path;
    }

    /// <summary>A path in the fixture folder that deliberately has no file behind it.</summary>
    public static string MissingFile()
    {
      System.IO.Directory.CreateDirectory(Directory);
      return Path.Combine(Directory, "this-file-does-not-exist-" + Guid.NewGuid().ToString("N") + ".mp4");
    }

    /// <summary>An empty folder that is not a libVLC install.</summary>
    public static string EmptyDirectory()
    {
      string path = Path.Combine(Directory, "not-vlc-" + Guid.NewGuid().ToString("N"));
      System.IO.Directory.CreateDirectory(path);
      return path;
    }

    /// <summary>A folder that only pretends to hold libVLC, to prove the lookup looks for the right file.</summary>
    public static string FakeEngineDirectory(string libraryFileName)
    {
      string path = EmptyDirectory();
      File.WriteAllText(Path.Combine(path, libraryFileName), "not a library");
      return path;
    }
  }
}
