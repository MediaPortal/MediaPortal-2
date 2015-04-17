using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// A relationship extractor is responsible for enriching a media item with related data.
  /// </summary>
  /// <remarks>
  /// </remarks>
  public interface IRelationshipExtractor
  {
    /// <summary>
    /// Returns the metadata descriptor for this metadata relationship extractor.
    /// </summary>
    RelationshipExtractorMetadata Metadata { get; }

    bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid role, Guid linkedRole, out ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedAspectData, bool forceQuickMode);
  }
}
