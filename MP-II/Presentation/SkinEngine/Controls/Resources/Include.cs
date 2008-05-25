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

using Presentation.SkinEngine.XamlParser;
using Presentation.SkinEngine.Loader;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Resources
{
  public class Include : IInclude, IInitializable, IDeepCopyable
  {
    #region Private fields

    object _content;
    string _includeName;

    #endregion

    public virtual void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Include i = source as Include;
      _content = copyManager.GetCopy(i._content);
      Source = copyManager.GetCopy(i.Source);
    }

    #region Public properties    

    public string Source
    {
      get { return _includeName; }
      set { _includeName = value; }
    }

    #endregion

    #region IInclude implementation

    public object Content
    {
      get { return _content; }
    }               

    #endregion

    #region IInitializable implementation

    public virtual void Initialize(IParserContext context)
    {
      XamlLoader loader = new XamlLoader();
      _content = loader.Load(_includeName);
    }

    #endregion
  }
}
