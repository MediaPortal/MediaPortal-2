// Tests.Frontend/TestSkinSettingsLive.cs: the skin-settings screen is a remote control, not an
// application form. What the viewer flips there must show on the television in the same moment, not
// "when the screen next opens" - a shelf that opened under the old answers rereads itself the instant
// the settings file is written. This guards the wire between the two: the Saved event of the settings
// file and the live shelf that listens to it.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Avalonia;
using Avalonia.Headless.NUnit;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using NUnit.Framework;

namespace Tests.Frontend
{
    /// <summary>
    /// What the settings screen writes, the living shelf obeys - fanart, its dimming and the watched
    /// corners all follow the flip without the shelf being opened again.
    /// </summary>
    [TestFixture]
    public class TestSkinSettingsLive
    {
        [AvaloniaTest]
        public void WhatTheSettingsScreenWritesTheLiveShelfFollowsAtOnce()
        {
            string wasTheme = FrontendTheme.CurrentName(Application.Current!);
            string previousDir = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR") ?? "";
            string isolated = Path.Combine(Path.GetTempPath(), "mp2-settings-live-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", isolated);
            try
            {
                FrontendTheme.Apply(Application.Current!, "nereus", out _);

                // The shelf opens while Nereus is worn and the fanart is wanted.
                SkinSettingsBase opening = SkinSettingsBase.LoadFor("nereus", null, out _) ?? new NereusSkinSettings();
                opening.EnableFanart = true;
                opening.FanartOverlayOpacity = 0.5;
                opening.EnableListWatchedFlags = true;
                opening.EnableGridWatchedFlags = true;
                opening.EnableCoverWatchedFlags = true;
                opening.Save(null, out _);

                MediaBrowserViewModel shelf = new MediaBrowserViewModel(null, (HomeSection?)null);
                Assert.That(shelf.FanartShown, Is.True);
                Assert.That(shelf.FanartOverlay, Is.EqualTo(0.5));
                Assert.That(shelf.WatchFlagsShown, Is.True);

                List<string?> changes = new List<string?>();
                ((INotifyPropertyChanged)shelf).PropertyChanged += delegate (object? s, PropertyChangedEventArgs e)
                {
                    changes.Add(e.PropertyName);
                };

                // The viewer flips the switches on the settings screen - one save, no reopening.
                SkinSettingsBase flipping = SkinSettingsBase.LoadFor("nereus", null, out _) ?? new NereusSkinSettings();
                flipping.EnableFanart = false;
                flipping.EnableListWatchedFlags = false;
                flipping.EnableGridWatchedFlags = false;
                flipping.EnableCoverWatchedFlags = false;
                flipping.Save(null, out _);

                Assert.That(shelf.FanartShown, Is.False, "fanart after flip");
                Assert.That(shelf.WatchFlagsShown, Is.False, "corners after flip");
                // The shelf said "read all of them again", which is how a bound layer learns of a
                // picture that has just been taken away.
                Assert.That(changes.Exists(delegate (string? name) { return name == null; }), Is.True,
                    "changes: " + string.Join(",", changes.ConvertAll(delegate (string? n) { return n ?? "<all>"; })));

                // And back on again, because a remote control works in both directions.
                SkinSettingsBase back = SkinSettingsBase.LoadFor("nereus", null, out _) ?? new NereusSkinSettings();
                back.EnableFanart = true;
                back.FanartOverlayOpacity = 0.25;
                back.Save(null, out _);
                Assert.That(shelf.FanartShown, Is.True);
                Assert.That(shelf.FanartOverlay, Is.EqualTo(0.25));
            }
            finally
            {
                FrontendTheme.Apply(Application.Current!, wasTheme, out _);
                Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", previousDir.Length == 0 ? null : previousDir);
            }
        }
    }
}
