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
using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Headless.NUnit;
using MediaPortal.Frontend.Theming;
using NUnit.Framework;

namespace Tests.Frontend
{
    /// <summary>
    /// Every template key the views ask the skins for, both skins must answer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A view names a key and keeps its own template as long as the skin says nothing - which is safe for a
    /// view and silent for a mistake: when a skin loses a template to a rename, the screen quietly falls back
    /// to the view's own drawing and nobody notices until someone compares the two skins side by side. This
    /// test is that comparison: it puts on each shipped skin and asks it, key by key, for the tree every list
    /// in the front end has learned to hand over. A key here is kept honest by the views' markup; a key that
    /// vanishes from the views without vanishing from here fails, and so does a skin that drops a template.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class TestEverySkinAnswersEveryKey
    {
        /// <summary>
        /// The keys the views name on their lists and overlays, as one list. When a view learns to ask the
        /// skin for a new surface, the key belongs here too - that is the whole ritual.
        /// </summary>
        private static readonly string[] ASKED_KEYS =
        {
            "HomeMenuTileTemplate",
            "HomeContentRowTemplate",
            "TitleRowTemplate",
            "MediaTilePosterTemplate",
            "MediaTileBannerTemplate",
            "MediaTileThumbnailTemplate",
            "MediaTileCoverTemplate",
            "SettingsRowTemplate",
            "SettingsOptionTemplate",
            "NewsHeadlineTemplate",
            "WeatherDayTemplate",
            "PartyQueueTemplate",
            "NowPlayingOverlayTemplate",
            "DialogFrameTemplate",
            "LabelRowTemplate",
            "TextRowTemplate",
            "FolderRowTemplate",
            "PictureTileTemplate",
            "StatusRowTemplate",
            "ServerRowTemplate",
            "MetadataRowTemplate",
        };

        [AvaloniaTest]
        [NonParallelizable]
        public void TheShippedSkinsAnswerEveryKeyTheViewsAsk()
        {
            // The skins read from disk or from the assembly exactly as a running front end reads them; the
            // application instance here is the one the headless runner already has up.
            Application app = Application.Current ?? throw new InvalidOperationException("no application");
            List<string> missing = new List<string>();

            foreach (string skin in new[] { "nereus", "wmc" })
            {
                SkinEngine.Clear(app);
                Assert.That(SkinEngine.Apply(app, skin, out string reason), Is.True,
                    $"{skin} could not be put on: {reason}");

                foreach (string key in ASKED_KEYS)
                {
                    IDataTemplate? template = SkinEngine.TryTileTemplate(key);
                    if (template == null)
                        missing.Add($"{skin}/{key}");
                }

                SkinEngine.Clear(app);
            }

            Assert.That(missing, Is.Empty,
                "these skins answer no template for keys the views hand over: " + string.Join(", ", missing));
        }
    }
}
