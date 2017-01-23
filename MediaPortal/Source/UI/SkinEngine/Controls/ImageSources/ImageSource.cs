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

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using SharpDX;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Controls.ImageSources
{
  /// <summary>
  /// This class defines a most basic requirements for an ImageSource derived class.
  /// ImageSources are used by controls (like <see cref="Visuals.Image"/>) to provide format independent 
  /// visual content in a flexible, expandable and consistent manner.
  /// </summary>
  public abstract class ImageSource : DependencyObject, IObservable
  {
    protected WeakEventMulticastDelegate _objectChanged = new WeakEventMulticastDelegate();

    public event ObjectChangedDlgt ObjectChanged
    {
      add { _objectChanged.Attach(value); }
      remove { _objectChanged.Detach(value); }
    }

    /// <summary>
    /// Load any external resources.
    /// </summary>
    public abstract void Allocate();

    /// <summary>
    /// Unload any external resources.
    /// </summary>
    public abstract void Deallocate();

    /// <summary>
    /// Performs setup of the source based on the target rect to fill.
    /// </summary>
    /// <param name="ownerRect">The client area of the owner control we will be rendering to.</param>
    /// <param name="zOrder">Z-Order of the image.</param>
    /// <param name="skinNeutralAR">If set to <c>true</c>, the size calculation will take into account that
    /// the screen might be stretched. The image will still be drawn with its natural aspect ratio. If set to
    /// <c>false</c>, the image will simply be stretched as every other control on the screen.</param>
    public abstract void Setup(RectangleF ownerRect, float zOrder, bool skinNeutralAR);

    /// <summary>
    /// Renders this image source to the client area using the passed <paramref name="renderContext"/>.
    /// </summary>
    /// <param name="renderContext">The current rendering context</param>
    /// <param name="stretchMode">The mode of stretching to perform when the image doesn't fit the client area.</param>
    /// <param name="stretchDirection">The condition required for stretching to take place.</param>
    public abstract void Render(RenderContext renderContext, Stretch stretchMode, StretchDirection stretchDirection);

    /// <summary>
    /// Gets the original size (before stretching but after rotation) of the content represented by this source.
    /// </summary>
    public abstract SizeF SourceSize { get; }

    /// <summary>
    /// Gets a value indicating whether this source has loaded it's resources.
    /// </summary>
    public abstract bool IsAllocated { get; }

    public void OnPropertyChanged()
    {
      FireChanged();
    }

    public void FireChanged()
    {
      _objectChanged.Fire(new object[] {this});
    }

    /// <summary>
    /// This is a helper provided to assist derived Sources when scaling their content to
    /// the owner size.
    /// </summary>
    /// <param name="target">The total available space.</param>
    /// <param name="source">The unscaled source size.</param>
    /// <param name="stretchMode">The <see cref="Stretch"/> mode that determines which stretching technique to use.</param>
    /// <param name="direction">The <see cref="StretchDirection"/> that determines when to perform scaling.</param>
    /// <returns>The scaled source size, which may be larger than the <paramref name="target"/> size.</returns>
    public SizeF StretchSource(SizeF target, SizeF source, Stretch stretchMode, StretchDirection direction)
    {
      if (direction == StretchDirection.DownOnly && source.Width <= target.Width && source.Height <= target.Height)
        return source;
      if (direction == StretchDirection.UpOnly && source.Width >= target.Width && source.Height >= target.Height)
        return source;

      switch (stretchMode)
      {
        case Stretch.None:
          // Original size
          break;
        case Stretch.Fill:
          // Stretch to fit
          source = target;
          break;
        case Stretch.Uniform:
          // Keep aspect ratio and show borders
          {
            float ratio = System.Math.Min(target.Width / source.Width, target.Height / source.Height);
            source.Width *= ratio;
            source.Height *= ratio;
          }
          break;
        case Stretch.UniformToFill:
          // Keep aspect ratio, zoom in to avoid borders
          {
            float ratio = System.Math.Max(target.Width / source.Width, target.Height / source.Height);
            source.Width *= ratio;
            source.Height *= ratio;
          }
          break;
      }
      return source;
    }

    /// <summary>
    /// Notifies this image source that the rendering was stopped for this image. Can be overridden by sub classes.
    /// </summary>
    public virtual void VisibilityLost()
    {
    }
  }
}
