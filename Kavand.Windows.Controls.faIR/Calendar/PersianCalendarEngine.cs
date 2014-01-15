using System;
using System.Globalization;
using System.Linq;

namespace Kavand.Windows.Controls.faIR {

    public class PersianCalendarEngine : CalendarEngine {

        private readonly System.Globalization.Calendar _calendar;
        private readonly CultureInfo _cultureInfo;

        public PersianCalendarEngine() {
            _cultureInfo = new PersianCultureInfo();
            // _calendar = new PersianCalendar();
            // for now:
            _calendar = _cultureInfo.Calendar;
        }

        // for now, we just support our own culture, etc.
        public override CultureInfo GetCulture(System.Windows.FrameworkElement element) {
            return _cultureInfo;
        }

        #region Properties overrides

        public override System.Globalization.Calendar Calendar {
            get { return _calendar; }
        }

        public override DateTime MaxSupportedDateTime {
            get { return Calendar.MaxSupportedDateTime; }
        }

        public override DateTime MinSupportedDateTime {
            get { return Calendar.MinSupportedDateTime; }
        }

        #endregion

        #region virtual overrides

        public override DateTime? AddYears(DateTime time, int years) {
            var day = Calendar.GetDayOfMonth(time);
            var month = Calendar.GetMonth(time);
            var year = Calendar.GetYear(time) + years;
            return Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
        }

        public override DateTime? SetYear(DateTime sourceDate, DateTime requestedYearDate) {
            var sourceYear = Calendar.GetYear(sourceDate);
            var requestedYear = Calendar.GetYear(requestedYearDate);
            var yearsToAdd = requestedYear - sourceYear;
            return AddYears(sourceDate, yearsToAdd);
        }

        public override DateTime? SetYearMonth(DateTime sourceDate, DateTime requestedYearMonthDate) {
            var result = SetYear(sourceDate, requestedYearMonthDate);
            if (result.HasValue) {
                var sourceMonth = Calendar.GetMonth(sourceDate);
                var requestedMonth = Calendar.GetMonth(requestedYearMonthDate);
                var monthToAdd = requestedMonth - sourceMonth;
                result = AddMonths(result.Value, monthToAdd);
            }
            return result;
        }

        public override int CompareYearMonth(DateTime dt1, DateTime dt2) {
            var year1 = Calendar.GetYear(dt1);
            var year2 = Calendar.GetYear(dt2);
            var month1 = Calendar.GetMonth(dt1);
            var month2 = Calendar.GetMonth(dt2);
            return (year1 - year2) * 12 + (month1 - month2);
        }

        public override int CompareYears(DateTime dt1, DateTime dt2) {
            var year1 = Calendar.GetYear(dt1);
            var year2 = Calendar.GetYear(dt2);
            return year1 - year2;
        }

        #endregion

        #region GetString() implementations

        public override string GetDayString(DateTime dateTime, CultureInfo culture) {
            var day = Calendar.GetDayOfMonth(dateTime);
            var dateFormat = GetDateFormat(culture);
            var str = day.ToString(_cultureInfo);
            return str;
        }

        public override string GetDayString(DateTime? dateTime, CultureInfo culture) {
            return dateTime.HasValue ? GetDayString(dateTime.Value, culture) : string.Empty;
        }

        public override string GetYearString(DateTime dateTime, CultureInfo culture) {
            var year = Calendar.GetYear(dateTime);
            var dateFormat = GetDateFormat(culture);
            var str = year.ToString(dateFormat);
            return str;
        }

        public override string GetYearString(DateTime? dateTime, CultureInfo culture) {
            return dateTime.HasValue ? GetYearString(dateTime.Value, culture) : string.Empty;
        }

        public override string GetYearMonthPatternString(DateTime? dateTime, CultureInfo culture) {
            if (!dateTime.HasValue)
                return string.Empty;
            var dateFormat = GetDateFormat(culture);
            var str = dateTime.Value.ToString(dateFormat.YearMonthPattern, dateFormat);
            return str;
        }

        public override string GetDecadeRangeString(DateTime decadeStart, bool isRightToLeft, CultureInfo culture) {
            var decade = Calendar.GetYear(decadeStart);
            var dateFormat = GetDateFormat(culture);
            var num = isRightToLeft ? decade : decade + 9;
            var str = (isRightToLeft ? decade + 9 : decade).ToString(dateFormat) + " - " + num.ToString(dateFormat);
            return str;
        }

        public override string GetDateString(DateTime? date, CultureInfo culture) {
            var str = string.Empty;
            var dateFormat = GetDateFormat(culture);
            if (date.HasValue)
                str = date.Value.Date.ToString(dateFormat);
            return str;
        }

        public override string GetLongDateString(DateTime? date, CultureInfo culture) {
            var str = string.Empty;
            var dateFormat = GetDateFormat(culture);
            if (date.HasValue)
                str = date.Value.Date.ToString(dateFormat.LongDatePattern, dateFormat);
            return str;
        }

        #endregion

        #region GetName[s]() implementations

        public override string[] GetShortestDayNames(CultureInfo culture) {
            var dateFormat = GetDateFormat(culture);
            return dateFormat.ShortestDayNames;
        }

        public override string[] GetAbbreviatedMonthNames(CultureInfo culture) {
            var dateFormat = GetDateFormat(culture);
            return dateFormat.AbbreviatedMonthNames;
        }

        #endregion

        #region Calculation implementations

        public override DayOfWeek GetFirstDayOfWeek(CultureInfo culture) {
            var dateFormat = GetDateFormat(culture);
            return dateFormat.FirstDayOfWeek;
        }

        public override DayOfWeek GetHolidayOfWeek(CultureInfo culture) {
            return DayOfWeek.Friday;
        }

        public override DateTime GetFirstOfMonth(DateTime dateTime) {
            var year = Calendar.GetYear(dateTime);
            var month = Calendar.GetMonth(dateTime);
            dateTime = new DateTime(year, month, 1, Calendar);
            return dateTime;
        }

        public override int GetNumberOfDisplayedDaysFromPreviousMonth(
            DateTime firstOfMonth, DayOfWeek? firstDayOfWeek, CultureInfo culture) {
            var dayOfWeek = Calendar.GetDayOfWeek(firstOfMonth);
            firstDayOfWeek = firstDayOfWeek.HasValue
                                 ? firstDayOfWeek
                                 : GetFirstDayOfWeek(culture);
            var num = (dayOfWeek - firstDayOfWeek.Value + 7) % 7;
            return num == 0 ? 7 : num;
        }

        public override DateTime GetMonth(DateTime dateTime, int nthMonth) {
            var year = Calendar.GetYear(dateTime);
            return new DateTime(year, nthMonth, 1, Calendar);
        }

        public override int GetDecadeStart(DateTime dateTime) {
            var year = Calendar.GetYear(dateTime);
            var month = Calendar.GetMonth(dateTime);
            var day = Calendar.GetDayOfMonth(dateTime);
            year = year - year % 10;
            return new DateTime(year, month, day, Calendar).Year;
        }

        public override int GetDecadeEnd(DateTime dateTime) {
            return GetDecadeStart(dateTime) + 9;
        }

        public override DateTime GetFirstOfYear(DateTime dateTime) {
            var year = Calendar.GetYear(dateTime);
            return new DateTime(year, 1, 1, Calendar).Date;
        }

        public override DateTime CurrectDecadeContext(DateTime dateTime) {
            var day = Calendar.GetDayOfMonth(dateTime);
            var month = Calendar.GetMonth(dateTime);
            if (month == 12 && (day == 29 || day == 30))
                return Calendar.AddDays(dateTime, 1);
            return dateTime;
        }

        public override bool IsOutOfDecade(DateTime decadeStart, DateTime decadeEnd, DateTime decade) {
            var startYear = Calendar.GetYear(decadeStart);
            var endYear = Calendar.GetYear(decadeEnd);
            var year = Calendar.GetYear(decade);
            return year < startYear || year > endYear;
        }

        #endregion

        public override DateTimeFormatInfo GetDateFormat(CultureInfo culture) {
            //return GetDateFormatInternal(culture);
            // For now, we just return:
            return _cultureInfo.DateTimeFormat;
        }

        internal static DateTimeFormatInfo GetDateFormatInternal(CultureInfo culture) {
            if (culture is PersianCultureInfo || culture.Calendar is PersianCalendar)
                return culture.DateTimeFormat;
            var foundCal = culture.OptionalCalendars.OfType<PersianCalendar>().FirstOrDefault();
            if (foundCal != null) {
                var dtfi = ((CultureInfo)culture.Clone()).DateTimeFormat;
                if (!dtfi.IsReadOnly)
                    dtfi.Calendar = foundCal;
                return dtfi;
            }
            return GetDefaultDateFormat(culture);
        }

        internal static DateTimeFormatInfo GetDefaultDateFormat(CultureInfo culture) {
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