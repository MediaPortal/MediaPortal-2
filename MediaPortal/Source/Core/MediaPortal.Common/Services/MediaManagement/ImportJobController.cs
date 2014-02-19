#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks;

namespace MediaPortal.Common.Services.MediaManagement
{
  public class ImportJobController
  {
    #region Variables

    private readonly ImportJobInformation _importJobInformation;
    private readonly ImporterWorkerNewGen _parent;

    private readonly DirectoryUnfoldBlock _directoryUnfoldBlock;

    #endregion

    #region Constructor

    public ImportJobController(ImportJobInformation importJobInformation, ImporterWorkerNewGen parent)
    {
      _importJobInformation = importJobInformation;
      _parent = parent;

      _directoryUnfoldBlock = new DirectoryUnfoldBlock(importJobInformation.BasePath);
      _directoryUnfoldBlock.LinkTo(DataflowBlock.NullTarget<PendingImportResource>());
    }

    #endregion

    #region Public properties

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
    #endregion

    #region Base overrides

    public override string ToString()
    {
      return String.Format("ImportJob for path ='{0}', ImportJobType='{1}', IncludeSubdirectories='{2}'", _importJobInformation.BasePath, _importJobInformation.JobType, _importJobInformation.IncludeSubDirectories);
    }

    #endregion
  }
}
