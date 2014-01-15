using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Kavand.Windows.Controls {

    /// <summary>
    /// Represents the text input of a <see cref="T:Kavand.Windows.Controls.DatePicker"/>.
    /// </summary>
    [TemplatePart(Name = PartWatermarkName, Type = typeof(ContentControl))]
    public sealed class DatePickerTextBox : TextBox, IVisualStateUpdateable {

        public static readonly Type Typeof = typeof(DatePickerTextBox);

        internal static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register("Watermark", typeof(object), Typeof, new PropertyMetadata(OnWatermarkPropertyChanged));

        private ContentControl _elementWatermark;

        private const string PartWatermarkName = "PART_Watermark";

        internal object Watermark {
            get { return GetValue(WatermarkProperty); }
            set { SetValue(WatermarkProperty, value); }
        }

        static DatePickerTextBox() {
            DefaultStyleKeyProperty.OverrideMetadata(Typeof, new FrameworkPropertyMetadata(Typeof));
            TextProperty.OverrideMetadata(Typeof, new FrameworkPropertyMetadata(VisualStates.OnVisualStatePropertyChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Windows.Controls.Primitives.DatePickerTextBox"/> class.
        /// </summary>
        public DatePickerTextBox() {
            SetCurrentValue(WatermarkProperty, "Default Watermark Text");
            Loaded += OnLoaded;
            IsEnabledChanged += OnDatePickerTextBoxIsEnabledChanged;
        }

        /// <summary>
        /// Builds the visual tree for the <see cref="T:System.Windows.Controls.Primitives.DatePickerTextBox"/> when a new template is applied.
        /// </summary>
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            _elementWatermark = ExtractTemplatePart<ContentControl>(PartWatermarkName);
            if (_elementWatermark != null)
                _elementWatermark.SetBinding(ContentControl.ContentProperty, new Binding("Watermark") { Source = this });
            OnWatermarkChanged();
            UpdateVisualState();
        }

        protected override void OnGotFocus(RoutedEventArgs e) {
            base.OnGotFocus(e);
            if (!IsEnabled || string.IsNullOrEmpty(Text))
                return;
            Select(0, Text.Length);
        }

        public void UpdateVisualState() {
            UpdateVisualState(true);
        }

        public void UpdateVisualState(bool useTransitions) {
            VisualStates.UpdateVisualState(this, useTransitions);
            if (Watermark != null && string.IsNullOrEmpty(Text))
                VisualStates.GoToState(this, useTransitions, VisualStates.StateWatermarked, VisualStates.StateUnwatermarked);
            else
                VisualStates.GoToState(this, useTransitions, VisualStates.StateUnwatermarked);
        }

        private T ExtractTemplatePart<T>(string partName) where T : DependencyObject {
            var templateChild = GetTemplateChild(partName);
            return templateChild as T;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            ApplyTemplate();
        }

        private void OnDatePickerTextBoxIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var flag = (bool)e.NewValue;
            SetCurrentValue(IsReadOnlyProperty, BooleanBoxes.Box(!flag));
        }

        private void OnWatermarkChanged() {
            if (_elementWatermark == null)
                return;
            var control = Watermark as Control;
            if (control == null)
                return;
            control.IsTabStop = false;
            control.IsHitTestVisible = false;
        }

        private static void OnWatermarkPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) {
            var datePickerTextBox = sender as DatePickerTextBox;
            Debug.Assert(datePickerTextBox != null);
            datePickerTextBox.OnWatermarkChanged();
            datePickerTextBox.UpdateVisualState();
        }
    }

}