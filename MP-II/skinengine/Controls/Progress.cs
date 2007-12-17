#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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

using MediaPortal.Core.Properties;

namespace SkinEngine.Controls
{
  public class Progress : Image
  {
    private IFloatProperty _progress;

    /// <summary>
    /// Initializes a new instance of the <see cref="Image"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    public Progress(Control parent)
      : base(parent)
    {
      _progress = new FloatProperty(0.0f);
    }

    public IFloatProperty ProgressProperty
    {
      get { return _progress; }
      set { _progress = value; }
    }

    public override void Render(uint timePassed)
    {
      float totalWidth = Width;
      Width = Width*(_progress.Evaluate(this, Container)/100.0f);
      base.Render(timePassed);
      Width = totalWidth;
    }
  }
}