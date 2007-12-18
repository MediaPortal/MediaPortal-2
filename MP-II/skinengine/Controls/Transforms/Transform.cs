using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using MediaPortal.Core.Properties;

namespace Skinengine.Controls.Transforms
{
  public class Transform : Property
  {
    protected bool _needUpdate = true;
    protected Matrix _matrix = Matrix.Identity;

    public virtual void OnPropertyChanged()
    {
      _needUpdate = true;
      base.SetValue(this);
      Fire();
    }

    public virtual void GetTransform(out Matrix m)
    {
      if (_needUpdate)
      {
        UpdateTransform();
        _needUpdate = false;
      }
      m = _matrix;
    }

    public virtual void UpdateTransform()
    {
    }
  }
}
