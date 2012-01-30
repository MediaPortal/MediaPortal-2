using MediaPortal.Common.General;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public class GenericImageEffect : ImageEffect
  {
    #region Protected fields

    protected AbstractProperty _partialShaderEffectProperty;

    #endregion

    #region Ctor & maintainance

    public GenericImageEffect()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _partialShaderEffectProperty = new SProperty(typeof(string), "effects\\none");
    }

    void Attach()
    {
      _partialShaderEffectProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _partialShaderEffectProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      GenericImageEffect el = (GenericImageEffect) source;
      PartitialEffectName = el.PartitialEffectName;
      Attach();
    }

    private void OnPropertyChanged(AbstractProperty property, object oldvalue)
    {
      _partialShaderEffect = "effects\\" + PartitialEffectName;
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }

    #endregion

    #region Properties

    public AbstractProperty PartialShaderEffectProperty
    {
      get { return _partialShaderEffectProperty; }
    }

    public string PartitialEffectName
    {
      get { return (string) _partialShaderEffectProperty.GetValue(); }
      set { _partialShaderEffectProperty.SetValue(value); }
    }

    #endregion
  }
}
