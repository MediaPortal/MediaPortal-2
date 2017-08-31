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

using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Utilities.DeepCopy;
using System;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Markup extension which performs a media item query using a <see cref="RelationshipFilter"/> and
  /// updates the target property with the returned <see cref="Common.MediaManagement.MediaItem"/>(s).
  /// </summary>
  public class RelationshipQueryExtension : MediaItemQueryExtension
  {
    #region Protected fields

    protected AbstractProperty _roleProperty;
    protected AbstractProperty _linkedRoleProperty;
    protected AbstractProperty _linkedMediaItemIdProperty;

    #endregion

    #region Ctor/Init

    public RelationshipQueryExtension()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _roleProperty = new SProperty(typeof(Guid?), null);
      _linkedRoleProperty = new SProperty(typeof(Guid?), null);
      _linkedMediaItemIdProperty = new SProperty(typeof(Guid?), null);
    }

    void Attach()
    {
      _roleProperty.Attach(OnPropertyChanged);
      _linkedRoleProperty.Attach(OnPropertyChanged);
      _linkedMediaItemIdProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _roleProperty.Detach(OnPropertyChanged);
      _linkedRoleProperty.Detach(OnPropertyChanged);
      _linkedMediaItemIdProperty.Detach(OnPropertyChanged);
    }

    #endregion

    #region Event handlers

    protected void OnPropertyChanged(AbstractProperty property, object oldValue)
    {
      if (_active)
        UpdateFilter();
    }

    #endregion

    #region Protected methods

    protected void UpdateFilter()
    {
      Guid? role = Role;
      Guid? linkedRole = LinkedRole;
      Guid? linkedMediaItemId = LinkedMediaItemId;
      //If all properties are valid, create the RelationshipFilter
      if (role.HasValue && linkedRole.HasValue && linkedMediaItemId.HasValue)
        //Setting a valid filter causes the underlying MediaItemQueryExtension to
        //perform the actual query
        Filter = new RelationshipFilter(role.Value, linkedRole.Value, linkedMediaItemId.Value);
    }

    #endregion

    #region Base overrides

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      RelationshipQueryExtension rqe = (RelationshipQueryExtension)source;
      Role = rqe.Role;
      LinkedRole = rqe.LinkedRole;
      LinkedMediaItemId = rqe.LinkedMediaItemId;
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }

    #endregion

    #region GUI Properties

    public AbstractProperty RoleProperty
    {
      get { return _roleProperty; }
    }

    public Guid? Role
    {
      get { return (Guid?)_roleProperty.GetValue(); }
      set { _roleProperty.SetValue(value); }
    }

    public AbstractProperty LinkedRoleProperty
    {
      get { return _linkedRoleProperty; }
    }

    public Guid? LinkedRole
    {
      get { return (Guid?)_linkedRoleProperty.GetValue(); }
      set { _linkedRoleProperty.SetValue(value); }
    }

    public AbstractProperty LinkedMediaItemIdProperty
    {
      get { return _linkedMediaItemIdProperty; }
    }

    public Guid? LinkedMediaItemId
    {
      get { return (Guid?)_linkedMediaItemIdProperty.GetValue(); }
      set { _linkedMediaItemIdProperty.SetValue(value); }
    }

    #endregion
  }
}
