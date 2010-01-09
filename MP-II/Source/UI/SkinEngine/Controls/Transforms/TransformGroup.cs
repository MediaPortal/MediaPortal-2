#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.General;
using SlimDX;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Transforms
{
  public class TransformGroup : Transform, IAddChild<Transform>
  {
    #region Private fields

    AbstractProperty _childrenProperty;

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
      _childrenProperty.Attach(OnPropertyChanged);
      Children.ObjectChanged += OnChildrenChanged;
    }

    void Detach()
    {
      _childrenProperty.Detach(OnPropertyChanged);
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

    protected void OnChildrenChanged(IObservable observable)
    {
      _needUpdate = true;
      _needUpdateRel = true;
      Fire();
    }

    protected void OnPropertyChanged(AbstractProperty property)
    {
      _needUpdate = true;
      _needUpdateRel = true;
      Fire();
    }

    public AbstractProperty ChildrenProperty
    {
      get { return _childrenProperty; }
    }

    public TransformCollection Children
    {
      get { return (TransformCollection)_childrenProperty.GetValue(); }
    }

    public override void UpdateTransform()
    {
      base.UpdateTransform();
      _matrix = Matrix.Identity;
      foreach (Transform t in Children)
      {
        Matrix m;
        t.GetTransform(out m);
        _matrix *= m;
      }
    }

    public override void UpdateTransformRel()
    {
      base.UpdateTransformRel();
      _matrixRel = Matrix.Identity;
      foreach (Transform t in Children)
      {
        Matrix m;
        t.GetTransformRel(out m);
        _matrixRel *= m;
      }
    }

    #region IAddChild Members

    public void AddChild(Transform o)
    {
      Children.Add(o);
    }

    #endregion
  }
}
