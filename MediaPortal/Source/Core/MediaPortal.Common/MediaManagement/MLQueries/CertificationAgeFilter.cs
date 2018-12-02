#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System.Xml.Serialization;
using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Common.MediaManagement.MLQueries
{
  /// <summary>
  /// Specifies an age for which to filters media items by.
  /// </summary>
  public class CertificationAgeFilter : IFilter
  {
    protected int _age;
    protected bool _includeRestrictedContent;
    protected bool _includeUnratedContent;
    protected Guid _contentTypeId;

    public CertificationAgeFilter(Guid contentMIATypeId, int requiredMinimumAge, bool includeParentGuidedContent = false, bool includeUnrated = false)
    {
      _contentTypeId = contentMIATypeId;
      _age = requiredMinimumAge;
      _includeRestrictedContent = includeParentGuidedContent;
      _includeUnratedContent = includeUnrated;
    }
    //Necessary
    public CertificationAgeFilter(IEnumerable<Guid> queryNecessaryMIATypeId, int requiredMinimumAge, bool includeParentGuidedContent = false, bool includeUnrated = false)
    {
      foreach(var miaId in queryNecessaryMIATypeId)
      {
        if(miaId == MovieAspect.ASPECT_ID || miaId == EpisodeAspect.ASPECT_ID || miaId == SeriesAspect.ASPECT_ID)
        {
          _contentTypeId = miaId;
          break;
        }
      }
      _age = requiredMinimumAge;
      _includeRestrictedContent = includeParentGuidedContent;
      _includeUnratedContent = includeUnrated;
    }

    [XmlIgnore]
    public Guid ContentMIATypeId
    {
      get { return _contentTypeId; }
      set { _contentTypeId = value; }
    }

    [XmlIgnore]
    public int RequiredMinimumAge
    {
      get { return _age; }
      set { _age = value; }
    }

    [XmlIgnore]
    public bool IncludeParentGuidedContent
    {
      get { return _includeRestrictedContent; }
      set { _includeRestrictedContent = value; }
    }

    [XmlIgnore]
    public bool IncludeUnratedContent
    {
      get { return _includeUnratedContent; }
      set { _includeUnratedContent = value; }
    }

    public override string ToString()
    {
      return " REQUIRED_AGE <= " + _age;
    }

    #region Additional members for the XML serialization

    internal CertificationAgeFilter() { }

    [XmlAttribute("UnratedContent")]
    public bool XML_IncludeUnratedContent
    {
      get { return _includeUnratedContent; }
      set { _includeUnratedContent = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Age")]
    public int XML_Age
    {
      get { return _age; }
      set { _age = value; }
    }

    [XmlAttribute("RestrictedContent")]
    public bool XML_IncludeRestrictedContent
    {
      get { return _includeRestrictedContent; }
      set { _includeRestrictedContent = value; }
    }

    [XmlAttribute("ContentMIATypeId")]
    public string XML_ContentMIATypeId
    {
      get { return SerializationHelper.SerializeGuid(_contentTypeId); }
      set { _contentTypeId = SerializationHelper.DeserializeGuid(value); }
    }

    #endregion
  }
}
