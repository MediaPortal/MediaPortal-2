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
using System.Collections.Generic;
using LibVLCSharp.Shared;

namespace MediaPortal.Frontend.Player
{
    /// <summary>
    /// One short sound the interface plays over and over: the click of a confirm, the step of a walk
    /// through a list. The reference's skins shipped these as WAV files beside their pictures and played
    /// the worn skin's own file on every move, which is what this front end does now - and this is the
    /// only place the sound side of libVLC is touched, for the same reason the video side is touched only
    /// by <see cref="LibVlcPlayer"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One quiet libVLC instance serves every cue in the process: a cue is a few kilobytes and a tenth of
    /// a second, and starting a media engine per footstep would cost more than the footstep is worth.
    /// When libVLC cannot be had - no native library on this machine, no sound card, a headless test -
    /// the cue says nothing and fails once, quietly, forever.
    /// </para>
    /// </remarks>
    public sealed class SoundCue : IDisposable
    {
        private static readonly object GATE = new object();
        private static LibVLC? _lib;
        private static bool _broken;
        private static readonly Dictionary<string, Media> MEDIA =
            new Dictionary<string, Media>(StringComparer.OrdinalIgnoreCase);

        private readonly string _file;
        private MediaPlayer? _player;

        /// <summary>
        /// A cue for one sound file. Nothing is read or opened yet: a skin that carries sounds nobody ever
        /// hears must not cost a media engine at start-up.
        /// </summary>
        public SoundCue(string file)
        {
            _file = file;
        }

        /// <summary>Whether the machine has ever refused the sound engine; from then on nobody asks again.</summary>
        public static bool EngineIsDead
        {
            get
            {
                lock (GATE)
                {
                    return _broken;
                }
            }
        }

        /// <summary>Play the cue from its first sample, even while it is still playing.</summary>
        public void Play()
        {
            try
            {
                lock (GATE)
                {
                    if (_broken)
                        return;
                    LibVLC lib = EnsureLib();
                    _player ??= new MediaPlayer(lib) { Volume = 45 };
                    Media media = EnsureMedia(lib, _file);
                    _player.Stop();
                    _player.Play(media);
                }
            }
            catch (Exception)
            {
                // A sound the machine cannot make stays a sound the machine cannot make; no dialog is
                // worth one missing footstep.
                lock (GATE)
                {
                    _broken = true;
                }
            }
        }

        public void Dispose()
        {
            lock (GATE)
            {
                _player?.Dispose();
                _player = null;
            }
        }

        private static LibVLC EnsureLib()
        {
            if (_lib != null)
                return _lib;
            Core.Initialize();
            _lib = new LibVLC("--quiet", "--no-video", "--no-media-library");
            return _lib;
        }

        private static Media EnsureMedia(LibVLC lib, string file)
        {
            if (MEDIA.TryGetValue(file, out Media? known))
                return known;
            Media media = new Media(lib, new Uri(file));
            MEDIA[file] = media;
            return media;
        }
    }
}
