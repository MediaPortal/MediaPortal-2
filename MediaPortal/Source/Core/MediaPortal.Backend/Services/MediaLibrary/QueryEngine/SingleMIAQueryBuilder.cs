using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using System.Collections.Generic;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
    public class SingleMIAQueryBuilder : MainQueryBuilder
    {
        public SingleMIAQueryBuilder(MIA_Management miaManagement, IEnumerable<QueryAttribute> simpleSelectAttributes,
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
            return (miam is SingleMediaItemAspectMetadata);
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
            GenerateSqlStatement(true, null, out groupSizeAlias, null, out attributeAliases, out statementStr, out bindVars);
        }

        /// <summary>
        /// Generates the SQL statement for the underlaying query specification.
        /// </summary>
        /// <param name="mediaItemIdAlias">Alias of the media item's IDs in the result set.</param>
        /// <param name="miamAliases">Returns the aliases of the ID columns of the joined media item aspect tables. With this mapping,
        /// the caller can check if a MIA type was requested or not. That is needed for optional requested MIA types.</param>
        /// <param name="attributeAliases">Returns the aliases for all selected attributes.</param>
        /// <param name="statementStr">SQL statement which was built by this method.</param>
        /// <param name="bindVars">Bind variables to be inserted into the returned <paramref name="statementStr"/>.</param>
        public void GenerateSqlStatement(out string mediaItemIdAlias,
            out IDictionary<MediaItemAspectMetadata, string> miamAliases,
            out IDictionary<QueryAttribute, string> attributeAliases,
            out string statementStr, out IList<BindVar> bindVars)
        {
            miamAliases = new Dictionary<MediaItemAspectMetadata, string>();
            IDictionary<MediaItemAspectMetadata, string> indexAliases = new Dictionary<MediaItemAspectMetadata, string>();
            GenerateSqlStatement(false, miamAliases, out mediaItemIdAlias, indexAliases, out attributeAliases, out statementStr, out bindVars);
        }
    }
}
