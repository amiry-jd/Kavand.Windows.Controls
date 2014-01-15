using System;
using System.Globalization;
using System.Windows;

namespace Kavand.Windows.Controls {

    public abstract class CalendarEngine {

        #region virtual implementations

        public virtual int GetDaysInMonth(DateTime dateTime) {
            return Calendar.GetDaysInMonth(dateTime.Year, dateTime.Month);
        }

        public virtual DateTime? AddDays(DateTime dateTime, int days) {
            try {
                return Calendar.AddDays(dateTime, days);
            } catch (ArgumentException) {
                return new DateTime?();
            }
        }

        public virtual DateTime? AddMonths(DateTime dateTime, int months) {
            try {
                return Calendar.AddMonths(dateTime, months);
            } catch (ArgumentException) {
                return new DateTime?();
            }
        }

        public virtual DateTime? AddYears(DateTime time, int years) {
            try {
                return Calendar.AddYears(time, years);
            } catch (ArgumentException) {
                return new DateTime?();
            }
        }

        public virtual DateTime? SetYear(DateTime sourceDate, DateTime requestedYearDate) {
            return AddYears(sourceDate, requestedYearDate.Year - sourceDate.Year);
        }

        public virtual DateTime? SetYearMonth(DateTime sourceDate, DateTime requestedYearMonthDate) {
            var nullable = SetYear(sourceDate, requestedYearMonthDate);
            if (nullable.HasValue)
                nullable = AddMonths(nullable.Value, requestedYearMonthDate.Month - sourceDate.Month);
            return nullable;
        }

        public virtual int CompareDays(DateTime dt1, DateTime dt2) {
            return DateTime.Compare(dt1.DiscardTime(), dt2.DiscardTime());
        }

        public virtual int CompareYearMonth(DateTime dt1, DateTime dt2) {
            return (dt1.Year - dt2.Year) * 12 + (dt1.Month - dt2.Month);
        }

        public virtual int CompareYears(DateTime dt1, DateTime dt2) {
            return dt1.Year - dt2.Year;
        }

        public virtual bool InRange(DateTime date, DateRange range) {
            return InRange(date, range.Start, range.End);
        }

        public virtual bool InRange(DateTime date, DateTime start, DateTime end) {
            return CompareDays(date, start) > -1 && CompareDays(date, end) < 1;
        }

        public virtual CultureInfo GetCulture(FrameworkElement element) {
            var culture = DependencyPropertyHelper.GetValueSource(element, FrameworkElement.LanguageProperty).BaseValueSource
                                  != BaseValueSource.Default ? element.GetCultureInfo() : CultureInfo.CurrentCulture;
            return culture;
        }

        public virtual string[] GetDayNames(DayNameMode mode, CultureInfo culture) {
            var format = GetDateFormat(culture);
            switch (mode) {
                case DayNameMode.Shortest:
                    return format.ShortestDayNames;
                case DayNameMode.Abbreviated:
                    return format.AbbreviatedDayNames;
                case DayNameMode.Normal:
                    return format.DayNames;
                default:
                    throw new ArgumentOutOfRangeException("mode");
            }
        }

        public virtual string[] GetMonthNames(MonthNameMode mode, CultureInfo culture) {
            var format = GetDateFormat(culture);
            switch (mode) {
                case MonthNameMode.Abbreviated:
                    return format.AbbreviatedMonthNames;
                case MonthNameMode.Normal:
                    return format.MonthNames;
                case MonthNameMode.AbbreviatedGenitive:
                    return format.AbbreviatedMonthGenitiveNames;
                case MonthNameMode.NormalGenitive:
                    return format.MonthGenitiveNames;
                default:
                    throw new ArgumentOutOfRangeException("mode");
            }
        }

        #endregion

        #region Properties abstracts
        public abstract System.Globalization.Calendar Calendar { get; }
        public abstract DateTime MaxSupportedDateTime { get; }
        public abstract DateTime MinSupportedDateTime { get; }
        #endregion

        #region GetString() abstracts
        public abstract string GetDayString(DateTime dateTime, CultureInfo culture);
        public abstract string GetDayString(DateTime? dateTime, CultureInfo culture);
        public abstract string GetYearString(DateTime dateTime, CultureInfo culture);
        public abstract string GetYearString(DateTime? dateTime, CultureInfo culture);
        public abstract string GetYearMonthPatternString(DateTime? dateTime, CultureInfo culture);
        public abstract string GetDecadeRangeString(DateTime decadeStart, bool isRightToLeft, CultureInfo culture);
        public abstract string GetDateString(DateTime? date, CultureInfo culture);
        public abstract string GetLongDateString(DateTime? date, CultureInfo culture);
        #endregion

        #region GetName[s]() abstracts
        public abstract string[] GetShortestDayNames(CultureInfo culture);
        public abstract string[] GetAbbreviatedMonthNames(CultureInfo culture);
        #endregion

        #region Calculation abstracts
        public abstract DayOfWeek GetFirstDayOfWeek(CultureInfo culture);
        public abstract DayOfWeek GetHolidayOfWeek(CultureInfo culture);
        public abstract DateTime GetFirstOfMonth(DateTime dateTime);
        public abstract int GetNumberOfDisplayedDaysFromPreviousMonth(DateTime firstOfMonth, DayOfWeek? firstDayOfWeek, CultureInfo culture);
        public abstract DateTime GetMonth(DateTime dateTime, int nthMonth);
        public abstract int GetDecadeStart(DateTime dateTime);
        public abstract int GetDecadeEnd(DateTime dateTime);
        public abstract DateTime GetFirstOfYear(DateTime dateTime);
        public abstract DateTime CurrectDecadeContext(DateTime dateTime);
        public abstract bool IsOutOfDecade(DateTime decadeStart, DateTime decadeEnd, DateTime decade);
        #endregion

        public abstract DateTimeFormatInfo GetDateFormat(CultureInfo culture);

        public static DayOfWeek GetCurrentCultureFirstDayOfWeek() {
            var culture = CultureInfo.CurrentCulture;
            var dtfi = culture.DateTimeFormat;
            return dtfi.FirstDayOfWeek;
        }

        public static DayOfWeek GetCurrentCultureHolidayOfWeek() {
            return DayOfWeek.Sunday;
        }

        public static string GetCurrentCultureDateStringFormat() {
            var culture = CultureInfo.CurrentCulture;
            var dtfi = culture.DateTimeFormat;
            return dtfi.LongDatePattern;
        }

        #region overrides
        public override string ToString() {
            return GetType().Name;
        }
        #endregion
    }
}