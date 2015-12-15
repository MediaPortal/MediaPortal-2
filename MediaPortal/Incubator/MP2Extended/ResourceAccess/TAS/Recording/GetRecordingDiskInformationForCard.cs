using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.TAS.Misc.BaseClasses;
using MediaPortal.Plugins.MP2Extended.Utils;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Recording
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(int), Nullable = false)]
  internal class GetRecordingDiskInformationForCard : BaseCard, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;

      int idInt;
      if (!Int32.TryParse(id, out idInt))
      {
        throw new BadRequestException(String.Format("GetRecordingDiskInformationForCard: Couldn't convert id to int: {0}", id));
      }
      
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetRecordingDiskInformationForCard: ITvProvider not found");

      ITunerInfo tunerInfo = ServiceRegistration.Get<ITvProvider>() as ITunerInfo;

      if (tunerInfo == null)
        throw new BadRequestException("GetRecordingDiskInformationForCard: ITunerInfo not present");

      List<ICard> cards;
      tunerInfo.GetCards(out cards);

      return DiskSpaceInformation.GetSpaceInformation(cards.Select(card => Card(card)).Single(x => x.Id == idInt).RecordingFolder);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}