using System;
using MediaPortal.Common.MediaManagement;
using Newtonsoft.Json.Serialization;

namespace MediaPortal.Plugins.AspNetWebApi.Json
{
  class MediaItemResolver : DefaultContractResolver
  {
    protected override JsonObjectContract CreateObjectContract(Type objectType)
    {
      JsonObjectContract contract = base.CreateObjectContract(objectType);
      if (objectType == typeof(MediaItem))
      {
        contract.Converter = new MediaItemJsonConverter();
      }
      return contract;
    }
  }
}
