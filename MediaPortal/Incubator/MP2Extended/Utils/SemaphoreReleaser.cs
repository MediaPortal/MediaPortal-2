using System;
using System.Threading;

namespace MediaPortal.Plugins.MP2Extended.Utils
{
  /// <summary>
  /// Utility class that wraps an <see cref="IDisposable"/> around an entered <see cref="SemaphoreSlim"/>
  /// to allow the use of <code>using(...)</code> semantics to ensure release of the <see cref="SemaphoreSlim"/>.
  /// </summary>
  class SemaphoreReleaser : IDisposable
  {
    SemaphoreSlim _semaphore;

    /// <summary>
    /// Creates a new <see cref="SemaphoreReleaser"/> that releases the specified <see cref="SemaphoreSlim"/>
    /// when disposed. Callers should ensure that the specified <see cref="SemaphoreSlim"/> has been entered
    /// before creating this instance.
    /// </summary>
    /// <param name="semaphore">The semaphore to release on dispose, must already be in the entered state.</param>
    public SemaphoreReleaser(SemaphoreSlim semaphore)
    {
      _semaphore = semaphore;
    }

    /// <summary>
    /// Event that gets called before the <see cref="SemaphoreSlim"/>
    /// is about to be released.
    /// </summary>
    public event EventHandler Releasing;

    protected virtual void OnReleasing()
    {
      Releasing?.Invoke(this, EventArgs.Empty);
    }

    #region IDisposable
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        // Event handler might throw exceptions so use a
        // try finally to ensure the semaphore is released
        try
        {
          if (disposing)
            OnReleasing();
        }
        finally
        {
          _semaphore?.Release();
          disposedValue = true;
        }
      }
    }

    /// <summary>
    /// Releases the <see cref="SemaphoreSlim"/>.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    ~SemaphoreReleaser()
    {
      // Failsafe to ensure the semaphore is
      // released if Dispose hasn't been called
      Dispose(false);
    }
    #endregion
  }
}
