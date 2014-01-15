using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Kavand.Windows.Controls {

    /// <summary>
    /// Represents a day on a <see cref="T:Kavand.Windows.Controls.Calendar"/>.
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

    [TemplateVisualState(GroupName = VisualStates.GroupHighlight, Name = VisualStates.StateHighlightOn)]
    [TemplateVisualState(GroupName = VisualStates.GroupHighlight, Name = VisualStates.StateHighlightOff)]

    [TemplateVisualState(GroupName = VisualStates.GroupActive, Name = VisualStates.StateActive)]
    [TemplateVisualState(GroupName = VisualStates.GroupActive, Name = VisualStates.StateInactive)]

    [TemplateVisualState(GroupName = VisualStates.GroupDay, Name = VisualStates.StateRegularDay)]
    [TemplateVisualState(GroupName = VisualStates.GroupDay, Name = VisualStates.StateToday)]

    [TemplateVisualState(GroupName = VisualStates.GroupBlackout, Name = VisualStates.StateNormalDay)]
    [TemplateVisualState(GroupName = VisualStates.GroupBlackout, Name = VisualStates.StateBlackoutDay)]

    [TemplateVisualState(GroupName = VisualStates.GroupCalendarButtonFocus, Name = VisualStates.StateCalendarButtonFocused)]
    [TemplateVisualState(GroupName = VisualStates.GroupCalendarButtonFocus, Name = VisualStates.StateCalendarButtonUnfocused)]
    public sealed class DayButton : Button, IVisualStateUpdateable {

        #region IsToday

        internal static readonly DependencyPropertyKey IsTodayPropertyKey
            = DependencyProperty.RegisterReadOnly("IsToday", typeof(bool), typeof(DayButton),
                new FrameworkPropertyMetadata(false, VisualStates.OnVisualStatePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.DayButton.IsToday"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.DayButton.IsToday"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty IsTodayProperty = IsTodayPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value that indicates whether the date represented by this button is the current date.
        /// </summary> 
        /// <returns>
        /// true if the date is the current date; otherwise, false.
        /// </returns>
        public bool IsToday {
            get { return (bool)GetValue(IsTodayProperty); }
        }

        #endregion

        #region IsSelected

        internal static readonly DependencyPropertyKey IsSelectedPropertyKey
            = DependencyProperty.RegisterReadOnly("IsSelected", typeof(bool), typeof(DayButton),
                new FrameworkPropertyMetadata(false, VisualStates.OnVisualStatePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.DayButton.IsSelected"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.DayButton.IsSelected"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty IsSelectedProperty = IsSelectedPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value that indicates whether the date represented by this button is selected.
        /// </summary> 
        /// <returns>
        /// true if the date is selected; otherwise, false.
        /// </returns>
        public bool IsSelected {
            get { return (bool)GetValue(IsSelectedProperty); }
        }

        #endregion

        #region IsInactive

        internal static readonly DependencyPropertyKey IsInactivePropertyKey
            = DependencyProperty.RegisterReadOnly("IsInactive", typeof(bool), typeof(DayButton),
            new FrameworkPropertyMetadata(false, VisualStates.OnVisualStatePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.DayButton.IsInactive"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.DayButton.IsInactive"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty IsInactiveProperty = IsInactivePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value that indicates whether this button represents a day that is not in the currently displayed month.
        /// </summary> 
        /// <returns>
        /// true if the button represents a day that is not in the currently displayed month; otherwise, false.
        /// </returns>
        public bool IsInactive {
            get { return (bool)GetValue(IsInactiveProperty); }
        }

        #endregion

        #region IsBlackedOut

        internal static readonly DependencyPropertyKey IsBlackedOutPropertyKey
            = DependencyProperty.RegisterReadOnly("IsBlackedOut", typeof(bool), typeof(DayButton),
                new FrameworkPropertyMetadata(false, VisualStates.OnVisualStatePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.DayButton.IsBlackedOut"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.DayButton.IsBlackedOut"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty IsBlackedOutProperty = IsBlackedOutPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value that indicates whether the date is unavailable.
        /// </summary> 
        /// <returns>
        /// true if the date unavailable; otherwise, false.
        /// </returns>
        public bool IsBlackedOut {
            get { return (bool)GetValue(IsBlackedOutProperty); }
        }

        #endregion

        #region IsHighlighted

        internal static readonly DependencyPropertyKey IsHighlightedPropertyKey
            = DependencyProperty.RegisterReadOnly("IsHighlighted", typeof(bool), typeof(DayButton),
                new FrameworkPropertyMetadata(false, VisualStates.OnVisualStatePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="P:Kavand.Windows.Controls.DayButton.IsHighlighted"/> dependency property.
        /// </summary> 
        /// <returns>
        /// The identifier for the <see cref="P:Kavand.Windows.Controls.DayButton.IsHighlighted"/> dependency property.
        /// </returns>
        public static readonly DependencyProperty IsHighlightedProperty = IsHighlightedPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value that indicates whether this button is highlighted.
        /// </summary> 
        /// <returns>
        /// true if the button is highlighted; otherwise, false.
        /// </returns>
        public bool IsHighlighted {
            get { return (bool)GetValue(IsHighlightedProperty); }
        }

        #endregion

        internal Calendar Owner { get; set; }

        static DayButton() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DayButton), new FrameworkPropertyMetadata(typeof(DayButton)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Kavand.Windows.Controls.DayButton"/> class.
        /// </summary>
        // ReSharper disable EmptyConstructor
        public DayButton() {
            // ReSharper restore EmptyConstructor
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            UpdateVisualState(true);
        }

        protected override AutomationPeer OnCreateAutomationPeer() {
            return new CalendarButtonAutomationPeer(this);
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
            if (!VisualStateChangeSuspended) {
                ChangeVisualState(useTransitions);
            }
        }

        internal void ChangeVisualState(bool useTransitions) {

            if (IsSelected)
                VisualStates.GoToState(this, useTransitions, VisualStates.StateSelected, VisualStates.StateUnselected);
            else
                VisualStateManager.GoToState(this, VisualStates.StateUnselected, useTransitions);

            if (IsHighlighted)
                VisualStates.GoToState(this, useTransitions, VisualStates.StateHighlightOn, VisualStates.StateHighlightOff);
            else
                VisualStateManager.GoToState(this, VisualStates.StateHighlightOff, useTransitions);

            if (!IsInactive)
                VisualStates.GoToState(this, useTransitions, VisualStates.StateActive, VisualStates.StateInactive);
            else
                VisualStateManager.GoToState(this, VisualStates.StateInactive, useTransitions);

            if (IsToday && Owner != null && Owner.IsTodayHighlighted)
                VisualStates.GoToState(this, useTransitions, VisualStates.StateToday, VisualStates.StateRegularDay);
            else
                VisualStateManager.GoToState(this, VisualStates.StateRegularDay, useTransitions);

            if (IsBlackedOut)
                VisualStates.GoToState(this, useTransitions, VisualStates.StateBlackoutDay, VisualStates.StateNormalDay);
            else
                VisualStateManager.GoToState(this, VisualStates.StateNormalDay, useTransitions);

            if (IsKeyboardFocused)
                VisualStates.GoToState(this, useTransitions,
                    VisualStates.StateCalendarButtonFocused,
                    VisualStates.StateCalendarButtonUnfocused);
            else
                VisualStateManager.GoToState(this, VisualStates.StateCalendarButtonUnfocused, useTransitions);

            // VisualStates.UpdateVisualState(this, useTransitions);

        }

        internal void NotifyNeedsVisualStateUpdate() {
            UpdateVisualState();
        }

        #endregion

        internal void SetContentInternal(string value) {
            SetCurrentValue(ContentProperty, value);
        }

    }

}