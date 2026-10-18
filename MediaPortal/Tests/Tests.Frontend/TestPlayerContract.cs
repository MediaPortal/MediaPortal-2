using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using MediaPortal.Frontend.Player;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The parts of the player that need no engine: what a media location accepts, how a track is labelled, and
  /// where the engine is looked for.
  /// </summary>
  /// <remarks>
  /// These run everywhere, including a machine with no VLC installed and a container that never will have one.
  /// That is the point: the sentences a player produces when it cannot play are as much a product behaviour as
  /// the playing, and they must not be tested only on the developer's machine.
  /// </remarks>
  [TestFixture]
  public class TestPlayerContract
  {
    // ---- what a media location will and will not hold ----------------------------------------------

    [Test]
    public void FileLocationNeedsAnAbsolutePath()
    {
      Assert.Throws<ArgumentException>(() => MediaLocation.FromFile("clips\\silent.wav"));
      Assert.Throws<ArgumentException>(() => MediaLocation.FromFile(""));
    }

    [Test]
    public void FileLocationKeepsThePathAndIsKnownToBeAFile()
    {
      string path = Path.Combine(Path.GetTempPath(), "mp2-sample.wav");
      MediaLocation location = MediaLocation.FromFile(path);
      Assert.That(location.IsFile, Is.True);
      Assert.That(location.EngineString, Is.EqualTo(path));
      Assert.That(location.Describe(), Is.EqualTo(path));
    }

    [Test]
    public void StreamSchemesAreAcceptedAndUnknownOnesAreRefused()
    {
      Assert.That(MediaLocation.FromUrl("http://host/stream.m3u8").IsFile, Is.False);
      Assert.That(MediaLocation.FromUrl("rtsp://host/live").EngineString, Does.Contain("rtsp://"));
      Assert.That(MediaLocation.FromUrl("udp://@239.1.2.3:1234").IsFile, Is.False);
      Assert.Throws<ArgumentException>(() => MediaLocation.FromUrl("mailto:someone@example.com"));
      Assert.Throws<ArgumentException>(() => MediaLocation.FromUrl("not a url"));
    }

    [Test]
    public void AFileUrlBecomesAPathSoThatThereIsOnlyOneWayToNameAFile()
    {
      MediaLocation location = MediaLocation.FromUrl(new Uri("file:///tmp/one/two.mp4"));
      Assert.That(location.IsFile, Is.True, "a file: URI is a file, not a stream");
      Assert.That(Path.GetFileName(location.EngineString), Is.EqualTo("two.mp4"));
    }

    [Test]
    public void CredentialsNeverSurviveDescribe()
    {
      // The reason this exists is the same as on the server: what a request or a media address looks like in a log
      // is a security property, and a TV log is uploaded to a forum by somebody who never saw the password.
      MediaLocation location = MediaLocation.FromUrl("rtsp://viewer:sup3rs3cret@cam.local/live");
      Assert.That(location.Describe(), Does.Not.Contain("sup3rs3cret"));
      Assert.That(location.Describe(), Does.Not.Contain("viewer:"));
      Assert.That(location.Describe(), Does.Contain("cam.local"));
      Assert.That(location.ToString(), Does.Not.Contain("sup3rs3cret"), "ToString must not be the unsafe one");
    }

    [Test]
    public void AFileAndAUrlWithTheSameTextAreNotTheSameLocation()
    {
      // A path this platform accepts, not a POSIX-looking string: on Windows "/tmp/x" is rooted but not fully
      // qualified, and FromFile refuses it - correctly, so the test asks its question with a real path.
      string path = Path.Combine(Path.GetTempPath(), "mp2-same-text");
      Assert.That(MediaLocation.FromFile(path).Equals(MediaLocation.FromFile(path)), Is.True);
      Assert.That(MediaLocation.FromFile(path).Equals(MediaLocation.FromUrl("https://host" + "/mp2-same-text")),
          Is.False, "a path and an address are different kinds of location even with similar text");
    }

    // ---- how the engine is found --------------------------------------------------------------------

    [Test]
    public void AConfiguredFolderWithoutTheEngineIsRefusedByName()
    {
      string folder = PlayerFixture.EmptyDirectory();
      bool found = NativeLibVlc.TryFind(folder, out string? directory, out string detail);
      Assert.That(found, Is.False, "an empty folder is not libVLC");
      Assert.That(directory, Is.Null);
      Assert.That(detail, Does.Contain(folder));
      foreach (string name in NativeLibVlc.LibraryFileNames)
        Assert.That(detail, Does.Contain(name), "the sentence must say what it looked for");
    }

    [Test]
    public void AFolderIsAcceptedWhenItHoldsTheLibraryThisPlatformWants()
    {
      string[] names = new List<string>(NativeLibVlc.LibraryFileNames).ToArray();
      Assert.That(names, Is.Not.Empty, "every platform that searches must search for something");
      string folder = PlayerFixture.FakeEngineDirectory(names[0]);
      Assert.That(NativeLibVlc.ContainsEngine(folder, out string? file), Is.True);
      Assert.That(file, Is.EqualTo(names[0]));
      Assert.That(NativeLibVlc.TryFind(folder, out string? directory, out string _), Is.True);
      Assert.That(directory, Is.EqualTo(folder));
    }

    [Test]
    public void TheEnvironmentVariableIsUsedWhenNoFolderIsConfigured()
    {
      string folder = PlayerFixture.FakeEngineDirectory(
          new List<string>(NativeLibVlc.LibraryFileNames).ToArray()[0]);
      Environment.SetEnvironmentVariable(NativeLibVlc.EnvironmentVariableName, folder);
      try
      {
        Assert.That(NativeLibVlc.TryFind(null, out string? directory, out string detail), Is.True);
        Assert.That(directory, Is.EqualTo(folder));
        Assert.That(detail, Does.Contain("configured"));
      }
      finally
      {
        Environment.SetEnvironmentVariable(NativeLibVlc.EnvironmentVariableName, null);
      }
    }

    [Test]
    public void TheSearchPlacesAreThePlacesThisPlatformPutsVLC()
    {
      IReadOnlyList<string> directories = NativeLibVlc.DefaultDirectories();
      TestContext.Progress.WriteLine($"  libVLC search path for this platform: {string.Join(", ", directories)}");
      if (OperatingSystem.IsWindows())
      {
        Assert.That(directories, Is.Not.Empty);
        Assert.That(directories[0], Does.EndWith(Path.Combine("VideoLAN", "VLC")));
      }
      else if (OperatingSystem.IsLinux())
      {
        Assert.That(directories, Does.Contain("/usr/lib"));
      }
    }

    [Test]
    public void ADirectoryFromAnotherPlatformIsNotMistakenForEngineFolder()
    {
      // Windows looks for libvlc.dll and Linux for libvlc.so.5; a folder holding only the other platform's file
      // must be refused, or a mounted Linux root would half-initialise a player on Windows.
      string otherPlatformFile = OperatingSystem.IsWindows() ? "libvlc.so.5" : "libvlc.dll";
      string folder = PlayerFixture.FakeEngineDirectory(otherPlatformFile);
      Assert.That(NativeLibVlc.ContainsEngine(folder, out string? file), Is.False);
      Assert.That(file, Is.Null);
    }

    // ---- tracks, labels, and the shape of options ---------------------------------------------------

    [Test]
    public void ATrackLabelPrefersLanguageThenDescriptionThenId()
    {
      Assert.That(new PlaybackTrack(TrackKind.Audio, 3, "Deutsch", "deu").Label, Is.EqualTo("deu - Deutsch"));
      Assert.That(new PlaybackTrack(TrackKind.Audio, 3, "Deutsch").Label, Is.EqualTo("Deutsch"));
      Assert.That(new PlaybackTrack(TrackKind.Audio, 3).Label, Is.EqualTo("3"));
      Assert.That(new PlaybackTrack(TrackKind.Subtitle, -1, "  ").Label, Is.EqualTo("-1"),
          "blank whitespace must not become a label");
    }

    [Test]
    public void TracksOfDifferentKindsWithTheSameIdAreNotEqual()
    {
      Assert.That(new PlaybackTrack(TrackKind.Audio, 1).Equals(new PlaybackTrack(TrackKind.Subtitle, 1)), Is.False,
          "libVLC numbers audio and subtitle ids independently; -1 means off in both");
    }

    [Test]
    public void HeadlessAsksForTheOutputsThatNeedNothing()
    {
      // Asserted against the real list, not against the flag's name: "headless" that still opens a window or an
      // audio device is the failure a container would hit, and it would be reported as a hang rather than a bug.
      Assert.That(PlayerOptions.HeadlessArguments, Does.Contain("--vout=dummy"));
      Assert.That(PlayerOptions.HeadlessArguments, Does.Contain("--aout=dummy"));
      Assert.That(new PlayerOptions().Headless, Is.False, "a normal player must not be headless by default");
    }

    [Test]
    public void TheEngineLanguagePatternIsAPatternAndNotAGuess()
    {
      Assert.That(PlaybackTrack.ParseLanguageFromEngineName("Language (eng)"), Is.EqualTo("eng"));
      Assert.That(PlaybackTrack.ParseLanguageFromEngineName(" language (DEU) "), Is.EqualTo("DEU"));
      Assert.That(PlaybackTrack.ParseLanguageFromEngineName("Deutsch"), Is.Null);
      Assert.That(PlaybackTrack.ParseLanguageFromEngineName("Language ()"), Is.Null);
      Assert.That(PlaybackTrack.ParseLanguageFromEngineName("Language (english)"), Is.Null,
          "more than three letters is not a code this code understands");
      Assert.That(PlaybackTrack.ParseLanguageFromEngineName(null), Is.Null);
    }

    [Test]
    public void AFailureAlwaysCarriesASentence()
    {
      Assert.Throws<ArgumentException>(() => new PlayerFailedEventArgs("  "));
      Assert.That(new PlayerFailedEventArgs("no engine").ToString(), Is.EqualTo("no engine"));
    }
  }
}
