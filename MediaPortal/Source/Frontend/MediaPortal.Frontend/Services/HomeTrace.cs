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
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace MediaPortal.Frontend.Services
{
  /// <summary>
  /// The measuring instrument for the home screen's wall switch: when <c>MP_TRACE_HOME</c> names a file
  /// (or is set to <c>1</c>, which picks <c>mp_home_trace.txt</c> beside the other temp files), every
  /// station of the chain from stepping on a rail tile to the finished wall standing in the light writes
  /// one line into it - the step, the ask of the reading, its answer, the handover's freedom, the wall
  /// going up, the moment it is brought into the light, and the composition frames that carry it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Why an instrument and not an opinion: the felt lag of a switch could come from the reading, from the
  /// settling, from the burst of building the wall in, or from the step handler of the next tile tripping
  /// over a UI thread that is still busy with the wall before it. A screenshot cannot tell those apart and
  /// neither can a viewer standing in front of the television. The lines carry the milliseconds since the
  /// process started, so the chain of one switch reads as one story, and the frame ledger next to it says
  /// what the whole thing cost the screen.
  /// </para>
  /// <para>
  /// The frames are the composition's own beat, counted by the window while it is armed for them - the same
  /// beat <c>MP2_FRAMELOG</c> counts, kept here as running totals so a slow stretch names itself without
  /// anybody having to divide gaps by hand. Asking for frames keeps the window producing them, which a
  /// screen left alone would not do; that is the instrument's price, and the runs that do not set the
  /// variable never pay it.
  /// </para>
  /// </remarks>
  public static class HomeTrace
  {
    /// <summary>The switch that turns the instrument on; its value names the file, <c>1</c> picks the default.</summary>
    public const string ENVIRONMENT_VARIABLE = "MP_TRACE_HOME";

    /// <summary>
    /// A frame that took longer than this to come is one the viewer could see: two sixtieths at a plain
    /// refresh rate, the same line the frame log arguments draw for the glasses.
    /// </summary>
    public const double SLOW_FRAME_MS = 40.0;

    /// How often the running frame totals are written out while the pump turns.
    private const double SUMMARY_INTERVAL_MS = 2000.0;

    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private static readonly object LedgerLock = new object();
    private static readonly List<PendingFrames> Pending = new List<PendingFrames>();

    /// The file the ledger goes to; <c>null</c> means the instrument is off, which is every run that was
    /// started without the variable and every test that never heard of it.
    private static readonly string? FilePath;

    private static long _frames;
    private static long _slowFrames;
    private static int _lastGen0;
    private static int _lastGen1;
    private static int _lastGen2;
    private static long _lastAllocated;    private static double _lastFrameAt;
    private static double _lastSummaryAt;
    private static double _worstGapMs;

    static HomeTrace()
    {
      string? asked = Environment.GetEnvironmentVariable(ENVIRONMENT_VARIABLE);
      if (string.IsNullOrEmpty(asked))
        return;

      string path = asked.Trim() == "1"
          ? Path.Combine(Path.GetTempPath(), "mp_home_trace.txt")
          : asked;

      try
      {
        // The ledger starts empty on every run: an instrument that appends to yesterday's numbers measures
        // a story nobody watched.
        File.WriteAllText(path, string.Empty);
      }
      catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
      {
        return;
      }

      FilePath = path;
      Write("trace", "start pid=" + Environment.ProcessId);
    }

    /// <summary>Whether anybody asked for the numbers. Every call in here answers without work when they did not.</summary>
    public static bool Enabled => FilePath != null;

    /// <summary>
    /// Writes one station of the chain. <paramref name="station"/> names the place and
    /// <paramref name="detail"/> says which switch (generation, tile, cost) the line belongs to.
    /// </summary>
    public static void Log(string station, string detail = "")
    {
      if (FilePath == null)
        return;

      Write(station, detail);
    }

    /// <summary>
    /// Marks the beat something was written in and asks for the next
    /// <paramref name="frames"/> composition frames to be written as they arrive: what lies between the
    /// mark and the first of them is the measure-and-build burst of the wall, because a transform that was
    /// only written down is paid for when the frame comes to draw it.
    /// </summary>
    public static void NextFrames(string tag, int frames = 3)
    {
      if (FilePath == null || frames <= 0)
        return;

      lock (LedgerLock)
      {
        Pending.Add(new PendingFrames(tag, frames, Clock.Elapsed.TotalMilliseconds));
      }
    }

    /// <summary>
    /// One composition frame, counted by the window that pumps them: keeps the running totals, writes the
    /// frames that came too slowly, writes the totals every couple of seconds, and answers whoever asked
    /// for the frames that are coming next.
    /// </summary>
    public static void FrameTick()
    {
      if (FilePath == null)
        return;

      double now = Clock.Elapsed.TotalMilliseconds;
      double gap = _lastFrameAt > 0 ? now - _lastFrameAt : 0;
      _lastFrameAt = now;
      _frames++;

      if (gap > SLOW_FRAME_MS)
      {
        _slowFrames++;
        if (gap > _worstGapMs)
          _worstGapMs = gap;
        Write("slowframe", gap.ToString("F1", CultureInfo.InvariantCulture));
      }

      if (_lastSummaryAt == 0)
        _lastSummaryAt = now;
      else if (now - _lastSummaryAt >= SUMMARY_INTERVAL_MS)
      {
        _lastSummaryAt = now;
        // The collector's own ledger next to the frame ledger: a picture of a few megabytes is a large-object
        // heap allocation by itself, ten of them a step, and a collection of that heap stops the thread that
        // paints the screen.  A frame that came late is then not the drawing being late but the collector, and
        // only these numbers tell the two apart - they stand here so that the question is never answered by
        // opinion again.  What is written is the count since the last summary and the bytes allocated in it.
        int g0 = GC.CollectionCount(0) - _lastGen0;
        int g1 = GC.CollectionCount(1) - _lastGen1;
        int g2 = GC.CollectionCount(2) - _lastGen2;
        _lastGen0 = GC.CollectionCount(0);
        _lastGen1 = GC.CollectionCount(1);
        _lastGen2 = GC.CollectionCount(2);
        long allocated = GC.GetTotalAllocatedBytes(true);
        long got = allocated - _lastAllocated;
        _lastAllocated = allocated;

        Write("frames", string.Format(CultureInfo.InvariantCulture,
            "total={0} slow={1} worst={2:F1} gc={3}/{4}/{5} allocated={6}MB", _frames, _slowFrames, _worstGapMs,
            g0, g1, g2, got / (1024 * 1024)));
        _worstGapMs = 0;
      }

      lock (LedgerLock)
      {
        for (int i = Pending.Count - 1; i >= 0; i--)
        {
          PendingFrames waiting = Pending[i];
          waiting.Seen++;
          Write("frame", string.Format(CultureInfo.InvariantCulture,
              "{0} #{1}/{2} since={3:F1}", waiting.Tag, waiting.Seen, waiting.Wanted, now - waiting.MarkedAt));
          if (waiting.Seen >= waiting.Wanted)
            Pending.RemoveAt(i);
        }
      }
    }

    private static void Write(string station, string detail)
    {
      string path = FilePath!;
      string line = string.Format(CultureInfo.InvariantCulture, "{0,9:F1} {1} {2} [t{3}]{4}",
          Clock.Elapsed.TotalMilliseconds, station, detail,
          System.Threading.Thread.CurrentThread.ManagedThreadId, Environment.NewLine);
      try
      {
        lock (LedgerLock)
        {
          File.AppendAllText(path, line);
        }
      }
      catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
      {
        // A ledger nobody can write into is not a reason to stop drawing the screen.
      }
    }

    private sealed class PendingFrames
    {
      public PendingFrames(string tag, int wanted, double markedAt)
      {
        Tag = tag;
        Wanted = wanted;
        MarkedAt = markedAt;
      }

      public string Tag { get; }
      public int Wanted { get; }
      public double MarkedAt { get; }
      public int Seen { get; set; }
    }
  }
}
