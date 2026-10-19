using System.Collections.Generic;
using System.Threading;

namespace HttpServer
{
  /// <summary>
  /// Used to queue incoming requests.
  /// </summary>
  // TODO Albert, Team-MediaPortal: Add proper disposal, close _event
  internal class RequestQueue
  {
    private int _maxRequestCount = 25;
    private int _currentRequestCount;
    private int _maxQueueSize = 50;
    private readonly Queue<QueueItem> _queue = new Queue<QueueItem>();
    private readonly ManualResetEvent _event = new ManualResetEvent(false);
    private bool _canRun;
    private Thread _workerThread;
    private readonly ProcessRequestHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestQueue"/> class.
    /// </summary>
    /// <param name="handler">Called when a request should be processed.</param>
    public RequestQueue(ProcessRequestHandler handler)
    {
      _handler = handler;
    }

    /// <summary>
    /// Used two queue incoming requests to avoid
    /// thread starvation.
    /// </summary>
    private class QueueItem
    {
      public IHttpClientContext Context { get; set; }
      public IHttpRequest Request { get; set; }
    }

    /// <summary>
    /// Gets or sets maximum number of allowed simultaneous requests.
    /// </summary>
    public int MaxRequestCount
    {
      get { return _maxRequestCount; }
      set { _maxRequestCount = value; }
    }

    /// <summary>
    /// Gets or sets maximum number of requests queuing to be handled.
    /// </summary>
    public int MaxQueueSize
    {
      get { return _maxQueueSize; }
      set { _maxQueueSize = value; }
    }

    /// <summary>
    /// Specifies how many requests the HTTP server is currently processing.
    /// </summary>
    internal int CurrentRequestCount
    {
      get { return _currentRequestCount; }
      set { _currentRequestCount = value; }
    }

    internal bool ShouldQueue
    {
      get { return _currentRequestCount > _maxRequestCount; }
    }

    public void Enqueue(IHttpClientContext context, IHttpRequest request)
    {
      QueueItem item = new QueueItem {Context = context, Request = request};
      lock (_queue)
        _queue.Enqueue(item);
    }

    public void Trigger()
    {
    }

    public void Start()
    {
      _canRun = true;
      if (_workerThread == null)
        // A background thread, because Stop() may have to give up on it, and a thread nobody is waiting for
        // must not be a reason for the process to stay alive.
        _workerThread = new Thread(QueueThread) { IsBackground = true };

      if (!_workerThread.IsAlive)
        _workerThread.Start();
    }

    public void Stop()
    {
      // shut down worker thread.
      _canRun = false;
      _event.Set();
      // Check added by Albert, Team MediaPortal
      if (_workerThread != null)
      {
        // Waiting five seconds was the old limit, after which the thread was aborted. Aborting is impossible on
        // every .NET that is not the Windows Framework - it answers with PlatformNotSupportedException - so the
        // thread is left to finish the request it is busy with. It is a background thread, so it cannot keep the
        // process alive, and the dropped reference means a stopped queue is never started again behind its back.
        _workerThread.Join(5000);
        _workerThread = null;
      }
    }


    /// <summary>
    /// Used to process queued requests.
    /// </summary>
    private void QueueThread()
    {
      // No catch for ThreadAbortException any more: that exception only existed because Stop() could abort this
      // thread, which no .NET outside the Windows Framework allows. Whatever the loop throws now goes wherever an
      // unhandled exception from any other thread goes.
      while (_canRun)
      {
        _event.WaitOne(Timeout.Infinite, true);
        if (!_canRun)
          break;

        if (CurrentRequestCount > MaxRequestCount)
        {
          _event.Reset();
          continue;
        }

        QueueItem item;
        lock (_queue)
        {
          if (_queue.Count == 0)
          {
            _event.Reset();
            continue;
          }

          item = _queue.Dequeue();
        }

        ThreadPool.QueueUserWorkItem(ProcessRequest, item);
      }
    }

    private void ProcessRequest(object state)
    {
      QueueItem item = (QueueItem) state;
      _handler(item.Context, item.Request);
    }
  }

  /// <summary>
  /// Method used to process a queued request
  /// </summary>
  /// <param name="context">Context that the request was received from.</param>
  /// <param name="request">Request to process.</param>
  public delegate void ProcessRequestHandler(IHttpClientContext context, IHttpRequest request);
}