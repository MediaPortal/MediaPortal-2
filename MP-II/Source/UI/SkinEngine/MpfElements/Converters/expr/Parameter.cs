using System;
using System.Collections;

namespace Jyc.Expr
{
    public class Parameter
    {
        public Parameter()
        {
        }

        public Parameter(object  value)
        {
            this._value = value;
            if (value != null)
            {
                this._type = value.GetType();
            }
        }

        public Parameter(object value,Type type)
        {
            this._value = value;
            this._type = type;
        }

        object _value;

        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }
        Type _type;

        public Type Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public override string ToString()
        {
            if (_value != null)
                return _value.ToString();
            return "(null)";
        }
    }

    public sealed class ParameterCollection
    {
        Hashtable  _parameters;

        public ParameterCollection( )
        {
            _parameters = new Hashtable(); 
        }

        public void Clear()
        {
            _parameters.Clear();
        }

        public bool Contains(string name)
        {
            return _parameters.ContainsKey(name);
        } 

        public Parameter this[string name]
        {
            get
            {
                return (Parameter)_parameters[name]; 
            }
            set
            {
                if (value == null)
                    _parameters.Remove(name);
                else
                {
                    _parameters[name] = value;
                }
            }
        }
    }
}
