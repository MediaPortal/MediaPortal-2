using System;
using System.Reflection;

namespace Jyc.Expr
{
    class MemberOp : Operator
    {
        bool _isFunction = false;
        public MemberOp()
        {
        }

        public bool IsFunction
        {
            get { return _isFunction; }
            set { _isFunction = value; }
        }  

        public override OperatorPriority Priority
        {
            get 
            { 
                return OperatorPriority.Member; 
            }
        }

        public override string ToString()
        {
            if (this.IsFunction)
                return "function";
            return "member";
        }

        internal override Result Eval(Evaluator evaluater, Result[] argArray)
        {
            if (_isFunction)
            {
                return EvalFunc(evaluater, argArray); 
            }
            else
            {
                return EvalProp(evaluater, argArray);
            }
        }

        Result EvalProp(Evaluator evaluater, Result[] argArray)
        { 
            if (argArray.Length == 0)
            {
                throw new ArgumentException();
            }

            if (argArray.Length == 1)
            {
                return argArray[0];
            }

            string memberName = argArray[1].Value as string;
            if (memberName == null)
            {
                throw new ArgumentException();
            }

            if (argArray[0] == null)
            {
                throw new ArgumentNullException("argArray[0]");
            }

            object target = argArray[0].Value;
            if (target == null)
            {
                throw new ArgumentException();
            }

            Type type = null;
            
            BindingFlags bindingFlags = BindingFlags.Default;
            if (target is Type)
            {
                type = (Type)target;
                target = null;
                bindingFlags = BindingFlags.Static ; 
            }
            else
            {
                type = target.GetType();
                bindingFlags = BindingFlags.Instance; 
            }

            bindingFlags |= BindingFlags.Public; 
            PropertyInfo pi = type.GetProperty(memberName, bindingFlags);
            if (pi != null)
            {
                return new Result(pi.PropertyType, pi.GetValue(target, null));
            }

            FieldInfo fi = type.GetField(memberName, bindingFlags);
            if (fi != null)
            {
                return new Result(fi.FieldType, fi.GetValue(target));
            }

            throw new ArgumentException();

        }

        Result EvalFunc(Evaluator evaluater, Result[] argArray)
        {
            if (argArray.Length < 2)
            {
                throw new ArgumentException();
            }

            if (argArray[0] == null || argArray[1] == null)
            {
                throw new ArgumentNullException("argArray[0]");
            }

            object target = argArray[0].Value;
            if (target == null)
            {
                throw new ArgumentException();
            }

            string functionName = ConvertHelper.ToString(argArray[1].Value);

            Type type = null;
            BindingFlags bindingFlags = BindingFlags.Default;
            if (target is Type)
            {
                type = (Type)target;
                target = null;
                bindingFlags = BindingFlags.Static;
            }
            else
            {
                type = target.GetType();
                bindingFlags = BindingFlags.Instance;
            }
            bindingFlags |= BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.OptionalParamBinding;

            int length = argArray.Length - 2;
            object[] argValueAry = new object[length];
            for (int i = 0; i < length; i++)
            {
                argValueAry[i] = argArray[i + 2].Value;
            }
            Binder defaultBinder = Type.DefaultBinder;
            object ret = type.InvokeMember(functionName,
                bindingFlags, defaultBinder, target, argValueAry);
            if (ret != null)
                return new Result(ret.GetType(), ret);
            return new Result(null, null);

        }
    }
}
