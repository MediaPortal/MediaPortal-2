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

using System.Windows.Markup;
using MediaPortal.Common.General;
using SharpDX;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Transforms
{
  [ContentProperty("Children")]
  public class TransformGroup : Transform, IAddChild<Transform>
  {
    #region Protected fields

    protected AbstractProperty _childrenProperty;

    #endregion

    #region Ctor

    public TransformGroup()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
      Children.Dispose();
    }

    void Init()
    {
      _childrenProperty = new SProperty(typeof(TransformCollection), new TransformCollection());
    }

    void Attach()
    {
      _childrenProperty.Attach(OnChildrenChanged);
      Children.ObjectChanged += OnChildrenChanged;
    }

    void Detach()
    {
      _childrenProperty.Detach(OnChildrenChanged);
      Children.ObjectChanged -= OnChildrenChanged;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      TransformGroup g = (TransformGroup) source;
      foreach (Transform t in g.Children)
        Children.Add(copyManager.GetCopy(t));
      Attach();
    }

    #endregion

    protected void OnChildrenChanged(AbstractProperty prop, object oldVal)
    {
      TransformCollection tc = (TransformCollection) oldVal;
      if (tc != null)
        tc.ObjectChanged -= OnChildrenChanged;
      tc = Children;
      if (tc != null)
        tc.ObjectChanged += OnChildrenChanged;
      _needUpdate = true;
      Fire();
    }

    protected void OnChildrenChanged(IObservable observable)
    {
      _needUpdate = true;
      Fire();
    }

    public AbstractProperty ChildrenProperty
    {
      get { return _childrenProperty; }
    }

    public TransformCollection Children
    {
      get { return (TransformCollection) _childrenProperty.GetValue(); }
    }

    public override void UpdateTransform()
    {
      base.UpdateTransform();
      _matrix = Matrix.Identity;
      foreach (Transform t in Children)
        _matrix *= t.GetTransform();
    }

    #region IAddChild Members

    public void AddChild(Transform o)
    {
      Children.Add(o);
    }

    #endregion
  }
}
