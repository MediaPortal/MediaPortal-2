// Source from: http://madreflection.originalcoder.com/2009/12/generic-tryparse.html

namespace MediaPortal.Plugins.MP2Extended.Utils
{
	/// <summary>Extension method class containing generic parsing functionality.</summary>
	public static partial class GenericParsing
	{
		static GenericParsing()
		{
			SetTryParseMethod<string>(TryParseString);
			//SetTryParseMethod<Guid>(TryParseGuid);
		}
	}
}
