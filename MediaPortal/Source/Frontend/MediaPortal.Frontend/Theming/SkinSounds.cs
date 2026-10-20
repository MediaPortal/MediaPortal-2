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
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace MediaPortal.Frontend.Theming
{
    /// <summary>
    /// The sounds a skin carries and the reference's interface made with them: a step for every move
    /// through a list, a click for every press. The files are the skin's - <c>sounds\navigate.WAV</c>
    /// and <c>sounds\click.WAV</c> beside its pictures - so changing the skin changes the sound the
    /// television makes, and a skin without sounds leaves the room quiet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reference played these from the skin directory on every selection change and every OK; the
    /// same two moments are wired here, on the shell window itself, so no screen had to learn about it.
    /// A machine with no sound, or a headless test run, pays nothing: the sound engine fails once and is
    /// never asked again. <c>MP2_SKIN_SOUNDS=0</c> keeps even a loud machine quiet.
    /// </para>
    /// </remarks>
    public static class SkinSounds
    {
        private static readonly TimeSpan STEP_GAP = TimeSpan.FromMilliseconds(60);

        private static Player.SoundCue? _navigate;
        private static Player.SoundCue? _click;
        private static WornSkin? _wornFor;
        private static DateTime _lastStep = DateTime.MinValue;
        private static bool _attached;

        /// <summary>
        /// The one wiring: the shell window hears every selection and every press anywhere under it, and
        /// every change of skin re-reads the cues. Called once, when the window is put up.
        /// </summary>
        public static void Attach(Window shell)
        {
            if (_attached)
                return;
            _attached = true;

            if (string.Equals(Environment.GetEnvironmentVariable("MP2_SKIN_SOUNDS"), "0",
                StringComparison.Ordinal))
                return;

            shell.AddHandler(ListBox.SelectionChangedEvent,
                delegate (object? sender, SelectionChangedEventArgs e)
                {
                    if (e.AddedItems.Count > 0)
                        Navigate();
                }, RoutingStrategies.Bubble, handledEventsToo: true);
            shell.AddHandler(Button.ClickEvent,
                delegate { Click(); }, RoutingStrategies.Bubble, handledEventsToo: true);

            SkinEngine.Changed += delegate
            {
                if (Application.Current is Application app)
                    Refresh(app);
            };
            if (Application.Current is Application current)
                Refresh(current);
        }

        /// <summary>The step of a walk: the worn skin's navigate sound, at most sixty milliseconds apart.</summary>
        public static void Navigate()
        {
            DateTime now = DateTime.UtcNow;
            if (now - _lastStep < STEP_GAP)
                return;
            _lastStep = now;
            _navigate?.Play();
        }

        /// <summary>The press: the worn skin's click sound.</summary>
        public static void Click() => _click?.Play();

        /// <summary>
        /// Make the cues answer to the skin the application is wearing now. The skin engine calls this at
        /// the one moment it has already put a skin on or taken it off, so a skin put on is heard from its
        /// very first screen.
        /// </summary>
        public static void Refresh(Application app)
        {
            WornSkin? worn = SkinEngine.Worn(app);
            if (ReferenceEquals(worn, _wornFor))
                return;
            _wornFor = worn;

            _navigate?.Dispose();
            _click?.Dispose();
            _navigate = null;
            _click = null;
            if (worn == null || Player.SoundCue.EngineIsDead)
                return;

            string? folder = SoundFolder(worn);
            if (folder == null)
                return;
            string? navigate = FindSound(folder, "navigate");
            string? click = FindSound(folder, "click");
            if (navigate != null)
              _navigate = new Player.SoundCue(navigate);
            if (click != null)
              _click = new Player.SoundCue(click);
        }

        /// <summary>
        /// The sounds of the worn skin as files on disk: the skin folder's when the skin is worn from
        /// disk, and for a skin worn from inside the assembly, a copy of its sounds extracted once into
        /// the temporary folder - libVLC reads files, not resources.
        /// </summary>
        private static string? SoundFolder(WornSkin worn)
        {
            try
            {
                string disk = worn.Manifest.Directory ?? "";
                string onDisk = Path.Combine(disk, "sounds");
                if (Directory.Exists(onDisk))
                    return onDisk;

                string cache = Path.Combine(Path.GetTempPath(), "mp2-skin-sounds", worn.Manifest.Name);
                bool any = false;
                Directory.CreateDirectory(cache);
                foreach (string name in new[] { "navigate.WAV", "click.WAV", "navigate.wav", "click.wav" })
                {
                    string target = Path.Combine(cache, Path.GetFileName(name));
                    if (File.Exists(target))
                    {
                        any = true;
                        continue;
                    }
                    Uri uri = new Uri(Join(SkinEngine.AssetsRoot, worn.Manifest.Name, "sounds", name));
                    if (!AssetLoader.Exists(uri))
                        continue;
                    using Stream source = AssetLoader.Open(uri);
                    using Stream write = File.Create(target);
                    source.CopyTo(write);
                    any = true;
                }
                return any ? cache : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>The skin's file for one cue, whatever the casing of its extension is on disk.</summary>
        private static string? FindSound(string folder, string stem)
        {
            try
            {
                foreach (string file in Directory.EnumerateFiles(folder))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    string extension = Path.GetExtension(file);
                    if (string.Equals(name, stem, StringComparison.OrdinalIgnoreCase)
                        && (extension.Equals(".wav", StringComparison.OrdinalIgnoreCase)
                            || extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)))
                        return file;
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        private static string Join(params string[] parts)
        {
            string joined = parts[0];
            for (int i = 1; i < parts.Length; i++)
                joined = joined.EndsWith("/", StringComparison.Ordinal) ? joined + parts[i] : joined + "/" + parts[i];
            return joined;
        }
    }
}
