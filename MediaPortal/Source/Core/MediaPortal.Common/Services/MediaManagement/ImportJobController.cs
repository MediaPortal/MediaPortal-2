#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks;

namespace MediaPortal.Common.Services.MediaManagement
{
  /// <summary>
  /// An <see cref="ImportJobController"/> is responsible for handling a single ImportJob
  /// </summary>
  /// <remarks>
  /// It holds the TPL Datflow network for the ImportJob and is responsible for cancelling
  /// and suspending the particular ImportJob. If it determines that the <see cref="ImporterWorkerNewGen"/>
  /// shoud be suspended (e.g. because on the MP2 Client side the MP2 Server is disconnected) it notifies the
  /// <see cref="ImporterWorkerNewGen"/> (_parent) which will then make sure that all <see cref="ImportJobController"/>s
  /// are suspended.
  /// ToDo: Handle cancellation with a <see cref="CancellationToken"/>
  /// ToDo: Handle suspension
  /// ToDo: Make this class (or a separate status class) serializable by the XMLSerializer to be able to save the status on shutdown
  /// </remarks>
  public class ImportJobController
  {
    #region Variables

    private readonly ImportJobInformation _importJobInformation;
    private readonly ImporterWorkerNewGen _parent;

    private DirectoryUnfoldBlock _directoryUnfoldBlock;

    #endregion

    #region Constructor

    public ImportJobController(ImportJobInformation importJobInformation, ImporterWorkerNewGen parent)
    {
      _importJobInformation = importJobInformation;
      _parent = parent;

      SetupDataflowBlocks();
      LinkDataflowBlocks();
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Returns a <see cref="Task"/> that represents the status of the ImportJob
    /// </summary>
    public Task Completion
    {
      get
      {
        // Todo: This task shall complete when the ImportJob is completed. Most likely return the Completion property of the last DataflowBlock
        return _directoryUnfoldBlock.Completion;
      }      
    }

    #endregion

    #region Public methods

    public void Cancel()
    {      
    }

    public void Suspend()
    {      
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Instantiates all the necessary DataflowBlocks for the given ImportJob
    /// </summary>
    /// <remarks>
    /// ToDo: We need to handle three cases here:
    /// - BasePath points to a single resource
    /// - BasePath points to a directory which is not a single resource and the ImportJob does not include subdirectories
    /// - BasePath points to a directory which is not a single resource and the ImportJob does include subdirectories
    /// </remarks>
    private void SetupDataflowBlocks()
    {
      _directoryUnfoldBlock = new DirectoryUnfoldBlock(_importJobInformation.BasePath);
    }

    private void LinkDataflowBlocks()
    {
      _directoryUnfoldBlock.LinkTo(DataflowBlock.NullTarget<PendingImportResource>());
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return String.Format("ImportJob for path ='{0}', ImportJobType='{1}', IncludeSubdirectories='{2}'", _importJobInformation.BasePath, _importJobInformation.JobType, _importJobInformation.IncludeSubDirectories);
    }

    #endregion
  }
}
