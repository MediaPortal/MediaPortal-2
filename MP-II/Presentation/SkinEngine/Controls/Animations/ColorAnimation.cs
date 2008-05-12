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

using System.Drawing;
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.MarkupExtensions;

namespace Presentation.SkinEngine.Controls.Animations
{

  public class ColorAnimation : Timeline
  {
    Property _fromProperty;
    Property _toProperty;
    Property _byProperty;

    #region Ctor
    /// <summary>
    /// Initializes a new instance of the <see cref="ColorAnimation"/> class.
    /// </summary>
    public ColorAnimation()
    {
      Init();
    }

    public ColorAnimation(ColorAnimation a)
      : base(a)
    {
      Init();
      From = a.From;
      To = a.To;
      By = a.By;
    }

    public override object Clone()
    {
      ColorAnimation result = new ColorAnimation(this);
      BindingMarkupExtension.CopyBindings(this, result);
      return result;
    }

    void Init()
    {
      _fromProperty = new Property(typeof(Color), Color.Black);
      _toProperty = new Property(typeof(Color), Color.White);
      _byProperty = new Property(typeof(Color), Color.Beige);
    }
    #endregion

    #region Public properties
    /// <summary>
    /// Gets or sets from property.
    /// </summary>
    /// <value>From property.</value>
    public Property FromProperty
    {
      get
      {
        return _fromProperty;
      }
      set
      {
        _fromProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets from.
    /// </summary>
    /// <value>From.</value>
    public Color From
    {
      get
      {
        return (Color)_fromProperty.GetValue();
      }
      set
      {
        _fromProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets to property.
    /// </summary>
    /// <value>To property.</value>
    public Property ToProperty
    {
      get
      {
        return _toProperty;
      }
      set
      {
        _toProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets to.
    /// </summary>
    /// <value>To.</value>
    public Color To
    {
      get
      {
        return (Color)_toProperty.GetValue();
      }
      set
      {
        _toProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the by property.
    /// </summary>
    /// <value>The by property.</value>
    public Property ByProperty
    {
      get
      {
        return _byProperty;
      }
      set
      {
        _byProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the by.
    /// </summary>
    /// <value>The by.</value>
    public Color By
    {
      get
      {
        return (Color)_byProperty.GetValue();
      }
      set
      {
        _byProperty.SetValue(value);
      }
    }

    #endregion

    #region Animation methods

    /// <summary>
    /// Animates the property.
    /// </summary>
    /// <param name="timepassed">The timepassed.</param>
    protected override void AnimateProperty(AnimationContext context, uint timepassed)
    {
      if (context.DataDescriptor == null) return;
      Color c;
      double distA = ((double)(To.A - From.A)) / Duration.TotalMilliseconds;
      distA *= timepassed;
      distA += From.A;

      double distR = ((double)(To.R - From.R)) / Duration.TotalMilliseconds;
      distR *= timepassed;
      distR += From.R;

      double distG = ((double)(To.G - From.G)) / Duration.TotalMilliseconds;
      distG *= timepassed;
      distG += From.G;

      double distB = ((double)(To.B - From.B)) / Duration.TotalMilliseconds;
      distB *= timepassed;
      distB += From.B;

      c = Color.FromArgb((int)distA, (int)distR, (int)distG, (int)distB);

      context.DataDescriptor.Value = c;
    }

    public override void Stop(AnimationContext context)
    {
      if (IsStopped(context)) return;
      context.State = State.Idle;
      if (context.DataDescriptor != null)
      {
        context.DataDescriptor.Value = OriginalValue;

      }
    }

    public override void Start(AnimationContext context, uint timePassed)
    {
      if (!IsStopped(context))
        Stop(context);
      //find context.Property...

      context.State = State.Starting;

      context.TimeStarted = timePassed;
      context.State = State.WaitBegin;
    }

    #endregion

  }
}

