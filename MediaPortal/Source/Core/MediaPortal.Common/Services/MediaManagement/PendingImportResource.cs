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
using System.Xml.Serialization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.MediaManagement
{
  /// <summary>
  /// Holds the data of one pending import resource.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class PendingImportResource
  {
    protected Guid _parentDirectory;
    protected IFileSystemResourceAccessor _resourceAccessor;
    protected bool _isValid = true;

    public PendingImportResource(Guid parentDirectory, IFileSystemResourceAccessor resourceAccessor)
    {
      _parentDirectory = parentDirectory;
      _resourceAccessor = resourceAccessor;
    }

    public void Dispose()
    {
      try
      {
        if (_resourceAccessor != null)
          _resourceAccessor.Dispose();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("PendingImportResource: Could not dispose resource", e);
      }
    }

    [XmlIgnore]
    public Guid ParentDirectory
    {
      get { return _parentDirectory; }
    }

    [XmlIgnore]
    public IFileSystemResourceAccessor ResourceAccessor
    {
      get { return _resourceAccessor; }
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

    internal PendingImportResource() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("ParentDirectory")]
    public Guid XML_ParentDirectory
    {
      get { return _parentDirectory; }
      set { _parentDirectory = value; }
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
          IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
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