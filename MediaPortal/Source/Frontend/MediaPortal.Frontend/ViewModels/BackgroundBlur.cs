/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
    General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The out-of-focus copy of a background, made away from the thread that draws the screen and remembered for
  /// the next time the same picture is worn.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Blurring is not cheap: a full-HD backdrop wants a hundred and fifty milliseconds of arithmetic, which is a
  /// sixth of a second - and a television counts every one of them while the viewer is walking the menu. Made on
  /// the thread that draws, that time is felt as the shelf sticking at exactly the moment the viewer moves,
  /// because a background is changed by moving. So the work happens here, on a worker thread, while the screen
  /// keeps wearing the background it was wearing until this one is ready.
  /// </para>
  /// <para>
  /// What is remembered is the blurred picture as bytes rather than the picture as the screen holds it, because
  /// bytes belong to no thread: any screen may ask on any thread and the answer stays useful. The copies are
  /// small by their nature - a blurred picture is kept at a third of its size, see
  /// <see cref="Views.SkinFanartConverter.BLUR_SCALE"/> - so a good dozen of them fit in a few megabytes, which
  /// is enough for the wall a viewer actually walks back and forth over.
  /// </para>
  /// <para>
  /// A copy is looked for by what the bytes <i>are</i> rather than by which array happens to hold them: two
  /// screens asking for the same backdrop after their memory of it was dropped get the same answer, and the
  /// arithmetic is done once per picture and per turn of the skin's dial.
  /// </para>
  /// </remarks>
  public sealed class BackgroundBlur
  {
    /// <summary>How much room the remembered copies may take together, when nobody asked for less.</summary>
    public const long DEFAULT_ROOM_BYTES = 24L * 1024 * 1024;

    /// <summary>
    /// How much room the remembered copies may take together. A few megabytes hold the dozen backdrops a viewer
    /// actually walks back and forth over, and a blurred copy weighs a few hundred kilobytes because it is kept
    /// at a third of its size. A test asks for less on purpose.
    /// </summary>
    public static long RoomBytes = DEFAULT_ROOM_BYTES;

    /// <summary>
    /// The copies this machine holds. One holder for the whole run, because the home screen and the shelf behind
    /// it ask for the same backdrops and there is no sense in blurring a picture twice for two screens.
    /// </summary>
    private static readonly BackgroundBlur Worn = new BackgroundBlur();

    private readonly Dictionary<string, byte[]> _copies = new Dictionary<string, byte[]>();

    /// <summary>The order the copies were last asked for, oldest first.</summary>
    private readonly List<string> _askedFor = new List<string>();

    private long _taken;

    private BackgroundBlur()
    {
    }

    /// <summary>
    /// The blurred copy of these bytes at this radius, ready whenever the caller is done waiting - and null when
    /// the bytes hold no picture to blur.
    /// </summary>
    /// <param name="source">The backdrop as it came from the server.</param>
    /// <param name="radius">How far out of focus, in the pixels the skin's dial speaks.</param>
    public static Task<byte[]?> BlurredAsync(byte[] source, double radius)
    {
      if (source == null || source.Length == 0 || radius <= 0)
        return Task.FromResult<byte[]?>(null);

      string key = KeyOf(source) + "@" + radius.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

      byte[]? held;
      lock (Worn)
      {
        Worn._copies.TryGetValue(key, out held);
        Worn.AskedFor(key);
      }

      if (held != null)
        return Task.FromResult<byte[]?>(held);

      return Task.Run(delegate
      {
        byte[]? blurred = Views.SkinFanartConverter.BlurToPng(source, radius, out _, out _);
        if (blurred != null)
          Worn.Remember(key, blurred);

        return blurred;
      });
    }

    /// <summary>
    /// What the copies take together, which is what a test - and a viewer with a small machine - wants to know.
    /// </summary>
    public static long RoomTaken
    {
      get { lock (Worn) return Worn._taken; }
    }

    /// <summary>
    /// Forgetting every copy, which the screen that changed the dial does not need - an old copy is answered
    /// under its own radius - but which keeps a long run from holding a library of backgrounds.
    /// </summary>
    public static void Forget()
    {
      lock (Worn)
      {
        Worn._copies.Clear();
        Worn._askedFor.Clear();
        Worn._taken = 0;
      }
    }

    /// <summary>
    /// A short mark saying which bytes these are, so that the same backdrop found again is found again in the
    /// copies rather than blurred a second time.
    /// </summary>
    private static string KeyOf(byte[] source)
    {
      using SHA1 sha = SHA1.Create();
      return Convert.ToBase64String(sha.ComputeHash(source));
    }

    /// <summary>Filing one copy, and letting the oldest go while the room the copies were given is over.</summary>
    private void Remember(string key, byte[] copy)
    {
      lock (this)
      {
        if (_copies.TryGetValue(key, out byte[]? before))
          _taken -= before.Length;

        _copies[key] = copy;
        _taken += copy.Length;
        AskedFor(key);

        while (_taken > RoomBytes && _askedFor.Count > 1)
        {
          string oldest = _askedFor[0];
          _askedFor.RemoveAt(0);

          if (_copies.TryGetValue(oldest, out byte[]? gone))
          {
            _copies.Remove(oldest);
            _taken -= gone.Length;
          }
        }
      }
    }

    /// <summary>Moving one key to the end of the "last asked for" line, which is how the oldest is known.</summary>
    private void AskedFor(string key)
    {
      _askedFor.Remove(key);
      _askedFor.Add(key);
    }
  }
}
