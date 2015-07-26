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
using MediaPortal.PackageCore.Package.Root;

namespace MediaPortal.PackageCore.Package.Content
{
  [DebuggerVisualizer("Content({Content.Count})")]
  partial class ContentModel : ICheckable
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

    public ContentBaseModel GetContent(string name)
    {
      return Contents.FirstOrDefault(content => String.Equals(content.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region private/internal methods

    public void Initialize(PackageModel package, ILogger log)
    {
      Package = package;

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
      get { return "Content"; }
    }

    public bool CheckElements(ILogger log)
    {
      return Contents.Aggregate(true, (current, content) => current & content.CheckElements(log));
    }

    #endregion
  }
}