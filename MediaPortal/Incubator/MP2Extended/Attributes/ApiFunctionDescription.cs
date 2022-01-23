#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

namespace MediaPortal.Plugins.MP2Extended.Attributes
{
  class ApiFunctionDescription : Attribute
  {
    public enum FunctionType
    {
      Json,
      Stream,
      Html
    }

    public string Summary;
    public Type ReturnType { set; get; }
    public FunctionType Type;
  }
  [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
  public class ApiFunctionParam : Attribute
  {
    public string Name { set; get; }
    public Type Type { set; get; }
    public bool Nullable { set; get; }
  }
}
