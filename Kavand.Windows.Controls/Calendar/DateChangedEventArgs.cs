using System;
using System.Windows;

namespace Kavand.Windows.Controls {

    /// <summary>
    /// Provides data for the <see cref="E:Kavand.Windows.Controls.Calendar.DisplayDateChanged"/> event.
    /// </summary>
    public class DateChangedEventArgs : RoutedEventArgs {

        private readonly DateTime? _removedDate;
        private readonly DateTime? _addedDate;

        /// <summary>
        /// Gets or sets the date to be newly displayed.
        /// </summary>
        /// 
        /// <returns>
        /// The date to be newly displayed.
        /// </returns>
        public DateTime? AddedDate {
            get { return _addedDate; }
        }

        /// <summary>
        /// Gets or sets the date that was previously displayed.
        /// </summary>
        /// 
        /// <returns>
        /// The date that was previously displayed.
        /// </returns>
        public DateTime? RemovedDate {
            get { return _removedDate; }
        }

        internal DateChangedEventArgs(DateTime? removedDate, DateTime? addedDate) {
            _removedDate = removedDate;
            _addedDate = addedDate;
        }
    }
}