using System;
using System.Globalization;
using System.Reflection;

#pragma warning disable 169
// ReSharper disable InconsistentNaming
namespace Kavand.Windows.Controls.faIR {

    public sealed class PersianDateTimeFormatInfo {

        #region day parts

        public const string AMDesignator = "صبح";

        public const string PMDesignator = "عصر";

        public const DayOfWeek FirstDayOfWeek = DayOfWeek.Saturday;

        private static readonly string[] _dayNames = new[] { "یکشنبه", "دوشنبه", "سه‌شنبه", "چهار‌شنبه", "پنجشنبه", "جمعه", "شنبه" };

        public static string[] DayNames { get { return _dayNames.Clone() as string[]; } }

        private static readonly string[] _abbreviatedDayNames = new[] { "یکـ", "دوشـ", "سهـ", "چهـ", "پنـ", "جمـ", "شنـ" };
        public static string[] AbbreviatedDayNames { get { return _abbreviatedDayNames.Clone() as string[]; } }

        private static readonly string[] _shortestDayNames = new[] { "ی", "د", "س", "چ", "پ", "ج", "ش" };
        public static string[] ShortestDayNames { get { return _shortestDayNames.Clone() as string[]; } }

        #endregion

        #region month parts

        private static readonly string[] _monthNames = new[] {
            "فروردین", "اردیبهشت", "خرداد","تیر", "مرداد", "شهریور","مهر", "آبان", "آذر","دی", "بهمن", "اسفند", ""
        };

        public static string[] MonthGenitiveNames { get { return _monthNames.Clone() as string[]; } }

        public static string[] MonthNames { get { return _monthNames.Clone() as string[]; } }

        private static readonly string[] _abbreviatedMonthNames = new[] {
            "فرو", "ارد", "خرد", "تیر", "مرد", "شهر", "مهر", "آبا", "آذر", "دی", "بهم", "اسف", ""
        };

        public static string[] AbbreviatedMonthGenitiveNames { get { return _abbreviatedMonthNames.Clone() as string[]; } }

        public static string[] AbbreviatedMonthNames { get { return _abbreviatedMonthNames.Clone() as string[]; } }

        #endregion

        #region patterns

        public const string DateSeparator = "/";
        public const string TimeSeparator = ":";

        private const string FullDateTimePattern_2 = "dddd dd MMMM yyyy";
        public const string FullDateTimePattern = "tt hh:mm:ss yyyy mmmm dd dddd";

        private const string LongDatePattern_2 = "dd MMMM yyyy";
        public const string LongDatePattern = "yyyy MMMM dd, dddd";

        private const string ShortDatePattern_2 = "dd/MM/yy";
        public const string ShortDatePattern = "yyyy/MM/dd";

        public const string YearMonthPattern = "MMMM yyyy";
        private const string YearMonthPattern_2 = "yyyy, MMMM";

        public const string MonthDayPattern = "dd MMMM";

        public const string LongTimePattern = "hh:mm:ss TT";
        private const string LongTimePattern_2 = "hh:mm:ss tt";

        private const string ShortTimePattern_2 = "HH:mm";
        public const string ShortTimePattern = "hh:mm tt";

        #endregion

        public static   CalendarWeekRule CalendarWeekRule {get { return CalendarWeekRule.FirstDay; }}


        public const string CalendarFieldName = "calendar";
        public const string IsReadonlyFieldName = "m_isReadOnly";

        public static DateTimeFormatInfo GetFormatInfo(System.Globalization.Calendar calendar) {
            var format = new DateTimeFormatInfo {
                AbbreviatedDayNames = AbbreviatedDayNames,
                AbbreviatedMonthGenitiveNames = AbbreviatedMonthGenitiveNames,
                AbbreviatedMonthNames = AbbreviatedMonthNames,
                AMDesignator = AMDesignator,
                DateSeparator = DateSeparator,
                DayNames = DayNames,
                FirstDayOfWeek = FirstDayOfWeek,
                FullDateTimePattern = FullDateTimePattern,
                LongDatePattern = LongDatePattern,
                LongTimePattern = LongTimePattern,
                MonthDayPattern = MonthDayPattern,
                MonthGenitiveNames = MonthGenitiveNames,
                MonthNames = MonthNames,
                PMDesignator = PMDesignator,
                ShortDatePattern = ShortDatePattern,
                ShortestDayNames = ShortestDayNames,
                ShortTimePattern = ShortTimePattern,
                TimeSeparator = TimeSeparator,
                YearMonthPattern = YearMonthPattern,
                CalendarWeekRule = CalendarWeekRule
            };
            //Make format information readonly to fix cloning problems that might happen with other controls.
            SetFieldValue(format, IsReadonlyFieldName, true);
            SetFieldValue(format, CalendarFieldName, calendar);
            return format;
        }

        private static void SetFieldValue(object owner, string fieldName, object value) {
            if (owner == null)
                throw new ArgumentNullException("owner");
            var type = owner.GetType();
            var field = GetField(type, fieldName);
            if (field == null)
                throw new ArgumentException();
            field.SetValue(owner, value);
        }

        private static FieldInfo GetField(IReflect type, string fieldName) {
            return type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        }

    }
}
// ReSharper restore InconsistentNaming
#pragma warning restore 169