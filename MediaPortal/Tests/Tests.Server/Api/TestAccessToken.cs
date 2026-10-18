#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Auth;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.UserProfileDataManagement;
using Moq;
using NUnit.Framework;

namespace Tests.Server.Api
{
  /// <summary>
  /// Tests for the token that identifies a client to the HTTP API, for the key it is signed with, for the
  /// credential check behind it, and for the handler that reads it off a request.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These cover the part of the server that used to be Owin. On .NET Framework the answer to "who is this
  /// request" came from middleware in <c>Microsoft.Owin.Security.OAuth</c>, so the project had no tests for it -
  /// there was nothing of its own to test. Replacing that middleware means the rules are now MP2's code and
  /// therefore MP2's responsibility: an accepted token that should have been rejected is a hole in the front door,
  /// so the rejections are what most of these tests are about.
  /// </para>
  /// <para>
  /// Nothing here starts a listener. The handlers take a <see cref="DefaultHttpContext"/>, the credential check
  /// takes a mocked profile store, and the HTTP behaviour on top of them is measured from outside by
  /// <c>scripts/ci/run-server-check.ps1</c> against a real server on both Windows and Linux - which is the only way
  /// to prove the parts are wired into the pipeline in the right order.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestAccessToken
  {
    private const string PASSWORD = "***";
    private const string PROFILE_NAME = "Georg";

    static byte[] Key(byte seed = 7)
    {
      byte[] key = new byte[MediaPortalSigningKey.KEY_BYTES];
      for (int i = 0; i < key.Length; i++)
        key[i] = (byte)(seed + i);
      return key;
    }

    static UserProfile Profile(UserProfileType type = UserProfileType.UserProfile, string password = PASSWORD)
    {
      return new UserProfile(Guid.NewGuid(), PROFILE_NAME, type,
          password == null ? null : UserProfile.HashPassword(password));
    }

    static Mock<IUserProfileDataManagement> StoreFor(UserProfile profile)
    {
      Mock<IUserProfileDataManagement> store = new Mock<IUserProfileDataManagement>();
      store.Setup(m => m.GetProfileByNameAsync(PROFILE_NAME))
          .ReturnsAsync(new MediaPortal.Common.Async.AsyncResult<UserProfile>(true, profile));
      store.Setup(m => m.LoginProfileAsync(It.IsAny<Guid>())).ReturnsAsync(true);
      return store;
    }

    static string Base64(string value)
    {
      return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    static string ClaimValue(ClaimsIdentity identity, string claimType)
    {
      // ClaimsIdentity has FindFirst returning a Claim; FindFirstValue is on ClaimsPrincipal only.
      Claim claim = identity != null ? identity.FindFirst(claimType) : null;
      return claim != null ? claim.Value : null;
    }

    static ClaimsIdentity Identity(bool admin = true)
    {
      ClaimsIdentity identity = new ClaimsIdentity(ResourceServer.MEDIAPORTAL_AUTHENTICATION_TYPE);
      identity.AddClaim(new Claim(MediaPortalAccessToken.NAME_CLAIM, PROFILE_NAME));
      identity.AddClaim(new Claim(MediaPortalAccessToken.PROFILE_ID_CLAIM, Guid.NewGuid().ToString()));
      identity.AddClaim(new Claim(MediaPortalAccessToken.ROLE_CLAIM, admin ? "Admin" : "User"));
      return identity;
    }

    // ---------------------------------------------------------------------------------------------
    // The token itself
    // ---------------------------------------------------------------------------------------------

    [Test]
    public void ATokenHandsBackTheIdentityItWasIssuedFor()
    {
      ClaimsIdentity issued = Identity();
      string token = MediaPortalAccessToken.Create(Key(), issued, TimeSpan.FromMinutes(5));

      ClaimsIdentity validated;
      string error;
      Assert.That(MediaPortalAccessToken.TryValidate(Key(), token, out validated, out error), Is.True, error);
      Assert.That(error, Is.Null);
      Assert.That(validated.IsAuthenticated, Is.True);
      Assert.That(validated.AuthenticationType, Is.EqualTo(ResourceServer.MEDIAPORTAL_AUTHENTICATION_TYPE),
          "MediaPortalAuthorizeAttribute compares this exact string; a different one means 403 for a valid token");
      Assert.That(ClaimValue(validated, MediaPortalAccessToken.NAME_CLAIM), Is.EqualTo(PROFILE_NAME));
      Assert.That(ClaimValue(validated, MediaPortalAccessToken.PROFILE_ID_CLAIM),
          Is.EqualTo(ClaimValue(issued, MediaPortalAccessToken.PROFILE_ID_CLAIM)));
      Assert.That(ClaimValue(validated, MediaPortalAccessToken.ROLE_CLAIM), Is.EqualTo("Admin"));
    }

    [Test]
    public void ATokenIsTwoBase64urlPartsAndCarriesNoPaddingOrUnsafeCharacters()
    {
      string token = MediaPortalAccessToken.Create(Key(), Identity(), TimeSpan.FromMinutes(5));
      string[] parts = token.Split('.');
      Assert.That(parts.Length, Is.EqualTo(2));
      foreach (string part in parts)
      {
        Assert.That(part, Does.Not.Contain("+").And.Not.Contain("/").And.Not.Contain("="),
            "a token goes into an HTTP header; '=' padding and '+' or '/' would need every consumer to escape it");
      }
    }

    [Test]
    public void ChangingOneCharacterOfATokenMakesItStopWorking()
    {
      string token = MediaPortalAccessToken.Create(Key(), Identity(), TimeSpan.FromMinutes(5));
      // Flip one character of the payload, keeping the length and the alphabet the same: the only thing that can
      // reject this is the signature.
      int position = token.IndexOf('.') / 2;
      char original = token[position];
      char replacement = original == 'A' ? 'B' : 'A';
      string tampered = token.Substring(0, position) + replacement + token.Substring(position + 1);

      ClaimsIdentity validated;
      string error;
      Assert.That(MediaPortalAccessToken.TryValidate(Key(), tampered, out validated, out error), Is.False);
      Assert.That(validated, Is.Null);
      Assert.That(error, Is.EqualTo("the signature does not match"));
    }

    [Test]
    public void ATokenFromAnotherServerIsRejected()
    {
      string token = MediaPortalAccessToken.Create(Key(11), Identity(), TimeSpan.FromMinutes(5));

      ClaimsIdentity validated;
      string error;
      Assert.That(MediaPortalAccessToken.TryValidate(Key(12), token, out validated, out error), Is.False);
      Assert.That(error, Is.EqualTo("the signature does not match"));
    }

    [Test]
    public void AnExpiredTokenIsRejected()
    {
      string token = MediaPortalAccessToken.Create(Key(), Identity(), TimeSpan.FromMinutes(5));

      ClaimsIdentity validated;
      string error;
      Assert.That(MediaPortalAccessToken.TryValidate(Key(), token, out validated, out error,
          DateTimeOffset.UtcNow.AddMinutes(6)), Is.False);
      Assert.That(error, Does.StartWith("the token expired at"));

      // The same token a second before that is still good, so the test measured the expiry and not a typo.
      Assert.That(MediaPortalAccessToken.TryValidate(Key(), token, out validated, out error,
          DateTimeOffset.UtcNow.AddMinutes(4)), Is.True, error);
    }

    [Test]
    public void GarbageIsRefusedWithoutThrowing()
    {
      ClaimsIdentity validated;
      string error;
      foreach (string garbage in new[] { "", "  ", "no-dot-here", "a.b.c", "!!!!.!!!!", "." })
      {
        Assert.That(MediaPortalAccessToken.TryValidate(Key(), garbage, out validated, out error), Is.False,
            "'" + garbage + "' must not authenticate anybody");
        Assert.That(validated, Is.Null);
        Assert.That(error, Is.Not.Null.And.Not.Empty);
      }
    }

    [Test]
    public void TheErrorNeverRepeatsTheToken()
    {
      string token = MediaPortalAccessToken.Create(Key(21), Identity(), TimeSpan.FromMinutes(5));
      ClaimsIdentity validated;
      string error;
      MediaPortalAccessToken.TryValidate(Key(22), token, out validated, out error);

      Assert.That(error, Does.Not.Contain(token));
      Assert.That(error, Does.Not.Contain(PROFILE_NAME),
          "the reason is written to the server log, where a claim value would be somebody's data");
    }

    [Test]
    public void SigningWithoutAKeyIsRefusedRatherThanSilentlyWeakened()
    {
      Assert.That(() => MediaPortalAccessToken.Create(new byte[0], Identity(), TimeSpan.FromMinutes(5)),
          Throws.ArgumentException);

      ClaimsIdentity validated;
      string error;
      string token = MediaPortalAccessToken.Create(Key(), Identity(), TimeSpan.FromMinutes(5));
      Assert.That(MediaPortalAccessToken.TryValidate(null, token, out validated, out error), Is.False);
      Assert.That(error, Is.EqualTo("the server has no signing key"));
    }

    // ---------------------------------------------------------------------------------------------
    // The signing key
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// A settings store that keeps one <see cref="ServerSettings"/> in memory, the way the real one keeps one row
    /// per settings class: every load hands out a copy of what the last save left.
    /// </summary>
    static Mock<ISettingsManager> SettingsStore(string initialSigningKey)
    {
      string stored = initialSigningKey;
      Mock<ISettingsManager> settings = new Mock<ISettingsManager>();
      settings.Setup(s => s.Load<ServerSettings>())
          .Returns(() => new ServerSettings { WebAccessTokenSigningKey = stored });
      settings.Setup(s => s.Save(It.IsAny<object>()))
          .Callback<object>(o => stored = ((ServerSettings)o).WebAccessTokenSigningKey);
      return settings;
    }

    [Test]
    public void TheFirstTokenRequestMakesAndKeepsASigningKey()
    {
      Mock<ISettingsManager> settings = SettingsStore("");
      bool created;
      byte[] key = MediaPortalSigningKey.Resolve(settings.Object, out created);

      Assert.That(created, Is.True);
      Assert.That(key.Length, Is.EqualTo(MediaPortalSigningKey.KEY_BYTES));
      settings.Verify(s => s.Save(It.IsAny<object>()), Times.Once);

      // And the next lookup - a restart of the process - finds the stored one instead of making a new one, which is
      // what keeps tokens valid across a restart.
      byte[] again = MediaPortalSigningKey.Resolve(settings.Object, out created);
      Assert.That(again, Is.EqualTo(key));
      Assert.That(created, Is.False);
      settings.Verify(s => s.Save(It.IsAny<object>()), Times.Once);
    }

    [Test]
    public void TwoServersDoNotStartOutWithTheSameKey()
    {
      Assert.That(MediaPortalSigningKey.Create(), Is.Not.EqualTo(MediaPortalSigningKey.Create()));
    }

    [Test]
    public void AKeyThatCannotBeReadIsReplacedRatherThanGuessedAt()
    {
      // A truncated or hand-edited value must not become "hash the string and use that" - that would be a key
      // anybody can compute.
      Mock<ISettingsManager> settings = SettingsStore("not base64 at all!");
      bool created;
      byte[] key = MediaPortalSigningKey.Resolve(settings.Object, out created);

      Assert.That(created, Is.True);
      Assert.That(key.Length, Is.EqualTo(MediaPortalSigningKey.KEY_BYTES));
      Assert.That(Convert.ToBase64String(key), Is.Not.EqualTo("not base64 at all!"));
    }

    [Test]
    public void AStorableKeyHasToBeLongEnough()
    {
      byte[] key;
      Assert.That(MediaPortalSigningKey.TryDecode(null, out key), Is.False);
      Assert.That(MediaPortalSigningKey.TryDecode("", out key), Is.False);
      Assert.That(MediaPortalSigningKey.TryDecode(Convert.ToBase64String(new byte[16]), out key), Is.False,
          "a 16 byte key is half of HMAC-SHA256's recommended size; accepting a short stored value would let a weak key in through the back door");
      Assert.That(MediaPortalSigningKey.TryDecode(Convert.ToBase64String(new byte[32]), out key), Is.True);
      Assert.That(key.Length, Is.EqualTo(32));
    }

    [Test]
    public void WithoutASettingsStoreTheKeyIsEphemeralButStillUsable()
    {
      bool created;
      byte[] key = MediaPortalSigningKey.Resolve(null, out created);
      Assert.That(created, Is.True);

      string token = MediaPortalAccessToken.Create(key, Identity(), TimeSpan.FromMinutes(5));
      ClaimsIdentity validated;
      string error;
      Assert.That(MediaPortalAccessToken.TryValidate(key, token, out validated, out error), Is.True, error);
    }

    // ---------------------------------------------------------------------------------------------
    // The credential check
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task TheOwnerOfTheRightPasswordGetsAnIdentity()
    {
      UserProfile profile = Profile();
      MediaPortalCredentials.Result result =
          await MediaPortalCredentials.GrantResourceOwnerCredentialsAsync(PROFILE_NAME, Base64(PASSWORD),
              StoreFor(profile).Object).ConfigureAwait(false);

      Assert.That(result.Granted, Is.True, result.Description);
      Assert.That(ClaimValue(result.Identity, MediaPortalAccessToken.NAME_CLAIM), Is.EqualTo(PROFILE_NAME));
      Assert.That(ClaimValue(result.Identity, MediaPortalAccessToken.PROFILE_ID_CLAIM),
          Is.EqualTo(profile.ProfileId.ToString()));
    }

    [Test]
    public async Task APasswordIsComparedAgainstTheStoredHashNotAgainstItself()
    {
      MediaPortalCredentials.Result wrong =
          await MediaPortalCredentials.GrantResourceOwnerCredentialsAsync(PROFILE_NAME, Base64("wrong"),
              StoreFor(Profile()).Object).ConfigureAwait(false);

      Assert.That(wrong.Granted, Is.False);
      Assert.That(wrong.Error, Is.EqualTo(MediaPortalCredentials.ERROR_INVALID_GRANT));
      // The same answer as for an unknown profile, so that the endpoint cannot be used to find out which names
      // exist.
      Assert.That(wrong.Description, Is.EqualTo("The user name or password is incorrect."));
    }

    [Test]
    public async Task AnUnknownProfileIsRejectedWithTheStandardOAuthError()
    {
      Mock<IUserProfileDataManagement> store = new Mock<IUserProfileDataManagement>();
      store.Setup(m => m.GetProfileByNameAsync(It.IsAny<string>()))
          .ReturnsAsync(new MediaPortal.Common.Async.AsyncResult<UserProfile>(false, null));

      MediaPortalCredentials.Result result =
          await MediaPortalCredentials.GrantResourceOwnerCredentialsAsync("nobody", Base64(PASSWORD), store.Object)
              .ConfigureAwait(false);

      Assert.That(result.Granted, Is.False);
      Assert.That(result.Error, Is.EqualTo(MediaPortalCredentials.ERROR_INVALID_GRANT));
      store.Verify(m => m.LoginProfileAsync(It.IsAny<Guid>()), Times.Never,
          "a failed check must not log anybody in");
    }

    [Test]
    public async Task AProfileWithoutAPasswordLetsAnyoneInWhichIsWhatItMeans()
    {
      // The rule MP2 has always had: an empty stored password means the profile is not protected. Recorded here so
      // that a future change to it is a decision somebody takes on purpose.
      MediaPortalCredentials.Result result =
          await MediaPortalCredentials.GrantResourceOwnerCredentialsAsync(PROFILE_NAME, Base64("anything"),
              StoreFor(Profile(password: null)).Object).ConfigureAwait(false);

      Assert.That(result.Granted, Is.True, result.Description);
    }

    [Test]
    public async Task AProfileThatRestrictsSomethingIsAPlainUser()
    {
      UserProfile unrestricted = Profile();
      UserProfile restricted = Profile();
      restricted.EnableRestrictionGroups = true;

      MediaPortalCredentials.Result admin =
          await MediaPortalCredentials.GrantResourceOwnerCredentialsAsync(PROFILE_NAME, Base64(PASSWORD),
              StoreFor(unrestricted).Object).ConfigureAwait(false);
      MediaPortalCredentials.Result user =
          await MediaPortalCredentials.GrantResourceOwnerCredentialsAsync(PROFILE_NAME, Base64(PASSWORD),
              StoreFor(restricted).Object).ConfigureAwait(false);

      Assert.That(ClaimValue(admin.Identity, MediaPortalAccessToken.ROLE_CLAIM), Is.EqualTo("Admin"));
      Assert.That(ClaimValue(user.Identity, MediaPortalAccessToken.ROLE_CLAIM), Is.EqualTo("User"));

      UserProfile clientProfile = Profile(UserProfileType.ClientProfile);
      MediaPortalCredentials.Result asClient =
          await MediaPortalCredentials.GrantResourceOwnerCredentialsAsync(PROFILE_NAME, Base64(PASSWORD),
              StoreFor(clientProfile).Object).ConfigureAwait(false);
      Assert.That(ClaimValue(asClient.Identity, MediaPortalAccessToken.ROLE_CLAIM), Is.EqualTo("User"),
          "a client profile is not a person and must not be handed the administrator role");
    }

    [Test]
    public async Task TheProfileIsLoggedInSoThatItsSharesAndRestrictionsApply()
    {
      UserProfile profile = Profile();
      Mock<IUserProfileDataManagement> store = StoreFor(profile);

      await MediaPortalCredentials.GrantResourceOwnerCredentialsAsync(PROFILE_NAME, Base64(PASSWORD), store.Object)
          .ConfigureAwait(false);

      store.Verify(m => m.LoginProfileAsync(profile.ProfileId), Times.Once);
    }

    [Test]
    public void TheWirePasswordIsBase64AndSomethingThatIsNotStaysItself()
    {
      Assert.That(MediaPortalCredentials.DecodePassword(Base64(PASSWORD)), Is.EqualTo(PASSWORD));
      Assert.That(MediaPortalCredentials.DecodePassword(""), Is.Empty);
      Assert.That(MediaPortalCredentials.DecodePassword(null), Is.Empty);
      Assert.That(MediaPortalCredentials.DecodePassword("not base64 !"), Is.EqualTo("not base64 !"),
          "a password that is not valid base64 is passed through, otherwise such a password could never be used");
    }

    // ---------------------------------------------------------------------------------------------
    // The bearer handler
    // ---------------------------------------------------------------------------------------------

    class SingleOptions<TOptions> : IOptionsMonitor<TOptions> where TOptions : class, new()
    {
      public TOptions CurrentValue { get { return new TOptions(); } }
      public TOptions Get(string name) { return new TOptions(); }
      public IDisposable OnChange(Action<TOptions, string> listener) { return new Noop(); }
      class Noop : IDisposable { public void Dispose() { } }
    }

    static async Task<AuthenticateResult> AuthenticateAsync(IMediaPortalAccessTokenProtector protector,
        string authorizationHeader)
    {
      DefaultHttpContext context = new DefaultHttpContext();
      if (authorizationHeader != null)
        context.Request.Headers["Authorization"] = authorizationHeader;

      MediaPortalBearerHandler handler = new MediaPortalBearerHandler(
          new SingleOptions<AuthenticationSchemeOptions>(), NullLoggerFactory.Instance, UrlEncoder.Default, protector);
      await ((IAuthenticationHandler)handler).InitializeAsync(new AuthenticationScheme(
          MediaPortalBearerHandler.SCHEME, null, typeof(MediaPortalBearerHandler)), context).ConfigureAwait(false);
      return await handler.AuthenticateAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task ARequestWithoutAHeaderIsLeftAnonymous()
    {
      AuthenticateResult result = await AuthenticateAsync(
          new MediaPortalAccessTokenProtector(Key()), null).ConfigureAwait(false);
      Assert.That(result.Succeeded, Is.False);
      Assert.That(result.None, Is.True, "no header is not a failure; the endpoint decides whether anonymous is allowed");
    }

    [Test]
    public async Task ARequestForAnotherSchemeIsLeftToThatScheme()
    {
      AuthenticateResult result = await AuthenticateAsync(
          new MediaPortalAccessTokenProtector(Key()), "Basic Z2Vvcmc6c2VjcmV0").ConfigureAwait(false);
      Assert.That(result.None, Is.True);
    }

    [Test]
    public async Task ATokenTheServerSignedAuthenticatesTheCaller()
    {
      MediaPortalAccessTokenProtector protector = new MediaPortalAccessTokenProtector(Key());
      string token = protector.Create(Identity());

      AuthenticateResult result = await AuthenticateAsync(protector, "Bearer " + token).ConfigureAwait(false);

      Assert.That(result.Succeeded, Is.True, result.Failure != null ? result.Failure.Message : result.ToString());
      Assert.That(result.Ticket.AuthenticationScheme, Is.EqualTo(MediaPortalBearerHandler.SCHEME));
      Assert.That(result.Ticket.Principal.Identity.IsAuthenticated, Is.True);
      Assert.That(result.Ticket.Principal.Identity.Name, Is.EqualTo(PROFILE_NAME));
    }

    [Test]
    public async Task ATamperedTokenIsRefusedAndSaysSoWithoutThrowing()
    {
      MediaPortalAccessTokenProtector protector = new MediaPortalAccessTokenProtector(Key());
      string token = protector.Create(Identity());

      AuthenticateResult tampered = await AuthenticateAsync(protector, "Bearer " + token.Substring(4)).ConfigureAwait(false);
      Assert.That(tampered.Succeeded, Is.False);
      Assert.That(tampered.None, Is.False, "a token that was presented and is wrong is a failure, not an anonymous request");

      AuthenticateResult garbage = await AuthenticateAsync(protector, "Bearer not.a.token").ConfigureAwait(false);
      Assert.That(garbage.Succeeded, Is.False);
    }

    [Test]
    public void TheHandlerAndTheFilterAgreeOnTheNameOfTheScheme()
    {
      // The whole 403-for-everybody problem came from these two agreeing on nothing; the filter compares the
      // identity's authentication type against ResourceServer's constant, and the handler's ticket carries the
      // scheme name.
      Assert.That(MediaPortalBearerHandler.SCHEME, Is.EqualTo(ResourceServer.MEDIAPORTAL_AUTHENTICATION_TYPE));
    }
  }
}
