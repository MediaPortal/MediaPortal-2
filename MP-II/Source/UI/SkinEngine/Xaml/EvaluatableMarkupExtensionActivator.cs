using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Xaml
{
  public class EvaluatableMarkupExtensionActivator
  {
    protected IParserContext _context;
    protected IEvaluableMarkupExtension _eme;
    protected IDataDescriptor _dd;

    public EvaluatableMarkupExtensionActivator(IParserContext context, IEvaluableMarkupExtension eme, IDataDescriptor dd)
    {
      _context = context;
      _eme = eme;
      _dd = dd;
    }

    public void Activate()
    {
      object value;
      if (!_eme.Evaluate(out value))
        throw new XamlBindingException("Could not evaluate markup extension '{0}'", _eme);
      _context.AssignValue(_dd, value);
    }
  }
}