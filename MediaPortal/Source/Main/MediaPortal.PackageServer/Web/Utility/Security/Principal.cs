using System;
using System.Security.Principal;
using MediaPortal.PackageServer.Domain.Entities.Enumerations;

namespace MediaPortal.PackageServer.Utility.Security
{
  public class Principal : IPrincipal
  {
    public long UserID { get; private set; }
    public Role Role { get; private set; }
    public IIdentity Identity { get; private set; }

    public Principal(long userID, string alias, Role role)
    {
      UserID = userID;
      Role = role;
      Identity = new GenericIdentity(alias);
    }

    public bool IsInRole(string role)
    {
      return string.Equals(Role.ToString(), role, StringComparison.InvariantCultureIgnoreCase);
    }
  }
}