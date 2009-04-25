using System; 
using System.Text;
using System.ComponentModel;

namespace Jyc.Expr
{
    static class ConvertHelper
    { 
        public static object ChangeType(object value, Type conversionType)
        {
            object o = null;
            try
            {
                o = System.Convert.ChangeType(value, conversionType);
            }
            catch
            {
                if (conversionType == null)
                {
                    throw new ArgumentNullException("conversionType");
                }
                if (value == null)
                {
                    if (conversionType.IsValueType)
                    {
                        throw new InvalidCastException("Cannot Cast Null To ValueType" );
                    }
                    return null;
                }
                TypeConverter typeConverter = TypeDescriptor.GetConverter(value);
                if (typeConverter == null)
                {
                    throw new InvalidCastException("Cannot Cast Null To  Target Type");
                }

                if (!typeConverter.CanConvertTo(conversionType))
                {
                    throw new InvalidCastException("Cannot Cast Null To  Target Type");
                }

                return typeConverter.ConvertTo(value, conversionType);

            }
            return o;
        }

        public static string ToString(object value )
        {
            object o = null;
            try
            {
                o = System.Convert.ChangeType(value, typeof(string));
            }
            catch
            {
                if (value == null)
                {
                    return string.Empty;
                }
                else if (value is DBNull)
                    return string.Empty;

                TypeConverter typeConverter = TypeDescriptor.GetConverter(value);
                if (typeConverter == null)
                {
                    throw new InvalidCastException("Cannot Cast Null To  Target Type");
                }

                if (!typeConverter.CanConvertTo(typeof(string)))
                {
                    throw new InvalidCastException("Cannot Cast Null To  Target Type");
                }

                return typeConverter.ConvertToString(value);

            }
            return (string)o;
        }
    }
}
