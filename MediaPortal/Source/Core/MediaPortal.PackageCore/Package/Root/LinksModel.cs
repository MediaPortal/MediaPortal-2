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
  [DebuggerDisplay("Links({Links.Count})")]
  partial class LinksModel : ICheckable
  {
    #region private fields

    private WeakReference<PackageModel> _package;

    #endregion

    #region public properties

    [XmlIgnore]
    public PackageModel Package
    {
      get
      {
        PackageModel package;
        if (_package != null && _package.TryGetTarget(out package))
        {
          return package;
        }
        return null;
      }
      private set { _package = new WeakReference<PackageModel>(value); }
    }

    #endregion

    #region public methods

    public void Initialize(PackageModel package, ILogger log)
    {
      Package = package;
      foreach (var link in Links)
      {
        link.Initialize(this, log);
      }
    }

    #endregion

    #region Implementation of ICheckable

    [XmlIgnore]
    public string ElementsName
    {
      get { return "Links"; }
    }

    public bool CheckElements(ILogger log)
    {
      return Links.Aggregate(true, (current, model) => current && model.CheckElements(log));
    }

    #endregion
  }
}