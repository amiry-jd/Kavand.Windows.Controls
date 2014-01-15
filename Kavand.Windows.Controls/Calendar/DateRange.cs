using System;
using System.ComponentModel;
using System.Threading;

namespace Kavand.Windows.Controls {

    /// <summary>
    /// Represents a range of dates in a <see cref="T:Kavand.Windows.Controls.Calendar"/>.
    /// </summary>
    public sealed class DateRange : INotifyPropertyChanged {

        private DateTime _end;
        private DateTime _start;
        private PropertyChangedEventHandler _propertyChanged;
        private EventHandler<DateRangeChangingEventArgs> _changing;

        /// <summary>
        /// Gets the last date in the represented range.
        /// </summary>
        /// 
        /// <returns>
        /// The last date in the represented range.
        /// </returns>
        public DateTime End {
            get {
                return CoerceEnd(_start, _end);
            }
            set {
                var end = CoerceEnd(_start, value);
                if (!(end != End))
                    return;
                OnChanging(new DateRangeChangingEventArgs(_start, end));
                _end = value;
                OnPropertyChanged(new PropertyChangedEventArgs("End"));
            }
        }

        /// <summary>
        /// Gets the first date in the represented range.
        /// </summary>
        /// 
        /// <returns>
        /// The first date in the represented range.
        /// </returns>
        public DateTime Start {
            get {
                return _start;
            }
            set {
                if (!(_start != value))
                    return;
                var end1 = End;
                var end2 = CoerceEnd(value, _end);
                OnChanging(new DateRangeChangingEventArgs(value, end2));
                _start = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Start"));
                if (!(end2 != end1))
                    return;
                OnPropertyChanged(new PropertyChangedEventArgs("End"));
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged {
            add {
                var changedEventHandler = _propertyChanged;
                PropertyChangedEventHandler comparand;
                do {
                    comparand = changedEventHandler;
                    changedEventHandler = Interlocked.CompareExchange(ref _propertyChanged, comparand + value, comparand);
                }
                while (changedEventHandler != comparand);
            }
            remove {
                var changedEventHandler = _propertyChanged;
                PropertyChangedEventHandler comparand;
                do {
                    comparand = changedEventHandler;
                    changedEventHandler = Interlocked.CompareExchange(ref _propertyChanged, comparand - value, comparand);
                }
                while (changedEventHandler != comparand);
            }
        }

        internal event EventHandler<DateRangeChangingEventArgs> Changing {
            add {
                var eventHandler = _changing;
                EventHandler<DateRangeChangingEventArgs> comparand;
                do {
                    comparand = eventHandler;
                    eventHandler = Interlocked.CompareExchange(ref _changing, comparand + value, comparand);
                }
                while (eventHandler != comparand);
            }
            remove {
                var eventHandler = _changing;
                EventHandler<DateRangeChangingEventArgs> comparand;
                do {
                    comparand = eventHandler;
                    eventHandler = Interlocked.CompareExchange(ref _changing, comparand - value, comparand);
                }
                while (eventHandler != comparand);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Kavand.Windows.Controls.DateRange"/> class.
        /// </summary>
        public DateRange()
            : this(DateTime.MinValue, DateTime.MaxValue) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Kavand.Windows.Controls.DateRange"/> class with a single date.
        /// </summary>
        /// <param name="day">The date to add.</param>
        public DateRange(DateTime day)
            : this(day, day) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Kavand.Windows.Controls.DateRange"/> class with a range of dates.
        /// </summary>
        /// <param name="start">The start of the range to be represented.</param><param name="end">The end of the range to be represented.</param>
        public DateRange(DateTime start, DateTime end) {
            _start = start;
            _end = end;
        }

        internal bool ContainsAny(DateRange range) {
            if (range.End >= Start)
                return End >= range.Start;
            return false;
        }

        private void OnChanging(DateRangeChangingEventArgs e) {
            var eventHandler = _changing;
            if (eventHandler != null)
                eventHandler(this, e);
        }

        private void OnPropertyChanged(PropertyChangedEventArgs e) {
            var changedEventHandler = _propertyChanged;
            if (changedEventHandler != null)
                changedEventHandler(this, e);
        }

        private static DateTime CoerceEnd(DateTime start, DateTime end) {
            return DateTime.Compare(start, end) > 0 ? start : end;
        }
    }

}