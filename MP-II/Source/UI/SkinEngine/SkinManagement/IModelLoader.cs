using System;

namespace MediaPortal.UI.SkinEngine.SkinManagement
{
  /// <summary>
  /// Callback interface to load GUI models.
  /// </summary>
  public interface IModelLoader
  {
    object GetOrLoadModel(Guid modelId);
  }
}