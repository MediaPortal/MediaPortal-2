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

namespace MediaPortal.PackageCore.Package.Root
{
  [DebuggerDisplay("Image({Type}: {Path})")]
  partial class ImageModel : ICheckable
  {
    #region private fields

    private WeakReference<ImagesModel> _parent;

    #endregion

    #region public properties

    [XmlIgnore]
    public ImagesModel Parent
    {
      get
      {
        ImagesModel package;
        if (_parent != null && _parent.TryGetTarget(out package))
        {
          return package;
        }
        return null;
      }
      private set { _parent = new WeakReference<ImagesModel>(value); }
    }

    [XmlIgnore]
    public PackageModel Package
    {
      get { return Parent == null ? null : Parent.Package; }
    }

    #endregion

    #region public methods

    public void Initialize(ImagesModel images, ILogger log)
    {
      Parent = images;
    }

    #endregion


    #region Implementation of ICheckable

    [XmlIgnore]
    public string ElementsName
    {
      get { return String.Format("Image {0}", Type); }
    }

    public bool CheckElements(ILogger log)
    {
      return this.CheckNotNullOrEmpty(Path, "Path", log);
    }

    #endregion
  }
}