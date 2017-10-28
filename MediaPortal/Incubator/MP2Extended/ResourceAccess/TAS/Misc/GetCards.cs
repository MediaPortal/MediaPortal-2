using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.TAS.Misc;
using MediaPortal.Plugins.MP2Extended.TAS.Misc.BaseClasses;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Misc
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  internal class GetCards : BaseCard
  {
    public IList<WebCard> Process()
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetCards: ITvProvider not found");

      ITunerInfo tunerInfo = ServiceRegistration.Get<ITvProvider>() as ITunerInfo;

      List<ICard> cards;
      tunerInfo.GetCards(out cards);

      return cards.Select(card => Card(card)).ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
