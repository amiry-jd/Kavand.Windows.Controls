using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Kavand.Windows.Controls {

    /// <summary>
    /// Represents a month or year on a <see cref="T:Kavand.Windows.Controls.Calendar"/> object.
    /// </summary>
    [TemplateVisualState(GroupName = VisualStates.GroupCommon, Name = VisualStates.StateNormal)]
    [TemplateVisualState(GroupName = VisualStates.GroupCommon, Name = VisualStates.StateMouseOver)]
    [TemplateVisualState(GroupName = VisualStates.GroupCommon, Name = VisualStates.StatePressed)]
    [TemplateVisualState(GroupName = VisualStates.GroupCommon, Name = VisualStates.StateDisabled)]

    [TemplateVisualState(GroupName = VisualStates.GroupFocus, Name = VisualStates.StateFocused)]
    [TemplateVisualState(GroupName = VisualStates.GroupFocus, Name = VisualStates.StateUnfocused)]

    [TemplateVisualState(GroupName = VisualStates.GroupValidation, Name = VisualStates.StateValid)]
    [TemplateVisualState(GroupName = VisualStates.GroupValidation, Name = VisualStates.StateInvalidFocused)]
    [TemplateVisualState(GroupName = VisualStates.GroupValidation, Name = VisualStates.StateInvalidUnfocused)]

    [TemplateVisualState(GroupName = VisualStates.GroupSelection, Name = VisualStates.StateSelected)]
    [TemplateVisualState(GroupName = VisualStates.GroupSelection, Name = VisualStates.StateUnselected)]

    [TemplateVisualState(GroupName = VisualStates.GroupActive, Name = VisualStates.StateActive)]
    [TemplateVisualState(GroupName = VisualStates.GroupActive, Name = VisualStates.StateInactive)]

    [TemplateVisualState(GroupName = VisualStates.GroupCalendarButtonFocus, Name = VisualStates.StateCalendarButtonFocused)]
    [TemplateVisualState(GroupName = VisualStates.GroupCalendarButtonFocus, Name = VisualStates.StateCalendarButtonUnfocused)]
    public sealed class MonthYearButton : Button, IVisualStateUpdateable {

        #region DependencyProperty HasSelectedDays

        internal static readonly DependencyPropertyKey HasSelectedDaysPropertyKey
            = DependencyProperty.RegisterReadOnly("HasSelectedDays", typeof(bool), typeof(MonthYearButton),
                new FrameworkPropertyMetadata(false, VisualStates.OnVisualStatePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.MonthYearButton.HasSelectedDays"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.MonthYearButton.HasSelectedDays"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty HasSelectedDaysProperty = HasSelectedDaysPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value that indicates whether this button represents a year or month that contains selected dates.
        /// </summary> 
        /// <returns>
        /// true if this button represents a year or month that contains selected dates; otherwise, false.
        /// </returns>
        public bool HasSelectedDays {
            get {
                return (bool)GetValue(HasSelectedDaysProperty);
            }
            internal set {
                SetValue(HasSelectedDaysPropertyKey, value);
            }
        }

        #endregion

        #region DependencyProperty IsInactive

        internal static readonly DependencyPropertyKey IsInactivePropertyKey
            = DependencyProperty.RegisterReadOnly("IsInactive", typeof(bool), typeof(MonthYearButton),
            new FrameworkPropertyMetadata(false, VisualStates.OnVisualStatePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.MonthYearButton.IsInactive"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.MonthYearButton.IsInactive"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty IsInactiveProperty = IsInactivePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value that indicates whether this button represents a year that is not in the currently displayed decade.
        /// </summary> 
        /// <returns>
        /// true if this button represents a day that is not in the currently displayed month, or a year that is not in the currently displayed decade; otherwise, false.
        /// </returns>
        public bool IsInactive {
            get {
                return (bool)GetValue(IsInactiveProperty);
            }
            internal set {
                SetValue(IsInactivePropertyKey, value);
            }
        }

        #endregion

        internal Calendar Owner { get; set; }

        static MonthYearButton() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MonthYearButton), new FrameworkPropertyMetadata(typeof(MonthYearButton)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Kavand.Windows.Controls.MonthYearButton"/> class.
        /// </summary>
        // ReSharper disable EmptyConstructor
        public MonthYearButton() {
            // ReSharper restore EmptyConstructor
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            UpdateVisualState(true);
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
            //if (VisualStateChangeSuspended)
            //    return;
            VisualStateChangeSuspended = true;
            ChangeVisualState(useTransitions);
            VisualStateChangeSuspended = false;
        }

        internal void ChangeVisualState(bool useTransitions) {

            // Update the SelectionStates group
            if (HasSelectedDays)
                VisualStates.GoToState(this, useTransitions, VisualStates.StateSelected, VisualStates.StateUnselected);
            else
                VisualStateManager.GoToState(this, VisualStates.StateUnselected, useTransitions);

            // Update the ActiveStates group 
            if (!IsInactive)
                VisualStates.GoToState(this, useTransitions, VisualStates.StateActive, VisualStates.StateInactive);
            else
                VisualStateManager.GoToState(this, VisualStates.StateInactive, useTransitions);

            // Update the FocusStates group
            if (IsKeyboardFocused)
                VisualStates.GoToState(this, useTransitions,
                    VisualStates.StateCalendarButtonFocused, VisualStates.StateCalendarButtonUnfocused);
            else
                VisualStateManager.GoToState(this, VisualStates.StateCalendarButtonUnfocused, useTransitions);

            // VisualStates.UpdateVisualState(this, useTransitions);
        }

        internal void NotifyNeedsVisualStateUpdate() {
            UpdateVisualState();
        }

        #endregion

        protected override AutomationPeer OnCreateAutomationPeer() {
            return new CalendarButtonAutomationPeer(this);
        }

        internal void SetContentInternal(string value) {
            SetCurrentValue(ContentProperty, value);
        }

    }

}