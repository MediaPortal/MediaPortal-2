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
using System.Linq;
using System.Xml.Serialization;
using MediaPortal.Common.Logging;
using MediaPortal.PackageCore.Package.Content;
using MediaPortal.PackageCore.Package.Root;

namespace MediaPortal.PackageCore.Package.Action
{
  partial class ActionsBaseModel : ICheckable
  {
    #region private fields

    private WeakReference<ContentBaseModel> _parent;

    #endregion

    #region public properties

    [XmlIgnore]
    public ContentBaseModel Parent
    {
      get
      {
        ContentBaseModel package;
        if (_parent != null && _parent.TryGetTarget(out package))
        {
          return package;
        }
        return null;
      }
      private set { _parent = new WeakReference<ContentBaseModel>(value); }
    }

    [XmlIgnore]
    public PackageModel Package
    {
      get { return Parent == null ? null : Parent.Package; }
    }

    #endregion

    #region public methods

    public virtual void SetAllOverwriteTrue()
    {
      foreach (var action in Actions)
      {
        var overwriteAction = action as CanOverwriteActionBaseModel;
        if (overwriteAction != null)
        {
          overwriteAction.Overwrite = true;
        }
      }
    }

    public void Install(InstallContext context)
    {
      foreach (var action in Actions)
      {
        action.Execute(context);
      }
    }

    #endregion

    #region internal/private methods

    internal virtual void Initialize(ContentBaseModel content, ILogger log)
    {
      Parent = content;
      foreach (var action in Actions)
      {
        action.Initialize(this, log);
      }
    }

    #endregion

    #region Implementation of ICheckable

    [XmlIgnore]
    public abstract string ElementsName { get; }

    public virtual bool CheckElements(ILogger log)
    {
      return Actions.Aggregate(true, (current, action) => current && action.CheckElements(log));
    }

    #endregion
  }
}