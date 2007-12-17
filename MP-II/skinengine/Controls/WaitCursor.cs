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

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DirectX;

namespace SkinEngine.Controls
{
  public class WaitCursor
  {
    #region variables

    private List<Image> _images;
    private bool _visible;
    private uint _startTick;
    private float _duration;
    private int _currentImage;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitCursor"/> class.
    /// </summary>
    public WaitCursor()
    {
      _startTick = 0;
      _currentImage = 0;
      Visible = false;
      _images = new List<Image>();
      string folder = String.Format(@"skin\" + SkinContext.SkinName + @"\media\");
      if (Directory.Exists(folder))
      {
        foreach (string filename in Directory.GetFiles(folder, "common.waiting.*.png"))
        {
          Image image = new Image(null);
          image.Source = Path.GetFileName(filename);
          _images.Add(image);
        }
      }
      Duration = 800;
    }

    /// <summary>
    /// Renders the waitcursor animation
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public void Render(uint timePassed)
    {
      if (!Visible)
      {
        _startTick = timePassed;
        return;
      }
      if (_images.Count == 0)
      {
        return;
      }
      if (_startTick == 0)
      {
        _startTick = timePassed;
      }
      uint passed = timePassed - _startTick;
      float duration = (Duration/((float) _images.Count));
      if (passed >= duration)
      {
        _currentImage++;
        if (_currentImage >= _images.Count)
        {
          _currentImage = 0;
        }
        _startTick = timePassed;
      }
      Image img = _images[(int) _currentImage];
      img.Position = new Vector3(SkinContext.Width/2 - 48, SkinContext.Height/2 - 48, 0);
      img.Render(timePassed);
    }

    /// <summary>
    /// Gets or sets the duration of the animation.
    /// </summary>
    /// <value>The duration.</value>
    public float Duration
    {
      get { return _duration; }
      set { _duration = value; }
    }

    /// <summary>
    /// Gets or sets a value whether the waitcursor is visible
    /// </summary>
    /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
    public bool Visible
    {
      get { return _visible; }
      set { _visible = value; }
    }
  }
}