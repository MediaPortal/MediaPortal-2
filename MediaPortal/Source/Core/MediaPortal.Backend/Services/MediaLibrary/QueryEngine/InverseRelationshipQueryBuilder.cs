using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
    public class InverseRelationshipQueryBuilder : MainQueryBuilder
    {
        public InverseRelationshipQueryBuilder(MIA_Management miaManagement, IEnumerable<QueryAttribute> simpleSelectAttributes,
            Guid[] linkedIds)
          : base(miaManagement, simpleSelectAttributes,
          null,
          new List<MediaItemAspectMetadata> { RelationshipAspect.Metadata }, new List<MediaItemAspectMetadata> { },
          new MediaItemIdFilter(linkedIds), null)
        {
        }

        protected override CompiledFilter CreateCompiledFilter(Namespace ns, BindVarNamespace bvNamespace, string outerMIIDJoinVariable, IList<TableJoin> tableJoins)
        {
          return new InverseRelationshipCompiledFilter(_miaManagement, (MediaItemIdFilter)_filter, ns, bvNamespace, outerMIIDJoinVariable, tableJoins);
        }

        protected override bool Include(MediaItemAspectMetadata miam)
        {
          return true;
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
