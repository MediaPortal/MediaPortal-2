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

using System;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.ContentManagement.AssetCore
{
  public abstract class TemporaryAssetCoreBase
  {
    protected DateTime _lastTimeUsed;
    protected double _lifetime = 30.0f;
    protected object _syncObj = new object();

    protected TemporaryAssetCoreBase()
    {
      KeepAlive();
    }

    public void KeepAlive() 
    { 
      _lastTimeUsed = SkinContext.FrameRenderingStartTime; 
    }

    public double SecondsSinceLastUse
    {
      get { return (SkinContext.FrameRenderingStartTime - _lastTimeUsed).TotalSeconds; }
    }

    public DateTime LastUsed
    {
      get { return _lastTimeUsed; }
    }

    public double Lifetime
    {
      get { return _lifetime; }
      set { _lifetime = value; }
    }

    public bool CanBeDeleted
    {
      get { return SecondsSinceLastUse > _lifetime; }
    }
  }
}