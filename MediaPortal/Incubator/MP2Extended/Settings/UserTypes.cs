namespace MediaPortal.Plugins.MP2Extended.Settings
{
  /// <summary>
  /// The lower the type the higher the rights.
  /// A type includes all the types above the defined type. e.g. Admin includes User.
  /// Admin is below User => Admin has higher rights than User and can do everything what User can do.
  /// </summary>
  public enum UserTypes
  {
    User, // there is no need to check the rights for Type User, because this is the lowest right class and the server protects everything already
    Admin
  }
}
