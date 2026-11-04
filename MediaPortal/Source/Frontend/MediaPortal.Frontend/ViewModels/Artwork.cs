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
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Frontend.Caching;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The pictures of titles: fetched once per title, remembered, and never asked for one at a time more than a
  /// handful of times at once.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Why this exists at all.</b> A shelf of sixty films is sixty rows, and MP2 draws a poster next to each of
  /// them - the old skin engine did the same thing through
  /// <c>ImageSourceFactory.CreateMediaItemThumbnailAspectSource</c>, which pulled the picture out of the media
  /// item and cached it by media item id. Over HTTP the same thing would mean sixty requests, so the three things
  /// that make that bearable are done here and nowhere else: an answer is remembered (scrolling back costs
  /// nothing), two requests for the same title become one, and the number of requests in flight is capped so that
  /// arriving at a shelf does not put sixty reads on a server that is also being asked for the next page.
  /// </para>
  /// <para>
  /// <b>What "no picture" means here.</b> Most titles in a real library have no poster, and a front end that
  /// refetched for each of them on every visit would spend its bandwidth proving it. A title the server answered
  /// "none" for is therefore kept in the cache like a picture is, as null; a title the server refused because it
  /// is not reachable is not, because that answer says something about the network rather than about the title.
  /// </para>
  /// <para>
  /// <b>Threading.</b> A view model is called from the UI thread, and the fetch is awaited without leaving it,
  /// but the answers come back over HTTP - and an answer that arrives while the front end is busy elsewhere is
  /// delivered wherever the thread pool puts it. So the three tables below (what is remembered, in what order,
  /// and what is on its way) are written under one lock, and the awaits happen outside it so that no fetch ever
  /// holds a lock. A caller on any thread is therefore allowed, and two callers that asked for the same title at
  /// the same moment get one request between them, whichever of them got there first.
  /// </para>
  /// <para>
  /// Nothing here decodes anything. The bytes go to the view, which turns them into an image; this class knows
  /// only about fetching and remembering, which is also what lets it be tested without a window.
  /// </para>
  /// </remarks>
  public sealed class Artwork
  {
    /// <summary>
    /// How many pictures are remembered before the oldest is dropped.
    /// </summary>
    /// <remarks>
    /// MP2's own thumbnails are at most 256 pixels across, so a remembered picture is tens of kilobytes and a
    /// full cache is a few megabytes. Two hundred titles is three or four shelves, which is what somebody walking
    /// back and forth through a library actually looks at; past that the oldest is dropped, and walking into a
    /// shelf nobody has visited for an hour pays one fetch per row again.
    /// </remarks>
    public const int MAX_REMEMBERED = 200;

    /// <summary>
    /// How many pictures may be on the wire at one moment.
    /// </summary>
    /// <remarks>
    /// Four. Enough that the pictures of a shelf arrive in a stream rather than one by one, small enough that a
    /// television on wifi and a server on the same network are not both busy with nothing else. A shelf of sixty
    /// rows is therefore sixty fetches queued rather than sixty requests.
    /// </remarks>
    public const int MAX_AT_ONCE = 4;

    private readonly Func<Guid, Task<byte[]?>> _fetch;

    /// <summary>
    /// Where a picture is looked for before the server is asked, or <c>null</c> for a front end that only has
    /// its own memory to remember with.
    /// </summary>
    private readonly IPictureStore? _store;

    /// <summary>How many pictures this object remembers before it drops the oldest.</summary>
    private readonly int _rememberedLimit;

    /// <summary>Pictures already answered for, by title. A null value means the server has none for it.</summary>
    private readonly Dictionary<Guid, byte[]?> _remembered = new Dictionary<Guid, byte[]?>();

    /// <summary>The titles in <see cref="_remembered"/>, least recently looked at first.</summary>
    private readonly List<Guid> _order = new List<Guid>();

    /// <summary>Titles whose picture is on its way, so that a second question about one waits instead of asking.</summary>
    private readonly Dictionary<Guid, Task<byte[]?>> _onTheWay = new Dictionary<Guid, Task<byte[]?>>();

    private readonly SemaphoreSlim _slots;

    /// <summary>
    /// Guards the three tables above. It is never held across an await, and it is never taken while the fetch
    /// itself is running, so a slow server cannot block anybody asking about a title it already knows.
    /// </summary>
    private readonly object _gate = new object();

    /// <summary>How many fetches this object has started and not finished.</summary>
    private int _running;

    /// <summary>
    /// @param fetch Where the bytes come from - for the front end, the server's picture endpoint.
    /// @param store This machine's shelf for this kind of picture, asked before the server. <c>null</c> is a
    /// front end that has only its own memory, which is what every test and every screen without a server gets.
    /// @param remembered How many pictures to hold, or 0 for <see cref="MAX_REMEMBERED"/>. The media settings
    /// answer this for a real machine, because two hundred backdrops are around 130 MB and a small box is
    /// allowed to say fifty.
    /// @param atOnce How many pictures may be on the wire at one moment, or 0 for <see cref="MAX_AT_ONCE"/>.
    /// </summary>
    public Artwork(Func<Guid, Task<byte[]?>> fetch, IPictureStore? store = null, int remembered = 0,
        int atOnce = 0)
    {
      _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
      _store = store;
      _rememberedLimit = remembered > 0 ? remembered : MAX_REMEMBERED;
      _slots = new SemaphoreSlim(atOnce > 0 ? atOnce : MAX_AT_ONCE);
    }

    /// <summary>How many titles this object holds an answer for, pictures and absences alike.</summary>
    public int Remembered
    {
      get { lock (_gate) return _remembered.Count; }
    }

    /// <summary>How many fetches are in flight right now, which is never more than <see cref="MAX_AT_ONCE"/>.</summary>
    public int Running
    {
      get { return Volatile.Read(ref _running); }
    }

    /// <summary>
    /// Whether an answer for this title is already held, without waiting for one that is on its way.
    /// </summary>
    /// <param name="id">The title.</param>
    /// <param name="picture">The bytes, or null when the server said it has none.</param>
    /// <returns>Whether this object already knows the answer.</returns>
    public bool TryPeek(Guid id, out byte[]? picture)
    {
      lock (_gate)
      {
        bool known = _remembered.TryGetValue(id, out picture);
        if (known)
          Touch(id);
        return known;
      }
    }

    /// <summary>
    /// The picture of one title: its bytes, or null when the server has none for it.
    /// </summary>
    /// <param name="id">The media item id.</param>
    /// <remarks>
    /// <para>
    /// Asks the server once. A second call for the same title returns the remembered answer, and a call made while
    /// one is in flight joins it rather than starting a second request - which matters because a row can be asked
    /// about twice in a scroll, once when it is realised and once when it is selected.
    /// </para>
    /// <para>
    /// A fetch that fails is passed on to the caller and forgotten, so the next question about that title tries
    /// again; the caller decides whether the viewer hears about it. For a shelf that decision is usually "no": the
    /// rows are already drawn and a poster that failed is not worth a line of complaint under a list that
    /// arrived.
    /// </para>
    /// </remarks>
    public Task<byte[]?> LoadAsync(Guid id)
    {
      TaskCompletionSource<byte[]?>? starter = null;
      Task<byte[]?>? pending = null;

      lock (_gate)
      {
        if (_remembered.TryGetValue(id, out byte[]? held))
        {
          Interlocked.Increment(ref READ_HITS);
          Touch(id);
          return Task.FromResult(held);
        }

        if (!_onTheWay.TryGetValue(id, out pending))
        {
          starter = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
          _onTheWay[id] = starter.Task;
        }
      }

      if (starter == null)
        return pending!;

      // Started outside the lock, so that a server that answers synchronously cannot re-enter the code above, and
      // the task handed back is the one registered in it: whoever asked first and whoever joined afterwards are
      // waiting on the same answer.
      _ = AskedAsync(id, starter);
      return starter.Task;
    }

    /// <summary>
    /// One request, with the slot, the cache write, and the answer handed to everybody waiting on this title.
    /// </summary>
    private async Task<byte[]?> AskedAsync(Guid id, TaskCompletionSource<byte[]?> starter)
    {
      try
      {
        byte[]? picture = await FetchedAsync(id).ConfigureAwait(true);
        Remember(id, picture);
        starter.TrySetResult(picture);
        return picture;
      }
      catch (Exception ex)
      {
        // The failure is handed to whoever is waiting rather than remembered: it says something about the moment,
        // not about the title, so the next question about it asks again. Nothing awaits the task this method
        // returns - the answer everybody waits on is the one above - so it ends here instead of escaping into a
        // task nobody is watching.
        starter.TrySetException(ex);
        return null;
      }
      finally
      {
        lock (_gate)
        {
          // Only the request this call started is removed. A caller that joined an existing one must not be able
          // to forget it while the first caller is still waiting on it.
          if (ReferenceEquals(_onTheWay.TryGetValue(id, out Task<byte[]?>? current) ? current : null, starter.Task))
            _onTheWay.Remove(id);
        }
      }
    }

    /// <summary>
    /// One picture: from this machine's shelf where it lies there, and from the server where it does not. A
    /// shelf answer takes no request slot, because it is a file on this machine rather than a read on somebody
    /// else's - which matters most for exactly the pictures that are biggest.
    /// </summary>
    private async Task<byte[]?> FetchedAsync(Guid id)
    {
      if (_store == null)
        return await FetchWithASlotAsync(id).ConfigureAwait(true);

      return await _store.LoadAsync(id, () => FetchWithASlotAsync(id)).ConfigureAwait(true);
    }

    private async Task<byte[]?> FetchWithASlotAsync(Guid id)
    {
      long queued_at = ReadingsClock.ElapsedMilliseconds;
      await _slots.WaitAsync().ConfigureAwait(true);
      Interlocked.Add(ref READ_QUEUED_MS, ReadingsClock.ElapsedMilliseconds - queued_at);
      Interlocked.Increment(ref _running);
      Interlocked.Increment(ref READ_RUNNING);

      long asked_at = ReadingsClock.ElapsedMilliseconds;
      try
      {
        byte[]? picture = await _fetch(id).ConfigureAwait(true);

        Interlocked.Increment(ref READ_FETCHES);
        Interlocked.Add(ref READ_FETCH_MS, ReadingsClock.ElapsedMilliseconds - asked_at);
        Interlocked.Add(ref READ_BYTES, picture?.LongLength ?? 0L);

        return picture;
      }
      finally
      {
        Interlocked.Decrement(ref _running);
        Interlocked.Decrement(ref READ_RUNNING);
        _slots.Release();
      }
    }

    // WHAT THE WIRE COSTS.  A cover that arrives late is experienced as one that "pops in" while the viewer
    // walks, and the three reasons for arriving late cannot be told apart from the screen: the picture was already
    // remembered, it waited for one of the few places on the wire, or the server itself was slow.  These counters
    // answer that, and whoever walks the shelf reads them into the trace with TakeFetchReadings.
    private static readonly System.Diagnostics.Stopwatch ReadingsClock = System.Diagnostics.Stopwatch.StartNew();
    private static long READ_HITS;
    private static long READ_FETCHES;
    private static long READ_FETCH_MS;
    private static long READ_QUEUED_MS;
    private static long READ_BYTES;
    private static int READ_RUNNING;

    /// <summary>
    /// What the pictures have cost since this was last asked, and the counts start again from zero.
    /// </summary>
    public static string TakeFetchReadings()
    {
      long hits = Interlocked.Exchange(ref READ_HITS, 0);
      long fetches = Interlocked.Exchange(ref READ_FETCHES, 0);
      long fetch_ms = Interlocked.Exchange(ref READ_FETCH_MS, 0);
      long queued_ms = Interlocked.Exchange(ref READ_QUEUED_MS, 0);
      long bytes = Interlocked.Exchange(ref READ_BYTES, 0);

      return string.Format(System.Globalization.CultureInfo.InvariantCulture,
          "wire hits={0} fetched={1}/{2}ms queued={3}ms running={4} mb={5:0.0}",
          hits, fetches, fetch_ms, queued_ms, Volatile.Read(ref READ_RUNNING), bytes / (1024.0 * 1024.0));
    }

    private void Remember(Guid id, byte[]? picture)
    {
      lock (_gate)
      {
        _remembered[id] = picture;
        Touch(id);

        while (_order.Count > _rememberedLimit)
        {
          Guid oldest = _order[0];
          _order.RemoveAt(0);
          _remembered.Remove(oldest);
        }
      }
    }

    /// <summary>
    /// Moves a title to the recent end of the remembered list. Called with <see cref="_gate"/> held.
    /// </summary>
    private void Touch(Guid id)
    {
      _order.Remove(id);
      _order.Add(id);
    }
  }
}
