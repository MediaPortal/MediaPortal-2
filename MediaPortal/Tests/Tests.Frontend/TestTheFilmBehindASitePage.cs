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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MediaPortal.Frontend.Services;

namespace Tests.Frontend
{
  /// <summary>
  /// The film behind the page of a video site. A trailer tagged by a metadata tool names a page, and a page is not
  /// a film - the player is handed a 303 byte web page instead of a picture and runs out of it at once. These are
  /// the two questions this has to answer before anything is played: is this address such a page, and what is the
  /// tool asked for the film behind it.
  /// </summary>
  /// <remarks>
  /// The answers here are the measured ones: the plain <c>tv</c> player client hands the player an address that
  /// answers HTTP 403, while <c>android</c> answers HTTP 206 and plays, both measured on a machine with VLC 3.0.20
  /// as the player. No test reaches out to a film site - the one test that asks the installed tool something asks
  /// it about its own command line only, which is answered on the machine.
  /// </remarks>
  [TestFixture]
  public class TestTheFilmBehindASitePage
  {
    private string _savedToolPath;

    [SetUp]
    public void SaveTheToolSetting()
    {
      _savedToolPath = Environment.GetEnvironmentVariable("MP2_YTDLP");
    }

    [TearDown]
    public void RestoreTheToolSetting()
    {
      Environment.SetEnvironmentVariable("MP2_YTDLP", _savedToolPath);
    }

    [Test]
    public void APagesAddressIsToldApartFromAFilmsAddress()
    {
      Assert.That(VideoSiteStream.IsSitePage("https://www.youtube.com/watch?v=bwzLiQZDw2I"), Is.True,
          "the address a metadata tool tags is a page");
      Assert.That(VideoSiteStream.IsSitePage("https://youtu.be/bwzLiQZDw2I"), Is.True,
          "the short form names the same film");
      Assert.That(VideoSiteStream.IsSitePage("https://m.youtube.com/watch?v=bwzLiQZDw2I"), Is.True,
          "the phone form names the same film");
      Assert.That(VideoSiteStream.IsSitePage("https://www.youtube.com/shorts/0F2CBEa13Vw"), Is.True,
          "a short is a film of its own");

      Assert.That(VideoSiteStream.IsSitePage("https://tv.apple.com/us/movie/x/mcm.110245417229"), Is.False,
          "a site nobody resolves here is left to the player, which says in its own words what it made of it");
      Assert.That(VideoSiteStream.IsSitePage(
              "https://www.filmtrailer.com/media_flash/58048_the_mummy_2017.mp4"), Is.False,
          "an address that already is a film is handed over as it stands");
      Assert.That(VideoSiteStream.IsSitePage(@"\\TIF-NAS\Filme\trailer.mp4"), Is.False,
          "a file on the shelf is no page");
      Assert.That(VideoSiteStream.IsSitePage("plugin://plugin.video.youtube/play/?video_id=VWqJifMMgZE"), Is.False,
          "a Kodi tag is turned into a page earlier, and is not asked of the resolver twice");
      Assert.That(VideoSiteStream.IsSitePage("https://www.youtube.com/channel/UCphXwsqUz0XxCl6qll74THw"), Is.False,
          "a channel names no film, and a resolver would answer with whatever the page happens to lead to");
      Assert.That(VideoSiteStream.IsSitePage("https://www.youtube.com/results?search_query=mummy"), Is.False,
          "a search page names no film either");
      Assert.That(VideoSiteStream.IsSitePage(null), Is.False, "nothing at all is no page");
      Assert.That(VideoSiteStream.IsSitePage(""), Is.False, "an empty address is no page");
    }

    [Test]
    public void TheFilmIsNamedByItsIdWhereverTheSitePutsIt()
    {
      Assert.That(VideoSiteStream.VideoIdOf("https://www.youtube.com/watch?v=bwzLiQZDw2I"), Is.EqualTo("bwzLiQZDw2I"));
      Assert.That(VideoSiteStream.VideoIdOf("https://www.youtube.com/watch?v=bwzLiQZDw2I&t=30"), Is.EqualTo("bwzLiQZDw2I"),
          "a page opened at half past a minute still names one film");
      Assert.That(VideoSiteStream.VideoIdOf("https://www.youtube.com/watch?app=desktop&v=bwzLiQZDw2I"), Is.EqualTo("bwzLiQZDw2I"),
          "the id is the id whatever stands before it");
      Assert.That(VideoSiteStream.VideoIdOf("https://youtu.be/bwzLiQZDw2I"), Is.EqualTo("bwzLiQZDw2I"));
      Assert.That(VideoSiteStream.VideoIdOf("https://youtu.be/bwzLiQZDw2I?si=lyxKfwSsLtw8FrZT"), Is.EqualTo("bwzLiQZDw2I"),
          "the share link carries a tracking tail behind the id");
      Assert.That(VideoSiteStream.VideoIdOf("https://www.youtube.com/shorts/0F2CBEa13Vw"), Is.EqualTo("0F2CBEa13Vw"));

      Assert.That(VideoSiteStream.VideoIdOf("https://youtu.be/"), Is.Null,
          "a site's front page names no film");
      Assert.That(VideoSiteStream.VideoIdOf("https://www.youtube.com/watch?v="), Is.Null,
          "an id that is not there is not a film");
      Assert.That(VideoSiteStream.VideoIdOf(@"\\TIF-NAS\Filme\trailer.mp4"), Is.Null, "a file names no film of a site");
    }

    [Test]
    public void TheToolIsAskedForOneAddressHoldingPictureAndSound()
    {
      string[] args = VideoSiteStream.BuildArguments("https://www.youtube.com/watch?v=bwzLiQZDw2I", "android");

      Assert.That(args, Does.Contain("--get-url"), "only the address is wanted, nothing is downloaded here");
      Assert.That(args, Does.Contain("--no-playlist"),
          "a trailer page can be the first entry of a playlist, and a playlist is several answers where one was asked for");
      Assert.That(args, Does.Contain("youtube:player_client=android"),
          "the plain client's address is refused by the site with HTTP 403 while this one is played");
      int atFormat = Array.IndexOf(args, "-f");
      Assert.That(atFormat, Is.GreaterThanOrEqualTo(0), "the shape of the answer is asked for");
      Assert.That(args[atFormat + 1], Is.EqualTo(VideoSiteStream.WantedFormat));
      Assert.That(VideoSiteStream.WantedFormat, Does.Contain("acodec!=none").And.Contain("vcodec!=none"),
          "two separate streams are of no use to a media player, which can be handed one address");
      Assert.That(args[args.Length - 1], Is.EqualTo("https://www.youtube.com/watch?v=bwzLiQZDw2I"),
          "the page is asked for last, the way the tool wants it");
    }

    [Test]
    public void ThreeSizesAreOfferedAndEachLineCarriesItsSizeAlone()
    {
      TrailerQuality[] qualities = VideoSiteStream.Qualities;

      // The table is the measured one, and it is the answer the dialog shows: smallest first, because a viewer
      // who does not think about it presses the top line and gets the film that starts without a wait.
      Assert.That(qualities, Has.Length.EqualTo(3), "three sizes, from the one that starts at once");
      Assert.That(qualities[0].Word, Is.EqualTo("360p"), "the top line is the size that needs no fetching");
      Assert.That(qualities[0].Muxed, Is.False,
          "the smallest size is an address the player is handed as it stands");
      Assert.That(qualities[0].PatienceSeconds, Is.EqualTo(0), "and is asked with no more patience than a lookup");
      Assert.That(qualities[0].Display, Is.EqualTo("360p"), "the line carries its size and nothing else");

      Assert.That(qualities[1].Word, Is.EqualTo("720p"),
          "the next line promises a size and does not promise more than it can keep");
      Assert.That(qualities[1].Muxed, Is.True,
          "above the smallest size the picture and the sound come as two pieces and have to be put together");

      // A choice says which picture it is and stops there. Whether the film has to be brought over first is the
      // machine's business and is said by the dialog that waits, not by a word appended to a size - and a number
      // of seconds on a line would be a promise this front end cannot keep, since how long a fetch takes depends
      // on the line the film comes over.
      foreach (TrailerQuality quality in qualities)
      {
        Assert.That(quality.Display, Is.EqualTo(quality.Word),
            "the line is the size, word for word: " + quality.Display);
        Assert.That(quality.Display, Does.Not.Match(@"\d+ ?s"),
            "no line promises seconds it cannot keep: " + quality.Display);
      }

      // How long the tool may think belongs to the size and is never shown: a lookup is done in a second or two,
      // while 1080p is 82 MiB on the measured trailer and has to be left alone until it is here.
      Assert.That(qualities[2].PatienceSeconds, Is.GreaterThan(qualities[1].PatienceSeconds),
          "the bigger the picture, the longer the tool is left to bring it");
      Assert.That(qualities[1].PatienceSeconds * 1000, Is.GreaterThan(VideoSiteStream.ResolveTimeoutMs),
          "a fetched size is not called off by the 20 s a lookup needs");

      // The player client belongs to the size: measured, the android client is the one whose address this
      // machine's player is answered with (HTTP 206, and it plays), while the shapes above 360p are only offered
      // to the embedded web client at all.
      Assert.That(qualities[0].PlayerClient, Is.EqualTo("android"),
          "the size that starts at once is asked of the client that was measured to answer");
      Assert.That(qualities[1].PlayerClient, Is.EqualTo(qualities[2].PlayerClient),
          "the two fetched sizes are asked of one and the same client");

      foreach (TrailerQuality quality in qualities)
      {
        Assert.That(quality.Format, Does.Contain("protocol=https"),
            "no size is ever asked for as a manifest, which this engine cannot open: " + quality.Word);
        Assert.That(quality.Format, Does.Contain("acodec"),
            "and no size is ever asked for without saying something about its sound: " + quality.Word);
      }
    }

    [Test]
    public void ASizeBringsItsOwnCommandAndTheFileItWasAskedFor()
    {
      TrailerQuality fetched = VideoSiteStream.Qualities[1];
      string[] args = VideoSiteStream.BuildArguments("https://www.youtube.com/watch?v=bwzLiQZDw2I", fetched,
          Path.Combine(Path.GetTempPath(), "MediaPortal", "trailer-bwzLiQZDw2I.mp4"));

      Assert.That(args[Array.IndexOf(args, "-f") + 1], Does.Contain("height<=720"),
          "the size's own shape stands in the place the shared command has for it, not the default one");
      Assert.That(args, Does.Contain("-o"), "a fetched size says where the film is to be written");
      Assert.That(args[Array.IndexOf(args, "-o") + 1], Does.EndWith("trailer-bwzLiQZDw2I.mp4"),
          "to the file that was asked for");
      Assert.That(args, Does.Contain("--no-part"),
          "and not half-written under a second name, since the caller looks for exactly the file it named");

      // The one thing that cannot be left in the line here: the shared command asks for the address only, and a
      // tool that answers an address never fetches the film - exit code nought, no file, and a viewer standing in
      // front of "the resolver said it was done, but the film is not there".
      Assert.That(args, Has.No.Member("--get-url"),
          "a fetched size asks for the film itself, not for the address of it");

      // And the container, because the picture and the sound of a big size often come in different ones; a merge
      // that has to pick a container picks a name too, and the caller looks for the name it gave.
      Assert.That(args[Array.IndexOf(args, "--merge-output-format") + 1], Is.EqualTo("mp4"),
          "the film is put together in the container the file name promises");

      // A size that is only a lookup must not leave a file lying around, so it must not be asked for one.
      string[] looked = VideoSiteStream.BuildArguments("https://www.youtube.com/watch?v=bwzLiQZDw2I",
          VideoSiteStream.Qualities[0], null);
      Assert.That(looked, Has.No.Member("-o"), "a looked-up size is handed to the player as an address");
      Assert.That(looked, Does.Contain("--get-url"), "and so it is the address that is asked of the tool");
      Assert.That(looked[Array.IndexOf(looked, "-f") + 1], Is.EqualTo(VideoSiteStream.WantedFormat),
          "and is asked for the one shape that carries picture and sound together");
    }

    [Test]
    public void AFetchedFilmGoesToOneFileNamedAfterItself()
    {
      string file = VideoSiteStream.TrailerFileOf("https://www.youtube.com/watch?v=bwzLiQZDw2I");

      Assert.That(file, Does.EndWith(Path.Combine("MediaPortal", "trailer-bwzLiQZDw2I.mp4")),
          "one film is one file, in this front end's own corner of the temp folder");
      Assert.That(file, Does.StartWith(Path.GetTempPath()),
          "and the corner is under the temporary folder, where a machine may be cleaned at any time");

      // The same film through another spelling of its address has to land in the same file: two entries of one
      // title in one library are commonly tagged differently, and nobody wants the same film twice.
      Assert.That(VideoSiteStream.TrailerFileOf("https://m.youtube.com/watch?v=bwzLiQZDw2I"), Is.EqualTo(file),
          "a short address is the same film");
      Assert.That(VideoSiteStream.TrailerFileOf("https://www.youtube.com/watch?v=dQw4w9WgXcQ"),
          Is.Not.EqualTo(file), "but a different film is a different file");
    }

    [Test]
    public void OnlyAFetchedFilmOfItsOwnAccordGoesAwayAtStartup()
    {
      string folder = Path.Combine(Path.GetTempPath(), "MediaPortal");
      Directory.CreateDirectory(folder);
      string old = Path.Combine(folder, "trailer-alt-00000000.mp4");
      string fresh = Path.Combine(folder, "trailer-frisch-00000000.mp4");
      string other = Path.Combine(folder, "was-anderes.mp4");
      try
      {
        File.WriteAllText(old, "alt");
        File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-2));
        File.WriteAllText(fresh, "frisch");
        File.WriteAllText(other, "anderes");

        int gone = VideoSiteStream.ForgetOldTrailers();

        Assert.That(File.Exists(old), Is.False, "a film a previous run brought over and never watched goes");
        Assert.That(gone, Is.EqualTo(1),
            "and exactly one goes, which is the one that was old: " + gone);
        Assert.That(File.Exists(fresh), Is.True,
            "a film that was brought over a minute ago may still be the one on screen");
        Assert.That(File.Exists(other), Is.True,
            "and nothing in the corner that this front end did not name is touched");
      }
      finally
      {
        foreach (string left in new[] { old, fresh, other })
          if (File.Exists(left))
            File.Delete(left);
      }
    }

    [Test]
    public void AToolKeptSomewhereOfItsOwnIsFoundByThatName()
    {
      string own = Path.Combine(Path.GetTempPath(), "yt-dlp-" + Guid.NewGuid().ToString("N") + ".exe");
      File.WriteAllBytes(own, new byte[] { 0x4D, 0x5A });
      try
      {
        Environment.SetEnvironmentVariable("MP2_YTDLP", "\"" + own + "\"");
        Assert.That(VideoSiteStream.FindTool(), Is.EqualTo(own),
            "a machine that keeps the tool off the PATH should not have to change its PATH");
      }
      finally
      {
        File.Delete(own);
      }
    }

    [Test]
    public void AToolThatIsNotThereIsSaidSoInsteadOfSilentlyFailing()
    {
      bool done = VideoSiteStream.TryPlayAddress("https://www.youtube.com/watch?v=bwzLiQZDw2I",
          VideoSiteStream.Qualities[0], out string stream, out string file, out string whyNot,
          Path.Combine(Path.GetTempPath(), "not-here-" + Guid.NewGuid().ToString("N") + ".exe"));

      Assert.That(done, Is.False, "a tool that cannot be run cannot find a film");
      Assert.That(stream, Is.Empty, "and nothing is handed to the player");
      Assert.That(file, Is.Empty, "nor is any file written by such a road");
      Assert.That(whyNot, Is.Not.Empty, "the viewer is told why in one sentence rather than left at a black screen");
    }

    /// <summary>
    /// The one test that asks the installed tool something, and it asks it about its own command line only: an
    /// option the tool does not know is answered before any address is looked at, so nothing is fetched. Asked
    /// this way rather than by reading <c>--help</c>, because the tool wraps its help text and a wrapped option
    /// name is not in it.
    /// </summary>
    [Test]
    public void TheToolKnowsEveryOptionItIsAsked()
    {
      string? tool = VideoSiteStream.FindTool();
      if (tool == null)
      {
        Assert.Ignore("this machine has no yt-dlp, so its command line cannot be asked here");
        return;
      }

      // Every command line this front end is ever going to run, from the size the trailer starts with to the
      // biggest one it might fetch: an option invented for one of them would else only ever be noticed by a
      // viewer standing in front of a screen that shows nothing.
      foreach (TrailerQuality quality in VideoSiteStream.Qualities)
      {
        string page = "https://www.youtube.com/watch?v=bwzLiQZDw2I";
        string[] asked = VideoSiteStream.BuildArguments(page, quality,
            quality.Muxed ? Path.Combine(Path.GetTempPath(), "mp2-test-trailer.mp4") : null);

        // The page is what is left out - not "the last argument", since a fetched size puts its file behind the
        // page and dropping the last one would leave the page in and have the tool resolve a real film.
        string[] withoutPage = asked.Where(argument => argument != page).ToArray();

        string answer = AskTheTool(tool, withoutPage);
        Assert.That(answer, Does.Not.Contain("no such option"),
            "the tool turned one of " + quality.Word + "'s options down: " + answer);
        Assert.That(answer, Does.Contain("You must provide at least one URL"),
            "and it got as far as missing the page, which is the point it should stop at");
      }
    }

    [Test]
    public void TheToolsOwnComplaintIsWhatTheViewerSees()
    {
      Assert.That(VideoSiteStream.ShortComplaint("yt-dlp.exe: error: no such option: --redirect\n", 2),
          Is.EqualTo("error: no such option: --redirect"),
          "measured: a usage error carries no capital ERROR, and quoting it back is the whole point of asking");
      Assert.That(VideoSiteStream.ShortComplaint("[youtube] ERROR: Sign in to confirm you're not a bot\n", 1),
          Does.Contain("Sign in to confirm"), "the site's own words, in the middle of a line, reach the screen");
      Assert.That(VideoSiteStream.ShortComplaint("some line nobody can use\n", 3),
          Is.EqualTo("the resolver answered with exit code 3."),
          "and when it said nothing a viewer can read, that is said instead");
    }

    /// <summary>
    /// Runs the tool with these arguments and hands back what it wrote, both channels together.
    /// </summary>
    private static string AskTheTool(string tool, string[] arguments)
    {
      string line = string.Empty;
      foreach (string argument in arguments)
        line += " " + (argument.Contains(' ') ? "\"" + argument + "\"" : argument);

      using Process process = Process.Start(new ProcessStartInfo(tool, line.Trim())
      {
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
      })!;

      string output = process.StandardOutput.ReadToEnd();
      string errors = process.StandardError.ReadToEnd();
      process.WaitForExit(30000);
      return output + errors;
    }
  }
}
