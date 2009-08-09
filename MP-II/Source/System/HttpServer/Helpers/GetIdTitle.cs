using System.Collections;

namespace HttpServer.Helpers
{
  /// <summary>
  /// Delegate used by <see cref="FormHelper.Select(string, System.Collections.IEnumerable, GetIdTitle, object, bool)"/> to populate select options.
  /// </summary>
  /// <param name="obj">current object (for instance a User).</param>
  /// <param name="id">Text that should be displayed in the value part of a &lt;optiongt;-tag.</param>
  /// <param name="title">Text shown in the select list.</param>
  /// <example>
  /// // Class that is going to be used in a SELECT-tag.
  /// public class User
  /// {
  ///     private readonly string _realName;
  ///     private readonly int _id;
  ///     public User(int id, string realName)
  ///     {
  ///         _id = id;
  ///         _realName = realName;
  ///     }
  ///     public string RealName
  ///     {
  ///         get { return _realName; }
  ///     }
  /// 
  ///     public int Id
  ///     {
  ///         get { return _id; }
  ///     }
  /// }
  /// 
  /// // Using an inline delegate to generate the select list
  /// public void UserInlineDelegate()
  /// {
  ///     List&lt;User&gt; items = new List&lt;User&gt;();
  ///     items.Add(new User(1, "adam"));
  ///     items.Add(new User(2, "bertial"));
  ///     items.Add(new User(3, "david"));
  ///     string htmlSelect = Select("users", "users", items, delegate(object o, out object id, out object value)
  ///                                                         {
  ///                                                             User user = (User)o;
  ///                                                             id = user.Id;
  ///                                                             value = user.RealName;
  ///                                                         }, 2, true);
  /// }
  /// 
  /// // Using an method as delegate to generate the select list.
  /// public void UseExternalDelegate()
  /// {
  ///     List&lt;User&gt; items = new List&lt;User&gt;();
  ///     items.Add(new User(1, "adam"));
  ///     items.Add(new User(2, "bertial"));
  ///     items.Add(new User(3, "david"));
  ///     string htmlSelect = Select("users", "users", items, UserOptions, 1, true);
  /// }
  /// 
  /// // delegate returning id and title
  /// public static void UserOptions(object o, out object id, out object title)
  /// {
  ///     User user = (User)o;
  ///     id = user.Id;
  ///     value = user.RealName;
  /// }    /// </example>
  public delegate void GetIdTitle(object obj, out object id, out string title);
}