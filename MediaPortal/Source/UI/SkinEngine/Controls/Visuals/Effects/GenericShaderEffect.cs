using MediaPortal.Common.General;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  public class GenericShaderEffect : ShaderEffect
  {
    #region Protected fields

    protected AbstractProperty _shaderEffectNameProperty;
    
    #endregion

    #region Ctor & maintainance

    public GenericShaderEffect()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _shaderEffectName = "normal";
      _shaderEffectNameProperty = new SProperty(typeof(string), _shaderEffectName);
    }

    void Attach()
    {
      _shaderEffectNameProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _shaderEffectNameProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      GenericShaderEffect el = (GenericShaderEffect) source;
      ShaderEffectName = el.ShaderEffectName;
      Attach();
    }

    private void OnPropertyChanged(AbstractProperty property, object oldvalue)
    {
      _shaderEffectName = ShaderEffectName;
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }
    
    #endregion

    #region Properties

    public AbstractProperty ShaderEffectNameProperty
    {
      get { return _shaderEffectNameProperty; }
    }

    public string ShaderEffectName
    {
      get { return (string) _shaderEffectNameProperty.GetValue(); }
      set { _shaderEffectNameProperty.SetValue(value); }
    }

    #endregion
  }
}
