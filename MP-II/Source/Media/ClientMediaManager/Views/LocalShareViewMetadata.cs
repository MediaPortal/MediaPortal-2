#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Media.ClientMediaManager.Views
{
  /// <summary>
  /// Holds the metadata of a view which is based on a local share path.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class LocalShareViewMetadata : ViewMetadata
  {
    #region Protected fields

    protected Guid _shareId;
    protected string _path;
    protected string _sharePath;

    #endregion

    internal LocalShareViewMetadata(Guid viewId, string displayName, Guid shareId, string path,
        Guid? parentViewId, IEnumerable<Guid> mediaItemAspectIds) :
        base(viewId, displayName, parentViewId, mediaItemAspectIds)
    {
      _shareId = shareId;
      _path = path;
      _sharePath = ServiceScope.Get<ISharesManagement>().GetShare(shareId).Path;
    }

    /// <summary>
    /// Returns the id of the media provider the view is based on.
    /// </summary>
    [XmlIgnore]
    public Guid ShareId
    {
      get { return _shareId; }
    }

    /// <summary>
    /// Returns the path in the share this view is based on. This path is relative to the share's location,
    /// so it is different from the <see cref="ProviderPath"/>, which denotes the full view's path at
    /// the underlaying provider.
    /// </summary>
    [XmlIgnore]
    public string Path
    {
      get { return _path; }
    }

    /// <summary>
    /// Returns the path of this share in the provider. This differs from the <see cref="Path"/> of the share!
    /// </summary>
    [XmlIgnore]
    public string ProviderPath
    {
      get { return System.IO.Path.Combine(_sharePath, _path); }
    }

    #region Additional members for the XML serialization

    internal LocalShareViewMetadata() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("ShareId")]
    public Guid XML_ShareId
    {
      get { return _shareId; }
      set { _shareId = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("Path")]
    public string XML_Path
    {
      get { return _path; }
      set { _path = value; }
    }

    #endregion
  }
}
