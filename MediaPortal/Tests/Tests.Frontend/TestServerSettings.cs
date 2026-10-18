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
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;

namespace Tests.Frontend
{
  /// <summary>
  /// The file the front end remembers the server's address with, on the platform the tests happen to run on.
  /// </summary>
  /// <remarks>
  /// These are the only tests of the front end that touch the disk for a reason of their own, so they are also the
  /// place where the two claims of that file are checked: that losing it is a first run rather than a crash, and that
  /// being interrupted while writing it does not destroy the address that was in it. Both are about what happens when
  /// a media centre is switched off, which happens constantly.
  /// </remarks>
  [TestFixture]
  public class TestServerSettings
  {
    private string _root = "";

    [SetUp]
    public void SetUp()
    {
      _root = Path.Combine(Path.GetTempPath(), "mp2-settings-tests-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
      try
      {
        Directory.Delete(_root, true);
      }
      catch (IOException)
      {
        // Nothing in here matters after the test; the folder is named after a GUID.
      }
    }

    private string PathCalled(string name)
    {
      return Path.Combine(_root, name, "frontend.json");
    }

    [Test]
    public void AFileThatWasNeverWrittenIsAFirstRunAndNotAProblem()
    {
      ServerSettings settings = ServerSettings.Load(PathCalled("absent"), out string? problem);

      Assert.That(problem, Is.Null, "a first start must not be reported as a fault");
      Assert.That(settings.ServerUrl, Is.Empty);
      Assert.That(settings.HasUsableUrl, Is.False);
    }

    [Test]
    public void AnAddressSurvivesBeingSavedAndReadBack()
    {
      string file = PathCalled("roundtrip");
      string message = new ServerSettings { ServerUrl = "http://192.168.4.4:55555/MediaPortal/Server" }
          .Save(file, out bool saved);

      Assert.That(saved, Is.True, message);
      ServerSettings read = ServerSettings.Load(file, out string? problem);
      Assert.That(problem, Is.Null);
      Assert.That(read.ServerUrl, Is.EqualTo("http://192.168.4.4:55555/MediaPortal/Server"));
    }

    [Test]
    public void SavingCreatesTheFolderItNeeds()
    {
      // The file name is fixed by the platform, but the folder in front of it does not exist on a fresh
      // installation; failing there would mean the front end can never be configured from a clean system.
      string file = PathCalled("deeply/nested/first/time");
      new ServerSettings { ServerUrl = "http://box/MediaPortal/Server" }.Save(file, out bool saved);

      Assert.That(saved, Is.True);
      Assert.That(File.Exists(file), Is.True);
    }

    [Test]
    public void AnEmptyFileIsAlsoAFirstRun()
    {
      string file = PathCalled("empty");
      Directory.CreateDirectory(Path.GetDirectoryName(file)!);
      File.WriteAllText(file, "");

      ServerSettings settings = ServerSettings.Load(file, out string? problem);
      Assert.That(problem, Is.Null, "a file truncated by a power cut is not a reason to complain about settings");
      Assert.That(settings.ServerUrl, Is.Empty);
    }

    [Test]
    public void AFileThatIsNotSettingsIsSaidAboutAndCarriedOnPast()
    {
      string file = PathCalled("broken");
      Directory.CreateDirectory(Path.GetDirectoryName(file)!);
      File.WriteAllText(file, "this is not json, and it has a } in the middle");

      ServerSettings settings = ServerSettings.Load(file, out string? problem);

      Assert.That(problem, Is.Not.Null, "somebody has to be told that the saved address could not be read");
      Assert.That(problem, Does.Contain(file), "and told which file is the problem");
      Assert.That(problem, Does.Contain("using the defaults"));
      Assert.That(settings.HasUsableUrl, Is.False, "the front end still starts, with nothing configured");
    }

    [Test]
    public void ASettingsFileWrittenByHandIsReadToo()
    {
      // The file is in the user's folder precisely so that it can be edited there - over ssh, on a machine with no
      // keyboard attached to the television. Comments and a trailing comma are how a person writes JSON.
      string file = PathCalled("byhand");
      Directory.CreateDirectory(Path.GetDirectoryName(file)!);
      File.WriteAllText(file, "{\n  // the box under the telly\n  \"server_url\": \"http://telly-box.local:55555/MediaPortal/Server\",\n}");

      ServerSettings settings = ServerSettings.Load(file, out string? problem);

      Assert.That(problem, Is.Null, "a hand-written file is allowed: " + problem);
      Assert.That(settings.ServerUrl, Is.EqualTo("http://telly-box.local:55555/MediaPortal/Server"));
    }

    [Test]
    public void OnlyHttpAndHttpsAreAddresses()
    {
      Assert.That(new ServerSettings { ServerUrl = "http://box/MediaPortal/Server" }.HasUsableUrl, Is.True);
      Assert.That(new ServerSettings { ServerUrl = "https://box.example/MediaPortal/Server" }.HasUsableUrl, Is.True);
      Assert.That(new ServerSettings { ServerUrl = "" }.HasUsableUrl, Is.False);
      Assert.That(new ServerSettings { ServerUrl = "box:55555" }.HasUsableUrl, Is.False, "no scheme is no address");
      Assert.That(new ServerSettings { ServerUrl = "file:///C:/MediaPortal/server" }.HasUsableUrl, Is.False,
          "a path on this machine is not a server to talk to");
      Assert.That(new ServerSettings { ServerUrl = "ftp://box/media" }.HasUsableUrl, Is.False,
          "the API speaks http; anything else would fail later and less clearly");
    }

    [Test]
    public void AFailedSaveSaysSoInsteadOfThrowing()
    {
      // The caller is a button on a screen. It can show a sentence; it cannot show an exception, and it must not
      // pretend the address was remembered when it was not.
      string blockingFile = Path.Combine(_root, "intheway");
      File.WriteAllText(blockingFile, "not a folder");
      string file = Path.Combine(blockingFile, "sub", "frontend.json");

      string message = new ServerSettings { ServerUrl = "http://box/MediaPortal/Server" }.Save(file, out bool saved);

      Assert.That(saved, Is.False, "the folder cannot be created, so nothing was saved");
      Assert.That(message, Does.StartWith("Could not be saved"));
      Assert.That(message, Does.Contain(": "), "the reason has to be in the sentence, not only in the exception");
    }

    [Test]
    public void BeingInterruptedWhileSavingLeavesTheOldAddressInPlace()
    {
      // The claim in the comment on ServerSettings: the new file is written next to the old one and moved over it.
      // The interruption itself cannot be simulated here, but what proves the mechanism is that no half-written file
      // is left behind and the old content survives a save that never got to run.
      string file = PathCalled("atomic");
      new ServerSettings { ServerUrl = "http://old-server/MediaPortal/Server" }.Save(file, out bool first);
      Assert.That(first, Is.True);

      // A second save that cannot work: same folder, but the destination is now a directory rather than a file.
      string blocking = file + ".new";
      Directory.CreateDirectory(blocking);
      new ServerSettings { ServerUrl = "http://new-server/MediaPortal/Server" }.Save(file, out bool second);

      Assert.That(second, Is.False);
      ServerSettings stillThere = ServerSettings.Load(file, out string? problem);
      Assert.That(problem, Is.Null);
      Assert.That(stillThere.ServerUrl, Is.EqualTo("http://old-server/MediaPortal/Server"),
          "a save that failed must not have taken the working address with it");
    }

    [Test]
    public void TheFileIsWhereThisPlatformPutsItsUserData()
    {
      string file = ServerSettings.DefaultFilePath;

      Assert.That(Path.IsPathRooted(file), Is.True,
          "an application that is started from a service or a desktop launcher has no idea what the current folder is");
      Assert.That(file, Does.EndWith("frontend.json"));
      Assert.That(file, Does.Contain(ServerSettings.SETTINGS_DIRECTORY_NAME));
      Assert.That(file, Does.Not.Contain("HKEY"), "the old client kept this in the registry; this one keeps a file");
    }

    /// <summary>
    /// The four places the address can come from, in the order that wins.
    /// </summary>
    /// <remarks>
    /// The third assertion is the one the container image paid for: it exports <c>MP2_SERVER</c> so that its own
    /// render step draws a populated screen, and a test that left the environment out of the question asked the
    /// image where it would go instead of asking the rule. On Linux that test failed, on Windows it passed, and
    /// neither of them had said what it meant.
    /// </remarks>
    [Test]
    public void TheAddressThatWinsIsTheOneNearestThePersonWhoStartedIt()
    {
      const string ARGUMENT = "http://10.0.0.1:55555/MediaPortal/Server/";
      const string ENVIRONMENT = "http://10.0.0.2:55555/MediaPortal/Server/";
      const string SAVED = "http://10.0.0.3:55555/MediaPortal/Server/";

      Assert.That(ServerApiClient.ResolveBaseUrl(ARGUMENT, ENVIRONMENT, SAVED), Is.EqualTo(new Uri(ARGUMENT)),
          "the person who typed the command meant it");
      Assert.That(ServerApiClient.ResolveBaseUrl(null, ENVIRONMENT, SAVED), Is.EqualTo(new Uri(ENVIRONMENT)),
          "an operator's environment outranks a decision taken on the screen months ago");
      Assert.That(ServerApiClient.ResolveBaseUrl(null, null, SAVED), Is.EqualTo(new Uri(SAVED)),
          "with nobody else saying anything, the server the viewer picked is the address");
      Assert.That(ServerApiClient.ResolveBaseUrl(null, null, null), Is.EqualTo(new Uri(ServerApiClient.DefaultUrl)),
          "and the last one is a guess, which is why all three others beat it");
      Assert.That(ServerApiClient.ResolveBaseUrl("   ", "  ", SAVED), Is.EqualTo(new Uri(SAVED)),
          "an all-blank entry is nobody saying anything, not an address made of spaces");
    }
  }
}
