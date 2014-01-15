using System;
using System.Collections;
using System.Windows;

namespace Kavand.Windows.Controls {

    /// <summary> 
    /// Workaround for Dev10 Bug 527138 UIElement.RaiseEvent(e) throws InvalidCastException when
    ///     e is of type SelectionChangedEventArgs 
    ///     e.RoutedEvent was registered with a handler not of type System.Windows.Controls.SelectionChangedEventHandler 
    /// </summary>
    public class SelectionChangedEventArgs : System.Windows.Controls.SelectionChangedEventArgs {

        /// <summary>
        /// Constructor
        /// </summary> 
        /// <param name="eventId">Routed Event</param>
        /// <param name="removedItems">Items removed from selection</param> 
        /// <param name="addedItems">Items added to selection</param> 
        public SelectionChangedEventArgs(RoutedEvent eventId, IList removedItems, IList addedItems) :
            base(eventId, removedItems, addedItems) {
        }

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget) {
            var handler = genericHandler as EventHandler<System.Windows.Controls.SelectionChangedEventArgs>;
            if (handler != null)
                handler(genericTarget, this);
            else
                base.InvokeEventHandler(genericHandler, genericTarget);
        }
    }

}