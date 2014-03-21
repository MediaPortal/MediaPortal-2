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
  /// <para>
  /// This class is not threadsafe. In particular the ResourceAccessor property must only be accesseed from one
  /// thread at a time. While flowing through the Dataflow network, TPL Dataflow ensures that only one thread
  /// accesses one particular object of this class.
  /// </para>
  /// ToDo: Adapt to additional DataflowBlocks
  /// </remarks>
  public class PendingImportResourceNewGen : IDisposable
  {
    #region Enums

    public enum DataflowNetworkPosition : byte 
    {
      None,
      DirectoryUnfoldBlock,
      // ToDo: Add additional DataflowBlocks
    }

    #endregion

    #region Variables

    // Runtime data not persisted during serialization
    private int _pendingImportResourceNumber;
    private ImportJobController _parentImportJobController;
    private bool _isValid;
    
    // Resource data recreated after deserialization
    // The _resourceAccessor will after deserialization only be created from the _resourcePathString
    // on demand, i.e. if the ResourceAccessor property is accessed
    private IFileSystemResourceAccessor _resourceAccessor;
    
    // Resource data that is (de)serializable
    private String _resourcePathString;
    private String _parentDirectoryResourcePathString;
    private bool _isSingleResource = true;
    private DataflowNetworkPosition _lastFinishedBlock;

    #endregion

    #region Constructor

    public PendingImportResourceNewGen(ResourcePath parentDirectory, IFileSystemResourceAccessor resourceAccessor, DataflowNetworkPosition lastFinishedBlock, ImportJobController parentImportJobController)
    {
      _parentDirectoryResourcePathString = (parentDirectory == null) ? "" : parentDirectory.Serialize();
      _resourceAccessor = resourceAccessor;      
      _lastFinishedBlock = lastFinishedBlock;
      _parentImportJobController = parentImportJobController;
      _pendingImportResourceNumber = _parentImportJobController.GetNumberOfNextPendingImportResource();

      _isValid = (_resourceAccessor != null);

      _parentImportJobController.RegisterPendingImportResource(this);
    }

    #endregion

    #region Public properties

    [XmlIgnore]
    public int PendingImportResourceNumber
    {
      get { return _pendingImportResourceNumber; }
    }

    [XmlIgnore]
    public ResourcePath ParentDirectory
    {
      get
      {
        if (_parentDirectoryResourcePathString == "")
            return null;
        return ResourcePath.Deserialize(_parentDirectoryResourcePathString);
      }
    }

    [XmlIgnore]
    public IFileSystemResourceAccessor ResourceAccessor
    {
      get
      {
        if (_resourceAccessor != null)
          return _resourceAccessor;
        _isValid = GetFsraFromResourcePathString(_resourcePathString, out _resourceAccessor);
        return _resourceAccessor;
      }
    }

    [XmlIgnore]
    public ResourcePath PendingResourcePath
    {
      get
      {
        if (!String.IsNullOrEmpty(_resourcePathString))
          return ResourcePath.Deserialize(_resourcePathString);
        return _resourceAccessor.CanonicalLocalResourcePath;
      }
    }

    [XmlIgnore]
    public bool IsSingleResource
    {
      get { return _isSingleResource; }
      set { _isSingleResource = value; }
    }

    [XmlIgnore]
    public DataflowNetworkPosition LastFinishedBlock
    {
      get { return _lastFinishedBlock; }
      set { _lastFinishedBlock = value; }
    }

    [XmlIgnore]
    public bool IsValid
    {
      get { return _isValid; }
      set
      {
        if (value)
          ServiceRegistration.Get<ILogger>().Warn("ImporterWorker / {0}: A PendingImportResource's IsValid property should not be set to true from outside.", _parentImportJobController);
        _isValid = value;
      }
    }

    #endregion

    #region Private Methods

    private bool GetFsraFromResourcePathString(String resourcePathString, out IFileSystemResourceAccessor fsra)
    {
      try
      {
        IResourceAccessor ra;
        if (!ResourcePath.Deserialize(resourcePathString).TryCreateLocalResourceAccessor(out ra))
        {
          fsra = null;
          ServiceRegistration.Get<ILogger>().Error("ImporterWorker / {0}: Could not create ResourceAccessor for resource '{1}': It is no filesystem resource", _parentImportJobController, resourcePathString);
          return false;
        }
        fsra = ra as IFileSystemResourceAccessor;
        if (fsra == null)
        {
          ra.Dispose();
          ServiceRegistration.Get<ILogger>().Error("ImporterWorker / {0}: Could not load resource '{1}': It is no filesystem resource", _parentImportJobController, resourcePathString);
          return false;
        }
      }
      catch (Exception ex)
      {
        fsra = null;
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker / {0}: Error creating ResourceAccessor for resource '{1}'", ex, _parentImportJobController, resourcePathString);
        return false;
      }
      return true;
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      string identifier = (_resourceAccessor != null) ? _resourceAccessor.CanonicalLocalResourcePath.ToString() : _resourcePathString ?? "<null>";
      return string.Format("PendingImportResource '{0}' (parent directory={1})", identifier, _parentDirectoryResourcePathString);
    }

    #endregion

    #region Interface Implementations

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
    }

    #endregion

    #region Additional members for the XML serialization

    /// <summary>
    /// Constructor for internal use of the XML serialization system only.
    /// </summary>
    internal PendingImportResourceNewGen()
    {
    }

    /// <summary>
    /// Initializes this PendingImportResource after deserialization
    /// </summary>
    /// <remarks>
    /// This method must be called onces after this PendingImportResource has been deserialized.
    /// It must not be called in any other circumstances.
    /// </remarks>
    /// <param name="parentImportJobController">ImportJobController this PendingImportResource belongs to</param>
    public void InitializeAfterDeserialization(ImportJobController parentImportJobController)
    {
      _parentImportJobController = parentImportJobController;
      _pendingImportResourceNumber = _parentImportJobController.GetNumberOfNextPendingImportResource();
      _isValid = true;
      _parentImportJobController.RegisterPendingImportResource(this);
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("ResourceAccessor")]
    public string XmlResourceAccessor
    {
      get
      {
        if (_resourcePathString != null)
          return _resourcePathString;
        return _resourceAccessor != null ? _resourceAccessor.CanonicalLocalResourcePath.Serialize() : "";
      }
      set { _resourcePathString = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("ParentDirectory")]
    public string XmlParentDirectoryResourcePathString
    {
      get { return _parentDirectoryResourcePathString; }
      set { _parentDirectoryResourcePathString = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("IsSingleResource")]
    public bool XmlIsSingleResource
    {
      get { return _isSingleResource; }
      set { _isSingleResource = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("LastFinishedBlock")]
    public DataflowNetworkPosition XmlLastFinishedBlock
    {
      get { return _lastFinishedBlock; }
      set { _lastFinishedBlock = value; }
    }

    #endregion
  }

}