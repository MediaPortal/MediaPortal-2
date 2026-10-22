#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
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
using MediaPortal.Common.Settings;

namespace MediaPortal.Common.Certifications
{
  /// <summary>
  /// Which country's age certificate the screens are told about, when the certificate the library stored belongs
  /// to a different one.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A title is usually imported with the certificate of the country that rated it, so a film bought in America
  /// arrives saying "PG-13" and a household in Germany reads a mark from a system it does not use. The reference
  /// answered that on the machine wearing the skin, translating the stored mark with the table in
  /// <see cref="CertificationMapper"/> at the moment it drew the label. This server answers it instead, for the
  /// plain reason that its screens are other programs: a television is sent words, and the words it is sent are
  /// chosen here.
  /// </para>
  /// <para>
  /// An empty answer means "leave it as it was imported", which is what the reference's own first entry in its
  /// list of countries said, and what this runs with. It is also the only answer that cannot be wrong, because
  /// the library's own mark is the one the importer read somewhere and can be traced back to.
  /// </para>
  /// <para>
  /// What a translation cannot do is invent a mark. When the table has nothing to answer with - the country
  /// the household named holds no marks of its own, or the stored words are not in the table at all - the
  /// stored words stand, rather than the screen showing a guess about how old somebody has to be. Which
  /// words are "in the table" is the mapper's own forgiving reading: it knows "Rated PG for rude humor"
  /// because it begins with a mark it holds, and that is exactly how the reference read a library's marks.
  /// </para>
  /// </remarks>
  public class MediaCertificationSettings
  {
    /// <summary>
    /// Two letter code of the country whose film certificate to show, or nothing to show the imported one.
    /// </summary>
    [Setting(SettingScope.Global, "")]
    public string DisplayMovieCertificationCountry { get; set; }

    /// <summary>
    /// Two letter code of the country whose series certificate to show, or nothing to show the imported one.
    /// </summary>
    [Setting(SettingScope.Global, "")]
    public string DisplaySeriesCertificationCountry { get; set; }

    /// <summary>
    /// The film certificate a screen should show for the certificate the library stored.
    /// </summary>
    public string WordsForMovie(string storedCertification)
    {
      return WordsFor(storedCertification, DisplayMovieCertificationCountry, true);
    }

    /// <summary>
    /// The series certificate a screen should show for the certificate the library stored.
    /// </summary>
    public string WordsForSeries(string storedCertification)
    {
      return WordsFor(storedCertification, DisplaySeriesCertificationCountry, false);
    }

    /// <summary>
    /// The certificate of <paramref name="country"/> that says what the stored one says, in the words that
    /// country writes.
    /// </summary>
    /// <remarks>
    /// The lookup is the forgiving one, because a library holds whatever the importer wrote and that is
    /// sometimes "USA: PG-13" and sometimes "PG-13". Once the stored mark is known, the mapper's own matching
    /// finds the equivalent in the asked-for country - the least strict mark there that still asks for at least
    /// the same age, which is how the reference chose it too. The name is what a screen shows; the identifier is
    /// what the table calls it internally, and no viewer was ever helped by "DE_FSK12".
    /// </remarks>
    private static string WordsFor(string storedCertification, string country, bool forFilm)
    {
      if (string.IsNullOrEmpty(storedCertification) || string.IsNullOrEmpty(country))
        return storedCertification;

      CertificationMapping stored;
      bool known = forFilm
          ? CertificationMapper.TryFindMovieCertification(storedCertification, out stored)
          : CertificationMapper.TryFindSeriesCertification(storedCertification, out stored);
      if (!known)
        return storedCertification;

      if (string.Equals(stored.CountryCode, country, StringComparison.OrdinalIgnoreCase))
        return storedCertification;

      CertificationMapping wished = forFilm
          ? CertificationMapper.FindMatchingMovieCertification(country, stored.CertificationId)
          : CertificationMapper.FindMatchingSeriesCertification(country, stored.CertificationId);
      return wished == null ? storedCertification : wished.Name;
    }
  }
}
