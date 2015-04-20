using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections.Generic;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
    public class MultipleMIAQueryBuilder : MainQueryBuilder
    {
        private MultipleMediaItemAspectMetadata _requestedMIA;

        public MultipleMIAQueryBuilder(MIA_Management miaManagement, IEnumerable<QueryAttribute> simpleSelectAttributes,
            SelectProjectionFunction selectProjectionFunction,
            MultipleMediaItemAspectMetadata requestedMIA,
            Guid[] mediaItemIds, IList<SortInformation> sortInformation)
            : base(miaManagement, simpleSelectAttributes,
            selectProjectionFunction,
            new List<MediaItemAspectMetadata> { requestedMIA }, new List<MediaItemAspectMetadata> { },
            new MediaItemIdFilter(mediaItemIds), sortInformation)
        {
            _requestedMIA = requestedMIA;
        }

        protected override bool Include(MediaItemAspectMetadata miam)
        {
            // Make it's only the requested MIA in use
            return miam == _requestedMIA;
        }

        /// <summary>
        /// Generates the SQL statement for the underlaying query specification.
        /// </summary>
        /// <param name="mediaItemIdAlias">Alias of the media item's IDs in the result set.</param>
        /// <param name="miamAliases">Returns the aliases of the ID columns of the joined media item aspect tables. With this mapping,
        /// the caller can check if a MIA type was requested or not. That is needed for optional requested MIA types.</param>
        /// <param name="indexAliases">Returns the aliases of the dex columns of the joined media item aspect tables.</param>
        /// <param name="attributeAliases">Returns the aliases for all selected attributes.</param>
        /// <param name="statementStr">SQL statement which was built by this method.</param>
        /// <param name="bindVars">Bind variables to be inserted into the returned <paramref name="statementStr"/>.</param>
        public void GenerateSqlStatement(out string mediaItemIdAlias,
            out IDictionary<QueryAttribute, string> attributeAliases,
            out string statementStr, out IList<BindVar> bindVars)
        {
            IDictionary<MediaItemAspectMetadata, string> miamAliases = new Dictionary<MediaItemAspectMetadata, string>();
            GenerateSqlStatement(false, miamAliases, out mediaItemIdAlias, out attributeAliases, out statementStr, out bindVars);
        }
    }
}
