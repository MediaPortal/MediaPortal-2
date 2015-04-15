using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;

namespace Test.OnlineLibraries
{
  public class MockMediaItemAspectTypeRegistration : IMediaItemAspectTypeRegistration
  {
    protected IDictionary<Guid, MediaItemAspectMetadata> _locallyKnownMediaItemAspectTypes = new Dictionary<Guid, MediaItemAspectMetadata>();

    public IDictionary<Guid, MediaItemAspectMetadata> LocallyKnownMediaItemAspectTypes
    {
      get { return _locallyKnownMediaItemAspectTypes; }
    }

    public void RegisterLocallyKnownMediaItemAspectType(MediaItemAspectMetadata miaType)
    {
      if (_locallyKnownMediaItemAspectTypes.ContainsKey(miaType.AspectId))
        return;
      _locallyKnownMediaItemAspectTypes.Add(miaType.AspectId, miaType);
    }
  }
}