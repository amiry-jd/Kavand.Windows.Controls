using System;

namespace Kavand.Windows.Controls {

    internal class DateRangeChangingEventArgs : EventArgs {

        private readonly DateTime _start;
        private readonly DateTime _end;

        public DateTime Start {
            get {
                return _start;
            }
        }

        public DateTime End {
            get {
                return _end;
            }
        }

        public DateRangeChangingEventArgs(DateTime start, DateTime end) {
            _start = start;
            _end = end;
        }
    }

}