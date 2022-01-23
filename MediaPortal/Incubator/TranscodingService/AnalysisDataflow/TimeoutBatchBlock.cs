#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Threading.Tasks.Dataflow;

namespace MediaPortal.Extensions.TranscodingService.AnalysisDataflow
{
  /// <summary>
  /// Implementation of a <see cref="BatchBlock{T}"/> that additionally triggers a batch
  /// after no new messages have been posted after a specified timeout.
  /// </summary>
  /// <typeparam name="T">The type of message to process.</typeparam>
  public class TimeoutBatchBlock<T> : IPropagatorBlock<T, T[]>
  {
    protected BatchBlock<T> _batchBlock;
    protected IPropagatorBlock<T, T> _timeoutBlock;
    protected Timer _timeoutTimer;
    protected IDisposable _link;
    
    public TimeoutBatchBlock(int batchSize, int timeout, GroupingDataflowBlockOptions dataflowBlockOptions)
    {
      _batchBlock = new BatchBlock<T>(batchSize, dataflowBlockOptions);
      _timeoutTimer = new Timer(o => _batchBlock.TriggerBatch());
      _timeoutBlock = new TransformBlock<T, T>(o =>
      {
        _timeoutTimer.Change(timeout, Timeout.Infinite);
        return o;
      }, new ExecutionDataflowBlockOptions { CancellationToken = dataflowBlockOptions.CancellationToken });

      _link = _timeoutBlock.LinkTo(_batchBlock, new DataflowLinkOptions { PropagateCompletion = true });
    }

    public Task Completion
    {
      get { return _batchBlock.Completion; }
    }

    public void Complete()
    {
      _timeoutBlock.Complete();
      using (ManualResetEvent timerDisposed = new ManualResetEvent(false))
      {
        _timeoutTimer.Dispose(timerDisposed);
        timerDisposed.WaitOne();
      }
    }

    T[] ISourceBlock<T[]>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T[]> target, out bool messageConsumed)
    {
      return ((ISourceBlock<T[]>)_batchBlock).ConsumeMessage(messageHeader, target, out messageConsumed);
    }

    public void Fault(Exception exception)
    {
      _timeoutBlock.Fault(exception);
    }

    public IDisposable LinkTo(ITargetBlock<T[]> target, DataflowLinkOptions linkOptions)
    {
      return _batchBlock.LinkTo(target, linkOptions);
    }

    DataflowMessageStatus ITargetBlock<T>.OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
    {
      return _timeoutBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    }

    void ISourceBlock<T[]>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T[]> target)
    {
      ((ISourceBlock<T[]>)_batchBlock).ReleaseReservation(messageHeader, target);
    }

    bool ISourceBlock<T[]>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T[]> target)
    {
      return ((ISourceBlock<T[]>)_batchBlock).ReserveMessage(messageHeader, target);
    }
  }
}
