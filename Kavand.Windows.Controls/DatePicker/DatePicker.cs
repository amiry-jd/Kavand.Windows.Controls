using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace Kavand.Windows.Controls {

    /// <summary> 
    /// Represents a control that allows the user to select a date.
    /// </summary> 
    [TemplateVisualState(GroupName = VisualStates.GroupCommon, Name = VisualStates.StateNormal)]
    [TemplateVisualState(GroupName = VisualStates.GroupCommon, Name = VisualStates.StateMouseOver)]
    [TemplateVisualState(GroupName = VisualStates.GroupCommon, Name = VisualStates.StateDisabled)]

    [TemplateVisualState(GroupName = VisualStates.GroupFocus, Name = VisualStates.StateFocused)]
    [TemplateVisualState(GroupName = VisualStates.GroupFocus, Name = VisualStates.StateFocusedDropDown)]
    [TemplateVisualState(GroupName = VisualStates.GroupFocus, Name = VisualStates.StateUnfocused)]

    [TemplateVisualState(GroupName = VisualStates.GroupValidation, Name = VisualStates.StateValid)]
    [TemplateVisualState(GroupName = VisualStates.GroupValidation, Name = VisualStates.StateInvalidFocused)]
    [TemplateVisualState(GroupName = VisualStates.GroupValidation, Name = VisualStates.StateInvalidUnfocused)]

    [TemplatePart(Name = PartRootName, Type = typeof(Grid))]
    [TemplatePart(Name = PartTextBoxName, Type = typeof(DatePickerTextBox))]
    [TemplatePart(Name = PartDropDownButtonName, Type = typeof(Button))]
    [TemplatePart(Name = PartPopupName, Type = typeof(Popup))]
    [TemplatePart(Name = PartCalendarName, Type = typeof(Calendar))]
    [TemplatePart(Name = PartTodayButtonName, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartClearButtonName, Type = typeof(ButtonBase))]
    public class DatePicker : Control, IVisualStateUpdateable {

        public static readonly Type Typeof = typeof(DatePicker);

        #region Constants

        private const string PartRootName = "PART_Root";
        private const string PartTextBoxName = "PART_TextBox";
        private const string PartDropDownButtonName = "PART_DropDownButton";
        private const string PartPopupName = "PART_Popup";
        private const string PartCalendarName = "PART_Calendar";
        private const string PartTodayButtonName = "PART_TodayButton";
        private const string PartClearButtonName = "PART_ClearButton";

        #endregion

        #region Data

        private Calendar _calendar;
        private ButtonBase _dropDownButton;
        private Popup _popUp;
        private DatePickerTextBox _textBox;
        private ButtonBase _todayButton;
        private ButtonBase _clearButton;
        private string _defaultText;
        private bool _disablePopupReopen;
        private IDictionary<DependencyProperty, bool> _isHandlerSuspended;
        private DateTime? _originalSelectedDate;
        private readonly DependencyPropertyDescriptor _isMouseOverPropertyDescriptor;
        private readonly DependencyPropertyDescriptor _isFocusedPropertyDescriptor;
        private readonly DispatcherTimer _timer;

        #endregion

        #region Public Events

        public static readonly RoutedEvent SelectedDateChangedEvent
            = EventManager.RegisterRoutedEvent("SelectedDateChanged", RoutingStrategy.Direct,
                typeof(EventHandler<System.Windows.Controls.SelectionChangedEventArgs>), Typeof);

        /// <summary> 
        /// Occurs when a date is selected.
        /// </summary>
        public event EventHandler<System.Windows.Controls.SelectionChangedEventArgs> SelectedDateChanged {
            add { AddHandler(SelectedDateChangedEvent, value); }
            remove { RemoveHandler(SelectedDateChangedEvent, value); }
        }

        protected virtual void OnSelectedDateChanged(System.Windows.Controls.SelectionChangedEventArgs e) {
            RaiseEvent(e);
        }

        /// <summary>
        /// Occurs when the drop-down Calendar is closed. 
        /// </summary>
        public event RoutedEventHandler CalendarClosed;

        protected virtual void OnCalendarClosed(RoutedEventArgs e) {
            var handler = CalendarClosed;
            if (null != handler)
                handler(this, e);
        }

        /// <summary>
        /// Occurs when the drop-down Calendar is opened. 
        /// </summary>
        public event RoutedEventHandler CalendarOpened;

        protected virtual void OnCalendarOpened(RoutedEventArgs e) {
            var handler = CalendarOpened;
            if (null != handler)
                handler(this, e);
        }

        /// <summary> 
        /// Occurs when text entered into the DatePicker cannot be parsed or the Date is not valid to be selected.
        /// </summary> 
        public event EventHandler<DatePickerDateValidationErrorEventArgs> DateValidationError;

        /// <summary>
        /// Raises the DateValidationError event.
        /// </summary>
        /// <param name="e">A DatePickerDateValidationErrorEventArgs that contains the event data.</param> 
        protected virtual void OnDateValidationError(DatePickerDateValidationErrorEventArgs e) {
            var handler = DateValidationError;
            if (handler != null)
                handler(this, e);
        }

        #endregion

        #region .ctor

        /// <summary>
        /// Static constructor
        /// </summary> 
        static DatePicker() {
            DefaultStyleKeyProperty.OverrideMetadata(Typeof, new FrameworkPropertyMetadata(Typeof));
            EventManager.RegisterClassHandler(Typeof, Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(OnPreviewMouseDown));
            EventManager.RegisterClassHandler(Typeof, GotFocusEvent, new RoutedEventHandler(OnGotFocus));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(Typeof, new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
            KeyboardNavigation.IsTabStopProperty.OverrideMetadata(Typeof, new FrameworkPropertyMetadata(false));
            IsEnabledProperty.OverrideMetadata(Typeof, new UIPropertyMetadata(OnIsEnabledChanged));
            //EventManager.RegisterClassHandler(Typeof, Mouse.MouseEnterEvent, new MouseEventHandler(MouseEnterHandler));
            //EventManager.RegisterClassHandler(Typeof, Mouse.MouseLeaveEvent, new MouseEventHandler(MouseLeaveHandler));
        }

        /// <summary>
        /// Initializes a new instance of the DatePicker class. 
        /// </summary> 
        public DatePicker() {
            FocusManager.SetIsFocusScope(this, false);
            _defaultText = string.Empty;
            _timer = new DispatcherTimer();
            _timer.Tick += RenewTodayButtonContent;

            // Binding to FirstDayOfWeek and DisplayDate wont work 
            SetCurrentValue(FirstDayOfWeekProperty, CalendarEngine.GetCurrentCultureFirstDayOfWeek());
            DisplayDate = DateTime.Today;
            _isMouseOverPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(IsMouseOverProperty, typeof(UIElement));
            if (_isMouseOverPropertyDescriptor != null)
                _isMouseOverPropertyDescriptor.AddValueChanged(this, VisualStatePropertyChangedHandler);
            _isFocusedPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(IsFocusedProperty, typeof(UIElement));
            if (_isFocusedPropertyDescriptor != null)
                _isFocusedPropertyDescriptor.AddValueChanged(this, VisualStatePropertyChangedHandler);
        }

        ~DatePicker() {
            if (_isMouseOverPropertyDescriptor != null)
                _isMouseOverPropertyDescriptor.RemoveValueChanged(this, VisualStatePropertyChangedHandler);
            if (_isFocusedPropertyDescriptor != null)
                _isFocusedPropertyDescriptor.RemoveValueChanged(this, VisualStatePropertyChangedHandler);
        }

        #endregion

        #region Public properties

        #region MaxDropDownHeight

        /// <summary>
        ///     DependencyProperty for MaxDropDownHeight 
        /// </summary>
        public static readonly DependencyProperty MaxDropDownHeightProperty
            = DependencyProperty.Register("MaxDropDownHeight", typeof(double), Typeof,
            new FrameworkPropertyMetadata(SystemParameters.PrimaryScreenHeight / 3, VisualStates.OnVisualStatePropertyChanged));

        /// <summary>
        ///     The maximum height of the popup
        /// </summary> 
        [Bindable(true), Category("Layout")]
        [TypeConverter(typeof(LengthConverter))]
        public double MaxDropDownHeight {
            get { return (double)GetValue(MaxDropDownHeightProperty); }
            set { SetValue(MaxDropDownHeightProperty, value); }
        }

        #endregion

        #region Engine

        public static readonly DependencyProperty EngineProperty
            = DependencyProperty.Register("Engine", typeof(CalendarEngine), Typeof,
                new PropertyMetadata(new GregorianCalendarEngine(), EngineChangedCallback), ValidateEngineCallback);

        private static void EngineChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            //((Calendar)d).UpdatePresenter();
        }

        private static bool ValidateEngineCallback(object value) {
            return value is CalendarEngine;
        }

        public CalendarEngine Engine {
            get { return (CalendarEngine)GetValue(EngineProperty); }
            set { SetValue(EngineProperty, value); }
        }

        #endregion

        #region BlackoutDates

        /// <summary> 
        /// Gets the days that are not selectable.
        /// </summary> 
        public BlackoutDatesCollection BlackoutDates {
            get { return _calendar.BlackoutDates; }
        }

        #endregion

        #region CalendarStyle

        /// <summary> 
        /// Gets or sets the style that is used when rendering the calendar.
        /// </summary> 
        public Style CalendarStyle {
            get { return (Style)GetValue(CalendarStyleProperty); }
            set { SetValue(CalendarStyleProperty, value); }
        }

        /// <summary> 
        /// Identifies the CalendarStyle dependency property.
        /// </summary> 
        public static readonly DependencyProperty CalendarStyleProperty =
            DependencyProperty.Register("CalendarStyle", typeof(Style), Typeof);

        #endregion

        #region DisplayDate

        /// <summary>
        /// Gets or sets the date to display.
        /// </summary> 
        ///
        public DateTime DisplayDate {
            get { return (DateTime)GetValue(DisplayDateProperty); }
            set { SetValue(DisplayDateProperty, value); }
        }

        /// <summary>
        /// Identifies the DisplayDate dependency property. 
        /// </summary>
        public static readonly DependencyProperty DisplayDateProperty =
            DependencyProperty.Register("DisplayDate", typeof(DateTime), Typeof,
                new FrameworkPropertyMetadata(DateTime.Now,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, CoerceDisplayDate));

        private static object CoerceDisplayDate(DependencyObject d, object value) {
            var dp = d as DatePicker;
            Debug.Assert(dp != null);
            if (dp._calendar == null)
                return value;
            // We set _calendar.DisplayDate in order to get _calendar to compute the coerced value
            dp._calendar.DisplayDate = (DateTime)value;
            return dp._calendar.DisplayDate;
        }

        #endregion

        #region DisplayDateEnd

        /// <summary>
        /// Gets or sets the last date to be displayed. 
        /// </summary>
        ///
        public DateTime? DisplayDateEnd {
            get { return (DateTime?)GetValue(DisplayDateEndProperty); }
            set { SetValue(DisplayDateEndProperty, value); }
        }

        /// <summary> 
        /// Identifies the DisplayDateEnd dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayDateEndProperty =
            DependencyProperty.Register("DisplayDateEnd", typeof(DateTime?), Typeof,
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnDisplayDateEndChanged, CoerceDisplayDateEnd));

        /// <summary>
        /// DisplayDateEndProperty property changed handler.
        /// </summary>
        /// <param name="d">DatePicker that changed its DisplayDateEnd.</param> 
        /// <param name="e">DependencyPropertyChangedEventArgs.</param>
        private static void OnDisplayDateEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var dp = d as DatePicker;
            Debug.Assert(dp != null);
            dp.CoerceValue(DisplayDateProperty);
        }

        private static object CoerceDisplayDateEnd(DependencyObject d, object value) {
            var dp = d as DatePicker;
            Debug.Assert(dp != null);
            if (dp._calendar == null)
                return value;
            // We set _calendar.DisplayDateEnd in order to get _calendar to compute the coerced value 
            dp._calendar.DisplayDateEnd = (DateTime?)value;
            return dp._calendar.DisplayDateEnd;
        }

        #endregion

        #region DisplayDateStart

        /// <summary> 
        /// Gets or sets the first date to be displayed.
        /// </summary>
        ///
        public DateTime? DisplayDateStart {
            get { return (DateTime?)GetValue(DisplayDateStartProperty); }
            set { SetValue(DisplayDateStartProperty, value); }
        }

        /// <summary>
        /// Identifies the DisplayDateStart dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayDateStartProperty =
            DependencyProperty.Register("DisplayDateStart", typeof(DateTime?), Typeof,
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnDisplayDateStartChanged, CoerceDisplayDateStart));

        /// <summary>
        /// DisplayDateStartProperty property changed handler.
        /// </summary> 
        /// <param name="d">DatePicker that changed its DisplayDateStart.</param>
        /// <param name="e">DependencyPropertyChangedEventArgs.</param> 
        private static void OnDisplayDateStartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var dp = d as DatePicker;
            Debug.Assert(dp != null);
            dp.CoerceValue(DisplayDateEndProperty);
            dp.CoerceValue(DisplayDateProperty);
        }

        private static object CoerceDisplayDateStart(DependencyObject d, object value) {
            var dp = d as DatePicker;
            Debug.Assert(dp != null);
            if (dp._calendar == null)
                return value;
            // We set _calendar.DisplayDateStart in order to get _calendar to compute the coerced value
            dp._calendar.DisplayDateStart = (DateTime?)value;
            return dp._calendar.DisplayDateStart;
        }

        #endregion

        #region FirstDayOfWeek

        /// <summary>
        /// Gets or sets the day that is considered the beginning of the week.
        /// </summary> 
        public DayOfWeek FirstDayOfWeek {
            get { return (DayOfWeek)GetValue(FirstDayOfWeekProperty); }
            set { SetValue(FirstDayOfWeekProperty, value); }
        }

        /// <summary>
        /// Identifies the FirstDayOfWeek dependency property.
        /// </summary> 
        public static readonly DependencyProperty FirstDayOfWeekProperty =
            DependencyProperty.Register("FirstDayOfWeek", typeof(DayOfWeek), Typeof, null, Calendar.IsValidDayOfWeek);

        #endregion

        #region IsDropDownOpen

        /// <summary>
        /// Gets or sets a value that indicates whether the drop-down Calendar is open or closed. 
        /// </summary>
        public bool IsDropDownOpen {
            get { return (bool)GetValue(IsDropDownOpenProperty); }
            set { SetValue(IsDropDownOpenProperty, value); }
        }

        /// <summary>
        /// Identifies the IsDropDownOpen dependency property. 
        /// </summary>
        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register("IsDropDownOpen", typeof(bool), Typeof,
                new FrameworkPropertyMetadata(false,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIsDropDownOpenChanged, OnCoerceIsDropDownOpen));

        private static object OnCoerceIsDropDownOpen(DependencyObject d, object baseValue) {
            var dp = d as DatePicker;
            Debug.Assert(dp != null);
            if (!dp.IsEnabled)
                return false;
            return baseValue;
        }

        /// <summary> 
        /// IsDropDownOpenProperty property changed handler.
        /// </summary> 
        /// <param name="d">DatePicker that changed its IsDropDownOpen.</param> 
        /// <param name="e">DependencyPropertyChangedEventArgs.</param>
        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var dp = d as DatePicker;
            Debug.Assert(dp != null);
            var newValue = (bool)e.NewValue;
            if (dp._popUp == null || dp._popUp.IsOpen == newValue)
                return;
            dp.UpdateVisualState();
            dp._popUp.IsOpen = newValue;
            if (!newValue)
                return;
            dp._originalSelectedDate = dp.SelectedDate;
            // When the popup is opened set focus to the DisplayDate button. 
            // Do this asynchronously because the IsDropDownOpen could
            // have been set even before the template for the DatePicker is 
            // applied. And this would mean that the visuals wouldn't be available yet. 
            dp.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)(() => {
                if (dp._calendar != null)
                    dp._calendar.Focus();
            }));
        }

        #endregion

        #region IsTodayHighlighted

        /// <summary>
        /// Gets or sets a value that indicates whether the current date will be highlighted. 
        /// </summary> 
        public bool IsTodayHighlighted {
            get { return (bool)GetValue(IsTodayHighlightedProperty); }
            set { SetValue(IsTodayHighlightedProperty, value); }
        }

        /// <summary>
        /// Identifies the IsTodayHighlighted dependency property. 
        /// </summary> 
        public static readonly DependencyProperty IsTodayHighlightedProperty =
            DependencyProperty.Register("IsTodayHighlighted", typeof(bool), Typeof);

        #endregion

        #region TodayButtonStyle

        public static readonly DependencyProperty TodayButtonStyleProperty
            = DependencyProperty.Register("TodayButtonStyle", typeof(Style), Typeof);

        public Style TodayButtonStyle {
            get { return (Style)GetValue(TodayButtonStyleProperty); }
            set { SetValue(TodayButtonStyleProperty, value); }
        }

        #endregion

        #region TodayButtonContent

        public static readonly DependencyProperty TodayButtonContentProperty
            = DependencyProperty.Register("TodayButtonContent", typeof(object), Typeof,
                new FrameworkPropertyMetadata(null, null, CoerceTodayButtonContent));

        public object TodayButtonContent {
            get { return GetValue(TodayButtonContentProperty); }
            set { SetValue(TodayButtonContentProperty, value); }
        }

        private static object CoerceTodayButtonContent(DependencyObject d, object basevalue) {
            var datePicker = (DatePicker)d;
            VerifyTodayButtonContentWithAutoMode(basevalue, datePicker.HasAutoTodayButtonContent);
            return basevalue;
        }

        #endregion

        #region HasAutoTodayButtonContent

        public static readonly DependencyProperty HasAutoTodayButtonContentProperty
            = DependencyProperty.Register("HasAutoTodayButtonContent", BooleanBoxes.Typeof, Typeof,
                new FrameworkPropertyMetadata(BooleanBoxes.TrueBox,
                    OnHasAutoTodayButtonContentChanged,
                    CoerceHasAutoTodayButtonContent));

        public bool HasAutoTodayButtonContent {
            get { return (bool)GetValue(HasAutoTodayButtonContentProperty); }
            set { SetValue(HasAutoTodayButtonContentProperty, value); }
        }

        private static object CoerceHasAutoTodayButtonContent(DependencyObject d, object basevalue) {
            var datePicker = (DatePicker)d;
            VerifyTodayButtonContentWithAutoMode(datePicker.TodayButtonContent, (bool)basevalue);
            return basevalue;
        }

        private static void OnHasAutoTodayButtonContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((DatePicker)d).SetTodayButtonContent();
        }

        #endregion

// ReSharper disable UnusedParameter.Local
        private static void VerifyTodayButtonContentWithAutoMode(object content, bool auto) {
            if (auto && content != null)
                throw new InvalidOperationException("Cannot set a content for today button, and also HasAutoTodayButtonContent = true at the same time");
        }
// ReSharper restore UnusedParameter.Local

        #region ClearButtonStyle

        public static readonly DependencyProperty ClearButtonStyleProperty
            = DependencyProperty.Register("ClearButtonStyle", typeof(Style), Typeof);

        public Style ClearButtonStyle {
            get { return (Style)GetValue(ClearButtonStyleProperty); }
            set { SetValue(ClearButtonStyleProperty, value); }
        }

        #endregion

        #region ClearButtonContent

        public static readonly DependencyProperty ClearButtonContentProperty
            = DependencyProperty.Register("ClearButtonContent", typeof(object), Typeof);

        public object ClearButtonContent {
            get { return GetValue(ClearButtonContentProperty); }
            set { SetValue(ClearButtonContentProperty, value); }
        }

        #endregion

        #region SelectedDate

        /// <summary> 
        /// Gets or sets the currently selected date.
        /// </summary>
        ///
        public DateTime? SelectedDate {
            get { return (DateTime?)GetValue(SelectedDateProperty); }
            set { SetValue(SelectedDateProperty, value); }
        }

        /// <summary>
        /// Identifies the SelectedDate dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register("SelectedDate", typeof(DateTime?), Typeof,
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedDateChanged, CoerceSelectedDate));

        /// <summary>
        /// SelectedDateProperty property changed handler.
        /// </summary> 
        /// <param name="d">DatePicker that changed its SelectedDate.</param>
        /// <param name="e">DependencyPropertyChangedEventArgs.</param> 
        private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var dp = d as DatePicker;
            Debug.Assert(dp != null);
            var addedItems = new Collection<DateTime>();
            var removedItems = new Collection<DateTime>();
            dp.CoerceValue(DisplayDateStartProperty);
            dp.CoerceValue(DisplayDateEndProperty);
            dp.CoerceValue(DisplayDateProperty);
            var addedDate = (DateTime?)e.NewValue;
            var removedDate = (DateTime?)e.OldValue;
            if (dp.SelectedDate.HasValue) {
                var day = dp.SelectedDate.Value;
                dp.SetTextInternal(dp.DateTimeToString(day));
                // When DatePickerDisplayDateFlag is TRUE, the SelectedDate change is coming from the Calendar UI itself,
                // so, we shouldn't change the DisplayDate since it will automatically be changed by the Calendar
                if ((day.Month != dp.DisplayDate.Month || day.Year != dp.DisplayDate.Year) &&
                    !dp._calendar.DatePickerDisplayDateFlag)
                    dp.SetCurrentValue(DisplayDateProperty, day);
                dp._calendar.DatePickerDisplayDateFlag = false;
            } else
                dp.SetWaterMarkText();
            if (addedDate.HasValue)
                addedItems.Add(addedDate.Value);
            if (removedDate.HasValue)
                removedItems.Add(removedDate.Value);
            dp.OnSelectedDateChanged(new SelectionChangedEventArgs(SelectedDateChangedEvent, removedItems, addedItems));
            /*
            DatePickerAutomationPeer peer = UIElementAutomationPeer.FromElement(dp) as DatePickerAutomationPeer;
            // Raise the propetyChangeEvent for Value if Automation Peer exist
            if (peer != null) {
                string addedDateString = addedDate.HasValue ? dp.DateTimeToString(addedDate.Value) : "";
                string removedDateString = removedDate.HasValue ? dp.DateTimeToString(removedDate.Value) : "";
                peer.RaiseValuePropertyChangedEvent(removedDateString, addedDateString);
            }
            */
        }

        private static object CoerceSelectedDate(DependencyObject d, object value) {
            var dp = (DatePicker)d;
            if (dp._calendar == null)
                return value;
            // We set _calendar.SelectedDate in order to get _calendar to compute the coerced value 
            dp._calendar.SelectedDate = (DateTime?)value;
            return dp._calendar.SelectedDate;
        }

        #endregion

        #region SelectedDateFormat

        /// <summary> 
        /// Gets or sets the format that is used to display the selected date. 
        /// </summary>
        public DatePickerFormat SelectedDateFormat {
            get { return (DatePickerFormat)GetValue(SelectedDateFormatProperty); }
            set { SetValue(SelectedDateFormatProperty, value); }
        }

        /// <summary> 
        /// Identifies the SelectedDateFormat dependency property. 
        /// </summary>
        public static readonly DependencyProperty SelectedDateFormatProperty =
            DependencyProperty.Register("SelectedDateFormat", typeof(DatePickerFormat), Typeof,
                new FrameworkPropertyMetadata(DatePickerFormat.Long,
                    OnSelectedDateFormatChanged), IsValidSelectedDateFormat);

        /// <summary>
        /// SelectedDateFormatProperty property changed handler. 
        /// </summary>
        /// <param name="d">DatePicker that changed its SelectedDateFormat.</param>
        /// <param name="e">DependencyPropertyChangedEventArgs.</param>
        private static void OnSelectedDateFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var dp = d as DatePicker;
            Debug.Assert(dp != null);
            if (dp._textBox == null)
                return;
            // Update DatePickerTextBox.Text
            if (string.IsNullOrEmpty(dp._textBox.Text)) {
                dp.SetWaterMarkText();
                return;
            }
            var date = dp.ParseText(dp._textBox.Text);
            if (date != null)
                dp.SetTextInternal(dp.DateTimeToString((DateTime)date));
        }

        private static bool IsValidSelectedDateFormat(object value) {
            var format = (DatePickerFormat)value;
            return format == DatePickerFormat.Long
                || format == DatePickerFormat.Short;
        }

        #endregion

        #region Text

        /// <summary>
        /// Gets or sets the text that is displayed by the DatePicker. 
        /// </summary> 
        public string Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// Identifies the Text dependency property. 
        /// </summary> 
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), Typeof,
                new FrameworkPropertyMetadata(string.Empty, OnTextChanged));

        /// <summary> 
        /// TextProperty property changed handler. 
        /// </summary>
        /// <param name="d">DatePicker that changed its Text.</param> 
        /// <param name="e">DependencyPropertyChangedEventArgs.</param>
        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var dp = d as DatePicker;
            Debug.Assert(dp != null);
            if (dp.IsHandlerSuspended(TextProperty))
                return;
            var newValue = e.NewValue as string;
            if (newValue == null) {
                dp.SetValueNoCallback(SelectedDateProperty, null);
                return;
            }
            if (dp._textBox != null)
                dp._textBox.Text = newValue;
            else
                dp._defaultText = newValue;
            dp.SetSelectedDate();
        }


        /// <summary>
        /// Sets the local Text property without breaking bindings 
        /// </summary> 
        /// <param name="value"></param>
        private void SetTextInternal(string value) {
            SetCurrentValue(TextProperty, value);
        }

        #endregion

        #region IsMouseOver property

        /// <summary>
        ///     The key needed set the read-only IsMouseOverProperty property.
        /// </summary> 
        //internal static readonly DependencyPropertyKey IsMouseOverPropertyKey =
        //    DependencyProperty.RegisterReadOnly("IsMouseOver",typeof(bool),Typeof,
        //        new PropertyMetadata(BooleanBoxes.FalseBox, IsMouseOverChangedCallback));

        //private static void IsMouseOverChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        //    ((DatePicker)d).UpdateVisualState();
        //}

        /// <summary> 
        ///     The dependency property for the IsMouseOver property.
        /// </summary> 
        //public new static readonly DependencyProperty IsMouseOverProperty =
        //    IsMouseOverPropertyKey.DependencyProperty;

        //public new bool IsMouseOver {
        //    get { return (bool)GetValue(IsMouseOverProperty); }
        //    internal set { SetValue(IsMouseOverPropertyKey, value); }
        //}

        //private static void MouseEnterHandler(object sender, MouseEventArgs e) {
        //    ((DatePicker)sender).IsMouseOver = true;
        //}

        //private static void MouseLeaveHandler(object sender, MouseEventArgs e) {
        //    ((DatePicker)sender).IsMouseOver = false;
        //}

        #endregion

        #endregion

        #region DependencyProperty 's override metadatas

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var dp = d as DatePicker;
            Debug.Assert(dp != null);
            dp.CoerceValue(IsDropDownOpenProperty);
            VisualStates.OnVisualStatePropertyChanged(d, e);
        }

        private static void VisualStatePropertyChangedHandler(object sender, EventArgs e) {
            ((DatePicker)sender).UpdateVisualState();
        }

        protected override void OnIsKeyboardFocusedChanged(DependencyPropertyChangedEventArgs e) {
            base.OnIsKeyboardFocusedChanged(e);
            UpdateVisualState();
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e) {
            base.OnIsKeyboardFocusWithinChanged(e);
            UpdateVisualState();
        }

        private static void OnPreviewMouseDown(object sender, MouseButtonEventArgs e) {
            var datePicker = (DatePicker)sender;
            var originalSource = e.OriginalSource as DependencyObject;
            if (Equals(datePicker._textBox, originalSource) || Equals(datePicker._dropDownButton, originalSource))
                return;
            if (GetIsFocusedAnyWay(datePicker))
                return;
            if (!datePicker.Focus())
                datePicker._textBox.Focus();
        }

        #endregion

        #region Internal Properties

        internal Calendar Calendar { get { return _calendar; } }
        internal TextBox TextBox { get { return _textBox; } }

        #endregion Internal Properties

        #region Public Methods

        /// <summary>
        /// Builds the visual tree for the DatePicker control when a new template is applied. 
        /// </summary>
        public override void OnApplyTemplate() {
            ReleaseCalendar();
            if (_popUp != null) {
                _popUp.RemoveHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(PopUp_PreviewMouseLeftButtonDown));
                _popUp.Opened -= PopUp_Opened;
                _popUp.Closed -= PopUp_Closed;
                _popUp.Child = null;
            }

            if (_dropDownButton != null) {
                _dropDownButton.Click -= DropDownButton_Click;
                _dropDownButton.RemoveHandler(MouseLeaveEvent, new MouseEventHandler(DropDownButton_MouseLeave));
            }

            if (_todayButton != null) {
                _todayButton.Click -= TodayButton_Click;
                BindingOperations.ClearAllBindings(_todayButton);
            }

            if (_clearButton != null) {
                _clearButton.Click -= ClearButton_Click;
                BindingOperations.ClearAllBindings(_clearButton);
            }

            if (_textBox != null) {
                FocusManager.SetIsFocusScope(_textBox, false);
                _textBox.Focusable = true;
                _textBox.RemoveHandler(KeyDownEvent, new KeyEventHandler(TextBox_KeyDown));
                _textBox.RemoveHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(TextBox_TextChanged));
                _textBox.RemoveHandler(LostFocusEvent, new RoutedEventHandler(TextBox_LostFocus));
            }

            base.OnApplyTemplate();

            _calendar = GetTemplateChild(PartCalendarName) as Calendar;
            InitializeCalendar();

            _popUp = GetTemplateChild(PartPopupName) as Popup;

            if (_popUp != null) {
                _popUp.AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(PopUp_PreviewMouseLeftButtonDown));
                _popUp.Opened += PopUp_Opened;
                _popUp.Closed += PopUp_Closed;
                // While we have a calendar part in control's template ,this line is no longer necessary
                // _popUp.Child = _calendar;
                if (IsDropDownOpen)
                    _popUp.IsOpen = true;
            }

            _dropDownButton = GetTemplateChild(PartDropDownButtonName) as Button;
            if (_dropDownButton != null) {
                _dropDownButton.Click += DropDownButton_Click;
                _dropDownButton.AddHandler(MouseLeaveEvent, new MouseEventHandler(DropDownButton_MouseLeave), true);
                // If the user does not provide a Content value in template, we 
                // provide a helper text that can be used in Accessibility. This
                // text is not shown on the UI, just used for Accessibility purposes
                if (_dropDownButton.Content == null)
                    _dropDownButton.Content = "Select";
            }

            _todayButton = GetTemplateChild(PartTodayButtonName) as ButtonBase;
            if (_todayButton != null) {
                _todayButton.Click += TodayButton_Click;
                _todayButton.SetBinding(StyleProperty, GetSelfBinding(TodayButtonStyleProperty));
                SetTodayButtonContent();
            }

            _clearButton = GetTemplateChild(PartClearButtonName) as ButtonBase;
            if (_clearButton != null) {
                _clearButton.Click += ClearButton_Click;
                _clearButton.SetBinding(StyleProperty, GetSelfBinding(ClearButtonStyleProperty));
                _clearButton.SetBinding(ContentControl.ContentProperty, GetSelfBinding(ClearButtonContentProperty));
            }

            _textBox = GetTemplateChild(PartTextBoxName) as DatePickerTextBox;

            if (SelectedDate == null) {
                SetWaterMarkText();
            }

            if (_textBox != null) {
                _textBox.AddHandler(KeyDownEvent, new KeyEventHandler(TextBox_KeyDown), true);
                _textBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(TextBox_TextChanged), true);
                _textBox.AddHandler(LostFocusEvent, new RoutedEventHandler(TextBox_LostFocus), true);

                if (SelectedDate == null) {
                    if (!string.IsNullOrEmpty(_defaultText)) {
                        _textBox.Text = _defaultText;
                        SetSelectedDate();
                    }
                } else
                    _textBox.Text = DateTimeToString((DateTime)SelectedDate);
            }
            UpdateVisualState(true);
        }

        /// <summary> 
        /// Provides a text representation of the selected date.
        /// </summary> 
        /// <returns>A text representation of the selected date, or an empty string if SelectedDate is a null reference.</returns>
        public override string ToString() {
            return SelectedDate == null ? string.Empty :
                SelectedDate.Value.ToString(Engine.GetDateFormat(Engine.GetCulture(this)));
        }

        #endregion

        #region Protected Methods

        /*
        /// <summary>
        /// Creates the automation peer for this DatePicker Control. 
        /// </summary> 
        /// <returns></returns>
        protected override AutomationPeer OnCreateAutomationPeer() {
            return new DatePickerAutomationPeer(this);
        }
        */

        #endregion Protected Methods

        #region Private Methods

        /// <summary> 
        ///     Called when this element gets focus.
        /// </summary>
        private static void OnGotFocus(object sender, RoutedEventArgs e) {
            // When Datepicker gets focus move it to the TextBox
            var picker = (DatePicker)sender;
            if ((e.Handled) || (picker._textBox == null))
                return;
            if (Equals(e.OriginalSource, picker)) {
                picker._textBox.Focus();
                e.Handled = true;
            } else if (Equals(e.OriginalSource, picker._textBox)) {
                picker._textBox.SelectAll();
                e.Handled = true;
            }
        }

        private void SetValueNoCallback(DependencyProperty property, object value) {
            SetIsHandlerSuspended(property, true);
            try {
                SetCurrentValue(property, value);
            } finally {
                SetIsHandlerSuspended(property, false);
            }
        }

        private bool IsHandlerSuspended(DependencyProperty property) {
            return _isHandlerSuspended != null && _isHandlerSuspended.ContainsKey(property);
        }

        private void SetIsHandlerSuspended(DependencyProperty property, bool value) {
            if (value) {
                if (_isHandlerSuspended == null)
                    _isHandlerSuspended = new Dictionary<DependencyProperty, bool>(2);
                _isHandlerSuspended[property] = true;
            } else {
                if (_isHandlerSuspended != null)
                    _isHandlerSuspended.Remove(property);
            }
        }

        private void PopUp_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var popup = sender as Popup;
            if (popup == null || popup.StaysOpen)
                return;
            if (_dropDownButton == null)
                return;
            if (_dropDownButton.InputHitTest(e.GetPosition(_dropDownButton)) != null)
                // This popup is being closed by a mouse press on the drop down button 
                // The following mouse release will cause the closed popup to immediately reopen. 
                // Raise a flag to block reopeneing the popup
                _disablePopupReopen = true;
        }

        private void PopUp_Opened(object sender, EventArgs e) {
            if (!IsDropDownOpen)
                SetCurrentValue(IsDropDownOpenProperty, true);
            // It's not necessary to chack _calendar for not be null. It cannot; never ever
            //if (_calendar != null) {
            _calendar.DisplayMode = CalendarMode.Month;
            _calendar.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            //}

            OnCalendarOpened(new RoutedEventArgs());
        }

        private void PopUp_Closed(object sender, EventArgs e) {
            if (IsDropDownOpen)
                SetCurrentValue(IsDropDownOpenProperty, false);

            if (_calendar.IsKeyboardFocusWithin)
                MoveFocus(new TraversalRequest(FocusNavigationDirection.First));

            OnCalendarClosed(new RoutedEventArgs());
        }

        private void Calendar_DayButtonMouseUp(object sender, MouseButtonEventArgs e) {
            SetCurrentValue(IsDropDownOpenProperty, false);
        }

        private void CalendarDayOrMonthButton_PreviewKeyDown(object sender, RoutedEventArgs e) {
            var c = sender as Calendar;
            var args = e as KeyEventArgs;

            Debug.Assert(c != null);
            Debug.Assert(args != null);

            if (args.Key == Key.Escape
                || (
                    (args.Key == Key.Enter || args.Key == Key.Space)
                    && c.DisplayMode == CalendarMode.Month)
                ) {
                SetCurrentValue(IsDropDownOpenProperty, false);
                if (args.Key == Key.Escape) {
                    SetCurrentValue(SelectedDateProperty, _originalSelectedDate);
                }
            }
        }

        private void Calendar_DisplayDateChanged(object sender, DateChangedEventArgs e) {
            if (e.AddedDate != DisplayDate && e.AddedDate != null)
                SetCurrentValue(DisplayDateProperty, (DateTime)e.AddedDate);
        }

        private void Calendar_SelectedDatesChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            Debug.Assert(e.AddedItems.Count < 2);

            if (e.AddedItems.Count > 0 && SelectedDate.HasValue && DateTime.Compare((DateTime)e.AddedItems[0], SelectedDate.Value) != 0) {
                SetCurrentValue(SelectedDateProperty, (DateTime?)e.AddedItems[0]);
                return;
            }
            if (e.AddedItems.Count == 0) {
                SetCurrentValue(SelectedDateProperty, null);
                return;
            }
            if (!SelectedDate.HasValue && e.AddedItems.Count > 0)
                SetCurrentValue(SelectedDateProperty, (DateTime?)e.AddedItems[0]);
        }

        private string DateTimeToString(DateTime d) {
            return DateTimeToString(d, SelectedDateFormat);
        }

        private string DateTimeToString(DateTime d, DatePickerFormat format) {
            var dtfi = Engine.GetDateFormat(Engine.GetCulture(this));
            switch (format) {
                case DatePickerFormat.Short:
                    return string.Format(CultureInfo.CurrentCulture, d.ToString(dtfi.ShortDatePattern, dtfi));
                case DatePickerFormat.Long:
                    return string.Format(CultureInfo.CurrentCulture, d.ToString(dtfi.LongDatePattern, dtfi));
            }
            return null;
        }

        private void DropDownButton_Click(object sender, RoutedEventArgs e) {
            TogglePopUp();
        }

        private void DropDownButton_MouseLeave(object sender, MouseEventArgs e) {
            _disablePopupReopen = false;
        }

        private void TogglePopUp() {
            if (IsDropDownOpen) {
                SetCurrentValue(IsDropDownOpenProperty, false);
                return;
            }
            if (_disablePopupReopen) {
                _disablePopupReopen = false;
            } else {
                SetSelectedDate();
                SetCurrentValue(IsDropDownOpenProperty, true);
            }
        }

        private void TodayButton_Click(object sender, RoutedEventArgs e) {
            SetSelectedDate(DateTime.Today);
            SetCurrentValue(IsDropDownOpenProperty, false);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e) {
            SetSelectedDate(new DateTime?());
        }

        private void ReleaseCalendar() {
            if (_calendar == null)
                return;
            _calendar.DayButtonMouseUp -= Calendar_DayButtonMouseUp;
            _calendar.DisplayDateChanged -= Calendar_DisplayDateChanged;
            _calendar.SelectedDatesChanged -= Calendar_SelectedDatesChanged;
            _calendar.DayOrMonthPreviewKeyDown -= CalendarDayOrMonthButton_PreviewKeyDown;

            BindingOperations.ClearAllBindings(_calendar);

            _calendar = null;
        }

        private void InitializeCalendar() {
            if (_calendar == null)
                throw new ElementPartNotFoundException(this, PartCalendarName, Calendar.Typeof);
            _calendar.DayButtonMouseUp += Calendar_DayButtonMouseUp;
            _calendar.DisplayDateChanged += Calendar_DisplayDateChanged;
            _calendar.SelectedDatesChanged += Calendar_SelectedDatesChanged;
            _calendar.DayOrMonthPreviewKeyDown += CalendarDayOrMonthButton_PreviewKeyDown;

            // This lines are not control's business:
            // _calendar.HorizontalAlignment = HorizontalAlignment.Left;
            // _calendar.VerticalAlignment = VerticalAlignment.Top;

            // This lines definitely are control's business! So user shouldn't change theme from XAML
            _calendar.SelectionMode = CalendarSelectionMode.SingleDate;
            _calendar.SetBinding(Calendar.EngineProperty, GetDatePickerBinding(EngineProperty));
            _calendar.SetBinding(ForegroundProperty, GetDatePickerBinding(ForegroundProperty));
            _calendar.SetBinding(StyleProperty, GetDatePickerBinding(CalendarStyleProperty));
            _calendar.SetBinding(Calendar.IsTodayHighlightedProperty, GetDatePickerBinding(IsTodayHighlightedProperty));
            _calendar.SetBinding(Calendar.FirstDayOfWeekProperty, GetDatePickerBinding(FirstDayOfWeekProperty));
            _calendar.SetBinding(FlowDirectionProperty, GetDatePickerBinding(FlowDirectionProperty));

            RenderOptions.SetClearTypeHint(_calendar, ClearTypeHint.Enabled);
        }

        private BindingBase GetDatePickerBinding(DependencyProperty property) {
            var binding = new Binding(property.Name) { Source = this };
            return binding;
        }

        // IT SHOULD RETURN NULL IF THE STRING IS NOT VALID, RETURN THE DATETIME VALUE IF IT IS VALID 
        /// <summary>
        /// Input text is parsed in the correct format and changed into a DateTime object. 
        /// If the text can not be parsed TextParseError Event is thrown.
        /// </summary>
        private DateTime? ParseText(string text) {
            // TryParse is not used in order to be able to pass the exception to the TextParseError event 
            try {
                var newSelectedDate = DateTime.Parse(text, Engine.GetDateFormat(Engine.GetCulture(this)));

                if (Calendar.IsValidDateSelection(_calendar, newSelectedDate)) {
                    return newSelectedDate;
                }
                var dateValidationError = new DatePickerDateValidationErrorEventArgs(new ArgumentOutOfRangeException("text"), text);
                OnDateValidationError(dateValidationError);
                if (dateValidationError.ThrowException)
                    throw dateValidationError.Exception;
            } catch (FormatException ex) {
                var textParseError = new DatePickerDateValidationErrorEventArgs(ex, text);
                OnDateValidationError(textParseError);
                if (textParseError.ThrowException && textParseError.Exception != null)
                    throw textParseError.Exception;
            }
            return null;
        }

        private bool ProcessDatePickerKey(KeyEventArgs e) {
            switch (e.Key) {
                case Key.System:
                    switch (e.SystemKey) {
                        case Key.Down:
                            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) {
                                TogglePopUp();
                                return true;
                            }
                            break;
                    }
                    break;
                case Key.Enter:
                    SetSelectedDate();
                    return true;
            }
            return false;
        }

        private void SetSelectedDate() {
            DateTime? dateTime;
            if (_textBox == null) {
                dateTime = SetTextBoxValue(_defaultText);
                if (!SelectedDate.Equals(dateTime))
                    SetCurrentValue(SelectedDateProperty, dateTime);
                return;
            }
            if (string.IsNullOrEmpty(_textBox.Text)) {
                if (SelectedDate.HasValue)
                    SetCurrentValue(SelectedDateProperty, null);
                return;
            }
            var dateText = _textBox.Text;
            if (SelectedDate != null) {
                // If the string value of the SelectedDate and the TextBox string value are equal,
                // we do not parse the string again 
                // if we do an extra parse, we lose data in M/d/yy format
                // ex: SelectedDate = DateTime(1008,12,19) but when "12/19/08" is parsed it is interpreted as DateTime(2008,12,19) 
                var selectedDate = DateTimeToString(SelectedDate.Value);
                if (string.Compare(selectedDate, dateText, StringComparison.Ordinal) == 0)
                    return;
            }
            dateTime = SetTextBoxValue(dateText);
            SetSelectedDate(dateTime);
        }

        /// <summary>
        /// Sets the selected date of the control. Also it controls all things needed to change the selected date.
        /// </summary>
        /// <param name="date">The date to set.</param>
        /// <remarks>
        /// This is the last part of <see cref="M:Kavand.Windows.Controls.DatePicker.SetSelectedDate"/> 
        /// methid which we moved it here, so we can use it in today and clear buttons' clicks.
        /// </remarks>
        private void SetSelectedDate(DateTime? date) {
            if (SelectedDate.Equals(date))
                return;
            SetCurrentValue(SelectedDateProperty, date);
            SetCurrentValue(DisplayDateProperty, date ?? DateTime.Today);
        }

        /// <summary> 
        ///     Set the Text property if it's not already set to the supplied value.  This avoids making the ValueSource Local.
        /// </summary> 
        private void SafeSetText(string s) {
            if (string.Compare(Text, s, StringComparison.Ordinal) != 0)
                SetCurrentValue(TextProperty, s);
        }

        private DateTime? SetTextBoxValue(string s) {
            if (string.IsNullOrEmpty(s)) {
                SafeSetText(s);
                return SelectedDate;
            }
            var d = ParseText(s);
            if (d != null) {
                SafeSetText(DateTimeToString((DateTime)d));
                return d;
            }
            // If parse error: 
            // TextBox should have the latest valid selecteddate value: 
            if (SelectedDate != null) {
                var newtext = DateTimeToString((DateTime)SelectedDate);
                SafeSetText(newtext);
                return SelectedDate;
            }
            SetWaterMarkText();
            return null;
        }

        private void SetWaterMarkText() {
            if (_textBox == null)
                return;
            var watermark = string.Empty;
            var dtfi = Engine.GetDateFormat(Engine.GetCulture(this));
            SetTextInternal(string.Empty);
            _defaultText = string.Empty;
            switch (SelectedDateFormat) {
                case DatePickerFormat.Long:
                    watermark = dtfi.LongDatePattern;
                    break;
                case DatePickerFormat.Short:
                    watermark = dtfi.ShortDatePattern;
                    break;
            }
            _textBox.Watermark = watermark;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e) {
            SetSelectedDate();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e) {
            e.Handled = ProcessDatePickerKey(e) || e.Handled;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
            SetValueNoCallback(TextProperty, _textBox.Text);
        }

        private void SetTodayButtonContent() {
            _timer.Stop();
            if (!HasAutoTodayButtonContent) {
                _todayButton.SetBinding(ContentControl.ContentProperty, GetSelfBinding(TodayButtonContentProperty));
                return;
            }
            BindingOperations.ClearBinding(_todayButton, ContentControl.ContentProperty);
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Start();
        }

        private void RenewTodayButtonContent(object sender, EventArgs e) {
            _timer.Stop();
            if (!HasAutoTodayButtonContent)
                return;
            _todayButton.SetValue(ContentControl.ContentProperty, DateTimeToString(DateTime.Today, DatePickerFormat.Long));
            var interval = DateTime.Now.AddDays(1).Date - DateTime.Now;
            _timer.Interval = interval;
            _timer.Start();
        }

        private BindingBase GetSelfBinding(DependencyProperty sourceProperty) {
            return new Binding(sourceProperty.Name) {
                Source = this
            };
        }

        #endregion Private Methods

        #region VisualState members

        /// <summary> 
        /// Update the current visual state of the control using transitions
        /// </summary> 
        public void UpdateVisualState() {
            UpdateVisualState(true);
        }

        /// <summary> 
        /// Update the current visual state of the control 
        /// </summary>
        /// <param name="useTransitions"> 
        /// true to use transitions when updating the visual state, false to
        /// snap directly to the new visual state.
        /// </param>
        public void UpdateVisualState(bool useTransitions) {
            ChangeVisualState(useTransitions);
        }

        internal void ChangeVisualState(bool useTransitions) {

            if (!IsEnabled)
                VisualStates.GoToState(this, useTransitions, VisualStates.StateDisabled, VisualStates.StateNormal);
            else if (IsMouseOver)
                VisualStates.GoToState(this, useTransitions, VisualStates.StateMouseOver, VisualStates.StateNormal);
            else
                VisualStates.GoToState(this, useTransitions, VisualStates.StateNormal);

            // Focus States Group 
            if (!GetIsFocusedAnyWay(this)) {
                VisualStateManager.GoToState(this, VisualStates.StateUnfocused, useTransitions);
            } else if (IsDropDownOpen) {
                VisualStateManager.GoToState(this, VisualStates.StateFocusedDropDown, useTransitions);
            } else {
                VisualStateManager.GoToState(this, VisualStates.StateFocused, useTransitions);
            }

            VisualStates.UpdateVisualStateBase(this, useTransitions);
        }

        private static bool GetIsFocusedAnyWay(UIElement element) {
            return element.IsKeyboardFocused || element.IsKeyboardFocusWithin || element.IsFocused;
        }

        #endregion
    }
}