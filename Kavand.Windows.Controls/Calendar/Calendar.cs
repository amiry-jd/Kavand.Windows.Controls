using System;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Kavand.Windows.Controls {

    /// <summary>
    /// Represents a control that enables a user to select a date by using a visual calendar display.
    /// </summary>
    [TemplatePart(Name = PartRootName, Type = typeof(Panel))]
    [TemplatePart(Name = PartViewPresenterName, Type = typeof(CalendarViewPresenter))]
    [TemplatePart(Name = PartHeaderButtonName, Type = typeof(Button))]
    [TemplatePart(Name = PartPreviousButtonName, Type = typeof(Button))]
    [TemplatePart(Name = PartNextButtonName, Type = typeof(Button))]
    public class Calendar : Control {

        #region Part names

        private const string PartRootName = "PART_Root";
        private const string PartViewPresenterName = "PART_ViewPresenter";
        private const string PartHeaderButtonName = "PART_HeaderButton";
        private const string PartPreviousButtonName = "PART_PreviousButton";
        private const string PartNextButtonName = "PART_NextButton";
        private const string PreviousButtonDefaultContent = "Prev";
        private const string NextButtonDefaultContent = "Next";

        #endregion

        #region static fields
        public static readonly Type Typeof = typeof(Calendar);
        #endregion

        #region instance fields and parts

        // part root not used!

        /// <summary>
        /// part view presenter
        /// </summary>
        private CalendarViewPresenter _viewPresenter;

        /// <summary>
        /// part header button
        /// </summary>
        private Button _headerButton;

        /// <summary>
        /// part previous button
        /// </summary>
        private Button _previousButton;

        /// <summary>
        /// part next button
        /// </summary>
        private Button _nextButton;

        /// <summary>
        /// Backend field for <see cref="P:Kavand.Windows.Controls.Calendar.HoverStart"/> property
        /// </summary>
        private DateTime? _hoverStart;

        /// <summary>
        /// Backend field for <see cref="P:Kavand.Windows.Controls.Calendar.HoverEnd"/> property 
        /// </summary>
        private DateTime? _hoverEnd;

        /// <summary>
        /// Detects if shift key is pressed
        /// </summary>
        private bool _isShiftPressed;

        private DateTime? _currentDate;

        #endregion

        #region .ctor

        static Calendar() {
            DefaultStyleKeyProperty.OverrideMetadata(Typeof, new FrameworkPropertyMetadata(Typeof));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(Typeof, new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(Typeof, new FrameworkPropertyMetadata(KeyboardNavigationMode.Contained));
            LanguageProperty.OverrideMetadata(Typeof, new FrameworkPropertyMetadata(OnLanguageChanged));
            EventManager.RegisterClassHandler(Typeof, GotFocusEvent, new RoutedEventHandler(OnGotFocus));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Kavand.Windows.Controls.Calendar"/> class.
        /// </summary>
        public Calendar() {
            SetCurrentValue(DisplayDateProperty, Engine.MinSupportedDateTime);
            _blackoutDates = new BlackoutDatesCollection(this);
            _selectedDates = new SelectedDatesCollection(this);
            SetCurrentValue(DisplayDateProperty, DateTime.Today);
        }

        #endregion

        #region internal properties

        internal bool DatePickerDisplayDateFlag { get; set; }

        internal DateTime DisplayDateInternal { get; private set; }

        internal DateTime DisplayDateEndInternal {
            get { return DisplayDateEnd.GetValueOrDefault(Engine.MaxSupportedDateTime); }
        }

        internal DateTime DisplayDateStartInternal {
            get { return DisplayDateStart.GetValueOrDefault(Engine.MinSupportedDateTime); }
        }

        internal DateTime CurrentDate {
            get { return _currentDate.GetValueOrDefault(DisplayDateInternal); }
            set { _currentDate = value; }
        }

        /// <summary>
        /// Gets or sets selection start position in multiselection mode
        /// </summary>
        internal DateTime? HoverStart {
            get { return SelectionMode != CalendarSelectionMode.None ? _hoverStart : new DateTime?(); }
            set { _hoverStart = value; }
        }

        /// <summary>
        /// Gets or sets selection end position in multiselection mode
        /// </summary>
        internal DateTime? HoverEnd {
            get { return SelectionMode != CalendarSelectionMode.None ? _hoverEnd : new DateTime?(); }
            set { _hoverEnd = value; }
        }

        internal CalendarViewPresenter ViewPresenter {
            get { return _viewPresenter; }
        }

        internal DateTime DisplayMonth {
            get { return Engine.GetFirstOfMonth(DisplayDate); }
        }

        internal DateTime DisplayYear {
            get { return Engine.GetFirstOfYear(DisplayDate); }
        }

        internal bool IsRightToLeft {
            get { return FlowDirection == FlowDirection.RightToLeft; }
        }

        #endregion

        #region regular properties

        // blackout dates
        #region property BlackoutDates

        private readonly BlackoutDatesCollection _blackoutDates;

        /// <summary>
        /// Gets a collection of dates that are marked as not selectable.
        /// </summary> 
        /// <returns>
        /// A collection of dates that cannot be selected. The default value is an empty collection.
        /// </returns>
        public BlackoutDatesCollection BlackoutDates { get { return _blackoutDates; } }

        #endregion

        // selected dates
        #region property SelectedDates

        private readonly SelectedDatesCollection _selectedDates;

        /// <summary>
        /// Gets a collection of selected dates.
        /// </summary> 
        /// <returns>
        /// A <see cref="T:Kavand.Windows.Controls.SelectedDatesCollection"/> object that contains the currently selected dates. The default is an empty collection.
        /// </returns>
        public SelectedDatesCollection SelectedDates { get { return _selectedDates; } }

        #endregion

        #endregion

        #region dependency properties

        #region DependencyProperty DayNameMode

        public static readonly DependencyProperty DayNameModeProperty
            = DependencyProperty.Register("DayNameMode", typeof(DayNameMode), Typeof,
                new PropertyMetadata(DayNameMode.Shortest, DayNameModeChangedCallback), ValidateDayNameModeCallback);

        private static bool ValidateDayNameModeCallback(object value) {
            var mode = (DayNameMode)value;
            switch (mode) {
                case DayNameMode.Shortest:
                case DayNameMode.Abbreviated:
                    return true;
                default:
                    return mode == DayNameMode.Normal;
            }
        }

        private static void DayNameModeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((Calendar)d).UpdatePresenter();
        }

        public DayNameMode DayNameMode {
            get { return (DayNameMode)GetValue(DayNameModeProperty); }
            set { SetValue(DayNameModeProperty, value); }
        }

        #endregion

        #region DependencyProperty MonthNameMode

        public static readonly DependencyProperty MonthNameModeProperty
            = DependencyProperty.Register("MonthNameMode", typeof(MonthNameMode), Typeof,
                new PropertyMetadata(MonthNameMode.Abbreviated, MonthNameModeChangedCallback), ValidateMonthNameModeCallback);

        private static bool ValidateMonthNameModeCallback(object value) {
            var mode = (MonthNameMode)value;
            switch (mode) {
                case MonthNameMode.Abbreviated:
                case MonthNameMode.AbbreviatedGenitive:
                case MonthNameMode.NormalGenitive:
                    return true;
                default:
                    return mode == MonthNameMode.Normal;
            }
        }

        private static void MonthNameModeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((Calendar)d).UpdatePresenter();
        }

        public MonthNameMode MonthNameMode {
            get { return (MonthNameMode)GetValue(MonthNameModeProperty); }
            set { SetValue(MonthNameModeProperty, value); }
        }

        #endregion

        // engine
        #region DependencyProperty Engine

        public static readonly DependencyProperty EngineProperty
            = DependencyProperty.Register("Engine", typeof(CalendarEngine), Typeof,
                new PropertyMetadata(new GregorianCalendarEngine(), EngineChangedCallback), ValidateEngineCallback);

        private static void EngineChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((Calendar)d).UpdatePresenter();
        }

        private static bool ValidateEngineCallback(object value) {
            return value is CalendarEngine;
        }

        public CalendarEngine Engine {
            get { return (CalendarEngine)GetValue(EngineProperty); }
            set { SetValue(EngineProperty, value); }
        }

        #endregion

        // first day of week
        #region DependencyProperty FirstDayOfWeek

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.FirstDayOfWeek"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.FirstDayOfWeek"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty FirstDayOfWeekProperty
            = DependencyProperty.Register("FirstDayOfWeek", typeof(DayOfWeek), Typeof,
                new FrameworkPropertyMetadata(CalendarEngine.GetCurrentCultureFirstDayOfWeek(),
                    OnDayOfWeekChanged), IsValidDayOfWeek);

        /// <summary>
        /// Gets or sets the day that is considered the beginning of the week.
        /// </summary> 
        /// <returns>
        /// A <see cref="T:System.DayOfWeek"/> that represents the beginning of the week. The default is the 
        /// <see cref="P:System.Globalization.DateTimeFormatInfo.FirstDayOfWeek"/> that is determined by the current culture.
        /// </returns>
        public DayOfWeek FirstDayOfWeek {
            get { return (DayOfWeek)GetValue(FirstDayOfWeekProperty); }
            set { SetValue(FirstDayOfWeekProperty, value); }
        }

        #endregion

        // holiday of week
        #region DependencyProperty HolidayOfWeek

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.HolidayOfWeek"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.HolidayOfWeek"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty HolidayOfWeekProperty
            = DependencyProperty.Register("HolidayOfWeek", typeof(DayOfWeek), Typeof,
                new FrameworkPropertyMetadata(CalendarEngine.GetCurrentCultureHolidayOfWeek(),
                    OnDayOfWeekChanged), IsValidDayOfWeek);

        /// <summary>
        /// Gets or sets the day that is considered the beginning of the week.
        /// </summary> 
        /// <returns>
        /// A <see cref="T:System.DayOfWeek"/> that represents the beginning of the week. The default is the 
        /// <see cref="P:System.Globalization.DateTimeFormatInfo.FirstDayOfWeek"/> that is determined by the current culture.
        /// </returns>
        public DayOfWeek HolidayOfWeek {
            get { return (DayOfWeek)GetValue(HolidayOfWeekProperty); }
            set { SetValue(HolidayOfWeekProperty, value); }
        }

        #endregion

        #region FirstDayOfWeek and HolidayOfWeek shared callbacks

        internal static bool IsValidDayOfWeek(object value) {
            var dayOfWeek = (DayOfWeek)value;
            switch (dayOfWeek) {
                case DayOfWeek.Sunday:
                case DayOfWeek.Monday:
                case DayOfWeek.Tuesday:
                case DayOfWeek.Wednesday:
                case DayOfWeek.Thursday:
                case DayOfWeek.Friday:
                case DayOfWeek.Saturday:
                    return true;
                default:
                    return false;
            }
        }

        private static void OnDayOfWeekChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((Calendar)d).UpdatePresenter();
        }

        #endregion

        // selected date
        #region DependencyProperty SelectedDate

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.SelectedDate"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.SelectedDate"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty SelectedDateProperty
            = DependencyProperty.Register("SelectedDate", typeof(DateTime?), Typeof,
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateChanged));

        private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var kalendar = (Calendar)d;
            if (kalendar.SelectionMode == CalendarSelectionMode.None && e.NewValue != null)
                throw new InvalidOperationException();
            var nullable = (DateTime?)e.NewValue;
            if (!IsValidDateSelection(kalendar, nullable))
                throw new ArgumentOutOfRangeException();
            if (!nullable.HasValue)
                kalendar.SelectedDates.ClearInternal(true);
            else if ((kalendar.SelectedDates.Count <= 0 || kalendar.SelectedDates[0] != nullable.Value)) {
                kalendar.SelectedDates.ClearInternal();
                kalendar.SelectedDates.Add(nullable.Value);
            }
            if (kalendar.SelectionMode != CalendarSelectionMode.SingleDate)
                return;
            if (nullable.HasValue)
                kalendar.CurrentDate = nullable.Value;
            kalendar.UpdatePresenter();
            UpdateSelectedDateString(kalendar);
        }

        /// <summary>
        /// Gets or sets the currently selected date.
        /// </summary> 
        /// <returns>
        /// The date currently selected. The default is null.
        /// </returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The specified date is outside the range 
        /// specified by <see cref="P:Kavand.Windows.Controls.Calendar.DisplayDateStart"/> 
        /// and <see cref="P:Kavand.Windows.Controls.Calendar.DisplayDateEnd"/>-or-The 
        /// specified date is in the <see cref="P:Kavand.Windows.Controls.Calendar.BlackoutDates"/> collection.</exception>
        /// <exception cref="T:System.InvalidOperationException">If set to anything other than null 
        /// when <see cref="P:Kavand.Windows.Controls.Calendar.SelectionMode"/> is set 
        /// to <see cref="F:System.Windows.Controls.CalendarSelectionMode.None"/>.</exception>
        public DateTime? SelectedDate {
            get { return (DateTime?)GetValue(SelectedDateProperty); }
            set { SetValue(SelectedDateProperty, value); }
        }

        #endregion

        // selected date string
        #region DependencyProperty SelectedDateString

        private static readonly DependencyPropertyKey SelectedDateStringPropertyKey
            = DependencyProperty.RegisterReadOnly("SelectedDateString", typeof(string), Typeof,
            new PropertyMetadata(string.Empty, SelectedDateStringChangedCallback, SelectedDateStringCoerceCallback));

        private static object SelectedDateStringCoerceCallback(DependencyObject d, object basevalue) {
            return basevalue ?? string.Empty;
        }

        private static void SelectedDateStringChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            // not used
        }

        public static readonly DependencyProperty SelectedDateStringProperty
            = SelectedDateStringPropertyKey.DependencyProperty;

        public string SelectedDateString {
            get { return (string)GetValue(SelectedDateStringProperty); }
            private set { SetValue(SelectedDateStringPropertyKey, value); }
        }

        #endregion

        // selected date string format
        #region DependencyProperty SelectedDateStringFormat

        public static readonly DependencyProperty SelectedDateStringFormatProperty
            = DependencyProperty.Register("SelectedDateStringFormat", typeof(string), Typeof, new PropertyMetadata(
                CalendarEngine.GetCurrentCultureDateStringFormat(), SelectedDateStringFormatChangedCallback), SelectedDateStringFormatValidateCallback);

        private static bool SelectedDateStringFormatValidateCallback(object value) {
            try {
                // ReSharper disable ReturnValueOfPureMethodIsNotUsed
                DateTime.Now.ToString((string)value);
                // ReSharper restore ReturnValueOfPureMethodIsNotUsed
                return true;
            } catch (FormatException) {
                return false;
            }
        }

        private static void SelectedDateStringFormatChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var kalendar = (Calendar)d;
            UpdateSelectedDateString(kalendar);
        }

        public string SelectedDateStringFormat {
            get { return (string)GetValue(SelectedDateStringFormatProperty); }
            set { SetValue(SelectedDateStringFormatProperty, value); }
        }

        #endregion

        #region SelectedDate & SelectedDateString & SelectedDateStringFormat

        private static void UpdateSelectedDateString(Calendar calendar) {
            calendar.SelectedDateString =
                !calendar.SelectedDate.HasValue ? string.Empty :
                calendar.SelectedDate.Value.ToString(calendar.SelectedDateStringFormat);
        }

        #endregion

        // selection mode
        #region DependencyProperty SelectionMode

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.SelectionMode"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.SelectionMode"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty SelectionModeProperty
            = DependencyProperty.Register("SelectionMode", typeof(CalendarSelectionMode), Typeof,
                new FrameworkPropertyMetadata(CalendarSelectionMode.SingleDate, OnSelectionModeChanged), IsValidSelectionMode);

        private static bool IsValidSelectionMode(object value) {
            var calendarSelectionMode = (CalendarSelectionMode)value;
            switch (calendarSelectionMode) {
                case CalendarSelectionMode.None:
                case CalendarSelectionMode.SingleDate:
                case CalendarSelectionMode.SingleRange:
                case CalendarSelectionMode.MultipleRange:
                    return true;
                default:
                    return false;
            }
        }

        private static void OnSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var calendar = (Calendar)d;
            calendar.HoverEnd = null;
            calendar.HoverStart = null;
            calendar.SelectedDates.ClearInternal(true);
            calendar.OnSelectionModeChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Gets or sets a value that indicates what kind of selections are allowed.
        /// </summary> 
        /// <returns>
        /// A value that indicates the current selection mode. The default 
        /// is <see cref="F:System.Windows.Controls.CalendarSelectionMode.SingleDate"/>.
        /// </returns>
        public CalendarSelectionMode SelectionMode {
            get { return (CalendarSelectionMode)GetValue(SelectionModeProperty); }
            set { SetValue(SelectionModeProperty, value); }
        }

        #endregion

        // display date
        #region DependencyProperty DisplayDate

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.DisplayDate"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.DisplayDate"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty DisplayDateProperty
            = DependencyProperty.Register("DisplayDate", typeof(DateTime), Typeof,
                new FrameworkPropertyMetadata(DateTime.MinValue,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnDisplayDateChanged, CoerceDisplayDate));

        private static void OnDisplayDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var kalendar = (Calendar)d;
            kalendar.DisplayDateInternal = kalendar.Engine.GetFirstOfMonth((DateTime)e.NewValue);
            kalendar.UpdatePresenter();
            kalendar.OnDisplayDateChanged(new DateChangedEventArgs((DateTime)e.OldValue, (DateTime)e.NewValue));
            UpdateDisplayDateString(kalendar);
        }

        private static object CoerceDisplayDate(DependencyObject d, object value) {
            var calendar = (Calendar)d;
            var dateTime = (DateTime)value;
            if (calendar.DisplayDateStart.HasValue && dateTime < calendar.DisplayDateStart.Value)
                value = calendar.DisplayDateStart.Value;
            else if (calendar.DisplayDateEnd.HasValue && dateTime > calendar.DisplayDateEnd.Value)
                value = calendar.DisplayDateEnd.Value;
            return value;
        }

        /// <summary>
        /// Gets or sets the date to display.
        /// </summary> 
        /// <returns>
        /// The date to display. The default is <see cref="P:System.DateTime.Today"/>.
        /// </returns>
        public DateTime DisplayDate {
            get { return (DateTime)GetValue(DisplayDateProperty); }
            set { SetValue(DisplayDateProperty, value); }
        }

        #endregion

        // display date string
        #region DependencyProperty DisplayDateString

        private static readonly DependencyPropertyKey DisplayDateStringPropertyKey
            = DependencyProperty.RegisterReadOnly("DisplayDateString", typeof(string), Typeof,
            new PropertyMetadata(string.Empty, DisplayDateStringChangedCallback, DisplayDateStringCoerceCallback));

        private static object DisplayDateStringCoerceCallback(DependencyObject d, object basevalue) {
            return basevalue ?? string.Empty;
        }

        private static void DisplayDateStringChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            // not used
        }

        public static readonly DependencyProperty DisplayDateStringProperty
            = DisplayDateStringPropertyKey.DependencyProperty;

        public string DisplayDateString {
            get { return (string)GetValue(DisplayDateStringProperty); }
            private set { SetValue(DisplayDateStringPropertyKey, value); }
        }

        #endregion

        // display date string format
        #region DependencyProperty DisplayDateStringFormat

        public static readonly DependencyProperty DisplayDateStringFormatProperty
            = DependencyProperty.Register("DisplayDateStringFormat", typeof(string), Typeof, new PropertyMetadata(
                CalendarEngine.GetCurrentCultureDateStringFormat(), DisplayDateStringFormatChangedCallback), DisplayDateStringFormatValidateCallback);

        private static bool DisplayDateStringFormatValidateCallback(object value) {
            try {
                // ReSharper disable ReturnValueOfPureMethodIsNotUsed
                DateTime.Now.ToString((string)value);
                // ReSharper restore ReturnValueOfPureMethodIsNotUsed
                return true;
            } catch (FormatException) {
                return false;
            }
        }

        private static void DisplayDateStringFormatChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var kalendar = (Calendar)d;
            UpdateDisplayDateString(kalendar);
        }

        public string DisplayDateStringFormat {
            get { return (string)GetValue(DisplayDateStringFormatProperty); }
            set { SetValue(DisplayDateStringFormatProperty, value); }
        }

        #endregion

        #region DisplayDate & DisplayDateString & DisplayDateStringFormat

        private static void UpdateDisplayDateString(Calendar calendar) {
            calendar.DisplayDateString = calendar.DisplayDate.ToString(calendar.DisplayDateStringFormat);
        }

        #endregion

        // display mode
        #region DependencyProperty DisplayMode

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.DisplayMode"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.DisplayMode"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty DisplayModeProperty
            = DependencyProperty.Register("DisplayMode", typeof(CalendarMode), Typeof,
                new FrameworkPropertyMetadata(CalendarMode.Month,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnDisplayModePropertyChanged), IsValidDisplayMode);

        private static bool IsValidDisplayMode(object value) {
            var calendarMode = (CalendarMode)value;
            switch (calendarMode) {
                case CalendarMode.Month:
                case CalendarMode.Year:
                    return true;
                default:
                    return calendarMode == CalendarMode.Decade;
            }
        }

        private static void OnDisplayModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var calendar = (Calendar)d;
            var newMode = (CalendarMode)e.NewValue;
            var oldMode = (CalendarMode)e.OldValue;
            switch (newMode) {
                case CalendarMode.Month:
                    if (oldMode == CalendarMode.Year || oldMode == CalendarMode.Decade) {
                        calendar.HoverEnd = null;
                        calendar.HoverStart = null;
                        calendar.CurrentDate = calendar.DisplayDate;
                    }
                    calendar.UpdatePresenter();
                    break;
                case CalendarMode.Year:
                case CalendarMode.Decade:
                    if (oldMode == CalendarMode.Month)
                        calendar.SetCurrentValue(DisplayDateProperty, calendar.CurrentDate);
                    calendar.UpdatePresenter();
                    break;
            }
            var args = new CalendarModeChangedEventArgs(oldMode, newMode);
            if (calendar._viewPresenter != null)
                calendar._viewPresenter.OnDisplayModeChanged(args);
            calendar.OnDisplayModeChanged(args);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the calendar displays a month, year, or decade.
        /// </summary> 
        /// <returns>
        /// A value that indicates what length of time the <see cref="T:Kavand.Windows.Controls.Calendar"/> should display.
        /// </returns>
        public CalendarMode DisplayMode {
            get { return (CalendarMode)GetValue(DisplayModeProperty); }
            set { SetValue(DisplayModeProperty, value); }
        }

        #endregion

        // display date end
        #region DependencyProperty DisplayDateEnd

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.DisplayDateEnd"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.DisplayDateEnd"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty DisplayDateEndProperty
            = DependencyProperty.Register("DisplayDateEnd", typeof(DateTime?), Typeof,
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnDisplayDateEndChanged, CoerceDisplayDateEnd));

        private static void OnDisplayDateEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var calendar = (Calendar)d;
            calendar.CoerceValue(DisplayDateProperty);
            calendar.UpdatePresenter();
        }

        private static object CoerceDisplayDateEnd(DependencyObject d, object value) {
            var calendar = (Calendar)d;
            var nullable = (DateTime?)value;
            if (nullable.HasValue) {
                if (calendar.DisplayDateStart.HasValue && nullable.Value < calendar.DisplayDateStart.Value)
                    value = calendar.DisplayDateStart;
                var maximumDate = calendar.SelectedDates.MaximumDate;
                if (maximumDate.HasValue && nullable.Value < maximumDate.Value)
                    value = maximumDate;
            }
            return value;
        }

        /// <summary>
        /// Gets or sets the last date in the date range that is available in the calendar.
        /// </summary> 
        /// <returns>
        /// The last date that is available in the calendar.
        /// </returns>
        public DateTime? DisplayDateEnd {
            get { return (DateTime?)GetValue(DisplayDateEndProperty); }
            set { SetValue(DisplayDateEndProperty, value); }
        }

        #endregion

        // display date start
        #region DependencyProperty DisplayDateStart

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.DisplayDateStart"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.DisplayDateStart"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty DisplayDateStartProperty
            = DependencyProperty.Register("DisplayDateStart", typeof(DateTime?), Typeof,
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnDisplayDateStartChanged, CoerceDisplayDateStart));

        private static void OnDisplayDateStartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var calendar = (Calendar)d;
            calendar.CoerceValue(DisplayDateEndProperty);
            calendar.CoerceValue(DisplayDateProperty);
            calendar.UpdatePresenter();
        }

        private static object CoerceDisplayDateStart(DependencyObject d, object value) {
            var calendar = (Calendar)d;
            var nullable = (DateTime?)value;
            if (nullable.HasValue) {
                var minimumDate = calendar.SelectedDates.MinimumDate;
                if (minimumDate.HasValue && nullable.Value > minimumDate.Value)
                    value = minimumDate;
            }
            return value;
        }

        /// <summary>
        /// Gets or sets the first date that is available in the calendar.
        /// </summary> 
        /// <returns>
        /// The first date that is available in the calendar. The default is null.
        /// </returns>
        public DateTime? DisplayDateStart {
            get { return (DateTime?)GetValue(DisplayDateStartProperty); }
            set { SetValue(DisplayDateStartProperty, value); }
        }

        #endregion

        // today button visiblity

        // clear button visiblity

        // is today highlighted
        #region DependencyProperty IsTodayHighlighted

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.IsTodayHighlighted"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.IsTodayHighlighted"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty IsTodayHighlightedProperty
            = DependencyProperty.Register("IsTodayHighlighted", typeof(bool), Typeof,
                new FrameworkPropertyMetadata(true, OnIsTodayHighlightedChanged));

        private static void OnIsTodayHighlightedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var calendar = (Calendar)d;
            var num = calendar.Engine.CompareYearMonth(calendar.DisplayDateInternal, DateTime.Today);
            if (num <= -2 || num >= 2)
                return;
            calendar.UpdatePresenter();
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the current date is highlighted.
        /// </summary> 
        /// <returns>
        /// true if the current date is highlighted; otherwise, false. The default is true.
        /// </returns>
        public bool IsTodayHighlighted {
            get { return (bool)GetValue(IsTodayHighlightedProperty); }
            set { SetValue(IsTodayHighlightedProperty, value); }
        }

        #endregion

        // header background brush
        #region DependencyProperty HeaderBackground

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.HeaderBackground"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.HeaderBackground"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty HeaderBackgroundProperty
            = DependencyProperty.Register("HeaderBackground", typeof(Brush), Typeof);

        /// <summary>
        /// Gets or sets the <see cref="T:System.Windows.Media.Brush"/> to fill the control's header part.
        /// </summary> 
        /// <returns>
        /// The current brush of the <see cref="P:Kavand.Windows.Controls.Calendar.HeaderBackground"/> property.
        /// </returns>
        public Brush HeaderBackground {
            get { return (Brush)GetValue(HeaderBackgroundProperty); }
            set { SetValue(HeaderBackgroundProperty, value); }
        }

        #endregion

        // header border brush
        #region DependencyProperty HeaderBorderBrush

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.HeaderBorderBrush"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.HeaderBorderBrush"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty HeaderBorderBrushProperty
            = DependencyProperty.Register("HeaderBorderBrush", typeof(Brush), Typeof);

        /// <summary>
        /// Gets or sets the <see cref="T:System.Windows.Media.Brush"/> to draw the control's header part's border.
        /// </summary> 
        /// <returns>
        /// The current brush of the <see cref="P:Kavand.Windows.Controls.Calendar.HeaderBorderBrush"/> property.
        /// </returns>
        public Brush HeaderBorderBrush {
            get { return (Brush)GetValue(HeaderBorderBrushProperty); }
            set { SetValue(HeaderBorderBrushProperty, value); }
        }

        #endregion

        // header border thickness
        #region DependencyProperty HeaderBorderThickness

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.HeaderBorderThickness"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.HeaderBorderThickness"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty HeaderBorderThicknessProperty
            = DependencyProperty.Register("HeaderBorderThickness", typeof(Thickness), Typeof);

        /// <summary>
        /// Gets or sets the <see cref="T:System.Windows.Thickness"/> to draw the control's header part's border.
        /// </summary> 
        /// <returns>
        /// The current thickness of the <see cref="P:Kavand.Windows.Controls.Calendar.HeaderBorderThickness"/> property.
        /// </returns>
        public Thickness HeaderBorderThickness {
            get { return (Thickness)GetValue(HeaderBorderThicknessProperty); }
            set { SetValue(HeaderBorderThicknessProperty, value); }
        }

        #endregion

        // PART_HeaderButton style
        #region DependencyProperty HeaderButtonStyle

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.HeaderButtonStyle"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.HeaderButtonStyle"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty HeaderButtonStyleProperty
            = DependencyProperty.Register("HeaderButtonStyle", typeof(Style), Typeof);

        /// <summary>
        /// Gets or sets the <see cref="T:System.Windows.Style"/> associated with the control's internal PART_HeaderButton button.
        /// </summary> 
        /// <returns>
        /// The current style of the PART_HeaderButton button.
        /// </returns>
        public Style HeaderButtonStyle {
            get { return (Style)GetValue(HeaderButtonStyleProperty); }
            set { SetValue(HeaderButtonStyleProperty, value); }
        }

        #endregion

        // PART_PreviousButton style
        #region DependencyProperty PreviousButtonStyle

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.PreviousButtonStyle"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.PreviousButtonStyle"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty PreviousButtonStyleProperty
            = DependencyProperty.Register("PreviousButtonStyle", typeof(Style), Typeof);

        /// <summary>
        /// Gets or sets the <see cref="T:System.Windows.Style"/> associated with the control's internal PART_PreviousButton button.
        /// </summary> 
        /// <returns>
        /// The current style of the PART_PreviousButton button.
        /// </returns>
        public Style PreviousButtonStyle {
            get { return (Style)GetValue(PreviousButtonStyleProperty); }
            set { SetValue(PreviousButtonStyleProperty, value); }
        }

        #endregion

        // PART_NextButton style
        #region DependencyProperty NextButtonStyle

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.NextButtonStyle"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.NextButtonStyle"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty NextButtonStyleProperty
            = DependencyProperty.Register("NextButtonStyle", typeof(Style), Typeof);

        /// <summary>
        /// Gets or sets the <see cref="T:System.Windows.Style"/> associated with the control's internal PART_NextButton button.
        /// </summary> 
        /// <returns>
        /// The current style of the PART_NextButton button.
        /// </returns>
        public Style NextButtonStyle {
            get { return (Style)GetValue(NextButtonStyleProperty); }
            set { SetValue(NextButtonStyleProperty, value); }
        }

        #endregion

        // PART_PreviousButton content
        #region DependencyProperty PreviousButtonContent

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.PreviousButtonContent"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.PreviousButtonContent"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty PreviousButtonContentProperty
            = DependencyProperty.Register("PreviousButtonContent", typeof(object),
                Typeof, new PropertyMetadata(PreviousButtonDefaultContent));

        /// <summary>
        /// Gets or sets the <see cref="T:System.Object"/> used as the control's internal PART_PreviousButton button's content.
        /// </summary> 
        /// <returns>
        /// The current content of the PART_PreviousButton button.
        /// </returns>
        public object PreviousButtonContent {
            get { return GetValue(PreviousButtonContentProperty); }
            set { SetValue(PreviousButtonContentProperty, value); }
        }

        #endregion

        // PART_NextButton content
        #region DependencyProperty NextButtonContent

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.NextButtonContent"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.NextButtonContent"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty NextButtonContentProperty
            = DependencyProperty.Register("NextButtonContent", typeof(object),
                Typeof, new PropertyMetadata(NextButtonDefaultContent));

        /// <summary>
        /// Gets or sets the <see cref="T:System.Object"/> used as the control's internal PART_NextButton button's content.
        /// </summary> 
        /// <returns>
        /// The current content of the PART_NextButton button.
        /// </returns>
        public object NextButtonContent {
            get { return GetValue(NextButtonContentProperty); }
            set { SetValue(NextButtonContentProperty, value); }
        }

        #endregion

        // view presenter style
        #region DependencyProperty ViewPresenterStyle

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.ViewPresenterStyle"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.ViewPresenterStyle"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty ViewPresenterStyleProperty
            = DependencyProperty.Register("ViewPresenterStyle", typeof(Style), Typeof);

        /// <summary>
        /// Gets or sets the <see cref="T:System.Windows.Style"/> associated with the control's internal <see cref="T:Kavand.Windows.Controls.CalendarViewPresenter"/> object.
        /// </summary> 
        /// <returns>
        /// The current style of the <see cref="T:Kavand.Windows.Controls.CalendarViewPresenter"/> object.
        /// </returns>
        public Style ViewPresenterStyle {
            get { return (Style)GetValue(ViewPresenterStyleProperty); }
            set { SetValue(ViewPresenterStyleProperty, value); }
        }

        #endregion

        // day button style
        #region DependencyProperty DayButtonStyle

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.DayButtonStyle"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.DayButtonStyle"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty DayButtonStyleProperty
            = DependencyProperty.Register("DayButtonStyle", typeof(Style), Typeof);

        /// <summary>
        /// Gets or sets the <see cref="T:System.Windows.Style"/> associated with the control's 
        /// internal <see cref="T:Kavand.Windows.Controls.DayButton"/> object.
        /// </summary> 
        /// <returns>
        /// The current style of the <see cref="T:Kavand.Windows.Controls.DayButton"/> object.
        /// </returns>
        public Style DayButtonStyle {
            get { return (Style)GetValue(DayButtonStyleProperty); }
            set { SetValue(DayButtonStyleProperty, value); }
        }

        #endregion

        // button style
        #region DependencyProperty MonthYearButtonStyle

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.Calendar.MonthYearButtonStyle"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.Calendar.MonthYearButtonStyle"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty MonthYearButtonStyleProperty
            = DependencyProperty.Register("MonthYearButtonStyle", typeof(Style), Typeof);

        /// <summary>
        /// Gets or sets the <see cref="T:System.Windows.Style"/> associated with the control's 
        /// internal <see cref="T:Kavand.Windows.Controls.MonthYearButton"/> object.
        /// </summary> 
        /// <returns>
        /// The current style of the <see cref="T:Kavand.Windows.Controls.MonthYearButton"/> object.
        /// </returns>
        public Style MonthYearButtonStyle {
            get { return (Style)GetValue(MonthYearButtonStyleProperty); }
            set { SetValue(MonthYearButtonStyleProperty, value); }
        }

        #endregion

        // day of week header visibility

        // use short day of week names

        // use short month names

        // language
        #region DependencyProperty Language

        private static void OnLanguageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var calendar = (Calendar)d;
            if (DependencyPropertyHelper.GetValueSource(d, FirstDayOfWeekProperty).BaseValueSource != BaseValueSource.Default)
                return;
            calendar.SetCurrentValue(FirstDayOfWeekProperty, calendar.Engine.GetFirstDayOfWeek(calendar.Engine.GetCulture(calendar)));
            calendar.SetCurrentValue(HolidayOfWeekProperty, calendar.Engine.GetHolidayOfWeek(calendar.Engine.GetCulture(calendar)));
            calendar.UpdatePresenter();
        }

        #endregion

        #endregion

        #region events and routed events

        // selected date changed

        // display date changed
        #region event DisplayDateChanged

        private EventHandler<DateChangedEventArgs> _displayDateChanged;

        /// <summary>
        /// Occurs when the <see cref="P:Kavand.Windows.Controls.Calendar.DisplayDate"/> property is changed.
        /// </summary>
        public event EventHandler<DateChangedEventArgs> DisplayDateChanged {
            add {
                var eventHandler = _displayDateChanged;
                EventHandler<DateChangedEventArgs> comparand;
                do {
                    comparand = eventHandler;
                    eventHandler = Interlocked.CompareExchange(ref _displayDateChanged, comparand + value, comparand);
                }
                while (eventHandler != comparand);
            }
            remove {
                var eventHandler = _displayDateChanged;
                EventHandler<DateChangedEventArgs> comparand;
                do {
                    comparand = eventHandler;
                    eventHandler = Interlocked.CompareExchange(ref _displayDateChanged, comparand - value, comparand);
                }
                while (eventHandler != comparand);
            }
        }

        /// <summary>
        /// Raises the <see cref="E:Kavand.Windows.Controls.Calendar.DisplayDateChanged"/> event.
        /// </summary>
        /// <param name="e">The data for the event. </param>
        protected virtual void OnDisplayDateChanged(DateChangedEventArgs e) {
            var eventHandler = _displayDateChanged;
            if (eventHandler != null)
                eventHandler(this, e);
        }

        #endregion

        // selected dates changed
        #region RoutedEvent SelectedDatesChanged

        /// <summary>
        /// Identifies the <see cref="E:Kavand.Windows.Controls.Calendar.SelectedDatesChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent SelectedDatesChangedEvent
            = EventManager.RegisterRoutedEvent("SelectedDatesChanged", RoutingStrategy.Direct,
            typeof(EventHandler<SelectionChangedEventArgs>), Typeof);

        /// <summary>
        /// Occurs when the collection returned by the <see cref="P:Kavand.Windows.Controls.Calendar.SelectedDates"/> property is changed.
        /// </summary>
        public event EventHandler<SelectionChangedEventArgs> SelectedDatesChanged {
            add {
                AddHandler(SelectedDatesChangedEvent, value);
            }
            remove {
                RemoveHandler(SelectedDatesChangedEvent, value);
            }
        }

        /// <summary>
        /// Raises the <see cref="E:Kavand.Windows.Controls.Calendar.SelectedDatesChanged"/> routed event.
        /// </summary>
        /// <param name="e">The data for the event. </param>
        protected virtual void OnSelectedDatesChanged(SelectionChangedEventArgs e) {
            RaiseEvent(e);
        }

        #endregion

        // selection mode changed
        #region event SelectionModeChanged

        private EventHandler<EventArgs> _selectionModeChanged;

        /// <summary>
        /// Occurs when the <see cref="P:Kavand.Windows.Controls.Calendar.SelectionMode"/> changes.
        /// </summary>
        public event EventHandler<EventArgs> SelectionModeChanged {
            add {
                var eventHandler = _selectionModeChanged;
                EventHandler<EventArgs> comparand;
                do {
                    comparand = eventHandler;
                    eventHandler = Interlocked.CompareExchange(ref _selectionModeChanged, comparand + value, comparand);
                }
                while (eventHandler != comparand);
            }
            remove {
                var eventHandler = _selectionModeChanged;
                EventHandler<EventArgs> comparand;
                do {
                    comparand = eventHandler;
                    eventHandler = Interlocked.CompareExchange(ref _selectionModeChanged, comparand - value, comparand);
                }
                while (eventHandler != comparand);
            }
        }

        /// <summary>
        /// Raises the <see cref="E:Kavand.Windows.Controls.Calendar.SelectionModeChanged"/> event.
        /// </summary>
        /// <param name="e">The data for the event. </param>
        protected virtual void OnSelectionModeChanged(EventArgs e) {
            var eventHandler = _selectionModeChanged;
            if (eventHandler != null)
                eventHandler(this, e);
        }

        #endregion

        // display mode changed
        #region event DisplayModeChanged

        private EventHandler<CalendarModeChangedEventArgs> _displayModeChanged;

        /// <summary>
        /// Occurs when the <see cref="P:Kavand.Windows.Controls.Calendar.DisplayMode"/> property is changed.
        /// </summary>
        public event EventHandler<CalendarModeChangedEventArgs> DisplayModeChanged {
            add {
                var eventHandler = _displayModeChanged;
                EventHandler<CalendarModeChangedEventArgs> comparand;
                do {
                    comparand = eventHandler;
                    eventHandler = Interlocked.CompareExchange(ref _displayModeChanged, comparand + value, comparand);
                }
                while (eventHandler != comparand);
            }
            remove {
                var eventHandler = _displayModeChanged;
                EventHandler<CalendarModeChangedEventArgs> comparand;
                do {
                    comparand = eventHandler;
                    eventHandler = Interlocked.CompareExchange(ref _displayModeChanged, comparand - value, comparand);
                }
                while (eventHandler != comparand);
            }
        }

        /// <summary>
        /// Raises the <see cref="E:Kavand.Windows.Controls.Calendar.DisplayModeChanged"/> event.
        /// </summary>
        /// <param name="e">The data for the event. </param>
        protected virtual void OnDisplayModeChanged(CalendarModeChangedEventArgs e) {
            var eventHandler = _displayModeChanged;
            if (eventHandler != null)
                eventHandler(this, e);
        }

        #endregion

        // engine changed

        // language changed

        #region event internal DayButtonMouseUp

        private MouseButtonEventHandler _dayButtonMouseUp;

        internal event MouseButtonEventHandler DayButtonMouseUp {
            add {
                var buttonEventHandler = _dayButtonMouseUp;
                MouseButtonEventHandler comparand;
                do {
                    comparand = buttonEventHandler;
                    buttonEventHandler = Interlocked.CompareExchange(ref _dayButtonMouseUp, comparand + value, comparand);
                }
                while (buttonEventHandler != comparand);
            }
            remove {
                var buttonEventHandler = _dayButtonMouseUp;
                MouseButtonEventHandler comparand;
                do {
                    comparand = buttonEventHandler;
                    buttonEventHandler = Interlocked.CompareExchange(ref _dayButtonMouseUp, comparand - value, comparand);
                }
                while (buttonEventHandler != comparand);
            }
        }

        internal void OnDayButtonMouseUp(MouseButtonEventArgs e) {
            var buttonEventHandler = _dayButtonMouseUp;
            if (buttonEventHandler == null)
                return;
            buttonEventHandler(this, e);
        }

        #endregion

        #region event internal DayOrMonthPreviewKeyDown

        private RoutedEventHandler _dayOrMonthPreviewKeyDown;

        internal event RoutedEventHandler DayOrMonthPreviewKeyDown {
            add {
                var routedEventHandler = _dayOrMonthPreviewKeyDown;
                RoutedEventHandler comparand;
                do {
                    comparand = routedEventHandler;
                    routedEventHandler = Interlocked.CompareExchange(ref _dayOrMonthPreviewKeyDown, comparand + value, comparand);
                }
                while (routedEventHandler != comparand);
            }
            remove {
                var routedEventHandler = _dayOrMonthPreviewKeyDown;
                RoutedEventHandler comparand;
                do {
                    comparand = routedEventHandler;
                    routedEventHandler = Interlocked.CompareExchange(ref _dayOrMonthPreviewKeyDown, comparand - value, comparand);
                }
                while (routedEventHandler != comparand);
            }
        }

        internal void OnDayOrMonthPreviewKeyDown(RoutedEventArgs e) {
            var routedEventHandler = _dayOrMonthPreviewKeyDown;
            if (routedEventHandler == null)
                return;
            routedEventHandler(this, e);
        }

        #endregion

        // got focus
        #region RoutedEvent OnGotFocus

        private static void OnGotFocus(object sender, RoutedEventArgs e) {
            var calendar = (Calendar)sender;
            if (e.Handled || !Equals(e.OriginalSource, calendar))
                return;
            if (calendar.SelectedDate.HasValue &&
                calendar.Engine.CompareYearMonth(calendar.SelectedDate.Value, calendar.DisplayDateInternal) == 0)
                calendar.FocusDate(calendar.SelectedDate.Value);
            else
                calendar.FocusDate(calendar.DisplayDate);
            e.Handled = true;
        }

        #endregion

        #endregion

        #region overrides

        #region override OnApplyTemplate()

        /// <summary>
        /// Builds the visual tree for the <see cref="T:Kavand.Windows.Controls.Calendar"/> control when a new template is applied.
        /// </summary>
        public override void OnApplyTemplate() {
            if (_viewPresenter != null)
                _viewPresenter.Owner = null;
            base.OnApplyTemplate();

            // _viewPresenter
            _viewPresenter = GetTemplateChild(PartViewPresenterName) as CalendarViewPresenter;
            if (_viewPresenter != null)
                _viewPresenter.Owner = this;

            // 
            if (_previousButton != null)
                _previousButton.Click -= PreviousButton_Click;
            _previousButton = GetTemplateChild(PartPreviousButtonName) as Button;
            if (_previousButton != null) {
                _previousButton.SetBinding(StyleProperty, GetSelfBinding(PreviousButtonStyleProperty));
                _previousButton.SetBinding(ContentControl.ContentProperty, GetSelfBinding(PreviousButtonContentProperty));
                _previousButton.Click += PreviousButton_Click;
            }

            if (_nextButton != null)
                _nextButton.Click -= NextButton_Click;
            _nextButton = GetTemplateChild(PartNextButtonName) as Button;
            if (_nextButton != null) {
                _nextButton.SetBinding(StyleProperty, GetSelfBinding(NextButtonStyleProperty));
                _nextButton.SetBinding(ContentControl.ContentProperty, GetSelfBinding(NextButtonContentProperty));
                _nextButton.Click += NextButton_Click;
            }

            if (_headerButton != null)
                _headerButton.Click -= HeaderButton_Click;
            _headerButton = GetTemplateChild(PartHeaderButtonName) as Button;
            if (_headerButton != null) {
                _headerButton.SetBinding(StyleProperty, GetSelfBinding(HeaderButtonStyleProperty));
                _headerButton.Click += HeaderButton_Click;
            }

            CurrentDate = DisplayDate;
            UpdatePresenter();
        }

        #endregion

        #region override ToString()

        /// <summary>
        /// Provides a text representation of the selected date.
        /// </summary> 
        /// <returns>
        /// A text representation of the selected date, or an empty string if <see cref="P:Kavand.Windows.Controls.Calendar.SelectedDate"/> is null.
        /// </returns>
        public override string ToString() {
            return SelectedDate == null ? string.Empty :
                SelectedDate.Value.ToString(Engine.GetDateFormat(Engine.GetCulture(this)));
        }

        #endregion

        #region override OnCreateAutomationPeer()

        /// <summary>
        /// Returns a <see cref="T:System.Windows.Automation.Peers.CalendarAutomationPeer"/> for use by the  automation infrastructure.
        /// </summary> 
        /// <returns>
        /// A <see cref="T:System.Windows.Automation.Peers.CalendarAutomationPeer"/> for the <see cref="T:Kavand.Windows.Controls.Calendar"/> object.
        /// </returns>
        protected override AutomationPeer OnCreateAutomationPeer() {
            //return (AutomationPeer)new CalendarAutomationPeer(this);
            return null;
        }

        #endregion

        #region override OnKeyDown()

        /// <summary>
        /// Provides class handling for the <see cref="E:System.Windows.UIElement.KeyDown"/> routed event that occurs when the user presses a key while this control has focus.
        /// </summary>
        /// <param name="e">The data for the event. </param>
        protected override void OnKeyDown(KeyEventArgs e) {
            if (!e.Handled)
                e.Handled = ProcessCalendarKey(e);
        }

        #endregion

        #region override OnKeyUp()

        /// <summary>
        /// Provides class handling for the <see cref="E:System.Windows.UIElement.KeyUp"/> routed event that occurs when the user releases a key while this control has focus.
        /// </summary>
        /// <param name="e">The data for the event. </param>
        protected override void OnKeyUp(KeyEventArgs e) {
            if (e.Handled || e.Key != Key.LeftShift && e.Key != Key.RightShift)
                return;
            ProcessShiftKeyUp();
        }

        #endregion

        #endregion

        #region handlers

        // on clear button clicked

        // on next button clicked
        private void NextButton_Click(object sender, RoutedEventArgs e) {
            OnNextClick();
        }

        // on previous button clicked
        private void PreviousButton_Click(object sender, RoutedEventArgs e) {
            OnPreviousClick();
        }

        // on header button clicked
        private void HeaderButton_Click(object sender, RoutedEventArgs e) {
            SetCurrentValue(
                DisplayModeProperty,
                DisplayMode == CalendarMode.Month ? CalendarMode.Year : CalendarMode.Decade);
            FocusDate(DisplayDate);
        }

        // on key down

        // handler: OnNextClick
        internal void OnNextClick() {
            var dateOffset = GetDateOffset(DisplayDate, 1, DisplayMode);
            if (!dateOffset.HasValue)
                return;
            MoveDisplayTo(Engine.GetFirstOfMonth(dateOffset.Value));
        }

        // handler: OnPreviousClick
        internal void OnPreviousClick() {
            var dateOffset = GetDateOffset(DisplayDate, -1, DisplayMode);
            if (!dateOffset.HasValue)
                return;
            MoveDisplayTo(Engine.GetFirstOfMonth(dateOffset.Value));
        }

        // handler: OnDayClick
        internal void OnDayClick(DateTime selectedDate) {
            if (SelectionMode == CalendarSelectionMode.None)
                CurrentDate = selectedDate;
            if (Engine.CompareYearMonth(selectedDate, DisplayDateInternal) != 0) {
                MoveDisplayTo(selectedDate);
            } else {
                UpdatePresenter();
                FocusDate(selectedDate);
            }
        }

        // handler: OnCalendarButtonPressed
        internal void OnCalendarButtonPressed(MonthYearButton b, bool switchDisplayMode) {
            if (!(b.DataContext is DateTime))
                return;
            var requestedDate = (DateTime)b.DataContext;
            var result = new DateTime?();
            var calendarMode = CalendarMode.Month;
            switch (DisplayMode) {
                case CalendarMode.Year:
                    result = Engine.SetYearMonth(DisplayDate, requestedDate);
                    calendarMode = CalendarMode.Month;
                    break;
                case CalendarMode.Decade:
                    result = Engine.SetYear(DisplayDate, requestedDate);
                    calendarMode = CalendarMode.Year;
                    break;
            }
            if (!result.HasValue)
                return;
            DisplayDate = result.Value;
            if (!switchDisplayMode)
                return;
            SetCurrentValue(DisplayModeProperty, calendarMode);
            FocusDate(DisplayMode == CalendarMode.Month ? CurrentDate : DisplayDate);
        }

        #endregion

        #region Keyboard handlers

        private bool ProcessCalendarKey(KeyEventArgs e) {
            if (DisplayMode == CalendarMode.Month) {
                var calendarDayButton = _viewPresenter != null ? _viewPresenter.GetCalendarDayButton(CurrentDate) : null;
                if (Engine.CompareYearMonth(CurrentDate, DisplayDateInternal) != 0 && calendarDayButton != null && !calendarDayButton.IsInactive)
                    return false;
            }
            bool ctrl;
            bool shift;
            KeyboardHelper.GetMetaKeyState(out ctrl, out shift);
            switch (e.Key) {
                case Key.Return:
                case Key.Space:
                    return ProcessEnterKey();
                case Key.Prior:
                    ProcessPageUpKey(shift);
                    return true;
                case Key.Next:
                    ProcessPageDownKey(shift);
                    return true;
                case Key.End:
                    ProcessEndKey(shift);
                    return true;
                case Key.Home:
                    ProcessHomeKey(shift);
                    return true;
                case Key.Left:
                    ProcessLeftKey(shift);
                    return true;
                case Key.Up:
                    ProcessUpKey(ctrl, shift);
                    return true;
                case Key.Right:
                    ProcessRightKey(shift);
                    return true;
                case Key.Down:
                    ProcessDownKey(ctrl, shift);
                    return true;
                default:
                    return false;
            }
        }

        private bool ProcessEnterKey() {
            switch (DisplayMode) {
                case CalendarMode.Year:
                    SetCurrentValue(DisplayModeProperty, CalendarMode.Month);
                    FocusDate(DisplayDate);
                    return true;
                case CalendarMode.Decade:
                    SetCurrentValue(DisplayModeProperty, CalendarMode.Year);
                    FocusDate(DisplayDate);
                    return true;
                default:
                    return false;
            }
        }

        private void ProcessPageUpKey(bool shift) {
            switch (DisplayMode) {
                case CalendarMode.Month:
                    var nonBlackoutDate = _blackoutDates.GetNonBlackoutDate(Engine.AddMonths(CurrentDate, -1), -1);
                    ProcessSelection(shift, nonBlackoutDate);
                    break;
                case CalendarMode.Year:
                    OnSelectedMonthChanged(Engine.AddYears(DisplayDate, -1));
                    break;
                case CalendarMode.Decade:
                    OnSelectedYearChanged(Engine.AddYears(DisplayDate, -10));
                    break;
            }
        }

        private void ProcessPageDownKey(bool shift) {
            switch (DisplayMode) {
                case CalendarMode.Month:
                    var nonBlackoutDate = _blackoutDates.GetNonBlackoutDate(Engine.AddMonths(CurrentDate, 1), 1);
                    ProcessSelection(shift, nonBlackoutDate);
                    break;
                case CalendarMode.Year:
                    OnSelectedMonthChanged(Engine.AddYears(DisplayDate, 1));
                    break;
                case CalendarMode.Decade:
                    OnSelectedYearChanged(Engine.AddYears(DisplayDate, 10));
                    break;
            }
        }

        private void ProcessEndKey(bool shift) {
            switch (DisplayMode) {
                case CalendarMode.Month:
                    var lastSelectedDate = new DateTime?(new DateTime(DisplayDateInternal.Year, DisplayDateInternal.Month, 1));
                    if (Engine.CompareYearMonth(Engine.MaxSupportedDateTime, lastSelectedDate.Value) > 0) {
                        var addMonths = Engine.AddMonths(lastSelectedDate.Value, 1);
                        if (addMonths != null)
                            lastSelectedDate = addMonths.Value;
                        var addDays = Engine.AddDays(lastSelectedDate.Value, -1);
                        if (addDays != null)
                            lastSelectedDate = addDays.Value;
                    } else
                        lastSelectedDate = Engine.MaxSupportedDateTime;
                    ProcessSelection(shift, lastSelectedDate);
                    break;
                case CalendarMode.Year:
                    OnSelectedMonthChanged(new DateTime(DisplayDate.Year, 12, 1));
                    break;
                case CalendarMode.Decade:
                    OnSelectedYearChanged(new DateTime(Engine.GetDecadeEnd(DisplayDate), 1, 1));
                    break;
            }
        }

        private void ProcessHomeKey(bool shift) {
            switch (DisplayMode) {
                case CalendarMode.Month:
                    var lastSelectedDate = new DateTime?(new DateTime(DisplayDateInternal.Year, DisplayDateInternal.Month, 1));
                    ProcessSelection(shift, lastSelectedDate);
                    break;
                case CalendarMode.Year:
                    OnSelectedMonthChanged(new DateTime(DisplayDate.Year, 1, 1));
                    break;
                case CalendarMode.Decade:
                    OnSelectedYearChanged(new DateTime(Engine.GetDecadeStart(DisplayDate), 1, 1));
                    break;
            }
        }

        private void ProcessLeftKey(bool shift) {
            var num = !IsRightToLeft ? -1 : 1;
            switch (DisplayMode) {
                case CalendarMode.Month:
                    var nonBlackoutDate = _blackoutDates.GetNonBlackoutDate(Engine.AddDays(CurrentDate, num), num);
                    ProcessSelection(shift, nonBlackoutDate);
                    break;
                case CalendarMode.Year:
                    OnSelectedMonthChanged(Engine.AddMonths(DisplayDate, num));
                    break;
                case CalendarMode.Decade:
                    OnSelectedYearChanged(Engine.AddYears(DisplayDate, num));
                    break;
            }
        }

        private void ProcessUpKey(bool ctrl, bool shift) {
            switch (DisplayMode) {
                case CalendarMode.Month:
                    if (ctrl) {
                        SetCurrentValue(DisplayModeProperty, CalendarMode.Year);
                        FocusDate(DisplayDate);
                        break;
                    }
                    var nonBlackoutDate = _blackoutDates.GetNonBlackoutDate(Engine.AddDays(CurrentDate, -7), -1);
                    ProcessSelection(shift, nonBlackoutDate);
                    break;
                case CalendarMode.Year:
                    if (ctrl) {
                        SetCurrentValue(DisplayModeProperty, CalendarMode.Decade);
                        FocusDate(DisplayDate);
                        break;
                    }
                    OnSelectedMonthChanged(Engine.AddMonths(DisplayDate, -4));
                    break;
                case CalendarMode.Decade:
                    if (ctrl)
                        break;
                    OnSelectedYearChanged(Engine.AddYears(DisplayDate, -4));
                    break;
            }
        }

        private void ProcessRightKey(bool shift) {
            var num = !IsRightToLeft ? 1 : -1;
            switch (DisplayMode) {
                case CalendarMode.Month:
                    var nonBlackoutDate = _blackoutDates.GetNonBlackoutDate(Engine.AddDays(CurrentDate, num), num);
                    ProcessSelection(shift, nonBlackoutDate);
                    break;
                case CalendarMode.Year:
                    OnSelectedMonthChanged(Engine.AddMonths(DisplayDate, num));
                    break;
                case CalendarMode.Decade:
                    OnSelectedYearChanged(Engine.AddYears(DisplayDate, num));
                    break;
            }
        }

        private void ProcessDownKey(bool ctrl, bool shift) {
            switch (DisplayMode) {
                case CalendarMode.Month:
                    if (ctrl && !shift)
                        break;
                    var nonBlackoutDate = _blackoutDates.GetNonBlackoutDate(Engine.AddDays(CurrentDate, 7), 1);
                    ProcessSelection(shift, nonBlackoutDate);
                    break;
                case CalendarMode.Year:
                    if (ctrl) {
                        SetCurrentValue(DisplayModeProperty, CalendarMode.Month);
                        FocusDate(DisplayDate);
                        break;
                    }
                    OnSelectedMonthChanged(Engine.AddMonths(DisplayDate, 4));
                    break;
                case CalendarMode.Decade:
                    if (ctrl) {
                        SetCurrentValue(DisplayModeProperty, CalendarMode.Year);
                        FocusDate(DisplayDate);
                        break;
                    }
                    OnSelectedYearChanged(Engine.AddYears(DisplayDate, 4));
                    break;
            }
        }

        private void ProcessShiftKeyUp() {
            if (!_isShiftPressed || SelectionMode != CalendarSelectionMode.SingleRange && SelectionMode != CalendarSelectionMode.MultipleRange)
                return;
            AddKeyboardSelection();
            _isShiftPressed = false;
            HoverEnd = null;
            HoverStart = null;
        }

        #region handlers

        private void OnSelectedMonthChanged(DateTime? selectedMonth) {
            if (!selectedMonth.HasValue)
                return;
            SetCurrentValue(DisplayDateProperty, selectedMonth.Value);
            UpdatePresenter();
            FocusDate(selectedMonth.Value);
        }

        private void OnSelectedYearChanged(DateTime? selectedYear) {
            if (!selectedYear.HasValue)
                return;
            SetCurrentValue(DisplayDateProperty, selectedYear.Value);
            UpdatePresenter();
            FocusDate(selectedYear.Value);
        }

        private void AddKeyboardSelection() {
            if (!HoverStart.HasValue)
                return;
            SelectedDates.ClearInternal();
            SelectedDates.AddRange(HoverStart.Value, CurrentDate);
        }

        #endregion

        #endregion

        #region functionalities

        internal void UpdatePresenter() {
            if (_viewPresenter != null)
                switch (DisplayMode) {
                    case CalendarMode.Month:
                        _viewPresenter.UpdateMonthMode();
                        break;
                    case CalendarMode.Year:
                        _viewPresenter.UpdateYearMode();
                        break;
                    case CalendarMode.Decade:
                        _viewPresenter.UpdateDecadeMode();
                        break;
                }
        }

        internal void FocusDate(DateTime date) {
            if (_viewPresenter != null)
                _viewPresenter.FocusDate(date);
        }

        private void MoveDisplayTo(DateTime? date) {
            if (!date.HasValue)
                return;
            var date1 = date.Value.Date;
            switch (DisplayMode) {
                case CalendarMode.Month:
                    SetCurrentValue(DisplayDateProperty, Engine.GetFirstOfMonth(date1));
                    CurrentDate = date1;
                    break;
                case CalendarMode.Year:
                case CalendarMode.Decade:
                    SetCurrentValue(DisplayDateProperty, date1);
                    break;
            }
            UpdatePresenter();
            FocusDate(date1);
        }

        private void ProcessSelection(bool shift, DateTime? lastSelectedDate) {
            if (SelectionMode == CalendarSelectionMode.None && lastSelectedDate.HasValue) {
                OnDayClick(lastSelectedDate.Value);
            } else {
                if (!lastSelectedDate.HasValue || !IsValidKeyboardSelection(this, lastSelectedDate.Value))
                    return;
                if (SelectionMode == CalendarSelectionMode.SingleRange || SelectionMode == CalendarSelectionMode.MultipleRange) {
                    SelectedDates.ClearInternal();
                    if (shift) {
                        _isShiftPressed = true;
                        if (!HoverStart.HasValue)
                            HoverStart = HoverEnd = CurrentDate;
                        if (!BlackoutDates.ContainsAny(DateTime.Compare(HoverStart.Value, lastSelectedDate.Value) >= 0 ? new DateRange(lastSelectedDate.Value, HoverStart.Value) : new DateRange(HoverStart.Value, lastSelectedDate.Value))) {
                            _currentDate = lastSelectedDate;
                            HoverEnd = lastSelectedDate;
                        }
                        OnDayClick(CurrentDate);
                    } else {
                        HoverStart = HoverEnd = CurrentDate = lastSelectedDate.Value;
                        AddKeyboardSelection();
                        OnDayClick(lastSelectedDate.Value);
                    }
                } else {
                    CurrentDate = lastSelectedDate.Value;
                    HoverEnd = null;
                    HoverStart = null;
                    if (SelectedDates.Count > 0)
                        SelectedDates[0] = lastSelectedDate.Value;
                    else
                        SelectedDates.Add(lastSelectedDate.Value);
                    OnDayClick(lastSelectedDate.Value);
                }
                UpdatePresenter();
            }
        }

        #region MonthMode header buttons management

        internal void SetMonthModeHeaderButton() {
            if (_headerButton == null)
                return;
            _headerButton.Content = Engine.GetYearMonthPatternString(DisplayDate, Engine.GetCulture(this));
            _headerButton.IsEnabled = true;
        }

        internal void SetMonthModeNextButton() {
            if (_nextButton == null)
                return;
            var dateTime = Engine.GetFirstOfMonth(DisplayDate);
            if (Engine.CompareYearMonth(dateTime, Engine.MaxSupportedDateTime) == 0)
                _nextButton.IsEnabled = false;
            else {
                var addMonths = Engine.AddMonths(dateTime, 1);
                if (addMonths != null)
                    _nextButton.IsEnabled = Engine.CompareDays(DisplayDateEndInternal, addMonths.Value) > -1;
                else
                    _nextButton.IsEnabled = false;
            }
        }

        internal void SetMonthModePreviousButton() {
            if (_previousButton == null)
                return;
            _previousButton.IsEnabled = Engine.CompareDays(DisplayDateStartInternal, Engine.GetFirstOfMonth(DisplayDate)) < 0;
        }

        #endregion

        #region YearMode header buttons management

        internal void SetYearModeHeaderButton() {
            if (_headerButton == null)
                return;
            _headerButton.IsEnabled = true;
            _headerButton.Content = Engine.GetYearString(DisplayDate, Engine.GetCulture(this));
        }

        internal void SetYearModeNextButton() {
            if (_nextButton == null)
                return;
            _nextButton.IsEnabled = DisplayDateEndInternal.Year != DisplayDate.Year;
        }

        internal void SetYearModePreviousButton() {
            if (_previousButton == null)
                return;
            _previousButton.IsEnabled = DisplayDateStartInternal.Year != DisplayDate.Year;
        }

        #endregion

        #region DecadeMode header buttons management

        internal void SetDecadeModeHeaderButton(DateTime decadeStart) {
            if (_headerButton == null)
                return;
            _headerButton.Content = Engine.GetDecadeRangeString(decadeStart, IsRightToLeft, Engine.GetCulture(this));
            _headerButton.IsEnabled = false;
        }

        internal void SetDecadeModeNextButton(int decadeEnd) {
            if (_nextButton == null)
                return;
            _nextButton.IsEnabled = DisplayDateEndInternal.Year > decadeEnd;
        }

        internal void SetDecadeModePreviousButton(int decade) {
            if (_previousButton == null)
                return;
            _previousButton.IsEnabled = decade > DisplayDateStartInternal.Year;
        }

        #endregion

        #endregion

        #region helpers

        private DateTime? GetDateOffset(DateTime date, int offset, CalendarMode displayMode) {
            var result = new DateTime?();
            switch (displayMode) {
                case CalendarMode.Month:
                    result = Engine.AddMonths(date, offset);
                    break;
                case CalendarMode.Year:
                    result = Engine.AddYears(date, offset);
                    break;
                case CalendarMode.Decade:
                    result = Engine.AddYears(DisplayDate, offset * 10);
                    break;
            }
            return result;
        }

        private BindingBase GetSelfBinding(DependencyProperty sourceProperty) {
            return new Binding(sourceProperty.Name) {
                Source = this
            };
        }

        #endregion

        #region validation helpers

        internal static bool IsValidDateSelection(Calendar cal, object value) {
            return value == null || !cal.BlackoutDates.Contains((DateTime)value);
        }

        private static bool IsValidKeyboardSelection(Calendar cal, object value) {
            if (value == null)
                return true;
            if (cal.BlackoutDates.Contains((DateTime)value) ||
                DateTime.Compare((DateTime)value, cal.DisplayDateStartInternal) < 0)
                return false;
            return DateTime.Compare((DateTime)value, cal.DisplayDateEndInternal) <= 0;
        }

        #endregion

        #region other members

        internal DayButton FindDayButtonFromDay(DateTime day) {
            if (_viewPresenter == null)
                return null;
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var calendarDayButton in _viewPresenter.GetCalendarDayButtons()) {
                // ReSharper restore LoopCanBeConvertedToQuery
                if (calendarDayButton.DataContext is DateTime
                    && Engine.CompareDays((DateTime)calendarDayButton.DataContext, day) == 0)
                    return calendarDayButton;
            }
            return null;
        }

        internal void OnSelectedDatesCollectionChanged(SelectionChangedEventArgs e) {
            if (!IsSelectionChanged(e))
                return;
            if (AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected) || AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementAddedToSelection) || AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection)) {
                //CalendarAutomationPeer calendarAutomationPeer = UIElementAutomationPeer.FromElement((UIElement)this) as CalendarAutomationPeer;
                //if (calendarAutomationPeer != null)
                //    calendarAutomationPeer.RaiseSelectionEvents(e);
            }
            CoerceFromSelection();
            OnSelectedDatesChanged(e);
        }

        private void CoerceFromSelection() {
            CoerceValue(DisplayDateStartProperty);
            CoerceValue(DisplayDateEndProperty);
            CoerceValue(DisplayDateProperty);
        }

        private static bool IsSelectionChanged(SelectionChangedEventArgs e) {
            if (e.AddedItems.Count != e.RemovedItems.Count)
                return true;
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (DateTime dateTime in e.AddedItems) {
                // ReSharper restore LoopCanBeConvertedToQuery
                if (!e.RemovedItems.Contains(dateTime))
                    return true;
            }
            return false;
        }

        #endregion

        public void NotifyOfCultureChanged() {
            InvalidateProperty(DisplayModeProperty);
            UpdatePresenter();
        }
    }
}