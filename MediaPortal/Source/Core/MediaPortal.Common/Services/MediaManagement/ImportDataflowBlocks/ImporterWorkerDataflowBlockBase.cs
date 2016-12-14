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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks
{
  /// <summary>
  /// Base class for all DataflowBlocks used in the <see cref="ImporterWorkerNewGen"/>
  /// </summary>
  /// <remarks>
  /// The <see cref="ImporterWorkerNewGen"/> makes heavy use of TPL Dataflow. As a result, an ImportJob technically consists of a
  /// chain of so called DataflowBlocks. Every DataflowBlock has a well defined responsibility (such as "find all the subdirectories
  /// of a given directory" or "find all the files in one directory" or "extract the metadata of the given MediaItem" or "save the
  /// metadata of a given MediaItem into the database", etc.) and is linked to the next DataflowBlock.
  /// The data that (automatically) "flows" through this chain of DataflowBlocks are <see cref="PendingImportResourceNewGen"/>-objects.
  /// These objects hold the information necessary for the respective next DataflowBlock to perform its task. A given
  /// <see cref="PendingImportResourceNewGen"/>-object represents a single MediaItem (such as a directory or a file).
  /// Every DataflowBlock in the chain described above may be of a different type. But every such type should
  /// derive from this abstract base class.
  /// This abstract base class consists of an <see cref="InputBlock"/>, an <see cref="InnerBlock"/> and an <see cref="OutputBlock"/>.
  /// The <see cref="InputBlock"/> is a TransformBlock&lt;PendingImportResourceNewGen, PendingImportResourceNewGen&gt;. The <see cref="OutputBlock"/>
  /// is a TransformManyBlock&lt;PendingImportResourceNewGen, PendingImportResourceNewGen&gt;.
  /// The <see cref="InnerBlock"/> variable is defined as IPropagatorBlock&lt;PendingImportResourceNewGen, PendingImportResourceNewGen&gt;.
  /// However, the <see cref="InnerBlock"/> is not initialized directly in this base class. A deriving class must implement the
  /// <see cref="CreateInnerBlock"/> method, which must return the <see cref="InnerBlock"/>. This method is called by
  /// the base class constructor to initialize the <see cref="InnerBlock"/> variable (Please read the comments on
  /// <see cref="CreateInnerBlock"/> what to take into account when implementing this method).
  /// This base class implements the IPropagatorBlock&lt;PendingImportResourceNewGen, PendingImportResourceNewGen&gt; interface.
  /// <see cref="InputBlock"/> is the ITargetBlock, <see cref="OutputBlock"/> is the ISourceBlock part of the IPropagatorBlock.
  /// Internally, <see cref="InputBlock"/> is linked to <see cref="InnerBlock"/>, which in turn is linked to <see cref="OutputBlock"/>.
  /// <see cref="InputBlock"/> sets the CurrentBlock property of the respective PendingImportResource to <see cref="_blockName"/>. As a
  /// result, after an ImportJob has been suspended to disk and restored from disk, the respective PendingImportResource will
  /// be restored to exactly this DataflowBlock in the chain - based on its <see cref="_blockName"/>. However, the CurrentBlock property will
  /// only be set, if the isRestorePointAfterDeserialization parameter of the constructor of this class is <c>true</c>. If it is <c>false</c>, the
  /// PendingImportResource will be restored to the last DataflowBlock in the chain where this parameter was true. This is helpful, if the
  /// preceding block stores information in the <see cref="PendingImportResourceNewGen"/>, which is not serialized to disk so that after
  /// deserialization, it is necessary to let the last block do its magic again.
  /// The <see cref="OutputBlock"/> counts the number of processed MediaItems and checks, whether the current
  /// <see cref="PendingImportResourceNewGen"/> is valid. If so, it propagates it to the next DataflowBlock. If it is not
  /// valid (in particular because the <see cref="InnerBlock"/> set the IsValid property to false), it is disposed and no
  /// <see cref="PendingImportResourceNewGen"/> object is propagated to the next DataflowBlock (this is the reason why the
  /// <see cref="OutputBlock"/> is a TransformManyBlock instead of a TransformBlock. A TransformBlock MUST return exactly one
  /// item, whereas the TransformManyBlock may return an empty collection of items).
  /// The real magic happens in the <see cref="InnerBlock"/>, which is indirectly defined by the deriving class. There are
  /// a few things to take into account when implementing a class deriving from this base class:
  /// - The main method of the <see cref="InnerBlock"/> MUST NOT throw exceptions, unless it is intended that the ImportJob is
  ///   cancelled in a faulted state. The import will then not be restarted automatically. All statements in the <see cref="InnerBlock"/> should
  ///   therefore reside in a try-block.
  /// - The main method of the <see cref="InnerBlock"/> MUST catch the <see cref="TaskCanceledException"/>.
  ///   The reaction to a <see cref="TaskCanceledException"/> MUST be just to return the current <see cref="PendingImportResourceNewGen"/>.
  /// - The main method of the <see cref="InnerBlock"/> MUST NOT (and cannot) access the database directly via the (private)
  ///   IMediaBrowsing and IImportResultHandler variables. It MUST use the protected async methods from this base class to
  ///   do so. The task returned by these methods will (when awaited) automatically take care of the <see cref="ImporterWorkerNewGen"/>
  ///   being suspended or (re)activated. If it is suspended while trying to access the database (e.g. because the
  ///   <see cref="ImporterWorkerNewGen"/> runs inside the MP2 Client and the MP2 Server was shutdown), these methods will
  ///   "block asynchronously" until
  ///     (a) the <see cref="ImporterWorkerNewGen"/> is (re)activated, or
  ///     (b) is shut down.
  ///   On shut down, awaiting the task returned by one of these methods will throw the above mentioned <see cref="TaskCanceledException"/>
  ///   after the current state of the ImportJob has been saved to disk.
  /// Additionally, this base class takes care of logging and measuring the time the respective DataflowBlock needs to finish its task.
  /// </remarks>
  internal abstract class ImporterWorkerDataflowBlockBase : IPropagatorBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>
  {
    #region Constants

    protected static readonly IEnumerable<Guid> EMPTY_MIA_ID_ENUMERATION = new Guid[] { };

    protected static readonly IEnumerable<Guid> DIRECTORY_MIA_ID_ENUMERATION = new[]
      {
        DirectoryAspect.ASPECT_ID
      };

    protected static readonly IEnumerable<Guid> PROVIDERRESOURCE_DIRECTORY_MIA_ID_ENUMERATION = new[]
      {
        ProviderResourceAspect.ASPECT_ID,
        DirectoryAspect.ASPECT_ID
      };

    protected static readonly IEnumerable<Guid> PROVIDERRESOURCE_IMPORTER_MIA_ID_ENUMERATION = new[]
      {
        ProviderResourceAspect.ASPECT_ID,
        ImporterAspect.ASPECT_ID
      };


    #endregion

    #region Variables

    private readonly String _blockName;
    private readonly bool _isRestorePointAfterDeserialization;
    private readonly TaskCompletionSource<object> _tcs;
    private readonly Stopwatch _stopWatch;
    private int _mediaItemsProcessed;
    private IDisposable _suspensionLink;
    private IMediaBrowsing _mediaBrowsingCallback;
    private IImportResultHandler _importResultHandler;

    protected readonly ImportJobInformation ImportJobInformation;
    protected readonly ExecutionDataflowBlockOptions InputBlockOptions;
    protected readonly ExecutionDataflowBlockOptions InnerBlockOptions;
    protected readonly ExecutionDataflowBlockOptions OutputBlockOptions;
    protected readonly IPropagatorBlock<PendingImportResourceNewGen, PendingImportResourceNewGen> InputBlock;
    protected readonly IPropagatorBlock<PendingImportResourceNewGen, PendingImportResourceNewGen> InnerBlock;
    protected readonly IPropagatorBlock<PendingImportResourceNewGen, PendingImportResourceNewGen> OutputBlock;
    protected readonly ImportJobController ParentImportJobController;
    protected readonly AsyncManualResetEvent Activated;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes the DataflowBlock
    /// </summary>
    /// <param name="importJobInformation"><see cref="ImportJobInformation"/> of the ImportJob this DataflowBlock belongs to</param>
    /// <param name="inputBlockOptions"><see cref="ExecutionDataflowBlockOptions"/> for the <see cref="InputBlock"/></param>
    /// <param name="innerBlockOptions"><see cref="ExecutionDataflowBlockOptions"/> for the <see cref="InnerBlock"/></param>
    /// <param name="outputBlockOptions"><see cref="ExecutionDataflowBlockOptions"/> for the <see cref="OutputBlock"/></param>
    /// <param name="blockname">Name of this DataflowBlock (must be unique in a given chain of DataflowBlocks)</param>
    /// <param name="isRestorePointAfterDeserialization">
    /// <c>true</c>, if after deserialiization from disk, <see cref="PendingImportResourceNewGen"/>s are restored to
    /// this block. If <c>false</c>, they are restored to the last passed DataflowBlock having this parameter set to <c>true</c>
    /// </param>
    /// <param name="parentImportJobController">ImportJobController to which this DataflowBlock belongs</param>
    protected ImporterWorkerDataflowBlockBase(ImportJobInformation importJobInformation, ExecutionDataflowBlockOptions inputBlockOptions, ExecutionDataflowBlockOptions innerBlockOptions, ExecutionDataflowBlockOptions outputBlockOptions, String blockname, bool isRestorePointAfterDeserialization, ImportJobController parentImportJobController)
    {
      _blockName = blockname;
      _isRestorePointAfterDeserialization = isRestorePointAfterDeserialization;
      ImportJobInformation = importJobInformation;
      InputBlockOptions = inputBlockOptions;
      InnerBlockOptions = innerBlockOptions;
      OutputBlockOptions = outputBlockOptions;
      ParentImportJobController = parentImportJobController;

      _stopWatch = new Stopwatch();
      _tcs = new TaskCompletionSource<object>();
      Activated = new AsyncManualResetEvent(InnerBlockOptions.CancellationToken);
      InputBlock = new TransformBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>(p => InputBlockMethod(p), InputBlockOptions);
      OutputBlock = new TransformManyBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>(p => OutputBlockMethod(p), OutputBlockOptions);
      // ReSharper disable once DoNotCallOverridableMethodsInConstructor
      InnerBlock = CreateInnerBlock();

      InnerBlock.LinkTo(OutputBlock, new DataflowLinkOptions { PropagateCompletion = true });
      InputBlock.Completion.ContinueWith(OnAnyBlockFaulted, TaskContinuationOptions.OnlyOnFaulted);
      InnerBlock.Completion.ContinueWith(OnAnyBlockFaulted, TaskContinuationOptions.OnlyOnFaulted);
      OutputBlock.Completion.ContinueWith(OnAnyBlockFaulted, TaskContinuationOptions.OnlyOnFaulted);
      Task.WhenAll(InputBlock.Completion, InnerBlock.Completion, OutputBlock.Completion).ContinueWith(OnAllBlocksFinished);
    }

    #endregion

    #region Abstract members

    /// <summary>
    /// Returns an instance of the <see cref="InnerBlock"/>
    /// </summary>
    /// <remarks>
    /// Be extremely careful when implementing this abstract (and therefore virtual) method in a derived class!
    /// It is called by the base class constructor, i.e. BEFORE the constructor of the derived class is executed.
    /// Therefore, the implementation of this method MUST NOT rely on anything that happens in the constructor of
    /// the derived class. Ideally, the code block of the derived class' constructor should be empty.
    /// However, this method is executed after the field initializers of the derived class (and this base classe) have
    /// finished, so an implementation of this class may safely assume that all field initializers of this base class
    /// and all derive classes ran before.
    /// </remarks>
    protected abstract IPropagatorBlock<PendingImportResourceNewGen, PendingImportResourceNewGen> CreateInnerBlock();

    #endregion

    #region Private methods

    /// <summary>
    /// Method performed on each <see cref="PendingImportResourceNewGen"/> by the <see cref="InputBlock"/>
    /// </summary>
    /// <remarks>
    /// Sets the CurrentBlock property of the <see cref="PendingImportResourceNewGen"/> if, after an ImportJob
    /// has been deserialized from disk, the <see cref="PendingImportResourceNewGen"/> should be restored to this block.
    /// </remarks>
    /// <param name="pendingImportResource"><see cref="PendingImportResourceNewGen"/> to be processed</param>
    /// <returns></returns>
    private PendingImportResourceNewGen InputBlockMethod(PendingImportResourceNewGen pendingImportResource)
    {
      if (_isRestorePointAfterDeserialization)
        pendingImportResource.CurrentBlock = _blockName;

      return pendingImportResource;
    }

    /// <summary>
    /// Method performed on each <see cref="PendingImportResourceNewGen"/> by the <see cref="OutputBlock"/>
    /// </summary>
    /// <remarks>
    /// - Filters and disposes invalid <see cref="PendingImportResourceNewGen"/>s.
    /// - Counts the <see cref="PendingImportResourceNewGen"/>s processed by this DataflowBlock
    /// </remarks>
    /// <param name="pendingImportResource"><see cref="PendingImportResourceNewGen"/> to be processed</param>
    /// <returns></returns>
    private IEnumerable<PendingImportResourceNewGen> OutputBlockMethod(PendingImportResourceNewGen pendingImportResource)
    {
      Interlocked.Increment(ref _mediaItemsProcessed);
      
      if (pendingImportResource.IsValid)
        return new HashSet<PendingImportResourceNewGen> { pendingImportResource };
      
      pendingImportResource.Dispose();
      return new HashSet<PendingImportResourceNewGen>();
    }
    
    /// <summary>
    /// Runs when any of <see cref="InputBlock"/>, <see cref="InnerBlock"/> or <see cref="OutputBlock"/> faults
    /// </summary>
    /// <param name="faultedTask">Completion property of the faulted DataflowBlock</param>
    private void OnAnyBlockFaulted(Task faultedTask)
    {
      // When one of the three DataflowBlocks faults, the faulted state is propagated to the following
      // DataflowBlocks, but not to the preceding DataflowBlocks. In this case we therefore fault all DataflowBlocks
      // that are not yet in a faulted state to ensure that all DataflowBlocks are completed and release their resources.
      if (!InputBlock.Completion.IsFaulted)
        InputBlock.Fault(faultedTask.Exception);
      if (!InnerBlock.Completion.IsFaulted)
        InnerBlock.Fault(faultedTask.Exception);
      if (!OutputBlock.Completion.IsFaulted)
        OutputBlock.Fault(faultedTask.Exception);
    }

    /// <summary>
    /// Runs when all DataflowBlocks are finished
    /// </summary>
    /// <param name="finishedTask">Task representing the state of all DataflowBlocks</param>
    private void OnAllBlocksFinished(Task finishedTask)
    {
      _stopWatch.Stop();
      if (finishedTask.IsFaulted)
      {
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker.{0}.{1}: Error after processing {2} MediaItems; time elapsed: {3}; MaxDegreeOfParallelism(InnerBlock) = {4}", ParentImportJobController, _blockName, _mediaItemsProcessed, _stopWatch.Elapsed, InnerBlockOptions.MaxDegreeOfParallelism);
        // ReSharper disable once AssignNullToNotNullAttribute
        _tcs.SetException(finishedTask.Exception);
      }
      else if (finishedTask.IsCanceled)
      {
        ServiceRegistration.Get<ILogger>().Debug("ImporterWorker.{0}.{1}: Canceled after processing {2} MediaItems; time elapsed: {3}; MaxDegreeOfParallelism(InnerBlock) = {4}", ParentImportJobController, _blockName, _mediaItemsProcessed, _stopWatch.Elapsed, InnerBlockOptions.MaxDegreeOfParallelism);
        _tcs.SetCanceled();
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Debug("ImporterWorker.{0}.{1}: Successfully processed {2} MediaItems; time elapsed: {3}; MaxDegreeOfParallelism(InnerBlock) = {4}", ParentImportJobController, _blockName, _mediaItemsProcessed, _stopWatch.Elapsed, InnerBlockOptions.MaxDegreeOfParallelism);
        _tcs.SetResult(null);
      }
    }

    #endregion

    #region Protected methods

    protected async Task<MediaItem> LoadLocalItem(ResourcePath path, IEnumerable<Guid> necessaryRequestedMiaTypeIds, IEnumerable<Guid> optionalRequestedMiaTypeIds)
    {
      while (true)
      {
        try
        {
          await Activated.WaitAsync();
          // ReSharper disable PossibleMultipleEnumeration
          return _mediaBrowsingCallback.LoadLocalItem(path, necessaryRequestedMiaTypeIds, optionalRequestedMiaTypeIds);
          // ReSharper restore PossibleMultipleEnumeration
        }
        catch (DisconnectedException)
        {
          ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}.{1}: MediaLibrary disconnected. Requesting suspension...", ParentImportJobController, _blockName);
          ParentImportJobController.ParentImporterWorker.RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.Suspend)).Wait();
        }
      }
    }

    protected async Task<ICollection<MediaItem>> Browse(Guid parentDirectoryId, IEnumerable<Guid> necessaryRequestedMiaTypeIds, IEnumerable<Guid> optionalRequestedMiaTypeIds)
    {
      while (true)
      {
        try
        {
          await Activated.WaitAsync();
          // ReSharper disable PossibleMultipleEnumeration
          return _mediaBrowsingCallback.Browse(parentDirectoryId, necessaryRequestedMiaTypeIds, optionalRequestedMiaTypeIds, null, false);
          // ReSharper restore PossibleMultipleEnumeration
        }
        catch (DisconnectedException)
        {
          ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}.{1}: MediaLibrary disconnected. Requesting suspension...", ParentImportJobController, _blockName);
          ParentImportJobController.ParentImporterWorker.RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.Suspend)).Wait();
        }
      }
    }

    protected async Task<ICollection<MediaItem>> GetUpdatableMediaItems(IEnumerable<Guid> necessaryRequestedMiaTypeIds, IEnumerable<Guid> optionalRequestedMiaTypeIds)
    {
      while (true)
      {
        try
        {
          await Activated.WaitAsync();
          // ReSharper disable PossibleMultipleEnumeration
          return _mediaBrowsingCallback.GetUpdatableMediaItems(necessaryRequestedMiaTypeIds, optionalRequestedMiaTypeIds);
          // ReSharper restore PossibleMultipleEnumeration
        }
        catch (DisconnectedException)
        {
          ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}.{1}: MediaLibrary disconnected. Requesting suspension...", ParentImportJobController, _blockName);
          ParentImportJobController.ParentImporterWorker.RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.Suspend)).Wait();
        }
      }
    }

    protected async Task<IDictionary<Guid, DateTime>> GetManagedMediaItemAspectCreationDates()
    {
      while (true)
      {
        try
        {
          await Activated.WaitAsync();
          return _mediaBrowsingCallback.GetManagedMediaItemAspectCreationDates();
        }
        catch (DisconnectedException)
        {
          ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}.{1}: MediaLibrary disconnected. Requesting suspension...", ParentImportJobController, _blockName);
          ParentImportJobController.ParentImporterWorker.RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.Suspend)).Wait();
        }
      }
    }

    protected async Task<ICollection<Guid>> GetAllManagedMediaItemAspectTypes()
    {
      while (true)
      {
        try
        {
          await Activated.WaitAsync();
          return _mediaBrowsingCallback.GetAllManagedMediaItemAspectTypes();
        }
        catch (DisconnectedException)
        {
          ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}.{1}: MediaLibrary disconnected. Requesting suspension...", ParentImportJobController, _blockName);
          ParentImportJobController.ParentImporterWorker.RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.Suspend)).Wait();
        }
      }
    }

    protected async Task<Guid> UpdateMediaItem(Guid parentDirectoryId, ResourcePath path, IEnumerable<MediaItemAspect> updatedAspects, ImportJobInformation jobInfo, bool isRefresh, CancellationToken cancelToken)
    {
      while (true)
      {
        try
        {
          await Activated.WaitAsync();
          // ReSharper disable PossibleMultipleEnumeration
          return _importResultHandler.UpdateMediaItem(parentDirectoryId, path, updatedAspects, isRefresh, jobInfo.BasePath, cancelToken);
          // ReSharper restore PossibleMultipleEnumeration
        }
        catch (DisconnectedException)
        {
          ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}.{1}: MediaLibrary disconnected. Requesting suspension...", ParentImportJobController, _blockName);
          ParentImportJobController.ParentImporterWorker.RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.Suspend)).Wait();
        }
      }
    }

    protected async Task DeleteMediaItem(ResourcePath path)
    {
      while (true)
      {
        try
        {
          await Activated.WaitAsync();
          _importResultHandler.DeleteMediaItem(path);
          return;
        }
        catch (DisconnectedException)
        {
          ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}.{1}: MediaLibrary disconnected. Requesting suspension...", ParentImportJobController, _blockName);
          ParentImportJobController.ParentImporterWorker.RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.Suspend)).Wait();
        }
      }
    }

    protected async Task DeleteUnderPath(ResourcePath path)
    {
      while (true)
      {
        try
        {
          await Activated.WaitAsync();
          _importResultHandler.DeleteUnderPath(path);
          return;
        }
        catch (DisconnectedException)
        {
          ServiceRegistration.Get<ILogger>().Info("ImporterWorker.{0}.{1}: MediaLibrary disconnected. Requesting suspension...", ParentImportJobController, _blockName);
          ParentImportJobController.ParentImporterWorker.RequestAction(new ImporterWorkerAction(ImporterWorkerAction.ActionType.Suspend)).Wait();
        }
      }
    }

    protected Task<IDictionary<Guid, IList<MediaItemAspect>>> ExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> existingAspects, bool importOnly)
    {
      // ToDo: This is a workaround. MetadataExtractors should have an async ExtractMetadata method that returns a Task.
      return Task.FromResult(ServiceRegistration.Get<IMediaAccessor>().ExtractMetadata(mediaItemAccessor, ImportJobInformation.MetadataExtractorIds, existingAspects, importOnly));
    }

    #endregion

    #region Public methods

    public void Activate(IMediaBrowsing mediaBrowsingCallback, IImportResultHandler importResultHandler)
    {
      _mediaBrowsingCallback = mediaBrowsingCallback;
      _importResultHandler = importResultHandler;
      _suspensionLink = InputBlock.LinkTo(InnerBlock, new DataflowLinkOptions { PropagateCompletion = true });
      _stopWatch.Start();
      Activated.Set();
    }

    public void Suspend()
    {
      Activated.Reset();
      if (_suspensionLink != null)
      {
        _suspensionLink.Dispose();
        _suspensionLink = null;
      }
      _stopWatch.Stop();
    }

    #endregion

    #region Interface implementations

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, PendingImportResourceNewGen messageValue, ISourceBlock<PendingImportResourceNewGen> source, bool consumeToAccept)
    {
      return InputBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    }

    public void Complete()
    {
      InputBlock.Complete();
    }

    void IDataflowBlock.Fault(Exception exception)
    {
      InputBlock.Fault(exception);
    }

    public Task Completion
    {
      get { return _tcs.Task; }
    }

    public IDisposable LinkTo(ITargetBlock<PendingImportResourceNewGen> target, DataflowLinkOptions linkOptions)
    {
      return OutputBlock.LinkTo(target, linkOptions);
    }

    PendingImportResourceNewGen ISourceBlock<PendingImportResourceNewGen>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<PendingImportResourceNewGen> target, out bool messageConsumed)
    {
      return OutputBlock.ConsumeMessage(messageHeader, target, out messageConsumed);
    }

    bool ISourceBlock<PendingImportResourceNewGen>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<PendingImportResourceNewGen> target)
    {
      return OutputBlock.ReserveMessage(messageHeader, target);
    }

    void ISourceBlock<PendingImportResourceNewGen>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<PendingImportResourceNewGen> target)
    {
      OutputBlock.ReleaseReservation(messageHeader, target);
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return _blockName;
    }

    #endregion
  }
}
