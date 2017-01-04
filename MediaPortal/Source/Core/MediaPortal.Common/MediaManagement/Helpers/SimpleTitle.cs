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

namespace MediaPortal.Common.MediaManagement.Helpers
{
  public struct SimpleTitle
  {
    public bool DefaultLanguage;
    public string Text;

    public SimpleTitle(string value)
    {
      Text = value;
      DefaultLanguage = true;
    }

    public SimpleTitle(string value, bool defaultLanguage)
    {
      Text = value;
      DefaultLanguage = defaultLanguage;
    }

    public static implicit operator SimpleTitle(string value)
    {
      return new SimpleTitle(value, true);
    }

    public bool IsEmpty
    {
      get
      {
        return string.IsNullOrEmpty(Text);
      }
    }

    public override string ToString()
    {
      return Text;
    }
  }
}
