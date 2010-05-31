namespace Jyc.Expr
{
    public class ParameterVariableHolder : IVariableHolder
    {
        ParameterCollection _parameters;

        public ParameterVariableHolder()
        {
            _parameters = new ParameterCollection();
        }

        public ParameterCollection Parameters
        {
            get { return _parameters; } 
        }

        object  GetVariable(string name)
        {
            return _parameters[name].Value;
        }

        #region IVariableHolder Members

        public bool Exists(string name)
        {
            return _parameters.Contains(name);
        }

        object IVariableHolder.GetVariable(string name)
        {
            return this.GetVariable(name);
        }

        #endregion
 
    }
}
