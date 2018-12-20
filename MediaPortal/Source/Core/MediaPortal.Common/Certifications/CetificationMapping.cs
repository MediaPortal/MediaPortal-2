#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace MediaPortal.Common.Certifications
{
  public class CertificationMapping
  {
    // Unique ID of the certification
    public string CertificationId { get; private set; }
    // ISO Alpha 2 code. Can be used RegionInfo
    public string CountryCode { get; private set; }
    // Display name
    public string Name { get; private set; }
    // Minimum age to allow content with this certification
    public int AllowedAge { get; private set; }
    // Minimum age to allow content with this certification with parental guidance
    public int AllowedParentalGuidedAge { get; private set; }
    // Notation used to identify this certification
    public string[] Notations { get; private set; }

    public CertificationMapping(string id, string country, string name, int allowedAge, int allowedParentalGuidedAge, params string[] notations)
    {
      CertificationId = id;
      CountryCode = country;
      Name = name;
      AllowedAge = allowedAge;
      AllowedParentalGuidedAge = allowedParentalGuidedAge;
      Notations = notations;
    }
  }
}
