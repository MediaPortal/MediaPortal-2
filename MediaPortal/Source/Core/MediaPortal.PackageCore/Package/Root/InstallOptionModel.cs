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
using System.Linq;
using System.Xml.Serialization;
using MediaPortal.Common.Logging;

namespace MediaPortal.PackageCore.Package.Root
{
  [DebuggerDisplay("InstallOption({Name}, IsDefault={IsDefault})")]
  partial class InstallOptionModel : ICheckable
  {
    #region private fields

    private WeakReference<InstallOptionsModel> _parent;

    #endregion

    #region public properties

    [XmlIgnore]
    public InstallOptionsModel Parent
    {
      get
      {
        InstallOptionsModel package;
        if (_parent != null && _parent.TryGetTarget(out package))
        {
          return package;
        }
        return null;
      }
      private set { _parent = new WeakReference<InstallOptionsModel>(value); }
    }

    [XmlIgnore]
    public PackageModel Package
    {
      get { return Parent == null ? null : Parent.Package; }
    }

    #endregion

    #region private/internal methods

    public void Initialize(InstallOptionsModel options, ILogger log)
    {
      Parent = options;

      foreach (var content in Contents)
      {
        content.Initialize(this, log);
      }
    }

    #endregion

    #region Implementation of ICheckable

    [XmlIgnore]
    public string ElementsName
    {
      get { return String.Format("Option {0}", Name); }
    }

    public bool CheckElements(ILogger log)
    {
      return Contents.Aggregate(
        this.CheckNotNullOrEmpty(Name, "Name", log), 
        (current, content) => current & content.CheckElements(log));
    }

    #endregion
  }
}