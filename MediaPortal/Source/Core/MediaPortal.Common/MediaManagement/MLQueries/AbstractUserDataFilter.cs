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

namespace MediaPortal.Common.MediaManagement.MLQueries
{
  public abstract class AbstractUserDataFilter : IUserDataFilter
  {
    protected Guid _userProfileId;
    protected string _userDataKey;

    protected AbstractUserDataFilter(Guid userProfileId, string userDataKey)
    {
      _userProfileId = userProfileId;
      _userDataKey = userDataKey;
    }

    [XmlIgnore]
    public Guid UserProfileId
    {
      get { return _userProfileId; }
      set { _userProfileId = value; }
    }

    [XmlIgnore]
    public string UserDataKey
    {
      get { return _userDataKey; }
      set { _userDataKey = value; }
    }

    #region Additional members for the XML serialization

    internal AbstractUserDataFilter() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("UserProfileId", IsNullable = false)]
    public string XML_UserProfileId
    {
      get { return SerializationHelper.SerializeGuid(_userProfileId); }
      set { _userProfileId = SerializationHelper.DeserializeGuid(value); }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("UserDataKey", IsNullable = false)]
    public string XML_UserDataKey
    {
      get { return _userDataKey; }
      set { _userDataKey = value; }
    }

    #endregion
  }
}
