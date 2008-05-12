#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

using System;
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.XamlParser;

namespace Presentation.SkinEngine.MarkupExtensions
{

  /// <summary>
  /// Implements the RelativeSource markup extension. This class will create a
  /// <see cref="BindingDependency"/> to handle updates between the two
  /// referenced properties/values, if all required parameters are specified.
  /// </summary>
  public class RelativeSource
  {
    public static RelativeSource Self = new RelativeSource("Self");
    public static RelativeSource TemplatedParent = new RelativeSource("TemplatedParent");
    // TODO: Not implemented yet
//    public static RelativeSource PreviousData = new RelativeSource("PreviousData");

    protected string _name;

    public RelativeSource(string name)
    {
      _name = name;
    }

    public bool Evaluate(Visual obj, out IDataDescriptor result)
    {
      result = null;
      if (obj == null)
        return false;
      if (this == Self)
      {
        result = new ValueDataDescriptor(obj);
        return true;
      }
      else if (this == TemplatedParent)
      {
        // FIXME Albert78: Use logical tree
        obj = obj.VisualParent;
        // FIXME: This doesn't work. We will have to find the templated parent here,
        // not the first control which is a Control!
        while (obj != null && obj.VisualParent != null && !(obj is Control))
          obj = obj.VisualParent;
        if (obj == null)
          return false;
        result = new ValueDataDescriptor(obj);
        return true;
      }
      else
        throw new NotImplementedException();
    }

    public override string ToString()
    {
      return _name;
    }
  }
}
