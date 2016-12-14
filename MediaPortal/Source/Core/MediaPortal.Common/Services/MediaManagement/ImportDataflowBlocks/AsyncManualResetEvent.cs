#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks
{
  /// <summary>
  /// A class that acts like a <see cref="ManualResetEvent"/>, but additionally can be "awaited" asynchronously.
  /// </summary>
  /// <remarks>
  /// Usage: await asyncManualResetEvent.WaitAsync();
  /// </remarks>
  internal class AsyncManualResetEvent
  {
    private volatile TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

    public AsyncManualResetEvent()
    {
    }

    public AsyncManualResetEvent(CancellationToken cancellationToken)
    {
      if (cancellationToken == null)
        throw new ArgumentNullException("cancellationToken");
      cancellationToken.Register(() => _tcs.TrySetCanceled());
    }

    public Task WaitAsync()
    {
      return _tcs.Task;
    }

    public void Set()
    {
      _tcs.TrySetResult(true);
    }

    public void Reset()
    {
      while (true)
      {
        var tcs = _tcs;
        #pragma warning disable 420
        // It is ok to call Interlocked members on volatile fields
        // ReSharper disable once CSharpWarnings::CS0420
        if (!tcs.Task.IsCompleted || Interlocked.CompareExchange(ref _tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
        #pragma warning restore 420
          return;
      }
    }
  }
}
