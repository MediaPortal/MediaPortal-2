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

    #region Protected methods

    protected void UpdateFilter()
    {
      Guid? role = Role;
      Guid? linkedRole = LinkedRole;
      Guid? linkedMediaItemId = LinkedMediaItemId;

      IFilter filter = null;
      //If all properties are valid, create the RelationshipFilter
      if (role.HasValue && linkedMediaItemId.HasValue)
        filter = new RelationshipFilter(role.Value, linkedRole.HasValue ? linkedRole.Value : Guid.Empty, linkedMediaItemId.Value);

      //Setting a valid filter causes the underlying MediaItemQueryExtension to
      //perform the actual query or setting to null resets the target property to null
      Filter = filter;
    }

    #endregion

    #region Base overrides

    protected override void OnBeginUpdate()
    {
      UpdateFilter();
      base.OnBeginUpdate();
    }

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
      Detach();
      base.Dispose();
    }

    #endregion

    #region GUI Properties

    public AbstractProperty RoleProperty
    {
      get { return _roleProperty; }
    }

    /// <summary>
    /// Role to use for the relationship filter.
    /// </summary>
    public Guid? Role
    {
      get { return (Guid?)_roleProperty.GetValue(); }
      set { _roleProperty.SetValue(value); }
    }

    public AbstractProperty LinkedRoleProperty
    {
      get { return _linkedRoleProperty; }
    }

    /// <summary>
    /// Optional linked role to use for the relationship filter.
    /// </summary>
    public Guid? LinkedRole
    {
      get { return (Guid?)_linkedRoleProperty.GetValue(); }
      set { _linkedRoleProperty.SetValue(value); }
    }

    public AbstractProperty LinkedMediaItemIdProperty
    {
      get { return _linkedMediaItemIdProperty; }
    }

    /// <summary>
    /// Linked media item id to use for the relationship filter.
    /// </summary>
    public Guid? LinkedMediaItemId
    {
      get { return (Guid?)_linkedMediaItemIdProperty.GetValue(); }
      set { _linkedMediaItemIdProperty.SetValue(value); }
    }

    #endregion
  }
}
