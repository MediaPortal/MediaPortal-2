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
using System.Xml.Serialization;
using MediaPortal.Common.Logging;
using MediaPortal.PackageCore.Package.Root;

namespace MediaPortal.PackageCore.Package.Action
{
  partial class ActionBaseModel : ICheckable
  {
    #region private fields

    private WeakReference<ActionsBaseModel> _parent;

    #endregion

    #region public properties

    [XmlIgnore]
    public ActionsBaseModel Parent
    {
      get
      {
        ActionsBaseModel package;
        if (_parent != null && _parent.TryGetTarget(out package))
        {
          return package;
        }
        return null;
      }
      private set { _parent = new WeakReference<ActionsBaseModel>(value); }
    }

    [XmlIgnore]
    public PackageModel Package
    {
      get { return Parent == null ? null : Parent.Package; }
    }

    #endregion

    #region public methods

    public abstract void Execute(InstallContext context);

    #endregion

    #region internal/private methods

    public virtual void Initialize(ActionsBaseModel actions, ILogger log)
    {
      Parent = actions;
    }

    #endregion

    #region Implementation of ICheckable

    [XmlIgnore]
    public abstract string ElementsName { get; }

    public virtual bool CheckElements(ILogger log)
    {
      return true;
    }

    #endregion
  }
}