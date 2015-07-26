#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Diagnostics;
using System.Xml.Serialization;
using MediaPortal.Common.Logging;
using MediaPortal.PackageCore.Package.Content;

namespace MediaPortal.PackageCore.Package.Root
{
  [DebuggerDisplay("ContentRef({Name})")]
  public partial class ContentRefModel : ICheckable
  {
    #region public properties

    [XmlIgnore]
    public PackageModel Package
    {
      get { return Option.Package; }
    }

    [XmlIgnore]
    public InstallOptionModel Option { get; private set; }

    [XmlIgnore]
    public ContentBaseModel ReferencedContent { get; private set; }

    #endregion

    #region private/internal methods

    public void Initialize(InstallOptionModel option, ILogger log)
    {
      Option = option;

      ReferencedContent = Package.Content.GetContent(Name);
    }

    #endregion

    #region Implementation of ICheckable

    [XmlIgnore]
    public string ElementsName
    {
      get { return String.Format("ContentReference {0}", Name); }
    }

    public bool CheckElements(ILogger log)
    {
      bool ok = this.CheckNotNullOrEmpty(Name, "Name", log);
      if (ReferencedContent == null)
      {
        if (log != null)
        {
          log.Error("{0}: The referenced content '{1}' could not be found", ElementsName, Name);
        }
        ok = false;
      }
      return ok;
    }

    #endregion
  }
}