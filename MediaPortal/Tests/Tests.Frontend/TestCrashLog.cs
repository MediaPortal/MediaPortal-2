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
using MediaPortal.Frontend.Settings;

namespace Tests.Frontend
{
  /// <summary>
  /// The last words of the front end, and where they are kept.
  /// </summary>
  /// <remarks>
  /// The settings wall took the client down over a selection index the list had kept from a level it had left,
  /// and the only place that said so was the Windows application log - which is not a place a person told to
  /// "prüfe die logs" thinks of. What is tested here is the answer to that: the death is written beside the
  /// front end's own settings, it says whether the process ended, and the writing itself can never be the
  /// second exception on the way out.
  /// </remarks>
  [TestFixture]
  public class TestCrashLog
  {
    /// <summary>Pointing the front end's settings at a folder of this one test, for as long as it needs it.</summary>
    private static string PointSettingsAt(out string folder)
    {
      folder = Path.Combine(Path.GetTempPath(), "mp2-crash-" + Guid.NewGuid().ToString("N"));
      return Path.Combine(folder, "frontend.json");
    }

    [Test]
    public void TheDeathOfTheClientIsWrittenBesideItsSettings()
    {
      string previous = Environment.GetEnvironmentVariable("MP2_SETTINGS_FILE");
      string settings = PointSettingsAt(out string folder);
      Environment.SetEnvironmentVariable("MP2_SETTINGS_FILE", settings);

      try
      {
        string words = CrashLog.Write(
            new InvalidOperationException("die Liste griff nach einem Platz, den die Stufe nicht hatte"), true);

        Assert.That(words, Does.Contain("griff nach einem Platz"),
            "the caller gets the words back, so a terminal and the file can say the same thing");

        string file = CrashLog.FilePath;
        Assert.That(Path.GetDirectoryName(file), Is.EqualTo(folder),
            "the crash is kept where the settings are kept, which is where anybody looking for a log goes");

        string written = File.ReadAllText(file);
        Assert.That(written, Does.Contain("InvalidOperationException"));
        Assert.That(written, Does.Contain("griff nach einem Platz"));
        Assert.That(written, Does.Contain("the process ended here"),
            "and it says whether the client died there, which is the first question about a black screen");
      }
      finally
      {
        Environment.SetEnvironmentVariable("MP2_SETTINGS_FILE", previous);
        if (Directory.Exists(folder))
          Directory.Delete(folder, true);
      }
    }

    [Test]
    public void ASecondDeathOfTheSameKindIsAppendedRatherThanReplacingTheFirst()
    {
      string previous = Environment.GetEnvironmentVariable("MP2_SETTINGS_FILE");
      string settings = PointSettingsAt(out string folder);
      Environment.SetEnvironmentVariable("MP2_SETTINGS_FILE", settings);

      try
      {
        CrashLog.Write("der erste Sturz", true);
        CrashLog.Write("der zweite Sturz", false);

        string written = File.ReadAllText(CrashLog.FilePath);
        Assert.That(written, Does.Contain("der erste Sturz").And.Contain("der zweite Sturz"),
            "a client that fell twice fell twice, and the first fall is the one that explains the second");
        Assert.That(written.IndexOf("der erste Sturz", StringComparison.Ordinal),
            Is.LessThan(written.IndexOf("der zweite Sturz", StringComparison.Ordinal)),
            "and they stand in the order they happened");
      }
      finally
      {
        Environment.SetEnvironmentVariable("MP2_SETTINGS_FILE", previous);
        if (Directory.Exists(folder))
          Directory.Delete(folder, true);
      }
    }

    [Test]
    public void WritingTheCrashIsNeverASecondCrash()
    {
      string previous = Environment.GetEnvironmentVariable("MP2_SETTINGS_FILE");
      string blocker = Path.Combine(Path.GetTempPath(), "mp2-crash-blocker-" + Guid.NewGuid().ToString("N"));
      File.WriteAllText(blocker, "not a folder");

      // The settings file asked for underneath a file: no folder can be made through a file, so every step of
      // the writing fails - and a failure there must not throw, because it would replace the exception being
      // reported with one about a log file.
      Environment.SetEnvironmentVariable("MP2_SETTINGS_FILE", Path.Combine(blocker, "frontend.json"));

      try
      {
        string words = "der Sturz, dessen Protokoll nicht geschrieben werden konnte";
        Assert.That(delegate { CrashLog.Write(words, true); }, Throws.Nothing);
      }
      finally
      {
        Environment.SetEnvironmentVariable("MP2_SETTINGS_FILE", previous);
        File.Delete(blocker);
      }
    }
  }
}
