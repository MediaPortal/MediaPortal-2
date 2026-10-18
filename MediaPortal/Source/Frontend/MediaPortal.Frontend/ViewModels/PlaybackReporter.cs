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
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// Carries what the playback screen says a viewer watched to the server that keeps the library, and says back
  /// what came of it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Three pieces know different halves of one job: the playback screen knows a viewing ended, only a client knows
  /// that a <i>server</i> is what a viewing is reported to, and only the application knows which server that
  /// happens to be at the time. This class holds the middle - it is told which client to ask, so that the question
  /// "what does the screen say when the server refuses the report?" has one home and is reachable without opening
  /// a window.
  /// </para>
  /// <para>
  /// <b>Reports are started and not waited for.</b> The viewer has already walked away from that film, and a media
  /// centre that holds up the next screen - or the shutdown - on a network round trip is worse than one that loses
  /// a "watched" mark now and then. Not waiting is no reason to be silent though: every outcome, including the
  /// server's own refusal, is written through the <c>say</c> the application handed over, because a missing
  /// "watched" tick is the sort of thing a person notices a week later with no idea when it stopped working.
  /// </para>
  /// </remarks>
  public sealed class PlaybackReporter : IDisposable
  {
    private readonly NowPlayingViewModel _screen;
    private readonly Func<ServerApiClient?> _clients;
    private readonly Action<string> _say;
    private readonly Action<Guid>? _watched;
    private Task? _lastReport;

    /// <param name="screen">The playback screen whose reports are carried to the server.</param>
    /// <param name="clients">
    /// Asks which server to report to, every time something is reported. Called late on purpose: the front end can
    /// be moved to another server while a film plays, and a report belongs to the server the film was picked from.
    /// </param>
    /// <param name="say">Where the outcome goes. The application passes its console.</param>
    /// <param name="watched">
    /// Told which title the library has just changed its mind about, so that a screen still showing the counters
    /// from before the film can go and read them again. Only called when the server took the report.
    /// </param>
    public PlaybackReporter(NowPlayingViewModel screen, Func<ServerApiClient?> clients, Action<string> say,
        Action<Guid>? watched)
    {
      _screen = screen ?? throw new ArgumentNullException("screen");
      _clients = clients ?? throw new ArgumentNullException("clients");
      _say = say ?? throw new ArgumentNullException("say");
      _watched = watched;
      _screen.PlaybackReported += OnReported;
    }

    /// <summary>
    /// The report last started, whether or not it has finished, or <c>null</c> while nothing has been reported.
    /// </summary>
    /// <remarks>
    /// The application never waits on this - that is the point of the class. It is exposed because "what did the
    /// screen say after the server refused" is otherwise a question with no answer, and because a task nobody holds
    /// a reference to is a task whose failure nobody can look at.
    /// </remarks>
    public Task? LastReport
    {
      get { return _lastReport; }
    }

    public void Dispose()
    {
      _screen.PlaybackReported -= OnReported;
    }

    private void OnReported(object? sender, PlaybackReportedEventArgs args)
    {
      ServerApiClient? client = _clients();
      if (client == null)
      {
        _say("watched:  " + args.Percentage + "% of " + args.MediaItemId
            + " - nowhere to report it, no server configured");
        return;
      }

      // Deliberately no continuation options. Asking for "only if it faulted" together with "only if it finished"
      // is not a question a finished task can answer - the two endings exclude each other - and the framework
      // notices: ContinueWith throws an ArgumentException rather than quietly picking one. Branching inside one
      // continuation says both outcomes instead of neither.
      _lastReport = client.ReportPlaybackAsync(args.MediaItemId, args.Percentage, CancellationToken.None)
          .ContinueWith(delegate (Task<MediaPlayback> report)
          {
            if (report.IsCanceled)
            {
              _say("watched:  " + args.Percentage + "% - the report was cancelled");
            }
            else if (report.IsFaulted)
            {
              _say("watched:  " + args.Percentage + "% - the server did not take the report ("
                  + DescribeFailure(report.Exception) + ")");
            }
            else
            {
              _say("watched:  " + args.Percentage + "% - the library now says it was played "
                  + report.Result.PlayCount + " time(s)");
              _watched?.Invoke(args.MediaItemId);
            }
          });
    }

    /// <summary>
    /// The innermost sentence of whatever a faulted task carries, because <c>AggregateException</c> around
    /// <c>ServerApiException</c> around a message is three layers between a person and what went wrong.
    /// </summary>
    private static string DescribeFailure(Exception? error)
    {
      Exception? inner = error;
      while (inner is AggregateException aggregate && inner.InnerException != null)
        inner = inner.InnerException;
      return inner == null ? "no reason given" : inner.Message;
    }
  }
}
