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
using System.Xml.Serialization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.MediaManagement
{
  /// <summary>
  /// Holds the data of one pending import resource for the <see cref="ImporterWorkerNewGen"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class is used to transport information from one DataflowBlock to the next one. Depending on
  /// where we are in the TPL Dataflow network, some of the properties of this class may or may not be valid.
  /// It is also used to persist the state of the <see cref="ImporterWorkerNewGen"/> when shut down while ImportJobs
  /// are still running. It is therefore necessary to also persist where in the TPL Dataflow network the
  /// <see cref="PendingImportResourceNewGen"/> was when shut-down occured and this class was persisted.
  /// </para>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// ToDo: Adapt to additional DataflowBlocks
  /// </remarks>
  public class PendingImportResourceNewGen : IDisposable
  {
    private readonly ImportJobController _parentImportJobController;
    
    private IFileSystemResourceAccessor _parentDirectory;
    private IFileSystemResourceAccessor _resourceAccessor;
    private bool _isSingleResource = true;
    private bool _isValid = true;

    public PendingImportResourceNewGen(IFileSystemResourceAccessor parentDirectory, IFileSystemResourceAccessor resourceAccessor, ImportJobController parentImportJobController)
    {
      _parentDirectory = parentDirectory;
      _resourceAccessor = resourceAccessor;
      _parentImportJobController = parentImportJobController;
      _parentImportJobController.RegisterPendingImportResource(this);
    }

    public void Dispose()
    {
      _parentImportJobController.UnregisterPendingImportResource(this);
      try
      {
        if (_resourceAccessor != null)
        {
          _resourceAccessor.Dispose();
          _resourceAccessor = null;
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("PendingImportResource: Could not dispose resource", e);
      }
      try
      {
        if (_parentDirectory != null)
        {
          _parentDirectory.Dispose();
          _parentDirectory = null;
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("PendingImportResource: Could not dispose resource", e);
      }
    }

    [XmlIgnore]
    public IFileSystemResourceAccessor ParentDirectory
    {
      get { return _parentDirectory; }
    }

    [XmlIgnore]
    public IFileSystemResourceAccessor ResourceAccessor
    {
      get { return _resourceAccessor; }
    }

    [XmlIgnore]
    public bool IsIngleResource
    {
      get { return _isSingleResource; }
      set { _isSingleResource = value; }
    }

    [XmlIgnore]
    public bool IsValid
    {
      get { return _isValid; }
    }

    public override string ToString()
    {
      return string.Format("PendingImportResource '{0}' (parent directory={1})", _resourceAccessor, _parentDirectory);
    }

    #region Additional members for the XML serialization

    internal PendingImportResourceNewGen()
    {
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("ResourceAccessor")]
    public string XML_ResourceAccessor
    {
      get { return _resourceAccessor.CanonicalLocalResourcePath.Serialize(); }
      set
      {
        try
        {
          IResourceAccessor ra;
          if (!ResourcePath.Deserialize(value).TryCreateLocalResourceAccessor(out ra))
          {
            _isValid = false;
            return;
          }
          var fsra = ra as IFileSystemResourceAccessor;
          if (fsra == null)
          {
            ra.Dispose();
            ServiceRegistration.Get<ILogger>().Error("PendingImportResource: Could not load resource '{0}': It is no filesystem resource", value);
          }
          _resourceAccessor = fsra;
        }
        catch (Exception)
        {
          _isValid = false;
        }
      }
    }

    #endregion
  }

}