using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Kavand.Windows.Controls {

    public static class VisualStates {

        internal static void OnVisualStatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var control = d as IVisualStateUpdateable;
            if (control != null)
                control.UpdateVisualState();
        }

        public const string GroupCommon = "CommonStates";
        public const string StateNormal = "Normal";
        public const string StateMouseOver = "MouseOver";
        public const string StatePressed = "Pressed";
        public const string StateDisabled = "Disabled";
        public const string StateReadOnly = "ReadOnly";

        public const string GroupFocus = "FocusStates";
        public const string StateFocused = "Focused";
        public const string StateFocusedDropDown = "FocusedDropDown";
        public const string StateUnfocused = "Unfocused";

        public const string GroupValidation = "ValidationStates";
        public const string StateValid = "Valid";
        public const string StateInvalidFocused = "InvalidFocused";
        public const string StateInvalidUnfocused = "InvalidUnfocused";

        public const string GroupSelection = "SelectionStates";
        public const string StateSelected = "Selected";
        public const string StateSelectedUnfocused = "SelectedUnfocused";
        public const string StateSelectedInactive = "SelectedInactive";
        public const string StateUnselected = "Unselected";

        public const string GroupHighlight = "HighlightStates";
        public const string StateHighlightOn = "HighlightOn";
        public const string StateHighlightOff = "HighlightOff";

        public const string GroupActive = "ActiveStates";
        public const string StateActive = "Active";
        public const string StateInactive = "Inactive";

        public const string GroupCalendarButtonFocus = "CalendarButtonFocusStates";
        public const string StateCalendarButtonUnfocused = "CalendarButtonUnfocused";
        public const string StateCalendarButtonFocused = "CalendarButtonFocused";

        public const string GroupDay = "DayStates";
        public const string StateToday = "Today";
        public const string StateRegularDay = "RegularDay";

        public const string GroupBlackout = "BlackoutDayStates";
        public const string StateBlackoutDay = "BlackoutDay";
        public const string StateNormalDay = "NormalDay";

        public const string GroupWatermark = "WatermarkStates";
        public const string StateUnwatermarked = "Unwatermarked";
        public const string StateWatermarked = "Watermarked";

        public static void GoToState(Control control, bool useTransitions, params string[] stateNames) {
            if (stateNames == null)
                return;
            foreach (var stateName in stateNames)
                if (VisualStateManager.GoToState(control, stateName, useTransitions))
                    break;
        }

        public static void UpdateVisualStateBase(Control control, bool useTransitions) {
            if (Validation.GetHasError(control))
                VisualStateManager.GoToState(
                    control,
                    control.IsKeyboardFocused ? StateInvalidFocused : StateInvalidUnfocused,
                    useTransitions);
            else
                VisualStateManager.GoToState(control, StateValid, useTransitions);
        }

        /// <summary> 
        ///     Change to the correct visual state for the ButtonBase. 
        /// </summary>
        /// <param name="button">The ButtonBase to change the state </param>
        /// <param name="useTransitions"> 
        ///     true to use transitions when updating the visual state, false to
        ///     snap directly to the new visual state.
        /// </param>
        public static void UpdateVisualState(ButtonBase button, bool useTransitions) {
            if (!button.IsEnabled)
                VisualStateManager.GoToState(button, StateDisabled, useTransitions);
            else if (button.IsPressed)
                VisualStateManager.GoToState(button, StatePressed, useTransitions);
            else if (button.IsMouseOver)
                VisualStateManager.GoToState(button, StateMouseOver, useTransitions);
            else
                VisualStateManager.GoToState(button, StateNormal, useTransitions);

            VisualStateManager.GoToState(
                button,
                button.IsKeyboardFocused ? StateFocused : StateUnfocused,
                useTransitions);

            UpdateVisualStateBase(button, useTransitions);
        }

        /// <summary> 
        ///     Change to the correct visual state for the TextBoxBase. 
        /// </summary>
        /// <param name="textBox">The TextBoxBase to change the state </param>
        /// <param name="useTransitions"> 
        ///     true to use transitions when updating the visual state, false to
        ///     snap directly to the new visual state.
        /// </param>
        public static void UpdateVisualState(TextBoxBase textBox, bool useTransitions) {
            // See ButtonBase.ChangeVisualState.
            // This method should be exactly like it, except we have a ReadOnly state instead of Pressed
            if (!textBox.IsEnabled)
                VisualStateManager.GoToState(textBox, StateDisabled, useTransitions);
            else if (textBox.IsReadOnly)
                VisualStateManager.GoToState(textBox, StateReadOnly, useTransitions);
            else if (textBox.IsMouseOver)
                VisualStateManager.GoToState(textBox, StateMouseOver, useTransitions);
            else
                VisualStateManager.GoToState(textBox, StateNormal, useTransitions);

            VisualStateManager.GoToState(
                textBox, textBox.IsKeyboardFocused ? StateFocused : StateUnfocused, useTransitions);

            UpdateVisualStateBase(textBox, useTransitions);

        }
    }
}