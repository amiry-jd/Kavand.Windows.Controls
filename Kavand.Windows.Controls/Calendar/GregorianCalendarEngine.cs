using System;
using System.Globalization;

namespace Kavand.Windows.Controls {

    // TODO: It's not needed to check GetDateFormat(culture) result with null. It'l never null

    public class GregorianCalendarEngine : CalendarEngine {

        private readonly System.Globalization.Calendar _calendar;

        public GregorianCalendarEngine() {
            _calendar = new GregorianCalendar();
        }

        #region Properties overrides

        public override System.Globalization.Calendar Calendar {
            get { return _calendar; }
        }

        public override DateTime MaxSupportedDateTime {
            get { return DateTime.MaxValue; }
        }

        public override DateTime MinSupportedDateTime {
            get { return DateTime.MinValue; }
        }

        #endregion

        #region GetString() implementations

        public override string GetDayString(DateTime dateTime, CultureInfo culture) {
            culture = culture ?? CultureInfo.CurrentCulture;
            var str = string.Empty;
            var dateFormat = GetDateFormat(culture);
            if (dateFormat != null)
                str = dateTime.Day.ToString(dateFormat);
            return str;
        }

        public override string GetDayString(DateTime? dateTime, CultureInfo culture) {
            return dateTime.HasValue ? GetDayString(dateTime.Value, culture) : string.Empty;
        }

        public override string GetYearString(DateTime dateTime, CultureInfo culture) {
            culture = culture ?? CultureInfo.CurrentCulture;
            var str = string.Empty;
            var dateFormat = GetDateFormat(culture);
            if (dateFormat != null)
                str = dateTime.Year.ToString(dateFormat);
            return str;
        }

        public override string GetYearString(DateTime? dateTime, CultureInfo culture) {
            return dateTime.HasValue ? GetYearString(dateTime.Value, culture) : string.Empty;
        }

        public override string GetYearMonthPatternString(DateTime? dateTime, CultureInfo culture) {
            var str = string.Empty;
            culture = culture ?? CultureInfo.CurrentCulture;
            var dateFormat = GetDateFormat(culture);
            if (dateTime.HasValue && dateFormat != null)
                str = dateTime.Value.ToString(dateFormat.YearMonthPattern, dateFormat);
            return str;
        }

        public override string GetDecadeRangeString(DateTime decadeStart, bool isRightToLeft, CultureInfo culture) {
            culture = culture ?? CultureInfo.CurrentCulture;
            var str = string.Empty;
            var decade = decadeStart.Year;
            var dateFormat = GetDateFormat(culture);
            if (dateFormat != null) {
                var num = isRightToLeft ? decade : decade + 9;
                str = (isRightToLeft ? decade + 9 : decade).ToString(dateFormat) + "-" + num.ToString(dateFormat);
            }
            return str;
        }

        public override string GetDateString(DateTime? date, CultureInfo culture) {
            culture = culture ?? CultureInfo.CurrentCulture;
            var str = string.Empty;
            var dateFormat = GetDateFormat(culture);
            if (date.HasValue && dateFormat != null)
                str = date.Value.Date.ToString(dateFormat);
            return str;
        }

        public override string GetLongDateString(DateTime? date, CultureInfo culture) {
            culture = culture ?? CultureInfo.CurrentCulture;
            var str = string.Empty;
            var dateFormat = GetDateFormat(culture);
            if (date.HasValue && dateFormat != null)
                str = date.Value.Date.ToString(dateFormat.LongDatePattern, dateFormat);
            return str;
        }

        #endregion

        #region GetName[s]() implementations

        public override string[] GetShortestDayNames(CultureInfo culture) {
            culture = culture ?? CultureInfo.CurrentCulture;
            var dateFormat = GetDateFormat(culture);
            return dateFormat != null ? dateFormat.ShortestDayNames : new string[0];
        }

        public override string[] GetAbbreviatedMonthNames(CultureInfo culture) {
            culture = culture ?? CultureInfo.CurrentCulture;
            var dateFormat = GetDateFormat(culture);
            return dateFormat != null ? dateFormat.AbbreviatedMonthNames : new string[0];
        }

        #endregion

        #region Calculation implementations

        public override DayOfWeek GetFirstDayOfWeek(CultureInfo culture) {
            culture = culture ?? CultureInfo.CurrentCulture;
            return GetDateFormat(culture).FirstDayOfWeek;
        }

        public override DayOfWeek GetHolidayOfWeek(CultureInfo culture) {
            return DayOfWeek.Sunday;
        }

        public override DateTime GetFirstOfMonth(DateTime dateTime) {
            return new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0);
        }

        public override int GetNumberOfDisplayedDaysFromPreviousMonth(
            DateTime firstOfMonth, DayOfWeek? firstDayOfWeek, CultureInfo culture) {
            var dayOfWeek = Calendar.GetDayOfWeek(firstOfMonth);
            culture = culture ?? CultureInfo.CurrentCulture;
            firstDayOfWeek = firstDayOfWeek.HasValue
                                 ? firstDayOfWeek
                                 : GetDateFormat(culture).FirstDayOfWeek;
            var num = (dayOfWeek - firstDayOfWeek.Value + 7) % 7;
            return num == 0 ? 7 : num;
        }

        public override DateTime GetMonth(DateTime dateTime, int nthMonth) {
            return new DateTime(dateTime.Year, nthMonth, 1);
        }

        public override int GetDecadeStart(DateTime dateTime) {
            return dateTime.Year - dateTime.Year % 10;
        }

        public override int GetDecadeEnd(DateTime dateTime) {
            return GetDecadeStart(dateTime) + 9;
        }

        public override DateTime GetFirstOfYear(DateTime dateTime) {
            return new DateTime(dateTime.Year, 1, 1);
        }

        public override DateTime CurrectDecadeContext(DateTime dateTime) {
            return dateTime;
        }

        public override bool IsOutOfDecade(DateTime decadeStart, DateTime decadeEnd, DateTime decade) {
            return decade < decadeStart || decade > decadeEnd;
        }

        #endregion

        public override DateTimeFormatInfo GetDateFormat(CultureInfo culture) {
            return GetDateFormatInternal(culture);
        }

        internal static DateTimeFormatInfo GetDateFormatInternal(CultureInfo culture) {
            if (culture.Calendar is GregorianCalendar)
                return culture.DateTimeFormat;

            GregorianCalendar foundCal = null;
            DateTimeFormatInfo dtfi;

            foreach (var cal in culture.OptionalCalendars) {
                if (!(cal is GregorianCalendar))
                    continue;
                // Return the first Gregorian calendar with CalendarType == Localized 
                // Otherwise return the first Gregorian calendar
                if (foundCal == null) {
                    foundCal = cal as GregorianCalendar;
                }

                if (((GregorianCalendar)cal).CalendarType == GregorianCalendarTypes.Localized) {
                    foundCal = cal as GregorianCalendar;
                    break;
                }
            }


            if (foundCal == null) {
                // if there are no GregorianCalendars in the OptionalCalendars list, use the invariant dtfi 
                dtfi = ((CultureInfo)CultureInfo.InvariantCulture.Clone()).DateTimeFormat;
                dtfi.Calendar = new GregorianCalendar();
            } else {
                dtfi = ((CultureInfo)culture.Clone()).DateTimeFormat;
                dtfi.Calendar = foundCal;
            }

            return dtfi;
        }
    }
}