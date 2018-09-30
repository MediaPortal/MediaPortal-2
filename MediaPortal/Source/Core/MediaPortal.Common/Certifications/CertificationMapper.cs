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

using MediaPortal.Common.Logging;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MediaPortal.Common.Certifications
{
  public class CertificationMapper
  {
    private static readonly List<CertificationMapping> MOVIE_CERTIFICATION_MAP = new List<CertificationMapping>();
    private static readonly List<CertificationMapping> SERIES_CERTIFICATION_MAP = new List<CertificationMapping>();

    public const int MAX_AGE = 18;

    static CertificationMapper()
    {
      //Must be in age requirement order
      MOVIE_CERTIFICATION_MAP = new List<CertificationMapping>()
      {
        //US
        new CertificationMapping("US_G", "US", "G", 0, 0, "G", "Rated G"),
        new CertificationMapping("US_PG", "US", "PG", 10, 0, "PG", "Rated PG "),
        new CertificationMapping("US_PG13", "US", "PG-13", 13, 13, "PG-13", "Rated PG-13"),
        new CertificationMapping("US_R", "US", "R", 17, 13, "R", "Rated R"),
        new CertificationMapping("US_NC17", "US", "NC-17", 17, 17, "NC-17", "Rated NC-17"),

        //DE
        new CertificationMapping("DE_FSK0", "DE", "FSK 0", 0, 0, "FSK 0", "FSK0", "0"),
        new CertificationMapping("DE_FSK6", "DE", "FSK 6", 6, 6, "FSK 6", "FSK6", "6", "ab 6"),
        new CertificationMapping("DE_FSK12", "DE", "FSK 12", 12, 6, "FSK 12", "FSK12", "12", "ab 12"),
        new CertificationMapping("DE_FSK16", "DE", "FSK 16", 16, 16, "FSK 16", "FSK16", "16", "ab 16"),
        new CertificationMapping("DE_FSK18", "DE", "FSK 18", 18, 18, "FSK 18", "FSK18", "18", "ab 18"),

        //GB
        new CertificationMapping("GB_UC", "GB", "UC", 0, 0, "UC", "Rated UC"),
        new CertificationMapping("GB_U", "GB", "U", 0, 0, "U", "Rated U"),
        new CertificationMapping("GB_PG", "GB", "PG", 8, 0, "PG", "Rated PG"),
        new CertificationMapping("GB_12A", "GB", "12A", 12, 8, "12A", "Rated 12A"),
        new CertificationMapping("GB_12", "GB", "12", 12, 12, "12", "Rated 12"),
        new CertificationMapping("GB_15", "GB", "15", 15, 15, "15", "Rated 15"),
        new CertificationMapping("GB_18", "GB", "18", 18, 18, "18", "Rated 18"),
        new CertificationMapping("GB_R18", "GB", "R18", 18, 18, "R18", "Rated R18", "X"),

        //RU
        new CertificationMapping("RU_Y", "RU", "Y", 0, 0, "Y", "0+" ),
        new CertificationMapping("RU_6", "RU", "6+", 6, 6, "6+" ),
        new CertificationMapping("RU_12", "RU", "12+", 12, 12, "12+" ),
        new CertificationMapping("RU_14", "RU", "14+", 14, 14, "14+" ),
        new CertificationMapping("RU_16", "RU", "16+", 16, 16, "16+" ),
        new CertificationMapping("RU_18", "RU", "18+", 18, 18, "18+" ),

        //NL
        new CertificationMapping("NL_AL", "NL", "AL", 0, 0, "AL" ),
        new CertificationMapping("NL_6", "NL", "6", 6, 6, "6" ),
        new CertificationMapping("NL_9", "NL", "9", 9, 9, "9" ),
        new CertificationMapping("NL_12", "NL", "12", 12, 12, "12" ),
        new CertificationMapping("NL_16", "NL", "16", 16, 16, "16" ),

        //JP
        new CertificationMapping("JP_G", "JP", "G", 0, 0, "G" ),
        new CertificationMapping("JP_PG12", "JP", "PG-12", 12, 0, "PG-12" ),
        new CertificationMapping("JP_R15", "JP", "R15+", 15, 15, "R15+" ),
        new CertificationMapping("JP_R18", "JP", "R18+", 18, 18, "R18+" ),

        //IT
        new CertificationMapping("IT_T", "IT", "T", 0, 0, "T" ),
        new CertificationMapping("IT_VM14", "IT", "V.M.14", 14, 14, "V.M.14", "VM14" ),
        new CertificationMapping("IT_VM18", "IT", "V.M.18", 18, 18, "V.M.18", "VM18" ),

        //IN
        new CertificationMapping("IN_U", "IN", "U", 0, 0, "U" ),
        new CertificationMapping("IN_UA", "IN", "UA", 12, 0, "UA" ),
        new CertificationMapping("IN_A", "IN", "A", 18, 18, "A" ),
        new CertificationMapping("IN_S", "IN", "S", 18, 18, "S" ),

        //GR
        new CertificationMapping("GR_13", "GR", "13", 13, 13, "13", "K-13" ),
        new CertificationMapping("GR_17", "GR", "17", 17, 17, "17", "K-17" ),
        new CertificationMapping("GR_18", "GR", "18", 18, 18, "18", "K-18" ),

        //FR
        new CertificationMapping("FR_U", "FR", "U", 0, 0, "U" ),
        new CertificationMapping("FR_10", "FR", "10", 10, 10, "10" ),
        new CertificationMapping("FR_12", "FR", "12", 12, 12, "12" ),
        new CertificationMapping("FR_16", "FR", "16", 16, 16, "16" ),
        new CertificationMapping("FR_18", "FR", "18", 18, 18, "18" ),

        //CA
        new CertificationMapping("CA_G", "CA", "G", 0, 0, "G" ),
        new CertificationMapping("CA_13", "CA", "13+", 13, 0, "13+" ),
        new CertificationMapping("CA_PG", "CA", "PG", 14, 0, "PG" ),
        new CertificationMapping("CA_14A", "CA", "14A", 14, 10, "14A" ),
        new CertificationMapping("CA_16", "CA", "16+", 16, 16, "16+" ),
        new CertificationMapping("CA_18A", "CA", "18A", 18, 14, "18A" ),
        new CertificationMapping("CA_18", "CA", "18+", 18, 18, "18+" ),
        new CertificationMapping("CA_R", "CA", "R", 18, 18, "R" ),
        new CertificationMapping("CA_A", "CA", "A", 18, 18, "A" ),

        //AU
        new CertificationMapping("AU_G", "AU", "G", 0, 0, "G" ),
        new CertificationMapping("AU_PG", "AU", "PG", 15, 0, "PG" ),
        new CertificationMapping("AU_E", "AU", "E", 15, 12, "E" ),
        new CertificationMapping("AU_M", "AU", "M", 15, 12, "M" ),
        new CertificationMapping("AU_MA15", "AU", "MA15+", 15, 15, "MA15+", "MA" ),
        new CertificationMapping("AU_R18", "AU", "R18+", 18, 18, "R18+", "R" ),
        new CertificationMapping("AU_X18", "AU", "X18+", 18, 18, "X18+", "X" ),
        new CertificationMapping("AU_RC", "AU", "RC", 18, 18, "RC" ),

        //CZ
        new CertificationMapping("CZ_U", "CZ", "U", 0, 0, "U" ),
        new CertificationMapping("CZ_PG", "CZ", "PG", 12, 0, "PG" ),
        new CertificationMapping("CZ_12", "CZ", "12", 12, 12, "12" ),
        new CertificationMapping("CZ_15", "CZ", "15", 15, 15, "15" ),
        new CertificationMapping("CZ_18", "CZ", "18", 18, 18, "18" ),
        new CertificationMapping("CZ_E", "CZ", "E", 18, 18, "E" ),

        //DK
        new CertificationMapping("DK_A", "DK", "A", 0, 0, "A" ),
        new CertificationMapping("DK_7", "DK", "7", 7, 0, "7" ),
        new CertificationMapping("DK_11", "DK", "11", 11, 11, "11" ),
        new CertificationMapping("DK_15", "DK", "15", 15, 15, "15" ),
        new CertificationMapping("DK_F", "DK", "F", 18, 18, "F" ),

        //EE
        new CertificationMapping("EE_PERE", "EE", "PERE", 0, 0, "PERE" ),
        new CertificationMapping("EE_L", "EE", "L", 0, 0, "L" ),
        new CertificationMapping("EE_MS6", "EE", "MS-6", 6, 0, "MS-6" ),
        new CertificationMapping("EE_MS12", "EE", "MS-12", 12, 8, "MS-12" ),
        new CertificationMapping("EE_K12", "EE", "K-12", 12, 12, "K-12" ),
        new CertificationMapping("EE_K14", "EE", "K-14", 14, 14, "K-14" ),
        new CertificationMapping("EE_K16", "EE", "K-16", 16, 16, "K-16" ),

        //BR
        new CertificationMapping("BR_L", "BR", "L", 0, 0, "L" ),
        new CertificationMapping("BR_10", "BR", "10", 10, 10, "10" ),
        new CertificationMapping("BR_12", "BR", "12", 12, 12, "12" ),
        new CertificationMapping("BR_14", "BR", "14", 14, 14, "14" ),
        new CertificationMapping("BR_16", "BR", "16", 16, 16, "16" ),
        new CertificationMapping("BR_18", "BR", "18", 18, 18, "18" ),

        //FI
        new CertificationMapping("FI_S", "FI", "S", 0, 0, "S" ),
        new CertificationMapping("FI_7", "FI", "7", 7, 7, "7", "K-7" ),
        new CertificationMapping("FI_12", "FI", "12", 12, 12, "12", "K-12" ),
        new CertificationMapping("FI_16", "FI", "16", 16, 16, "16", "K-16" ),
        new CertificationMapping("FI_18", "FI", "18", 18, 18, "18", "K-18" ),
        new CertificationMapping("FI_KK", "FI", "KK", 18, 18, "KK" ),

        //HU
        new CertificationMapping("HU_KN", "HU", "KN", 0, 0, "KN" ),
        new CertificationMapping("HU_6", "HU", "6", 6, 0, "6" ),
        new CertificationMapping("HU_12", "HU", "12", 12, 8, "12" ),
        new CertificationMapping("HU_16", "HU", "16", 16, 12, "16" ),
        new CertificationMapping("HU_18", "HU", "18", 18, 16, "18" ),
        new CertificationMapping("HU_X", "HU", "X", 18, 18, "X" ),

        //IS
        new CertificationMapping("IS_L", "IS", "L", 0, 0, "L" ),
        new CertificationMapping("IS_6", "IS", "6", 6, 6, "6" ),
        new CertificationMapping("IS_7", "IS", "7", 7, 7, "7" ),
        new CertificationMapping("IS_9", "IS", "9", 9, 9, "9" ),
        new CertificationMapping("IS_10", "IS", "10", 10, 01, "10" ),
        new CertificationMapping("IS_12", "IS", "12", 12, 12, "12" ),
        new CertificationMapping("IS_14", "IS", "14", 14, 14, "14" ),
        new CertificationMapping("IS_16", "IS", "16", 16, 16, "16" ),
        new CertificationMapping("IS_18", "IS", "18", 18, 18, "18" ),

        //IE
        new CertificationMapping("IE_G", "IE", "G", 0, 0, "G" ),
        new CertificationMapping("IE_PG", "IE", "PG", 8, 0, "PG" ),
        new CertificationMapping("IE_12A", "IE", "12A", 12, 8, "12A" ),
        new CertificationMapping("IE_12", "IE", "12", 12, 12, "12" ),
        new CertificationMapping("IE_15A", "IE", "15A", 15, 12, "15A" ),
        new CertificationMapping("IE_15", "IE", "15", 15, 15, "15" ),
        new CertificationMapping("IE_16", "IE", "16", 16, 16, "16" ),
        new CertificationMapping("IE_18", "IE", "18", 18, 18, "18" ),

        //NZ
        new CertificationMapping("NZ_G", "NZ", "G", 0, 0, "G" ),
        new CertificationMapping("NZ_PG", "NZ", "PG", 10, 0, "PG" ),
        new CertificationMapping("NZ_M", "NZ", "M", 10, 6, "M" ),
        new CertificationMapping("NZ_R13", "NZ", "R13", 13, 13, "R13", "13" ),
        new CertificationMapping("NZ_R15", "NZ", "R15", 15, 15, "R15", "15" ),
        new CertificationMapping("NZ_R16", "NZ", "R16", 16, 16, "R16", "16" ),
        new CertificationMapping("NZ_R18", "NZ", "R18", 18, 18, "R18", "18" ),
        new CertificationMapping("NZ_RP13", "NZ", "RP13", 13, 9, "RP13" ),
        new CertificationMapping("NZ_RP16", "NZ", "RP16", 16, 12, "RP16" ),
        new CertificationMapping("NZ_RP18", "NZ", "RP18", 18, 14, "RP18" ),
        new CertificationMapping("NZ_R", "NZ", "R", 18, 18, "R" ),

        //NO
        new CertificationMapping("NO_A", "NO", "A", 0, 0, "A" ),
        new CertificationMapping("NO_6", "NO", "6", 6, 0, "6" ),
        new CertificationMapping("NO_7", "NO", "7", 7, 6, "7" ),
        new CertificationMapping("NO_9", "NO", "9", 9, 6, "9" ),
        new CertificationMapping("NO_11", "NO", "11", 11, 9, "11" ),
        new CertificationMapping("NO_11", "NO", "12", 12, 9, "12" ),
        new CertificationMapping("NO_15", "NO", "15", 15, 12, "15" ),
        new CertificationMapping("NO_18", "NO", "18", 18, 18, "18" ),

        //PL
        new CertificationMapping("PL_AP", "PL", "AP", 0, 0, "AP" ),
        new CertificationMapping("PL_AL", "PL", "AL", 0, 0, "AL" ),
        new CertificationMapping("PL_7", "PL", "7", 7, 7, "7" ),
        new CertificationMapping("PL_12", "PL", "12", 12, 12, "12" ),
        new CertificationMapping("PL_15", "PL", "15", 15, 15, "15" ),
        new CertificationMapping("PL_21", "PL", "21", 21, 21, "21" ),

        //RO
        new CertificationMapping("RO_AP", "RO", "A.P.", 0, 0, "A.P.", "AP" ),
        new CertificationMapping("RO_AG", "RO", "A.G.", 0, 0, "A.G.", "AG" ),
        new CertificationMapping("RO_12", "RO", "12", 12, 0, "12", "AP-12", "AP12" ),
        new CertificationMapping("RO_15", "RO", "15", 15, 15, "15", "N-15", "N15" ),
        new CertificationMapping("RO_18", "RO", "18", 18, 18, "18", "IM-18", "IM18" ),
        new CertificationMapping("RO_18X", "RO", "18*", 18, 18, "18*", "IM-18-XXX", "IM18XXX" ),

        //BG
        new CertificationMapping("BG_A", "BG", "A", 0, 0, "A" ),
        new CertificationMapping("BG_B", "BG", "B", 0, 0, "B" ),
        new CertificationMapping("BG_C", "BG", "C", 12, 12, "C" ),
        new CertificationMapping("BG_D", "BG", "D", 16, 16, "D" ),
        new CertificationMapping("BG_X", "BG", "X", 18, 18, "X" ),

        //ES
        new CertificationMapping("ES_APTA", "ES", "APTA", 0, 0, "APTA", "APTAi" ),
        new CertificationMapping("ES_ER", "ES", "ER", 0, 0, "ER" ),
        new CertificationMapping("ES_7", "ES", "7", 7, 7, "7", "7i" ),
        new CertificationMapping("ES_12", "ES", "12", 12, 12, "12" ),
        new CertificationMapping("ES_16", "ES", "16", 16, 16, "16" ),
        new CertificationMapping("ES_18", "ES", "18", 18, 18, "18" ),
        new CertificationMapping("ES_PX", "ES", "PX", 18, 18, "PX", "X" ),

        //SE
        new CertificationMapping("SE_BTL", "SE", "BTL", 0, 0, "BTL" ),
        new CertificationMapping("SE_7", "SE", "7", 7, 0, "7" ),
        new CertificationMapping("SE_11", "SE", "11", 11, 7, "11" ),
        new CertificationMapping("SE_15", "SE", "15", 15, 15, "15" ),

        //CH
        new CertificationMapping("CH_0", "CH", "0", 0, 0, "0" ),
        new CertificationMapping("CH_7", "CH", "7", 7, 7, "7" ),
        new CertificationMapping("CH_10", "CH", "10", 10, 10, "10" ),
        new CertificationMapping("CH_12", "CH", "12", 12, 12, "12" ),
        new CertificationMapping("CH_14", "CH", "14", 14, 14, "14" ),
        new CertificationMapping("CH_16", "CH", "16", 16, 16, "16" ),
        new CertificationMapping("CH_18", "CH", "18", 18, 18, "18" ),

        //TH
        new CertificationMapping("TH_P", "TH", "P", 0, 0, "P" ),
        new CertificationMapping("TH_G", "TH", "G", 13, 0, "G" ),
        new CertificationMapping("TH_13", "TH", "13+", 13, 13, "13+" ),
        new CertificationMapping("TH_15", "TH", "15+", 15, 15, "15+" ),
        new CertificationMapping("TH_18", "TH", "18+", 18, 18, "18+" ),
        new CertificationMapping("TH_20", "TH", "20+", 20, 20, "20+" ),
        new CertificationMapping("TH_B", "TH", "Banned", 18, 18, "Banned" ),

        //PH
        new CertificationMapping("PH_G", "PH", "G", 0, 0, "G" ),
        new CertificationMapping("PH_PG", "PH", "PG", 13, 0, "PG" ),
        new CertificationMapping("PH_R13", "PH", "R-13", 13, 13, "R-13" ),
        new CertificationMapping("PH_R16", "PH", "R-16", 16, 16, "R-16" ),
        new CertificationMapping("PH_R18", "PH", "R-18", 18, 18, "R-18" ),
        new CertificationMapping("PH_X", "PH", "X", 18, 18, "X" ),

        //PT
        new CertificationMapping("PT_T", "PT", "M/0", 0, 0, "P�blicos", "T", "Para todos os p�blicos" ),
        new CertificationMapping("PT_M3", "PT", "M/3", 3, 3, "M/3", "M_3" ),
        new CertificationMapping("PT_M6", "PT", "M/6", 6, 6, "M/6", "M_6" ),
        new CertificationMapping("PT_M12", "PT", "M/12", 12, 12, "M/12", "M_12" ),
        new CertificationMapping("PT_M14", "PT", "M/14", 14, 14, "M/14", "M_14" ),
        new CertificationMapping("PT_M16", "PT", "M/16", 16, 16, "M/16", "M_16" ),
        new CertificationMapping("PT_M18", "PT", "M/18", 18, 18, "M/18", "M_18" ),
        new CertificationMapping("PT_P", "PT", "P", 18, 18, "P" ),

        //MY
        new CertificationMapping("MY_U", "MY", "U", 0, 0, "U" ),
        new CertificationMapping("MY_P13", "MY", "P13", 13, 9, "P13", "PG-13" ),
        new CertificationMapping("MY_18SG", "MY", "18SG", 18, 18, "18SG" ),
        new CertificationMapping("MY_18SX", "MY", "18SX", 18, 18, "18SX" ),
        new CertificationMapping("MY_18PA", "MY", "18PA", 18, 18, "18PA" ),
        new CertificationMapping("MY_18PL", "MY", "18PL", 18, 18, "18PL" ),
      };

      //Must be in age requirement order
      SERIES_CERTIFICATION_MAP = new List<CertificationMapping>()
      {
        //US
        new CertificationMapping("US_TVY", "US", "TV-Y", 0, 0, "TV-Y" ),
        new CertificationMapping("US_TVY7", "US", "TV-Y7", 7, 7, "TV-Y7", "TV-Y7" ),
        new CertificationMapping("US_TVG", "US", "TV-G", 0, 0, "TV-G" ),
        new CertificationMapping("US_TVPG", "US", "TV-PG", 7, 0, "TV-PG" ),
        new CertificationMapping("US_TV14", "US", "TV-14", 14, 14, "TV-14" ),
        new CertificationMapping("US_TVMA", "US", "TV-MA", 17, 17, "TV-MA" ),

        //DE
        new CertificationMapping("DE_FSK0", "DE", "FSK 0", 0, 0, "FSK 0", "FSK0", "0"),
        new CertificationMapping("DE_FSK6", "DE", "FSK 6", 6, 6, "FSK 6", "FSK6", "6", "ab 6"),
        new CertificationMapping("DE_FSK12", "DE", "FSK 12", 12, 6, "FSK 12", "FSK12", "12", "ab 12"),
        new CertificationMapping("DE_FSK16", "DE", "FSK 16", 16, 16, "FSK 16", "FSK16", "16", "ab 16"),
        new CertificationMapping("DE_FSK18", "DE", "FSK 18", 18, 18, "FSK 18", "FSK18", "18", "ab 18"),

        //GB
        new CertificationMapping("GB_UC", "GB", "UC", 0, 0, "UC", "Rated UC"),
        new CertificationMapping("GB_U", "GB", "U", 0, 0, "U", "Rated U"),
        new CertificationMapping("GB_PG", "GB", "PG", 8, 0, "PG", "Rated PG"),
        new CertificationMapping("GB_12A", "GB", "12A", 12, 8, "12A", "Rated 12A"),
        new CertificationMapping("GB_12", "GB", "12", 12, 12, "12", "Rated 12"),
        new CertificationMapping("GB_15", "GB", "15", 15, 15, "15", "Rated 15"),
        new CertificationMapping("GB_18", "GB", "18", 18, 18, "18", "Rated 18"),
        new CertificationMapping("GB_R18", "GB", "R18", 18, 18, "R18", "Rated R18"),

        //RU
        new CertificationMapping("RU_Y", "RU", "Y", 0, 0, "Y", "0+" ),
        new CertificationMapping("RU_6", "RU", "6+", 6, 6, "6+" ),
        new CertificationMapping("RU_12", "RU", "12+", 12, 12, "12+" ),
        new CertificationMapping("RU_14", "RU", "14+", 14, 14, "14+" ),
        new CertificationMapping("RU_16", "RU", "16+", 16, 16, "16+" ),
        new CertificationMapping("RU_18", "RU", "18+", 18, 18, "18+" ),

        //NL
        new CertificationMapping("NL_AL", "NL", "AL", 0, 0, "AL" ),
        new CertificationMapping("NL_6", "NL", "6", 6, 6, "6" ),
        new CertificationMapping("NL_9", "NL", "9", 9, 9, "9" ),
        new CertificationMapping("NL_12", "NL", "12", 12, 12, "12" ),
        new CertificationMapping("NL_16", "NL", "16", 16, 16, "16" ),

        //IN
        new CertificationMapping("IN_U", "IN", "U", 0, 0, "U" ),
        new CertificationMapping("IN_UA", "IN", "UA", 12, 8, "UA" ),
        new CertificationMapping("IN_A", "IN", "A", 18, 18, "A" ),
        new CertificationMapping("IN_S", "IN", "S", 18, 18, "S" ),

        //FR
        new CertificationMapping("FR_U", "FR", "U", 0, 0, "U" ),
        new CertificationMapping("FR_10", "FR", "10", 10, 10, "10" ),
        new CertificationMapping("FR_12", "FR", "12", 12, 12, "12" ),
        new CertificationMapping("FR_16", "FR", "16", 16, 16, "16" ),
        new CertificationMapping("FR_18", "FR", "18", 18, 18, "18" ),

        //CA
        new CertificationMapping("CA_E", "CA", "E", 0, 0, "E" ),
        new CertificationMapping("CA_G", "CA", "G", 0, 0, "G" ),
        new CertificationMapping("CA_PG", "CA", "PG", 8, 0, "PG" ),
        new CertificationMapping("CA_C", "CA", "C", 0, 0, "C" ),
        new CertificationMapping("CA_C8", "CA", "C8", 8, 8, "C8" ),
        new CertificationMapping("CA_13", "CA", "13+", 13, 13, "13+" ),
        new CertificationMapping("CA_14", "CA", "14+", 14, 14, "14+" ),
        new CertificationMapping("CA_16", "CA", "16+", 16, 16, "16+" ),
        new CertificationMapping("CA_18", "CA", "18+", 18, 18, "18+" ),
        new CertificationMapping("CA_R", "CA", "R", 18, 18, "R" ),
        new CertificationMapping("CA_A", "CA", "A", 18, 18, "A" ),

        //AU
        new CertificationMapping("AU_E", "AU", "E", 0, 0, "E" ),
        new CertificationMapping("AU_P", "AU", "P", 2, 2, "P" ),
        new CertificationMapping("AU_C", "AU", "C", 5, 5, "C" ),
        new CertificationMapping("AU_G", "AU", "G", 0, 0, "G" ),
        new CertificationMapping("AU_PG", "AU", "PG", 15, 0, "PG" ),
        new CertificationMapping("AU_M", "AU", "M", 15, 15, "M" ),
        new CertificationMapping("AU_MA15", "AU", "MA15+", 15, 15, "MA15+", "MA" ),
        new CertificationMapping("AU_AV15", "AU", "AV15+", 15, 15, "AV15+", "AV" ),
        new CertificationMapping("AU_R18", "AU", "R18+", 18, 18, "R18+", "R" ),
        new CertificationMapping("AU_X18", "AU", "X18+", 18, 18, "X18+", "X" ),
        new CertificationMapping("AU_RC", "AU", "RC", 18, 18, "RC" ),

        //BR
        new CertificationMapping("BR_L", "BR", "L", 0, 0, "L" ),
        new CertificationMapping("BR_10", "BR", "10", 10, 10, "10" ),
        new CertificationMapping("BR_12", "BR", "12", 12, 12, "12" ),
        new CertificationMapping("BR_14", "BR", "14", 14, 14, "14" ),
        new CertificationMapping("BR_16", "BR", "16", 16, 16, "16" ),
        new CertificationMapping("BR_18", "BR", "18", 18, 18, "18" ),

        //FI
        new CertificationMapping("FI_S", "FI", "S", 0, 0, "S" ),
        new CertificationMapping("FI_KE", "FI", "K-E", 0, 0, "K-E" ),
        new CertificationMapping("FI_K7", "FI", "K-7", 7, 7, "K-7" ),
        new CertificationMapping("FI_K12", "FI", "K-12", 12, 12, "K-12" ),
        new CertificationMapping("FI_K16", "FI", "K-16", 16, 16, "K-16" ),
        new CertificationMapping("FI_K18", "FI", "K-18", 18, 18, "K-18" ),

        //HU
        new CertificationMapping("HU_6", "HU", "6", 6, 0, "6" ),
        new CertificationMapping("HU_12", "HU", "12", 12, 8, "12" ),
        new CertificationMapping("HU_16", "HU", "16", 16, 12, "16" ),
        new CertificationMapping("HU_18", "HU", "18", 18, 16, "18" ),

        //NZ
        new CertificationMapping("NZ_G", "NZ", "G", 0, 0, "G" ),
        new CertificationMapping("NZ_PGR", "NZ", "PGR", 14, 10, "PGR" ),
        new CertificationMapping("NZ_M", "NZ", "M", 16, 16, "M" ),
        new CertificationMapping("NZ_16", "NZ", "16", 16, 16, "16" ),
        new CertificationMapping("NZ_18", "NZ", "18", 18, 18, "18" ),
        new CertificationMapping("NZ_AO", "NZ", "AO", 18, 18, "AO" ),

        //NO
        new CertificationMapping("NO_A", "NO", "A", 0, 0, "A" ),
        new CertificationMapping("NO_11", "NO", "12", 12, 9, "12" ),
        new CertificationMapping("NO_15", "NO", "15", 15, 12, "15" ),
        new CertificationMapping("NO_18", "NO", "18", 18, 18, "18" ),

        //PL
        new CertificationMapping("PL_7", "PL", "7", 7, 7, "7" ),
        new CertificationMapping("PL_12", "PL", "12", 12, 12, "12" ),
        new CertificationMapping("PL_16", "PL", "16", 16, 16, "16" ),

        //RO
        new CertificationMapping("RO_AP", "RO", "A.P.", 0, 0, "A.P.", "AP" ),
        new CertificationMapping("RO_12", "RO", "12", 12, 8, "12", "AP-12", "AP12" ),
        new CertificationMapping("RO_15", "RO", "15", 15, 15, "15", "N-15", "N15" ),
        new CertificationMapping("RO_18", "RO", "18", 18, 18, "18", "IM-18", "IM18" ),

        //ES
        new CertificationMapping("ES_SC", "ES", "SC", 0, 0, "SC"),
        new CertificationMapping("ES_TA", "ES", "TA", 0, 0, "TA"),
        new CertificationMapping("ES_I", "ES", "Infantil", 0, 0, "Infantil" ),
        new CertificationMapping("ES_10", "ES", "10", 7, 7, "10", "+10" ),
        new CertificationMapping("ES_12", "ES", "12", 12, 12, "12", "+12" ),
        new CertificationMapping("ES_13", "ES", "13", 13, 13, "13", "+13" ),
        new CertificationMapping("ES_16", "ES", "16", 16, 16, "16", "+16" ),
        new CertificationMapping("ES_18", "ES", "18", 18, 18, "18", "+18" ),

        //PT
        new CertificationMapping("PT_T", "PT", "T", 0, 0, "Todos", "T", "Para todos os p�blicos" ),
        new CertificationMapping("PT_10", "PT", "10", 10, 6, "10", "10AP" ),
        new CertificationMapping("PT_12", "PT", "12", 12, 8, "12", "12AP" ),
        new CertificationMapping("PT_16", "PT", "16", 16, 16, "16" ),
        new CertificationMapping("PT_18", "PT", "18", 18, 18, "18" ),
      };
    }

    private static bool TryFindCertification(List<CertificationMapping> map, string country, string cert, out CertificationMapping certification)
    {
      certification = null;
      if (string.IsNullOrEmpty(cert))
        return false;

      cert = cert.Trim();

      // For long strings like: "Rated PG for rude humor and mild action"
      if (cert.Length > 10)
      {
        certification = map.Where(c => (string.IsNullOrEmpty(country) || c.CountryCode.Equals(country, System.StringComparison.InvariantCultureIgnoreCase)) &&
          (c.CertificationId.Equals(cert, System.StringComparison.InvariantCultureIgnoreCase) ||
          c.Notations.Where(n => cert.StartsWith(n, System.StringComparison.InvariantCultureIgnoreCase) ||
          cert.StartsWith(c.CountryCode + ":" + n, System.StringComparison.InvariantCultureIgnoreCase) ||
          cert.StartsWith(GetCountryNotation(c.CountryCode) + ":" + n, System.StringComparison.InvariantCultureIgnoreCase) ||
          cert.StartsWith(c.CountryCode + ": " + n, System.StringComparison.InvariantCultureIgnoreCase) ||
          cert.StartsWith(GetCountryNotation(c.CountryCode) + ": " + n, System.StringComparison.InvariantCultureIgnoreCase)).Any())).FirstOrDefault();
      }
      else if (cert.Length > 0)
      {
        certification = map.Where(c => (string.IsNullOrEmpty(country) || c.CountryCode.Equals(country, System.StringComparison.InvariantCultureIgnoreCase)) &&
          (c.CertificationId.Equals(cert, System.StringComparison.InvariantCultureIgnoreCase) ||
          c.Notations.Where(n => cert.Equals(n, System.StringComparison.InvariantCultureIgnoreCase) ||
          cert.Equals(c.CountryCode + ":" + n, System.StringComparison.InvariantCultureIgnoreCase) ||
          cert.Equals(GetCountryNotation(c.CountryCode) + ":" + n, System.StringComparison.InvariantCultureIgnoreCase) ||
          cert.Equals(c.CountryCode + ": " + n, System.StringComparison.InvariantCultureIgnoreCase) ||
          cert.Equals(GetCountryNotation(c.CountryCode) + ": " + n, System.StringComparison.InvariantCultureIgnoreCase)).Any())).FirstOrDefault();
      }
      return certification != null;
    }

    private static string GetCountryNotation(string country)
    {
      if (country == "US")
        return "USA";
      if (country == "GB")
        return "UK";
      return new RegionInfo(country).EnglishName;
    }

    public static bool TryFindMovieCertification(string cert, out CertificationMapping certification)
    {
      return TryFindCertification(MOVIE_CERTIFICATION_MAP, null, cert, out certification);
    }

    public static bool TryFindMovieCertification(string country, string cert, out CertificationMapping certification)
    {
      return TryFindCertification(MOVIE_CERTIFICATION_MAP, country, cert, out certification);
    }

    public static IEnumerable<CertificationMapping> GetMovieCertificationsForAge(int age, bool includeParentalGuided)
    {
      if (includeParentalGuided)
        return MOVIE_CERTIFICATION_MAP.Where(c => c.AllowedParentalGuidedAge <= age);
      else
        return MOVIE_CERTIFICATION_MAP.Where(c => c.AllowedAge <= age);
    }

    public static IEnumerable<CertificationMapping> GetMovieCertificationsForAge(string country, int age, bool includeParentalGuided)
    {
      if (includeParentalGuided)
        return MOVIE_CERTIFICATION_MAP.Where(c => c.CountryCode.Equals(country, System.StringComparison.InvariantCultureIgnoreCase) &&
          c.AllowedParentalGuidedAge <= age);
      else
        return MOVIE_CERTIFICATION_MAP.Where(c => c.CountryCode.Equals(country, System.StringComparison.InvariantCultureIgnoreCase) &&
          c.AllowedAge <= age);
    }

    public static IEnumerable<CertificationMapping> GetMovieCertificationsForCountry(string country)
    {
      return MOVIE_CERTIFICATION_MAP.Where(c => c.CountryCode.Equals(country, System.StringComparison.InvariantCultureIgnoreCase));
    }

    public static bool TryFindSeriesCertification(string cert, out CertificationMapping certification)
    {
      return TryFindCertification(SERIES_CERTIFICATION_MAP, null, cert, out certification);
    }

    public static bool TryFindSeriesCertification(string country, string cert, out CertificationMapping certification)
    {
      return TryFindCertification(SERIES_CERTIFICATION_MAP, country, cert, out certification);
    }

    public static IEnumerable<CertificationMapping> GetSeriesCertificationsForAge(int age, bool includeParentalGuided)
    {
      if (includeParentalGuided)
        return SERIES_CERTIFICATION_MAP.Where(c => c.AllowedParentalGuidedAge <= age);
      else
        return SERIES_CERTIFICATION_MAP.Where(c => c.AllowedAge <= age);
    }

    public static IEnumerable<CertificationMapping> GetSeriesCertificationsForAge(string country, int age, bool includeParentalGuided)
    {
      if (includeParentalGuided)
        return SERIES_CERTIFICATION_MAP.Where(c => c.CountryCode.Equals(country, System.StringComparison.InvariantCultureIgnoreCase) && 
          c.AllowedParentalGuidedAge <= age);
      else
        return SERIES_CERTIFICATION_MAP.Where(c => c.CountryCode.Equals(country, System.StringComparison.InvariantCultureIgnoreCase) && 
          c.AllowedAge <= age);
    }

    public static IEnumerable<CertificationMapping> GetSeriesCertificationsForCountry(string country)
    {
      return SERIES_CERTIFICATION_MAP.Where(c => c.CountryCode.Equals(country, System.StringComparison.InvariantCultureIgnoreCase));
    }

    public static IEnumerable<string> GetSupportedMovieCertificationCountries()
    {
      return MOVIE_CERTIFICATION_MAP.Select(c => c.CountryCode).Distinct();
    }

    public static IEnumerable<string> GetSupportedSeriesCertificationCountries()
    {
      return SERIES_CERTIFICATION_MAP.Select(c => c.CountryCode).Distinct();
    }

    public static bool IsAgeAllowed(CertificationMapping cert, int age, bool includeParentalGuided)
    {
      if (cert == null)
        return true;
      if (cert.AllowedAge <= age && !includeParentalGuided)
        return true;
      if (cert.AllowedParentalGuidedAge <= age && includeParentalGuided)
        return true;
      return false;
    }

    public static IEnumerable<CertificationMapping> FindAllAllowedMovieCertifications(string cert)
    {
      CertificationMapping current = MOVIE_CERTIFICATION_MAP.Where(c => c.CertificationId.Equals(cert, System.StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
      if (current != null)
      {
        return MOVIE_CERTIFICATION_MAP.Where(c => c.AllowedAge <= current.AllowedAge && c.AllowedParentalGuidedAge <= current.AllowedParentalGuidedAge);
      }
      return new List<CertificationMapping>();
    }

    public static CertificationMapping FindMatchingMovieCertification(string country, string cert)
    {
      if (string.IsNullOrEmpty(country))
        return null;
      CertificationMapping current = MOVIE_CERTIFICATION_MAP.Where(c => c.CertificationId.Equals(cert, System.StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
      IEnumerable<CertificationMapping> matches = null;
      CertificationMapping bestMatch = null;
      if (current != null)
      {
        if (current.CountryCode == country)
        {
          return current;
        }
        matches = MOVIE_CERTIFICATION_MAP.Where(c => c.CountryCode.Equals(country, System.StringComparison.InvariantCultureIgnoreCase) &&
          c.AllowedAge >= current.AllowedAge && c.AllowedParentalGuidedAge >= current.AllowedParentalGuidedAge);
        if (matches != null)
        {
          foreach (CertificationMapping match in matches)
          {
            if (bestMatch == null || bestMatch.AllowedAge > match.AllowedAge ||
              (bestMatch.AllowedAge == match.AllowedAge && bestMatch.AllowedParentalGuidedAge > match.AllowedParentalGuidedAge))
            {
              bestMatch = match;
            }
          }
        }
      }
      return bestMatch;
    }

    public static IEnumerable<CertificationMapping> FindAllAllowedSeriesCertifications(string cert)
    {
      CertificationMapping current = SERIES_CERTIFICATION_MAP.Where(c => c.CertificationId.Equals(cert, System.StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
      if (current != null)
      {
        return SERIES_CERTIFICATION_MAP.Where(c => c.AllowedAge <= current.AllowedAge && c.AllowedParentalGuidedAge <= current.AllowedParentalGuidedAge);
      }
      return new List<CertificationMapping>();
    }

    public static CertificationMapping FindMatchingSeriesCertification(string country, string cert)
    {
      if (string.IsNullOrEmpty(country))
        return null;
      CertificationMapping current = SERIES_CERTIFICATION_MAP.Where(c => c.CertificationId.Equals(cert, System.StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
      IEnumerable<CertificationMapping> matches = null;
      CertificationMapping bestMatch = null;
      if (current != null)
      {
        if (current.CountryCode == country)
        {
          return current;
        }
        matches = SERIES_CERTIFICATION_MAP.Where(c => c.CountryCode.Equals(country, System.StringComparison.InvariantCultureIgnoreCase) &&
          c.AllowedAge >= current.AllowedAge && c.AllowedParentalGuidedAge >= current.AllowedParentalGuidedAge);
        if (matches != null)
        {
          foreach (CertificationMapping match in matches)
          {
            if (bestMatch == null || bestMatch.AllowedAge > match.AllowedAge ||
              (bestMatch.AllowedAge == match.AllowedAge && bestMatch.AllowedParentalGuidedAge > match.AllowedParentalGuidedAge))
            {
              bestMatch = match;
            }
          }
        }
      }
      return bestMatch;
    }

    public static ILogger Logger
    {
      get
      {
        return ServiceRegistration.Get<ILogger>();
      }
    }
  }
}
