#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.DirectX.RenderStrategy
{
  /// <summary>
  /// <see cref="IRenderStrategy"/> is used by the <seealso cref="GraphicsDevice"/>"/> to control the rendering of a scene. Each different
  /// strategy can control the <see cref="PresentMode"/>, do manual waiting on <see cref="BeginRender"/> and affect other parameters of the rendering
  /// system.
  /// <para>
  /// Not all <see cref="IRenderStrategy"/> modes can be used for MultiSampling screen modes (i.e. they require a specific
  /// <see cref="PresentMode"/> of <see cref="Present.None"/>).
  /// So classes that implement <see cref="IRenderStrategy"/> have to declare if they are compabtible with MultiSampling by implementing
  /// <see cref="IsMultiSampleCompatible"/>.
  /// </para>
  /// </summary>
  public interface IRenderStrategy
  {
    /// <summary>
    /// Gets the name of this render strategy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Returns <c>true</c> if strategy is MultiSampling compatible.
    /// </summary>
    bool IsMultiSampleCompatible { get; }

    /// <summary>
    /// Sets the target frame rate for rendering. This can be used for manual render delay handling.
    /// </summary>
    /// <param name="frameRate"></param>
    void SetTargetFrameRate(double frameRate);
    
    /// <summary>
    /// Gets the current frame rate, which was set before by <see cref="SetTargetFrameRate"/>.
    /// </summary>
    double TargetFrameRate { get; }

    /// <summary>
    /// Gets the time per frame in ms.
    /// </summary>
    double MsPerFrame { get; }

    /// <summary>
    /// <see cref="BeginRender"/> is called directly on the beginning of a render cycle.
    /// </summary>
    /// <param name="doWaitForNextFame"><c>true</c> for enabling manual waiting</param>
    void BeginRender(bool doWaitForNextFame);

    /// <summary>
    /// <see cref="EndRender"/> is called after the whole scene is rendered, but before Present().
    /// </summary>
    void EndRender();

    /// <summary>
    /// Gets the <see cref="Present"/> mode.
    /// </summary>
    Present PresentMode { get; }
  }
}
