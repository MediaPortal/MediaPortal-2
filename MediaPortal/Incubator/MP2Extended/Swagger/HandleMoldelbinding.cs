using Swashbuckle.SwaggerGen.Generator;

namespace MediaPortal.Plugins.MP2Extended.Swagger
{
  public class HandleModelbinding : IOperationFilter
  {
    public void Apply(Operation operation, OperationFilterContext context)
    {
      if (operation.Parameters == null) return;

      foreach (IParameter param in operation.Parameters)
      {
        if (param.In == "modelbinding")
          param.In = "query";
      }
    }
  }
}
