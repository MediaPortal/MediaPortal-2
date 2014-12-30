#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using SharpDX.DXGI;

namespace MediaPortal.UI.SkinEngine.DirectX.RenderStrategy
{
  abstract class AbstractStrategy : IRenderStrategy
  {
    protected const int SYNC_NONE = 0;
    protected const int SYNC_VBLANK = 1;

    #region Fields

    protected Rational _frameRate = new Rational(25, 1);
    protected double _msPerFrame;
    protected D3DSetup _setup;

    #endregion

    #region Constructor

    protected AbstractStrategy(D3DSetup setup)
    {
      _setup = setup;
      PresentFlags = PresentFlags.None;
    }

    #endregion

    #region IRenderStrategy implementation

    public virtual string Name
    {
      get { return GetType().Name; }
    }

    public Rational TargetFrameRate
    {
      get { return _frameRate; }
    }

    public double MsPerFrame
    {
      get { return _msPerFrame; }
    }

    public void SetTargetFrameRate(Rational frameRate)
    {
      _frameRate = frameRate;
      _msPerFrame = 1000d / ((double)_frameRate.Numerator / _frameRate.Denominator);
    }

    public virtual void BeginRender(bool doWaitForNextFame)
    { }

    public virtual void EndRender()
    { }

    public PresentFlags PresentFlags { get; protected set; }
    
    public int SyncInterval { get; protected set; }

    #endregion
  }
}
