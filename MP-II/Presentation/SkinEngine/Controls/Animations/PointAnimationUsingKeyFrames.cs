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

using MediaPortal.Presentation.Properties;
using SlimDX;
using Presentation.SkinEngine.XamlParser;
using Presentation.SkinEngine.MarkupExtensions;

namespace Presentation.SkinEngine.Controls.Animations
{
  public class PointAnimationUsingKeyFrames : Timeline, IAddChild
  {
    Property _keyFramesProperty;

    #region Ctor

    /// <summary>
    /// Initializes a new instance of the <see cref="PointAnimation"/> class.
    /// </summary>
    public PointAnimationUsingKeyFrames()
    {
      Init();
    }

    public PointAnimationUsingKeyFrames(PointAnimationUsingKeyFrames a)
      : base(a)
    {
      Init();
      //foreach (PointKeyFrame k in a.KeyFrames)
      //{
      //  KeyFrames.Add((PointKeyFrame)k.Clone());
      //}
      _keyFramesProperty.SetValue(a.KeyFrames);
    }

    public override object Clone()
    {
      PointAnimationUsingKeyFrames result = new PointAnimationUsingKeyFrames(this);
      BindingMarkupExtension.CopyBindings(this, result);
      return result;
    }

    void Init()
    {
      _keyFramesProperty = new Property(typeof(PointKeyFrameCollection), new PointKeyFrameCollection());
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Gets or sets the target name property.
    /// </summary>
    /// <value>The target name property.</value>
    public Property KeyFramesProperty
    {
      get
      {
        return _keyFramesProperty;
      }
      set
      {
        _keyFramesProperty = value;
      }
    }
    /// <summary>
    /// Gets or sets the name of the target.
    /// </summary>
    /// <value>The name of the target.</value>
    public PointKeyFrameCollection KeyFrames
    {
      get
      {
        return _keyFramesProperty.GetValue() as PointKeyFrameCollection;
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
      double time = 0;
      Vector2 start = new Vector2();
      for (int i = 0; i < KeyFrames.Count; ++i)
      {
        PointKeyFrame key = KeyFrames[i];
        if (key.KeyTime.TotalMilliseconds >= timepassed)
        {
          double progress = (timepassed - time);
          if (progress == 0)
          {
            context.DataDescriptor.Value = key.Value;
          }
          else
          {
            progress /= (key.KeyTime.TotalMilliseconds - time);
            Vector2 result = key.Interpolate(start, progress);
            context.DataDescriptor.Value = result;
          }
          return;
        }
        else
        {
          time = key.KeyTime.TotalMilliseconds;
          start = key.Value;
        }
      }
    }

    public override void Start(AnimationContext context, uint timePassed)
    {
      if (!IsStopped(context))
        Stop(context);

      context.State = State.Starting;
      if (KeyFrames.Count > 0)
      {
        Duration = KeyFrames[KeyFrames.Count - 1].KeyTime;
      }
      //find context.Property...


      context.TimeStarted = timePassed;
      context.State = State.WaitBegin;
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
    #endregion

    #region IAddChild Members

    public void AddChild(object o)
    {
      KeyFrames.Add((PointKeyFrame)o);
    }

    #endregion
  }
}
