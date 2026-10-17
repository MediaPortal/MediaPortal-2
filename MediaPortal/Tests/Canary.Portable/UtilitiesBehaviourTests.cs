using System;
using System.Globalization;
using System.Threading;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Conversion;
using NUnit.Framework;

namespace Canary.Portable
{
  /// <summary>
  /// Behaviour baseline for the MediaPortal.Utilities sources compiled into this project.
  ///
  /// These tests exist before any porting change, so that retargeting the real assembly is
  /// provably equivalent. They deliberately cover the boring corners (null, empty, negative,
  /// zero) because those are what a retarget or a BCL behaviour change tends to move.
  /// </summary>
  [TestFixture]
  public class ConversionTests
  {
    [SetUp]
    public void PinCulture()
    {
      _saved = CultureInfo.CurrentCulture;
      CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    [TearDown]
    public void RestoreCulture()
    {
      CultureInfo.CurrentCulture = _saved;
    }

    private CultureInfo _saved;

    [Test]
    public void SecondsToShortHMSString_Negative_ReturnsZero()
    {
      Assert.That(Conversion.SecondsToShortHMSString(-5), Is.EqualTo("0:00"));
    }

    [Test]
    public void SecondsToShortHMSString_DropsSeconds()
    {
      Assert.That(Conversion.SecondsToShortHMSString(3725), Is.EqualTo("1:02"));
    }

    [Test]
    public void SecondsToHMSString_WithHours_PadsMinutesAndSeconds()
    {
      Assert.That(Conversion.SecondsToHMSString(3725), Is.EqualTo("1:02:05"));
    }

    [Test]
    public void SecondsToHMSString_WithoutHours_OmitsHours()
    {
      Assert.That(Conversion.SecondsToHMSString(65), Is.EqualTo("1:05"));
    }

    [Test]
    public void SecondsToHMString_Formats()
    {
      Assert.That(Conversion.SecondsToHMString(3720), Is.EqualTo("01:02"));
      Assert.That(Conversion.SecondsToHMString(120), Is.EqualTo("0:02"));
    }

    /// <summary>
    /// Documents an existing defect; do not "fix" it by editing this test. The TimeSpan
    /// overload reads TimeSpan.Seconds - the 0..59 component - instead of the total duration,
    /// so 65 minutes formats as "0:00" and 90 minutes 30 seconds formats as "0:30". Every bit
    /// of information above a minute is silently discarded for callers that pass a TimeSpan.
    /// Pinned here so that the fix is a deliberate, separate commit instead of something a
    /// retarget changes by accident.
    /// </summary>
    [Test]
    public void SecondsToHMSString_TimeSpanOverload_DiscardsEverythingAboveOneMinute()
    {
      Assert.That(Conversion.SecondsToHMSString(TimeSpan.FromMinutes(65)), Is.EqualTo("0:00"));
      Assert.That(Conversion.SecondsToHMSString(TimeSpan.FromMinutes(90).Add(TimeSpan.FromSeconds(30))),
          Is.EqualTo("0:30"));
    }

    [Test]
    public void GetUnixTime_EpochIsZero()
    {
      Assert.That(Conversion.GetUnixTime(new DateTime(1970, 1, 1)), Is.EqualTo(0L));
      Assert.That(Conversion.GetUnixTime(new DateTime(1970, 1, 2)), Is.EqualTo(86400L));
    }

    [Test]
    public void DateToLongAndBack_RoundTrips()
    {
      DateTime dt = new DateTime(2020, 3, 4, 5, 6, 7);
      Assert.That(Conversion.DateToLong(dt), Is.EqualTo(20200304050607L));
      Assert.That(Conversion.LongToDate(20200304050607L), Is.EqualTo(dt));
    }

    [Test]
    public void LongToDate_InvalidInput_ReturnsMinValue()
    {
      Assert.That(Conversion.LongToDate(-1L), Is.EqualTo(DateTime.MinValue));
      // month 13 makes the DateTime ctor throw, which is swallowed
      Assert.That(Conversion.LongToDate(20201304000000L), Is.EqualTo(DateTime.MinValue));
    }
  }

  [TestFixture]
  public class StringUtilsTests
  {
    [Test]
    public void Escape_EscapesSpecialCharactersAndTheEscapeCharacterItself()
    {
      Assert.That(StringUtils.Escape("a+b", new[] { '+' }, '\\'), Is.EqualTo("a\\+b"));
      Assert.That(StringUtils.Escape("a\\b", new[] { '+' }, '\\'), Is.EqualTo("a\\\\b"));
    }

    [Test]
    public void Join_UsesSeparatorAndToleratesNullOrEmpty()
    {
      Assert.That(StringUtils.Join(", ", new[] { 1, 2, 3 }), Is.EqualTo("1, 2, 3"));
      Assert.That(StringUtils.Join(", ", new int[0]), Is.EqualTo(string.Empty));
      Assert.That(StringUtils.Join(", ", null), Is.EqualTo(string.Empty));
    }

    /// <summary>
    /// The name says Trim but the method only maps empty to null; " " survives. Pinned because
    /// callers rely on it and a "helpful" fix would change library grouping behaviour.
    /// </summary>
    [Test]
    public void TrimToNull_DoesNotTrimWhitespace()
    {
      Assert.That(StringUtils.TrimToNull(string.Empty), Is.Null);
      Assert.That(StringUtils.TrimToNull(null), Is.Null);
      Assert.That(StringUtils.TrimToNull(" "), Is.EqualTo(" "));
    }

    [Test]
    public void TrimToEmpty_MapsNullOrEmptyToEmpty()
    {
      Assert.That(StringUtils.TrimToEmpty(null), Is.EqualTo(string.Empty));
      Assert.That(StringUtils.TrimToEmpty("x"), Is.EqualTo("x"));
    }

    [Test]
    public void PrefixAndSuffixHelpers()
    {
      Assert.That(StringUtils.RemoveSuffixIfPresent("file.mp4", ".mp4"), Is.EqualTo("file"));
      Assert.That(StringUtils.RemoveSuffixIfPresent("file.mkv", ".mp4"), Is.EqualTo("file.mkv"));
      Assert.That(StringUtils.RemoveSuffixIfPresent(null, ".mp4"), Is.Null);
      Assert.That(StringUtils.CheckSuffix("a", "b"), Is.EqualTo("ab"));
      Assert.That(StringUtils.CheckSuffix("ab", "b"), Is.EqualTo("ab"));
      Assert.That(StringUtils.CheckSuffix(null, "b"), Is.Null);
      Assert.That(StringUtils.CheckPrefix("b", "a"), Is.EqualTo("ab"));
      Assert.That(StringUtils.RemovePrefixIfPresent("ab", "a"), Is.EqualTo("b"));
    }

    [Test]
    public void Repeat_RepeatsExactly()
    {
      Assert.That(StringUtils.Repeat("ab", 3), Is.EqualTo("ababab"));
      Assert.That(StringUtils.Repeat("x", 0), Is.EqualTo(string.Empty));
    }
  }

  [TestFixture]
  public class OrdinalStringComparerTests
  {
    [Test]
    public void NullsSortBeforeEverything()
    {
      OrdinalStringComparer comparer = new OrdinalStringComparer();
      Assert.That(comparer.Compare(null, null), Is.EqualTo(0));
      Assert.That(comparer.Compare(null, "a"), Is.LessThan(0));
      Assert.That(comparer.Compare("a", null), Is.GreaterThan(0));
    }

    [Test]
    public void NumbersSortByValueNotByText()
    {
      OrdinalStringComparer comparer = new OrdinalStringComparer();
      Assert.That(comparer.Compare("File 10", "File 1"), Is.GreaterThan(0));
      Assert.That(comparer.Compare("File 2", "File 10"), Is.LessThan(0));
    }

    [Test]
    public void CaseIgnoredByDefault()
    {
      Assert.That(new OrdinalStringComparer().Compare("file", "FILE"), Is.EqualTo(0));
    }

    /// <summary>
    /// Documents the emulated Windows Explorer quirk described on the class.
    /// </summary>
    [Test]
    public void DigitBearingNameSortsBeforeNameWithoutDigits()
    {
      OrdinalStringComparer comparer = new OrdinalStringComparer();
      Assert.That(comparer.Compare("list 2.txt", "list.txt"), Is.LessThan(0));
    }

    /// <summary>
      /// Portability hazard, not a preference: Compare() uses StringComparison.CurrentCulture, so
    /// ordering of non-ASCII names follows the ambient ICU collation. On Linux/Android that is
    /// the ICU default, on Windows it is NLS, and neither follows the other. Any UI list whose
    /// order is visible to the user therefore changes platform unless the culture is pinned or
    /// the comparison is made ordinal on purpose.
    /// </summary>
    [Test]
    public void OrderingDependsOnAmbientCulture()
    {
      CultureInfo saved = CultureInfo.CurrentCulture;
      try
      {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        int invariantResult = new OrdinalStringComparer().Compare("Ärger", "Ärger2");
        Assert.That(invariantResult, Is.LessThan(0));
        // Same input under a culture with different tailoring must at least be well defined.
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        int germanResult = new OrdinalStringComparer().Compare("Ärger", "Ärger2");
        Assert.That(germanResult, Is.LessThan(0));
      }
      finally
      {
        CultureInfo.CurrentCulture = saved;
      }
    }
  }
}