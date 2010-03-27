using System;
using System.Collections;
using System.Reflection;

namespace Jyc.Expr
{
    class IndexerOp : Operator
    {
        public override OperatorPriority Priority
        {
            get { return OperatorPriority.Indexer; }
        }   

        public override string ToString()
        {
            return "indexer";
        }

        internal override Result Eval(Evaluator evaluater, Result[] argArray)
        {
            if (argArray.Length < 2)
            {
                throw new ArgumentException();
            }

            object target = argArray[0].Value;
            if (target == null)
            {
                throw new ArgumentException();
            }
            int length = argArray.Length - 1;

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
            bindingFlags = BindingFlags.Public;

            Type[] argTypeArray = new Type[length];
            object[] argValueAry = new object[length];
            for (int i = 0; i < length; i++)
            {
                argTypeArray[i] = argArray[i + 1].Type;
                argValueAry[i] = argArray[i + 1].Value;
            }

            Binder defaultBinder = Type.DefaultBinder;
            PropertyInfo pi = type.GetProperty("Item", bindingFlags, defaultBinder, null, argTypeArray, null);
            if (pi == null)
            {
                pi = type.GetProperty("Items", bindingFlags, defaultBinder, null, argTypeArray, null);
            }
            if (pi != null)
            {
                return new Result(pi.PropertyType, pi.GetValue(target, argValueAry));
            }

            if (target == null)
            {
                throw new ArgumentException();
            }

            object ret;

            if (target is string)
            {
                string s = (string)target;
                int index = (int)ConvertHelper.ChangeType(argArray[1].Value, typeof(int));
                ret = s[index]; 
                return new Result(typeof(char), ret);
            }

            if (target is Array)
            {
                Array array = (Array)target;
                if (array.Rank != (argArray.Length - 1))
                {
                    throw new ArgumentException();
                }

                int[] indices = new int[length];
                for (int i = 0; i < length; i++)
                {
                    indices[i] = (int)ConvertHelper.ChangeType(argArray[i + 1].Value, typeof(int));
                }

                ret = array.GetValue(indices);
                if (ret != null)
                    return new Result(ret.GetType(), ret);
                return new Result(null, null);
            }

            if (target is IList)
            {
                IList list = (IList)target;
                if (argArray.Length != 2)
                {
                    throw new ArgumentException();
                }
                int index = (int)ConvertHelper.ChangeType(argArray[1].Value, typeof(int));
                ret = list[index];
                if (ret != null)
                    return new Result(ret.GetType(), ret);
                return new Result(null, null);
            }

            throw new ArgumentException();
        }
    }
}

