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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using System.Collections.Generic;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
    public class MIAQueryBuilder : MainQueryBuilder
    {
        public MIAQueryBuilder(MIA_Management miaManagement, IEnumerable<QueryAttribute> simpleSelectAttributes,
            SelectProjectionFunction selectProjectionFunction,
            IEnumerable<MediaItemAspectMetadata> necessaryRequestedMIAs, IEnumerable<MediaItemAspectMetadata> optionalRequestedMIAs,
            IFilter filter, IList<SortInformation> sortInformation) : base(miaManagement, simpleSelectAttributes,
            selectProjectionFunction,
            necessaryRequestedMIAs, optionalRequestedMIAs,
            filter, sortInformation)
        {
        }

        protected override bool Include(MediaItemAspectMetadata miam)
        {
            return (miam is SingleMediaItemAspectMetadata || miam is MultipleMediaItemAspectMetadata);
        }

        /// <summary>
        /// Generates an SQL statement for the underlaying query specification which contains groups of the same attribute
        /// values and a count column containing the size of each group.
        /// </summary>
        /// <param name="groupSizeAlias">Alias of the groups sizes in the result set.</param>
        /// <param name="attributeAliases">Returns the aliases for all selected attributes.</param>
        /// <param name="statementStr">SQL statement which was built by this method.</param>
        /// <param name="bindVars">Bind variables to be inserted into the returned <paramref name="statementStr"/>.</param>
        public void GenerateSqlGroupByStatement(out string groupSizeAlias, out IDictionary<QueryAttribute, string> attributeAliases,
            out string statementStr, out IList<BindVar> bindVars)
        {
            GenerateSqlStatement(true, null, out groupSizeAlias, out attributeAliases, out statementStr, out bindVars);
        }
    }
}
