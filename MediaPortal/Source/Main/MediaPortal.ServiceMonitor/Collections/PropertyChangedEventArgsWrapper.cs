using System.ComponentModel;

namespace MediaPortal.ServiceMonitor.Collections
{
	public class PropertyChangedEventArgsWrapper : PropertyChangedEventArgs
	{
		// ******************************************************************
		private PropertyChangedEventArgs _propertyChangedEventArgsOriginal;
		private object _senderOriginal;

		// ******************************************************************
		public PropertyChangedEventArgsWrapper(string propertyName, object senderOriginal, PropertyChangedEventArgs propertyChangedEventArgsOriginal)
			: base(propertyName)
		{
			_propertyChangedEventArgsOriginal = propertyChangedEventArgsOriginal;
			_senderOriginal = senderOriginal;
		}

		// ******************************************************************
		public PropertyChangedEventArgs PropertyChangedEventArgsOriginal
		{
			get { return _propertyChangedEventArgsOriginal; }
		}

		// ******************************************************************
	}
}
