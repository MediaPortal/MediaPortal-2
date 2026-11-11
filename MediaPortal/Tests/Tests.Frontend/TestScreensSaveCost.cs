using MediaPortal.Frontend.Settings;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;

namespace Tests.Frontend
{
  /// <summary>
  /// What one save of the screens' settings costs on this machine's disk. Asked on its own because a shape switch
  /// carries the save inside itself and the walk measures its halves in tens of milliseconds, which is no place
  /// to see two. The number matters because the shape a viewer walks INTO is written to disk in the very moment
  /// he walks it (MediaBrowserViewModel.SwitchView): two milliseconds is nothing, and it is paid inside the
  /// moment the viewer is waiting for. Measured here rather than argued about, and measured on a solid-state
  /// disk - a television box with a spinning one at the other end of the room pays more for the same line.
  /// Putting the write behind the key press was tried and gave this back: a shelf that is left the moment its
  /// shape was changed no longer remembers the shape, which is a promise worth more than two milliseconds.
  /// </summary>
  [TestFixture]
  public class TestScreensSaveCost
  {
    [Test]
    public void ASaveOfTheScreensSettingsIsWrittenDown()
    {
      string file = Path.Combine(Path.GetTempPath(),
          "mp2_screens_bench_" + Guid.NewGuid().ToString("N") + ".json");
      string? before = Environment.GetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE");
      Environment.SetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE", file);
      try
      {
        ScreenViewSettings screens = ScreenViewSettings.Load(null, out _);
        for (int i = 0; i < 20; i++)
          screens.AnswerFor("aspect" + i, ScreenViewSettings.NO_FILTER).Layout = "Grid";

        screens.Save(null, out bool warm);
        Assert.That(warm, Is.True, "the warm save wrote, or the file is somewhere else");

        Stopwatch watch = Stopwatch.StartNew();
        for (int i = 0; i < 200; i++)
          screens.Save(null, out _);
        watch.Stop();

        double[] samples = new double[40];
        for (int i = 0; i < 40; i++)
        {
          Stopwatch one = Stopwatch.StartNew();
          screens.Save(null, out _);
          one.Stop();
          samples[i] = one.Elapsed.TotalMilliseconds;
        }

        Array.Sort(samples);
        TestContext.WriteLine("20 screens in the file: mean of 200 saves "
            + (watch.Elapsed.TotalMilliseconds / 200).ToString("0.00") + " ms, median of 40 single saves "
            + samples[20].ToString("0.00") + " ms, slowest " + samples[39].ToString("0.00") + " ms");
      }
      finally
      {
        Environment.SetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE", before);
        if (File.Exists(file))
          File.Delete(file);
      }
    }
  }
}
