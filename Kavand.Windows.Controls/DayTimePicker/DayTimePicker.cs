using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Kavand.Windows.Controls {

    public sealed class DayTimePicker : TextBox {

        private static readonly Type Typeof = typeof(DayTimePicker);
        private static readonly TimeSpan DefaultMinValue = TimeSpan.Zero;
        private static readonly TimeSpan DefaultMaxValue = new TimeSpan(0, 23, 59, 59, 999);
        private static readonly TimeSpan HalfDay = new TimeSpan(0, 12, 0, 0, 0);
        private const string MaskFormat = "{0}{0}{1}{0}{0} {2}";
        private const string ValueFormat = "{0:D2}{2}{1:D2} {3}";
        private const char DefaultPlaceHolder = '_';
        private const char DefaultDelimiter = ':';
        private const int HourSegmentStartIndex = 0;
        private const int HourSegmentEndIndex = 2;
        private const int MinuteSegmentStartIndex = 3;
        private const int MinuteSegmentEndIndex = 5;
        private const int DesignatorSegmentStartIndex = 6;
        private int _caretPosition;
        private bool _isPm;
        private bool _changingTextByUser;
        private bool _refreshingValueInternally;

        #region property IsRightToLeft
        internal bool IsRightToLeft { get { return FlowDirection == FlowDirection.RightToLeft; } }
        #endregion

        #region property CurrentSegment

        private TimeSegment? _currentSegment;
        public TimeSegment? CurrentSegment {
            get { return _currentSegment; }
            set {
                if (_currentSegment == value)
                    return;
                _currentSegment = value;
                OnCurrentSegmentChanged();
            }
        }

        #endregion

        #region dependency property MinValue

        public static readonly DependencyProperty MinValueProperty
            = DependencyProperty.Register("MinValue", typeof(TimeSpan), Typeof,
                new PropertyMetadata(DefaultMinValue, MinValueChangedCallback));

        private static void MinValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {

        }

        public TimeSpan MinValue {
            get { return (TimeSpan)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        #endregion

        #region dependency property MaxValue

        public static readonly DependencyProperty MaxValueProperty
            = DependencyProperty.Register("MaxValue", typeof(TimeSpan), Typeof,
                new PropertyMetadata(DefaultMaxValue, MaxValueChangedCallback));

        private static void MaxValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {

        }

        public TimeSpan MaxValue {
            get { return (TimeSpan)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        #endregion

        #region dependency property Value

        public static readonly DependencyProperty ValueProperty
            = DependencyProperty.Register("Value", typeof(TimeSpan?), Typeof,
                new PropertyMetadata(null, ValueChangedCallback));

        private static void ValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var picker = (DayTimePicker)d;
            if (!picker._refreshingValueInternally)
                picker.RefreshText();
            picker.ValidateValue();
            picker.OnValueChanged(new ValueChangedEventArgs<TimeSpan?>((TimeSpan?)e.OldValue, (TimeSpan?)e.NewValue));
        }

        public TimeSpan? Value {
            get { return (TimeSpan?)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public event EventHandler<ValueChangedEventArgs<TimeSpan?>> ValueChanged;

        public void OnValueChanged(ValueChangedEventArgs<TimeSpan?> e) {
            var handler = ValueChanged;
            if (handler != null)
                handler(this, e);
        }

        #endregion

        #region dependency property AcceptNullValue

        public static readonly DependencyProperty AcceptNullValueProperty
            = DependencyProperty.Register("AcceptNullValue", BooleanBoxes.Typeof, Typeof,
                new PropertyMetadata(BooleanBoxes.TrueBox, AcceptNullValueChangedCallback));

        private static void AcceptNullValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((DayTimePicker)d).ValidateValue();
        }

        public bool AcceptNullValue {
            get { return (bool)GetValue(AcceptNullValueProperty); }
            set { SetValue(AcceptNullValueProperty, value); }
        }

        #endregion

        #region dependency property PlaceHolder

        public static readonly DependencyProperty PlaceHolderProperty
            = DependencyProperty.Register("PlaceHolder", typeof(char), Typeof,
                new FrameworkPropertyMetadata(DefaultPlaceHolder,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null, PlaceHolderCoerceCallback));

        private static object PlaceHolderCoerceCallback(DependencyObject d, object basevalue) {
            return basevalue ?? DefaultPlaceHolder;
        }

        public char PlaceHolder {
            get { return (char)GetValue(PlaceHolderProperty); }
            set { SetValue(PlaceHolderProperty, value); }
        }

        #endregion

        #region dependency property Delimiter

        public static readonly DependencyProperty DelimiterProperty
            = DependencyProperty.Register("Delimiter", typeof(char), Typeof,
                new FrameworkPropertyMetadata(DefaultDelimiter,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null, DelimiterCoerceCallback));

        private static object DelimiterCoerceCallback(DependencyObject d, object basevalue) {
            return basevalue ?? DefaultDelimiter;
        }

        public char Delimiter {
            get { return (char)GetValue(DelimiterProperty); }
            set { SetValue(DelimiterProperty, value); }
        }

        #endregion

        #region .ctor

        static DayTimePicker() {
            CursorProperty.OverrideMetadata(Typeof, new FrameworkPropertyMetadata(Cursors.Arrow));
        }

        public DayTimePicker() {
            // disabling paste command
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, (s, e) => { }));
        }

        #endregion

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            RefreshText();
        }

        protected override void OnTextChanged(TextChangedEventArgs e) {
            base.OnTextChanged(e);
            if (_changingTextByUser) {
                _refreshingValueInternally = true;
                RefreshValue();
                _refreshingValueInternally = false;
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e) {
            Focus();
            FixTextPart();
            var isInASegment = TryFocusOnCorrectSegment(e.GetPosition(this));
            if (isInASegment)
                _caretPosition = SelectionStart;
            e.Handled = true;
            base.OnPreviewMouseDown(e);
        }

        protected override void OnContextMenuOpening(ContextMenuEventArgs e) {
            e.Handled = true;
            base.OnContextMenuOpening(e);
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e) {
            if (e.Delta != 0) {
                _changingTextByUser = true;
                e.Handled = true;
                OnUpDownKeyPressed(e.Delta > 0);
                _changingTextByUser = false;
            }
            base.OnPreviewMouseWheel(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e) {
            _changingTextByUser = true;
            e.Handled = true;
            int number;
            if ((number = TryGetNumberValueFromKeyValue(e.Key)) != -1)
                OnNumericKeyPressed(number);
            else
                switch (e.Key) {
                    case Key.Right:
                        OnRightLeftKeyPressed(true);
                        break;
                    case Key.Left:
                        OnRightLeftKeyPressed(false);
                        break;
                    case Key.Up:
                        OnUpDownKeyPressed(true);
                        break;
                    case Key.Down:
                        OnUpDownKeyPressed(false);
                        break;
                    case Key.Back:
                        OnBackKeyPressed();
                        break;
                    case Key.Delete:
                        OnDeleteKeyPressed();
                        break;
                    case Key.Space:
                        OnSpaceKeyPressed();
                        break;
                    // case Key.Return: same as Key.Enter:
                    case Key.Enter:
                    case Key.Tab:
                        e.Handled = OnTabEnterKeyPressed();
                        break;
                }
            base.OnPreviewKeyDown(e);
            _changingTextByUser = false;
        }

        private void OnNumericKeyPressed(int number) {
            switch (CurrentSegment) {
                case TimeSegment.Hour:
                    InsertDigitAtHourSegment(number);
                    break;
                case TimeSegment.Minute:
                    InsertDigitAtMinuteSegment(number);
                    break;
            }
        }

        private void OnRightLeftKeyPressed(bool right) {
            var forward = right != IsRightToLeft;
            TryMoveSegment(forward, true);
        }

        private void OnUpDownKeyPressed(bool up) {
            switch (CurrentSegment) {
                case TimeSegment.Designator:
                    ToggleDesignator();
                    break;
                case TimeSegment.Hour:
                    IncreaseDecreaseHourValue(up);
                    break;
                case TimeSegment.Minute:
                    IncreaseDecreaseMinuteValue(up);
                    break;
            }
        }

        private void OnBackKeyPressed() {
            switch (CurrentSegment) {
                case TimeSegment.Designator:
                    CurrentSegment = TimeSegment.Minute;
                    _caretPosition = MinuteSegmentStartIndex;
                    break;
                case TimeSegment.Hour:
                    DoDeleteFromHourSegment();
                    break;
                case TimeSegment.Minute:
                    DoDeleteFromMinuteSegment();
                    break;
            }
        }

        private void OnDeleteKeyPressed() {
            switch (CurrentSegment) {
                case TimeSegment.Hour:
                    DoDeleteFromHourSegment();
                    break;
                case TimeSegment.Minute:
                    DoDeleteFromMinuteSegment();
                    break;
            }
        }

        private void OnSpaceKeyPressed() {
            switch (CurrentSegment) {
                case TimeSegment.Designator:
                    ToggleDesignator();
                    break;
                case TimeSegment.Hour:
                    DoDeleteFromHourSegment();
                    break;
                case TimeSegment.Minute:
                    DoDeleteFromMinuteSegment();
                    break;
            }
        }

        private void DoDeleteFromHourSegment() {
            DoDeleteFromSegment(HourSegmentStartIndex);
        }

        private void DoDeleteFromMinuteSegment() {
            DoDeleteFromSegment(MinuteSegmentStartIndex);
        }

        private void DoDeleteFromSegment(int index) {
            var text = Text.Substring(index, 2);
            if (text[1] != PlaceHolder) {
                _caretPosition = index + 1;
                InsertStringAtPosition(_caretPosition, GetString(PlaceHolder));
                OnCurrentSegmentChanged();
                return;
            }
            if (text[0] == PlaceHolder)
                return;
            _caretPosition = index;
            InsertStringAtPosition(_caretPosition, GetString(PlaceHolder));
            OnCurrentSegmentChanged();
        }

        private bool OnTabEnterKeyPressed() {
            bool shift, ctrl;
            KeyboardHelper.GetMetaKeyState(out ctrl, out shift);
            return TryMoveSegment(!shift, false);
        }

        private bool TryMoveSegment(bool forward, bool circling) {
            TimeSegment target;
            if (_currentSegment.HasValue)
                target = _currentSegment.Value + (forward ? 1 : -1);
            else
                target = forward ? TimeSegment.Hour : TimeSegment.Designator;
            if (target > TimeSegment.Designator)
                target = circling ? TimeSegment.Hour : TimeSegment.Designator;
            else if (target < TimeSegment.Hour)
                target = circling ? TimeSegment.Designator : TimeSegment.Hour;
            if (target != _currentSegment) {
                CurrentSegment = target;
                return true;
            }
            return false;
        }

        private void OnCurrentSegmentChanged() {
            int selectionStart = 0, selectionLength = 0;
            switch (CurrentSegment) {
                case TimeSegment.Hour:
                    selectionStart = HourSegmentStartIndex;
                    selectionLength = 2;
                    break;
                case TimeSegment.Minute:
                    selectionStart = MinuteSegmentStartIndex;
                    selectionLength = 2;
                    break;
                case TimeSegment.Designator:
                    selectionStart = DesignatorSegmentStartIndex;
                    selectionLength = Text.Length - DesignatorSegmentStartIndex;
                    break;
            }
            Dispatcher.Invoke(DispatcherPriority.Input, (Action)(() => Select(selectionStart, selectionLength)));
        }

        private bool TryFocusOnCorrectSegment(Point point) {
            var caretIndex = GetCharacterIndexFromPoint(point, false);
            if (caretIndex >= HourSegmentStartIndex && caretIndex <= HourSegmentEndIndex)
                CurrentSegment = TimeSegment.Hour;
            else if (caretIndex >= MinuteSegmentStartIndex && caretIndex <= MinuteSegmentEndIndex)
                CurrentSegment = TimeSegment.Minute;
            else if (caretIndex >= DesignatorSegmentStartIndex && caretIndex < Text.Length)
                CurrentSegment = TimeSegment.Designator;
            else {
                // we want to open a drop down menu here, so we have to set current segment to null:
                // CurrentSegment = null;
                // but for now, while our drop down is not ready, we just focus on first segment:
                CurrentSegment = TimeSegment.Hour;
            }
            return CurrentSegment != null;
        }

        private void FixTextPart() {

        }

        private int TryGetNumberValueFromKeyValue(Key key) {
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
                return key - Key.NumPad0;
            if (key >= Key.D0 && key <= Key.D9)
                return key - Key.D0;
            return -1;
        }

        private void IncreaseDecreaseHourValue(bool increase) {
            var temp = GetNumberAtPosition(HourSegmentStartIndex, 2);
            var hour = temp ?? 0;
            hour = increase ? ++hour : --hour;
            if (increase && (hour > HalfDay.Hours || hour > MaxValue.Hours || hour < MinValue.Hours))
                hour = 1;
            else if (!increase && hour <= 0)
                hour = HalfDay.Hours;
            if (hour == 12)
                ToggleDesignator();
            InsertStringAtPosition(HourSegmentStartIndex, GetString(hour, "D2"));
            OnCurrentSegmentChanged();
        }

        private void InsertDigitAtHourSegment(int digit) {
            if (_caretPosition == HourSegmentStartIndex)
                InsertDigitAtHourSegmentLeft(digit);
            else
                InsertDigitAtHourSegmentRight(digit);
            OnCurrentSegmentChanged();
        }

        private void InsertDigitAtHourSegmentLeft(int digit) {
            if (digit > 1) {
                InsertStringAtPosition(HourSegmentStartIndex, GetString(0));
                _caretPosition++;
                InsertDigitAtHourSegmentRight(digit, 0);
                return;
            }
            InsertStringAtPosition(HourSegmentStartIndex + 1, GetString(PlaceHolder));
            if (digit >= MinValue.Hours / 10 && digit <= MaxValue.Hours / 10) {
                InsertStringAtPosition(HourSegmentStartIndex, GetString(digit));
                _caretPosition++;
            } else
                InsertStringAtPosition(HourSegmentStartIndex, GetString(PlaceHolder));
        }

        private void InsertDigitAtHourSegmentRight(int digit, int? leftDigit = null) {
            // we don't want 00 at hour segment, so:
            leftDigit = leftDigit ?? GetNumberAtPosition(HourSegmentStartIndex, 1);
            if (leftDigit != null && leftDigit.Value == 0 && digit == 0)
                return;
            var temp = GetNumberAtPosition(HourSegmentStartIndex, 1);
            var hour = (temp.HasValue ? temp.Value * 10 : 0) + digit;
            if (hour <= HalfDay.Hours && hour >= MinValue.Hours && hour <= MaxValue.Hours) {
                InsertStringAtPosition(HourSegmentStartIndex + 1, GetString(digit));
                _caretPosition = MinuteSegmentStartIndex;
                CurrentSegment = TimeSegment.Minute;
            } else
                InsertStringAtPosition(HourSegmentStartIndex + 1, GetString(PlaceHolder));
        }

        private int? GetNumberAtPosition(int position, int length) {
            var s = Text.Substring(position, length);
            int n;
            if (int.TryParse(s, out n))
                return n;
            return null;
        }

        private void IncreaseDecreaseMinuteValue(bool increase) {
            var temp = GetNumberAtPosition(MinuteSegmentStartIndex, 2);
            var minute = temp ?? 0;
            minute = increase ? ++minute : --minute;
            if (increase && minute >= 60)
                minute = 0;
            else if (!increase && minute < 0)
                minute = 59;
            InsertStringAtPosition(MinuteSegmentStartIndex, GetString(minute, "D2"));
            OnCurrentSegmentChanged();
        }

        private void InsertDigitAtMinuteSegment(int digit) {
            if (_caretPosition == MinuteSegmentStartIndex)
                InsertDigitAtMinuteSegmentLeft(digit);
            else
                InsertDigitAtMinuteSegmentRight(digit);
            OnCurrentSegmentChanged();
        }

        private void InsertDigitAtMinuteSegmentLeft(int digit) {
            if (digit > 5) {
                InsertStringAtPosition(MinuteSegmentStartIndex, GetString(0));
                _caretPosition++;
                InsertDigitAtMinuteSegmentRight(digit);
                return;
            }
            InsertStringAtPosition(MinuteSegmentStartIndex + 1, GetString(PlaceHolder));
            if (digit >= MinValue.Minutes / 10 && digit <= MaxValue.Minutes / 10) {
                InsertStringAtPosition(MinuteSegmentStartIndex, GetString(digit));
                _caretPosition++;
            } else
                InsertStringAtPosition(MinuteSegmentStartIndex, GetString(PlaceHolder));
        }

        private void InsertDigitAtMinuteSegmentRight(int digit) {
            var temp = GetNumberAtPosition(MinuteSegmentStartIndex, 1);
            var minute = (temp.HasValue ? temp.Value * 10 : 0) + digit;
            if (minute >= MinValue.Minutes && minute <= MaxValue.Minutes) {
                InsertStringAtPosition(MinuteSegmentStartIndex + 1, GetString(digit));
                _caretPosition = DesignatorSegmentStartIndex;
                CurrentSegment = TimeSegment.Designator;
            } else
                InsertStringAtPosition(MinuteSegmentStartIndex + 1, GetString(PlaceHolder));
        }

        private void ToggleDesignator() {
            _isPm = !_isPm;
            InsertStringAtPosition(DesignatorSegmentStartIndex, _isPm
                ? GetDesignatorString(false) : GetDesignatorString(true), true);
            OnCurrentSegmentChanged();
        }

        private void InsertStringAtPosition(int position, string s, bool removeToEnd = false) {
            var text = !removeToEnd
                ? Text.Remove(position, s.Length).Insert(position, s)
                : Text.Remove(position).Insert(position, s);
            SetTextInternal(text);
        }

        private string GetString(int digit, string format = null) {
            return format == null
                ? digit.ToString(CultureInfo.CurrentUICulture.NumberFormat)
                : digit.ToString(format, CultureInfo.CurrentUICulture.NumberFormat);
        }

        private string GetString(char c) {
            return c.ToString(CultureInfo.InvariantCulture);
        }

        private string GetDesignatorString(bool am) {
            var c = CultureInfo.CurrentUICulture.DateTimeFormat;
            return am ? c.AMDesignator : c.PMDesignator;
        }

        private void RefreshText() {
            var text = GetFormatedValue();
            SetTextInternal(text);
        }

        private void SetTextInternal(string text) {
            if (string.Compare(Text, text, StringComparison.Ordinal) != 0)
                // while we dont want Text be bindable, so we call SetValue() to override any binding in XAML.
                SetValue(TextProperty, text);
        }

        private string GetFormatedPlaceHolder() {
            return string.Format(MaskFormat, PlaceHolder, Delimiter, GetDesignatorString(true));
        }

        private string GetFormatedValue() {
            if (Value == null)
                return GetFormatedPlaceHolder();
            var value = Value.Value;
            if (value < HalfDay) {
                _isPm = false;
            } else {
                _isPm = true;
                value = value - HalfDay;
            }
            var result = string.Format(ValueFormat, value.Hours, value.Minutes, Delimiter,
                _isPm ? GetDesignatorString(false) : GetDesignatorString(true));
            return result;
        }

        private void RefreshValue() {
            var hour = GetNumberAtPosition(HourSegmentStartIndex, 2);
            var minute = GetNumberAtPosition(MinuteSegmentStartIndex, 2);
            if (hour == null || minute == null) {
                SetCurrentValue(ValueProperty, null);
                return;
            }
            var value = new TimeSpan(hour.Value, minute.Value, 0);
            if (_isPm && value < HalfDay)
                value += HalfDay;
            else if (!_isPm && value >= HalfDay)
                value -= HalfDay;
            SetCurrentValue(ValueProperty, value);
        }

        private void ValidateValue() {
            var be = GetBindingExpression(ValueProperty);
            if (be == null)
                return;
            var value = Value;
            string content = null;
            if (!AcceptNullValue && value == null) {
                content = "Value cannot be null.";
            } else if (value > MaxValue || value < MinValue) {
                content = string.Format("Value should be between {0} and {1}", MinValue, MaxValue);
            }
            if (content == null)
                return;
            var vr = new ValidationError(new DataErrorValidationRule(), be) { ErrorContent = content };
            Validation.MarkInvalid(be, vr);
        }

    }

}