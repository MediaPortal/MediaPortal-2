using System;
using MediaPortal.Common.MediaManagement;
using Microsoft.AspNet.Http;
using Newtonsoft.Json.Serialization;

namespace MediaPortal.Plugins.AspNetWebApi.Json
{
  /// <summary>
  /// Tells Newtonsoft.Json to use <see cref="MediaItemJsonConverter"/> to convert objects
  /// of type <see cref="MediaItem"/> without decorating the MediaItem class with Attributes
  /// </summary>
  class MediaItemResolver : DefaultContractResolver
  {
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MediaItemResolver(IHttpContextAccessor httpContextAccessor)
    {
      _httpContextAccessor = httpContextAccessor;
    }

    protected override JsonObjectContract CreateObjectContract(Type objectType)
    {
      var contract = base.CreateObjectContract(objectType);
      if (objectType == typeof(MediaItem))
        contract.Converter = new MediaItemJsonConverter(_httpContextAccessor);
      return contract;
    }
  }
}
