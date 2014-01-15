using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Kavand.Windows.Controls {

    /// <summary>
    /// Represents the currently displayed month or year or decade on a <see cref="T:Kavand.Windows.Controls.Calendar"/>.
    /// </summary>
    [TemplatePart(Name = PartRootName, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartMonthViewName, Type = typeof(Grid))]
    [TemplatePart(Name = PartYearViewName, Type = typeof(Grid))]
    [TemplatePart(Name = PartDecadeViewName, Type = typeof(Grid))]
    [TemplatePart(Name = PartDisabledVisualName, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = DayTitleTemplateName, Type = typeof(DataTemplate))]
    [TemplateVisualState(GroupName = DisplayModeGroupName, Name = MonthToYearStateName)]
    [TemplateVisualState(GroupName = DisplayModeGroupName, Name = YearToMonthStateName)]
    [TemplateVisualState(GroupName = DisplayModeGroupName, Name = YearToDecadeStateName)]
    [TemplateVisualState(GroupName = DisplayModeGroupName, Name = DecadeToYearStateName)]
    [TemplateVisualState(GroupName = DisplayModeGroupName, Name = DecadeToMonthStateName)]
    public sealed class CalendarViewPresenter : Control, IVisualStateUpdateable {

        private const string DisplayModeGroupName = "DisplayModeStates";
        private const string MonthToYearStateName = "MonthToYear";
        private const string YearToMonthStateName = "YearToMonth";
        private const string YearToDecadeStateName = "YearToDecade";
        private const string DecadeToYearStateName = "DecadeToYear";
        private const string DecadeToMonthStateName = "DecadeToMonth";

        private const string PartRootName = "PART_Root";
        private const string PartMonthViewName = "PART_MonthView";
        private const string PartYearViewName = "PART_YearView";
        private const string PartDecadeViewName = "PART_DecadeView";
        private const string PartDisabledVisualName = "PART_DisabledVisual";
        private const string DayTitleTemplateName = "DayTitleTemplate";

        private static ComponentResourceKey _dayTitleTemplateResourceKey;
        private DataTemplate _dayTitleTemplate;
        private Grid _monthView;
        private Grid _yearView;
        private Grid _decadeView;
        private bool _isYearPressed;
        private bool _isMonthPressed;
        private bool _isDayPressed;

        internal Calendar Owner {
            get;
            set;
        }

        internal Grid MonthView {
            get { return _monthView; }
        }

        internal Grid YearView {
            get {
                return _yearView;
            }
        }

        internal Grid DecadeView {
            get {
                return _decadeView;
            }
        }

        private CalendarMode DisplayMode {
            get {
                return Owner == null ? CalendarMode.Month : Owner.DisplayMode;
            }
        }

        private DateTime DisplayDate {
            get {
                return Owner == null ? DateTime.Today : Owner.DisplayDate;
            }
        }

        private CalendarEngine _defaultEngine;
        private CalendarEngine Engine {
            get {
                return Owner != null ? Owner.Engine :
                    _defaultEngine ?? (_defaultEngine = new GregorianCalendarEngine());
            }
        }

        internal bool IsRightToLeft {
            get { return FlowDirection == FlowDirection.RightToLeft; }
        }

        internal DayNameMode DayNameMode {
            get { return Owner != null ? Owner.DayNameMode : DayNameMode.Shortest; }
        }

        internal MonthNameMode MonthNameMode {
            get { return Owner != null ? Owner.MonthNameMode : MonthNameMode.Abbreviated; }
        }

        #region DependencyProperty ViewRenderTransformOrigin

        private static readonly Point ViewRenderTransformOriginDefaultValue = new Point(.5, .5);

        public static readonly DependencyProperty ViewRenderTransformOriginProperty
            = DependencyProperty.Register(
                "ViewRenderTransformOrigin",
                typeof(Point),
                typeof(CalendarViewPresenter),
                new FrameworkPropertyMetadata(
                    ViewRenderTransformOriginDefaultValue,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    ViewRenderTransformOriginChangedCallback,
                    ViewRenderTransformOriginCoerceCallback));

        private static void ViewRenderTransformOriginChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {

        }

        private static object ViewRenderTransformOriginCoerceCallback(DependencyObject d, object basevalue) {
            if (!(basevalue is Point))
                throw new Exception();
            var value = (Point)basevalue;
            if (value.X < 0 || value.X > 1 || value.Y < 0 || value.Y > 1)
                return ViewRenderTransformOriginDefaultValue;
            return value;
        }

        public Point ViewRenderTransformOrigin {
            get { return (Point)GetValue(ViewRenderTransformOriginProperty); }
            set { SetValue(ViewRenderTransformOriginProperty, value); }
        }

        #endregion

        /// <summary>
        /// Gets or sets the resource key for the <see cref="T:System.Windows.DataTemplate"/> that displays the days of the week.
        /// </summary>
        /// 
        /// <returns>
        /// The resource key for the <see cref="T:System.Windows.DataTemplate"/> that displays the days of the week.
        /// </returns>
        public static ComponentResourceKey DayTitleTemplateResourceKey {
            get {
                return _dayTitleTemplateResourceKey ??
                       (_dayTitleTemplateResourceKey =
                        new ComponentResourceKey(typeof(CalendarViewPresenter), DayTitleTemplateName));
            }
        }

        static CalendarViewPresenter() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CalendarViewPresenter), new FrameworkPropertyMetadata(typeof(CalendarViewPresenter)));
            FocusableProperty.OverrideMetadata(typeof(CalendarViewPresenter), new FrameworkPropertyMetadata(false));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(CalendarViewPresenter), new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(CalendarViewPresenter), new FrameworkPropertyMetadata(KeyboardNavigationMode.Contained));
            IsEnabledProperty.OverrideMetadata(typeof(CalendarViewPresenter), new UIPropertyMetadata(VisualStates.OnVisualStatePropertyChanged));
        }

        /// <summary>
        /// Builds the visual tree for the <see cref="T:Kavand.Windows.Controls.CalendarViewPresenter"/> when a new template is applied.
        /// </summary>
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            _monthView = GetTemplateChild(PartMonthViewName) as Grid;
            _yearView = GetTemplateChild(PartYearViewName) as Grid;
            _decadeView = GetTemplateChild(PartDecadeViewName) as Grid;
            _dayTitleTemplate = null;
            if (Template != null && Template.Resources.Contains(DayTitleTemplateResourceKey))
                _dayTitleTemplate = Template.Resources[DayTitleTemplateResourceKey] as DataTemplate;
            PopulateGrids();
            if (Owner != null) {
                switch (Owner.DisplayMode) {
                    case CalendarMode.Month:
                        UpdateMonthMode();
                        break;
                    case CalendarMode.Year:
                        UpdateYearMode();
                        break;
                    case CalendarMode.Decade:
                        UpdateDecadeMode();
                        break;
                }
            } else
                UpdateMonthMode();
            InitializeDisplayMode();
        }

        protected override void OnMouseUp(MouseButtonEventArgs e) {
            base.OnMouseUp(e);
            if (IsMouseCaptured)
                ReleaseMouseCapture();
            _isYearPressed = false;
            _isMonthPressed = false;
            _isDayPressed = false;
            if (e.Handled || Owner.DisplayMode != CalendarMode.Month || !Owner.HoverEnd.HasValue)
                return;
            FinishSelection(Owner.HoverEnd.Value);
        }

        protected override void OnLostMouseCapture(MouseEventArgs e) {
            base.OnLostMouseCapture(e);
            if (IsMouseCaptured)
                return;
            _isDayPressed = false;
            _isMonthPressed = false;
            _isYearPressed = false;
        }

        #region PopulateGrids()

        private void PopulateGrids() {
            PopulateMonthViewGrid();
            PopulateYearViewGrid();
            PopulateDecadeViewGrid();
        }

        #endregion

        #region MonthView Features

        #region Populating MonthView Content

        private void PopulateMonthViewGrid() {
            if (_monthView == null)
                return;
            // todo short the loops and migrate to one loop
            for (var index = 0; index < 7; ++index) {
                var fe = _dayTitleTemplate != null ? (FrameworkElement)_dayTitleTemplate.LoadContent() : new ContentControl();
                fe.SetValue(Grid.RowProperty, 0);
                fe.SetValue(Grid.ColumnProperty, index);
                _monthView.Children.Add(fe);
            }
            for (var row = 1; row < 7; ++row) {
                for (var column = 0; column < 7; ++column) {
                    var cdb = new DayButton { Owner = Owner };
                    cdb.SetValue(Grid.RowProperty, row);
                    cdb.SetValue(Grid.ColumnProperty, column);
                    cdb.SetBinding(StyleProperty, GetOwnerBinding(Calendar.DayButtonStyleProperty));
                    cdb.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(Day_MouseLeftButtonDown), true);
                    cdb.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(Day_MouseLeftButtonUp), true);
                    cdb.AddHandler(MouseEnterEvent, new MouseEventHandler(Day_MouseEnter), true);
                    cdb.Click += Day_Clicked;
                    cdb.AddHandler(PreviewKeyDownEvent, new RoutedEventHandler(DayOrMonthOrYear_PreviewKeyDown), true);
                    _monthView.Children.Add(cdb);
                }
            }
        }

        #endregion

        #region MonthView Event Handlers

        private void Day_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var calendarDayButton = sender as DayButton;
            if (calendarDayButton == null || Owner == null || !(calendarDayButton.DataContext is DateTime))
                return;
            if (calendarDayButton.IsBlackedOut) {
                Owner.HoverStart = new DateTime?();
                return;
            }
            _isDayPressed = true;
            Mouse.Capture(this, CaptureMode.SubTree);
            calendarDayButton.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            bool ctrl;
            bool shift;
            KeyboardHelper.GetMetaKeyState(out ctrl, out shift);
            var date = (DateTime)calendarDayButton.DataContext;
            switch (Owner.SelectionMode) {
                case CalendarSelectionMode.SingleDate:
                    Owner.DatePickerDisplayDateFlag = true;
                    if (!ctrl) {
                        Owner.SelectedDate = date;
                        break;
                    }
                    Owner.SelectedDates.Toggle(date);
                    break;
                case CalendarSelectionMode.SingleRange:
                    Owner.SelectedDates.ClearInternal();
                    if (shift) {
                        if (!Owner.HoverStart.HasValue) {
                            Owner.HoverStart = Owner.HoverEnd = Owner.CurrentDate;
                        }
                        break;
                    }
                    Owner.HoverStart = Owner.HoverEnd = date;
                    break;
                case CalendarSelectionMode.MultipleRange:
                    if (!ctrl)
                        Owner.SelectedDates.ClearInternal();
                    if (shift) {
                        if (!Owner.HoverStart.HasValue) {
                            Owner.HoverStart = Owner.HoverEnd = Owner.CurrentDate;
                        }
                        break;
                    }
                    Owner.HoverStart = Owner.HoverEnd = date;
                    break;
            }
            Owner.CurrentDate = date;
            Owner.UpdatePresenter();
        }

        private void Day_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            var calendarDayButton = sender as DayButton;
            if (calendarDayButton == null || Owner == null)
                return;
            if (!calendarDayButton.IsBlackedOut)
                Owner.OnDayButtonMouseUp(e);
            if (!(calendarDayButton.DataContext is DateTime))
                return;
            FinishSelection((DateTime)calendarDayButton.DataContext);
            e.Handled = true;
        }

        private void Day_MouseEnter(object sender, MouseEventArgs e) {
            var calendarDayButton = sender as DayButton;
            if (calendarDayButton == null || calendarDayButton.IsBlackedOut
                || (e.LeftButton != MouseButtonState.Pressed || !_isDayPressed))
                return;
            calendarDayButton.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            if (Owner == null || !(calendarDayButton.DataContext is DateTime))
                return;
            var dateTime = (DateTime)calendarDayButton.DataContext;
            if (Owner.SelectionMode == CalendarSelectionMode.SingleDate) {
                Owner.DatePickerDisplayDateFlag = true;
                Owner.HoverEnd = null;
                Owner.HoverStart = null;
                if (Owner.SelectedDates.Count == 0)
                    Owner.SelectedDates.Add(dateTime);
                else
                    Owner.SelectedDates[0] = dateTime;
            } else {
                Owner.HoverEnd = dateTime;
                Owner.CurrentDate = dateTime;
                Owner.UpdatePresenter();
            }
        }

        private void Day_Clicked(object sender, RoutedEventArgs e) {
            if (Owner == null)
                return;
            var calendarDayButton = sender as DayButton;
            if (calendarDayButton == null || !(calendarDayButton.DataContext is DateTime) || calendarDayButton.IsBlackedOut)
                return;
            var dateTime = (DateTime)calendarDayButton.DataContext;
            bool ctrl;
            bool shift;
            KeyboardHelper.GetMetaKeyState(out ctrl, out shift);
            switch (Owner.SelectionMode) {
                case CalendarSelectionMode.SingleDate:
                    if (!ctrl) {
                        Owner.SelectedDate = dateTime;
                        break;
                    }
                    Owner.SelectedDates.Toggle(dateTime);
                    break;
                case CalendarSelectionMode.SingleRange:
                    Owner.SelectedDates.ClearInternal(true);
                    if (shift) {
                        Owner.SelectedDates.AddRangeInternal(Owner.CurrentDate, dateTime);
                        break;
                    }
                    Owner.SelectedDate = dateTime;
                    Owner.HoverStart = new DateTime?();
                    Owner.HoverEnd = new DateTime?();
                    break;
                case CalendarSelectionMode.MultipleRange:
                    if (!ctrl)
                        Owner.SelectedDates.ClearInternal(true);
                    if (shift) {
                        Owner.SelectedDates.AddRangeInternal(Owner.CurrentDate, dateTime);
                        break;
                    }
                    if (!ctrl) {
                        Owner.SelectedDate = dateTime;
                        break;
                    }
                    Owner.SelectedDates.Toggle(dateTime);
                    Owner.HoverStart = new DateTime?();
                    Owner.HoverEnd = new DateTime?();
                    break;
            }
            Owner.OnDayClick(dateTime);
        }

        #endregion

        #region MonthView Content Management

        internal void UpdateMonthMode() {
            SetMonthModeHeaderButton();
            SetMonthModePreviousButton();
            SetMonthModeNextButton();
            if (_monthView == null)
                return;
            SetMonthModeDayTitles();
            SetMonthModeCalendarDayButtons();
            AddMonthModeHighlight();
        }

        private void SetMonthModeHeaderButton() {
            if (Owner == null)
                return;
            Owner.SetMonthModeHeaderButton();
        }

        private void SetMonthModeNextButton() {
            if (Owner == null)
                return;
            Owner.SetMonthModeNextButton();
        }

        private void SetMonthModePreviousButton() {
            if (Owner == null)
                return;
            Owner.SetMonthModePreviousButton();
        }

        private void SetMonthModeDayTitles() {
            if (_monthView == null)
                return;
            // check for showing short names, abbr names, or full names
            // var dayNames = Engine.GetShortestDayNames(Engine.GetCulture(this));
            var dayNames = Engine.GetDayNames(DayNameMode, Engine.GetCulture(this));
            var firstDayOfWeekPosition = (int)(Owner != null
                ? Owner.FirstDayOfWeek
                : Engine.GetFirstDayOfWeek(Engine.GetCulture(this)));
            for (var index = 0; index < 7; ++index) {
                var fe = _monthView.Children[index] as FrameworkElement;
                if (fe != null && dayNames.Length > 0)
                    fe.DataContext = dayNames[(index + firstDayOfWeekPosition) % dayNames.Length];
            }
        }

        private void SetMonthModeCalendarDayButtons() {
            var firstOfMonth = Engine.GetFirstOfMonth(DisplayDate);
            var fromPreviousMonth = Engine.GetNumberOfDisplayedDaysFromPreviousMonth(
                firstOfMonth, Owner != null ? Owner.FirstDayOfWeek : (DayOfWeek?)null, Engine.GetCulture(this));
            var isGreaterThanMinDateTime = Engine.CompareYearMonth(firstOfMonth, Engine.MinSupportedDateTime) > 0;
            var isLessThanMaxDateTime = Engine.CompareYearMonth(firstOfMonth, Engine.MaxSupportedDateTime) < 0;
            var daysInMonth = Engine.GetDaysInMonth(firstOfMonth);
            var culture = Engine.GetCulture(this);
            const int num = 49;
            for (var index = 7; index < num; ++index) {
                var childButton = (DayButton)_monthView.Children[index];
                var days = index - fromPreviousMonth - 7;
                if ((isGreaterThanMinDateTime || days >= 0) && (isLessThanMaxDateTime || days < daysInMonth)) {
                    var theDay = Engine.AddDays(firstOfMonth, days);
                    SetMonthModeDayButtonState(childButton, theDay);
                    childButton.DataContext = theDay;
                    childButton.SetContentInternal(Engine.GetDayString(theDay, culture));
                } else {
                    SetMonthModeDayButtonState(childButton, new DateTime?());
                    childButton.DataContext = null;
                    childButton.SetContentInternal(Engine.GetDayString(new DateTime?(), culture));
                }
            }
        }

        private void AddMonthModeHighlight() {
            var owner = Owner;
            if (owner == null)
                return;
            if (owner.HoverStart.HasValue && owner.HoverEnd.HasValue) {
                var start = owner.HoverEnd.Value;
                var end = owner.HoverEnd.Value;
                var num1 = Engine.CompareDays(owner.HoverEnd.Value, owner.HoverStart.Value);
                if (num1 < 0)
                    end = owner.HoverStart.Value;
                else
                    start = owner.HoverStart.Value;
                const int num2 = 49;
                for (var index = 7; index < num2; ++index) {
                    var calendarDayButton = (DayButton)_monthView.Children[index];
                    if (calendarDayButton.DataContext is DateTime) {
                        var date = (DateTime)calendarDayButton.DataContext;
                        calendarDayButton.SetValue(DayButton.IsHighlightedPropertyKey, num1 != 0
                            && Engine.InRange(date, start, end));
                    } else
                        calendarDayButton.SetValue(DayButton.IsHighlightedPropertyKey, false);
                }
            } else {
                const int num = 49;
                for (var index = 7; index < num; ++index)
                    _monthView.Children[index].SetValue(DayButton.IsHighlightedPropertyKey, false);
            }
        }

        private void SetMonthModeDayButtonState(DayButton childButton, DateTime? dateToAdd) {
            if (Owner == null)
                return;
            if (!dateToAdd.HasValue) {
                childButton.Visibility = Visibility.Hidden;
                childButton.IsEnabled = false;
                childButton.SetValue(DayButton.IsBlackedOutPropertyKey, false);
                childButton.SetValue(DayButton.IsInactivePropertyKey, true);
                childButton.SetValue(DayButton.IsTodayPropertyKey, false);
                childButton.SetValue(DayButton.IsSelectedPropertyKey, false);
                return;
            }
            if (Engine.CompareDays(dateToAdd.Value, Owner.DisplayDateStartInternal) < 0
                || Engine.CompareDays(dateToAdd.Value, Owner.DisplayDateEndInternal) > 0) {
                childButton.IsEnabled = false;
                childButton.Visibility = Visibility.Hidden;
                return;
            }
            childButton.Visibility = Visibility.Visible;
            childButton.IsEnabled = true;
            childButton.SetValue(DayButton.IsBlackedOutPropertyKey, Owner.BlackoutDates.Contains(dateToAdd.Value));
            childButton.SetValue(DayButton.IsInactivePropertyKey, Engine.CompareYearMonth(dateToAdd.Value, Owner.DisplayDateInternal) != 0);
            childButton.SetValue(DayButton.IsTodayPropertyKey, Engine.CompareDays(dateToAdd.Value, DateTime.Today) == 0);
            childButton.NotifyNeedsVisualStateUpdate();
            var flag = false;
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var dt2 in Owner.SelectedDates)
                // ReSharper restore LoopCanBeConvertedToQuery
                flag = flag | Engine.CompareDays(dateToAdd.Value, dt2) == 0;
            childButton.SetValue(DayButton.IsSelectedPropertyKey, flag);
        }

        #endregion

        #endregion

        #region YearView Features

        #region Populating YearView Content

        private void PopulateYearViewGrid() {
            if (_yearView == null)
                return;
            for (var row = 0; row < 3; ++row) {
                for (var column = 0; column < 4; ++column) {
                    var cb = new MonthYearButton { Owner = Owner };
                    cb.SetValue(Grid.RowProperty, row);
                    cb.SetValue(Grid.ColumnProperty, column);
                    cb.SetBinding(StyleProperty, GetOwnerBinding(Calendar.MonthYearButtonStyleProperty));
                    cb.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(Month_MouseLeftButtonDown), true);
                    cb.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(MonthOrYear_MouseLeftButtonUp), true);
                    cb.AddHandler(MouseEnterEvent, new MouseEventHandler(Month_MouseEnter), true);
                    cb.AddHandler(PreviewKeyDownEvent, new RoutedEventHandler(DayOrMonthOrYear_PreviewKeyDown), true);
                    cb.Click += MonthOrYear_Clicked;
                    _yearView.Children.Add(cb);
                }
            }
        }

        #endregion

        #region YearView Event Handlers

        private void Month_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var b = sender as MonthYearButton;
            if (b == null)
                return;
            _isMonthPressed = true;
            Mouse.Capture(this, CaptureMode.SubTree);
            if (Owner == null)
                return;
            Owner.OnCalendarButtonPressed(b, false);
        }

        private void Month_MouseEnter(object sender, MouseEventArgs e) {
            var b = sender as MonthYearButton;
            if (b == null || !_isMonthPressed || Owner == null)
                return;
            Owner.OnCalendarButtonPressed(b, false);
        }

        #endregion

        #region YearView Content Management

        internal void UpdateYearMode() {
            SetYearModeHeaderButton();
            SetYearModePreviousButton();
            SetYearModeNextButton();
            if (_yearView == null)
                return;
            SetYearModeMonthButtons();
        }

        private void SetYearModeHeaderButton() {
            if (Owner == null)
                return;
            Owner.SetYearModeHeaderButton();
        }

        private void SetYearModeNextButton() {
            if (Owner == null)
                return;
            Owner.SetYearModeNextButton();
        }

        private void SetYearModePreviousButton() {
            if (Owner == null)
                return;
            Owner.SetYearModePreviousButton();
        }

        private void SetYearModeMonthButtons() {
            var num = 0;
            // check for month names display mode
            // var monthNames = Engine.GetAbbreviatedMonthNames(Engine.GetCulture(this));
            var monthNames = Engine.GetMonthNames(MonthNameMode,Engine.GetCulture(this));
            var monthNamesLength = monthNames.Length;
            foreach (var obj in _yearView.Children) {
                var cb = (MonthYearButton)obj;
                var theMonth = Engine.GetMonth(DisplayDate, num + 1);
                cb.DataContext = theMonth;
                var content = string.Empty;
                if (monthNamesLength > 0)
                    content = monthNames[num /* = (theMonth.Month - 1) */ % monthNamesLength];
                cb.SetContentInternal(content);
                cb.Visibility = Visibility.Visible;
                if (Owner != null) {
                    cb.HasSelectedDays = Engine.CompareYearMonth(theMonth, Owner.DisplayDateInternal) == 0;
                    if (Engine.CompareYearMonth(theMonth, Owner.DisplayDateStartInternal) < 0
                        || Engine.CompareYearMonth(theMonth, Owner.DisplayDateEndInternal) > 0) {
                        cb.IsEnabled = false;
                        cb.Opacity = 0.0;
                    } else {
                        cb.IsEnabled = true;
                        cb.Opacity = 1.0;
                    }
                }
                cb.IsInactive = false;
                ++num;
            }
        }

        #endregion

        #endregion

        #region DecadeView Features

        #region Populating DecadeView Content

        private void PopulateDecadeViewGrid() {
            if (_decadeView == null)
                return;
            for (var row = 0; row < 3; ++row) {
                for (var column = 0; column < 4; ++column) {
                    var cb = new MonthYearButton { Owner = Owner };
                    cb.SetValue(Grid.RowProperty, row);
                    cb.SetValue(Grid.ColumnProperty, column);
                    cb.SetBinding(StyleProperty, GetOwnerBinding(Calendar.MonthYearButtonStyleProperty));
                    cb.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(Year_MouseLeftButtonDown), true);
                    cb.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(MonthOrYear_MouseLeftButtonUp), true);
                    cb.AddHandler(MouseEnterEvent, new MouseEventHandler(Year_MouseEnter), true);
                    cb.AddHandler(PreviewKeyDownEvent, new RoutedEventHandler(DayOrMonthOrYear_PreviewKeyDown), true);
                    cb.Click += MonthOrYear_Clicked;
                    _decadeView.Children.Add(cb);
                }
            }
        }

        #endregion

        #region DecadeView Event Handlers

        private void Year_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var b = sender as MonthYearButton;
            if (b == null)
                return;
            _isYearPressed = true;
            Mouse.Capture(this, CaptureMode.SubTree);
            if (Owner == null)
                return;
            Owner.OnCalendarButtonPressed(b, false);
        }

        private void Year_MouseEnter(object sender, MouseEventArgs e) {
            var b = sender as MonthYearButton;
            if (b == null || !_isYearPressed || Owner == null)
                return;
            Owner.OnCalendarButtonPressed(b, false);
        }

        #endregion

        #region DecadeView Content Management

        internal void UpdateDecadeMode() {
            var scaleDate = Owner == null ? DateTime.Today : Owner.DisplayYear;
            var decadeStartYear = GetDecadeForDecadeMode(scaleDate);
            var decadeStart = new DateTime(decadeStartYear, scaleDate.Month, scaleDate.Day);
            decadeStart = Engine.CurrectDecadeContext(decadeStart);
            var decadeEndYear = decadeStartYear + 9;
            var decadeEnd = new DateTime(decadeEndYear, scaleDate.Month, scaleDate.Day);
            decadeEnd = Engine.CurrectDecadeContext(decadeEnd);
            SetDecadeModeHeaderButton(decadeStart);
            SetDecadeModePreviousButton(decadeStartYear);
            SetDecadeModeNextButton(decadeEndYear);
            if (_decadeView == null)
                return;
            SetYearButtons(decadeStart, decadeEnd);
        }

        private int GetDecadeForDecadeMode(DateTime selectedYear) {
            var num = Engine.GetDecadeStart(selectedYear);
            if (_isYearPressed && _decadeView != null) {
                var children = _decadeView.Children;
                var count = children.Count;
                if (count > 0) {
                    var calendarButton = children[0] as MonthYearButton;
                    if (calendarButton != null && calendarButton.DataContext is DateTime
                        && ((DateTime)calendarButton.DataContext).Year == selectedYear.Year)
                        return num + 10;
                }
                if (count > 1) {
                    var calendarButton = children[count - 1] as MonthYearButton;
                    if (calendarButton != null && calendarButton.DataContext is DateTime
                        && ((DateTime)calendarButton.DataContext).Year == selectedYear.Year)
                        return num - 10;
                }
            }
            return num;
        }

        private void SetDecadeModeHeaderButton(DateTime decadeStart) {
            if (Owner == null)
                return;
            Owner.SetDecadeModeHeaderButton(decadeStart);
        }

        private void SetDecadeModeNextButton(int decadeEnd) {
            if (Owner == null)
                return;
            Owner.SetDecadeModeNextButton(decadeEnd);
        }

        private void SetDecadeModePreviousButton(int decade) {
            if (Owner == null)
                return;
            Owner.SetDecadeModePreviousButton(decade);
        }

        private void SetYearButtons(DateTime decadeStart, DateTime decadeEnd) {
            var num = -1;
            foreach (var obj in _decadeView.Children) {
                var cb = (MonthYearButton)obj;
                var decade = decadeStart.AddYears(num);
                if (decade.Year <= Engine.MaxSupportedDateTime.Year
                    && decade.Year >= Engine.MinSupportedDateTime.Year) {
                    var dateTime = Engine.GetFirstOfYear(decade);
                    cb.DataContext = dateTime;
                    cb.SetContentInternal(Engine.GetYearString(dateTime, Engine.GetCulture(this)));
                    cb.Visibility = Visibility.Visible;
                    if (Owner != null) {
                        // the old alg: cb.HasSelectedDays = Owner.DisplayDate.Year == decade.Year;
                        cb.HasSelectedDays = Engine.CompareYears(Owner.DisplayDate, decade) == 0;
                        if (decade.Year < Owner.DisplayDateStartInternal.Year || decade.Year > Owner.DisplayDateEndInternal.Year) {
                            cb.IsEnabled = false;
                            cb.Opacity = 0.0;
                        } else {
                            cb.IsEnabled = true;
                            cb.Opacity = 1.0;
                        }
                    }
                    cb.IsInactive = Engine.IsOutOfDecade(decadeStart, decadeEnd, decade);
                } else {
                    cb.DataContext = null;
                    cb.IsEnabled = false;
                    cb.Opacity = 0.0;
                }
                ++num;
            }
        }

        #endregion

        #endregion

        #region Shared View Features

        #region Shared Event Handlers

        #region Month And Year

        private void MonthOrYear_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            var b = sender as MonthYearButton;
            if (b == null)
                return;
            _isMonthPressed = false;
            _isYearPressed = false;
            if (Owner == null)
                return;
            Owner.OnCalendarButtonPressed(b, true);
        }

        private void MonthOrYear_Clicked(object sender, RoutedEventArgs e) {
            var b = sender as MonthYearButton;
            if (b == null)
                return;
            Owner.OnCalendarButtonPressed(b, true);
        }

        #endregion

        #region Day And Month And Year

        private void DayOrMonthOrYear_PreviewKeyDown(object sender, RoutedEventArgs e) {
            if (Owner == null)
                return;
            Owner.OnDayOrMonthPreviewKeyDown(e);
        }

        #endregion

        #endregion

        #endregion

        #region DayButton accessors

        internal IEnumerable<DayButton> GetCalendarDayButtons() {
            const int count = 49;
            if (MonthView == null)
                yield break;
            var dayButtonsHost = MonthView.Children;
            for (var childIndex = 7; childIndex < count; ++childIndex) {
                var b = dayButtonsHost[childIndex] as DayButton;
                if (b != null)
                    yield return b;
            }
        }

        internal DayButton GetFocusedCalendarDayButton() {
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var calendarDayButton in GetCalendarDayButtons()) {
                // ReSharper restore LoopCanBeConvertedToQuery
                if (calendarDayButton != null && calendarDayButton.IsFocused)
                    return calendarDayButton;
            }
            return null;
        }

        internal DayButton GetCalendarDayButton(DateTime date) {
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var calendarDayButton in GetCalendarDayButtons()) {
                // ReSharper restore LoopCanBeConvertedToQuery
                if (calendarDayButton != null && calendarDayButton.DataContext is DateTime
                    && Engine.CompareDays(date, (DateTime)calendarDayButton.DataContext) == 0)
                    return calendarDayButton;
            }
            return null;
        }

        #endregion

        #region MonthYearButton accessors

        private IEnumerable<MonthYearButton> GetCalendarButtons(Panel view) {
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (UIElement uiElement in view.Children) {
                // ReSharper restore LoopCanBeConvertedToQuery
                var b = uiElement as MonthYearButton;
                if (b != null)
                    yield return b;
            }
        }

        internal MonthYearButton GetCalendarButton(Panel view, DateTime date, CalendarMode mode) {
            foreach (var cb in GetCalendarButtons(view)) {
                if (cb == null || !(cb.DataContext is DateTime))
                    continue;
                if (mode == CalendarMode.Year) {
                    if (Engine.CompareYearMonth(date, (DateTime)cb.DataContext) == 0)
                        return cb;
                } else if (Engine.CompareYears(date, (DateTime)cb.DataContext) == 0)
                    return cb;
            }
            return null;
        }

        internal MonthYearButton GetFocusedCalendarButton(Panel view) {
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var cb in GetCalendarButtons(view)) {
                // ReSharper restore LoopCanBeConvertedToQuery
                if (cb != null && cb.IsFocused)
                    return cb;
            }
            return null;
        }

        #endregion

        internal void FocusDate(DateTime date) {
            var frameworkElement = (FrameworkElement)null;
            switch (DisplayMode) {
                case CalendarMode.Month:
                    frameworkElement = GetCalendarDayButton(date);
                    break;
                case CalendarMode.Year:
                    frameworkElement = GetCalendarButton(_yearView, date, DisplayMode);
                    break;
                case CalendarMode.Decade:
                    frameworkElement = GetCalendarButton(_decadeView, date, DisplayMode);
                    break;
            }
            if (frameworkElement == null || frameworkElement.IsFocused)
                return;
            frameworkElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }

        private void EndDrag(bool ctrl, DateTime selectedDate) {
            if (Owner == null)
                return;
            Owner.CurrentDate = selectedDate;
            if (!Owner.HoverStart.HasValue)
                return;
            if (ctrl && (DateTime.Compare(Owner.HoverStart.Value, selectedDate) == 0
                && (Owner.SelectionMode == CalendarSelectionMode.SingleDate || Owner.SelectionMode == CalendarSelectionMode.MultipleRange)))
                Owner.SelectedDates.Toggle(selectedDate);
            else
                Owner.SelectedDates.AddRangeInternal(Owner.HoverStart.Value, selectedDate);
            Owner.OnDayClick(selectedDate);
        }

        private void FinishSelection(DateTime selectedDate) {
            bool ctrl;
            bool shift;
            KeyboardHelper.GetMetaKeyState(out ctrl, out shift);
            if (Owner.SelectionMode == CalendarSelectionMode.None || Owner.SelectionMode == CalendarSelectionMode.SingleDate)
                Owner.OnDayClick(selectedDate);
            else if (Owner.HoverStart.HasValue) {
                switch (Owner.SelectionMode) {
                    case CalendarSelectionMode.SingleRange:
                        Owner.SelectedDates.ClearInternal();
                        EndDrag(ctrl, selectedDate);
                        break;
                    case CalendarSelectionMode.MultipleRange:
                        EndDrag(ctrl, selectedDate);
                        break;
                }
            } else {
                var calendarDayButton = GetCalendarDayButton(selectedDate);
                if (calendarDayButton == null || !calendarDayButton.IsInactive || !calendarDayButton.IsBlackedOut)
                    return;
                Owner.OnDayClick(selectedDate);
            }
        }

        private BindingBase GetOwnerBinding(DependencyProperty sourceProperty) {
            return new Binding(sourceProperty.Name) {
                Source = Owner
            };
        }

        #region VisualState updating members

        internal bool VisualStateChangeSuspended { get; set; }

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
            if (VisualStateChangeSuspended)
                return;
            VisualStateChangeSuspended = true;
            ChangeVisualState(useTransitions);
            VisualStateChangeSuspended = false;
        }

        internal void ChangeVisualState(bool useTransitions) {
            VisualStateManager.GoToState(this, !IsEnabled ? "Disabled" : "Normal", useTransitions);
        }

        internal void NotifyNeedsVisualStateUpdate() {
            UpdateVisualState();
        }

        #endregion

        private void InitializeDisplayMode() {
            var mode = DisplayMode;
            switch (mode) {
                case CalendarMode.Month:
                    OnDisplayModeChanged(CalendarMode.Year, CalendarMode.Month, false);
                    break;
                case CalendarMode.Year:
                    OnDisplayModeChanged(CalendarMode.Month, CalendarMode.Year, false);
                    break;
                case CalendarMode.Decade:
                    OnDisplayModeChanged(CalendarMode.Year, CalendarMode.Decade, false);
                    break;
            }
        }

        public void OnDisplayModeChanged(CalendarModeChangedEventArgs args, bool useTransitions = true) {
            OnDisplayModeChanged(args.OldMode, args.NewMode, useTransitions);
        }

        public void OnDisplayModeChanged(CalendarMode oldMode, CalendarMode newMode, bool useTransitions = true) {
            UpdateViewRenderTransformOrigin(newMode);
            var vs = string.Format("{0}To{1}", oldMode, newMode);
            VisualStateManager.GoToState(this, vs, true);
        }

        private void UpdateViewRenderTransformOrigin(CalendarMode mode) {
            var point = ViewRenderTransformOriginDefaultValue;
            var date = Owner != null && Owner.SelectedDate.HasValue ? Owner.SelectedDate.Value : DisplayDate;
            switch (mode) {
                case CalendarMode.Month:
                    point = CalculateViewRenderTransformOrigin(_monthView, GetCalendarDayButton(date));
                    break;
                case CalendarMode.Year:
                    point = CalculateViewRenderTransformOrigin(
                        _yearView, GetCalendarButton(_yearView, date, CalendarMode.Year));
                    break;
                case CalendarMode.Decade:
                    point = CalculateViewRenderTransformOrigin(
                        _decadeView, GetCalendarButton(_decadeView, date, CalendarMode.Decade));
                    break;
            }
            ViewRenderTransformOrigin = point;
        }

        private Point CalculateViewRenderTransformOrigin(Panel panel, FrameworkElement element) {
            if (panel == null || element == null
                || Math.Abs(panel.ActualWidth - 0) < double.Epsilon
                || Math.Abs(panel.ActualHeight - 0) < double.Epsilon)
                return ViewRenderTransformOriginDefaultValue;

            // The Old Solution:
            //var panelPoint = panel.PointToScreen(ZeroPoint);
            //var elementPoint = element.PointToScreen(ZeroPoint);
            //var centerX = (elementPoint.X - panelPoint.X) + (element.ActualWidth / 2);
            //var centerY = (elementPoint.Y - panelPoint.Y) + (element.ActualHeight / 2);

            // The New One:
            var relativeElementPoint = element.TransformToAncestor(panel).Transform(new Point(0, 0));
            var centerX = (relativeElementPoint.X) + (element.ActualWidth / 2);
            var centerY = (relativeElementPoint.Y) + (element.ActualHeight / 2);

            var percentX = centerX * 100 / panel.ActualWidth;
            var percentY = centerY * 100 / panel.ActualHeight;
            var originX = percentX / 100;
            var originY = percentY / 100;

            return new Point(originX, originY);
        }

    }

}