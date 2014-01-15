using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Controls;

namespace Kavand.Windows.Controls {

    /// <summary>
    /// Represents a set of selected dates in a <see cref="T:Kavand.Windows.Controls.Calendar"/>.
    /// </summary>
    public sealed class SelectedDatesCollection : ObservableCollection<DateTime> {

        private readonly Collection<DateTime> _addedItems;
        private readonly Collection<DateTime> _removedItems;
        private readonly Thread _dispatcherThread;
        private bool _isAddingRange;
        private readonly Calendar _owner;
        private DateTime? _maximumDate;
        private DateTime? _minimumDate;

        internal DateTime? MinimumDate {
            get {
                if (Count < 1)
                    return new DateTime?();
                if (!_minimumDate.HasValue) {
                    var t2 = this[0];
                    foreach (var t1 in this) {
                        if (DateTime.Compare(t1, t2) < 0)
                            t2 = t1;
                    }
                    _maximumDate = t2;
                }
                return _minimumDate;
            }
        }

        internal DateTime? MaximumDate {
            get {
                if (Count < 1)
                    return new DateTime?();
                if (!_maximumDate.HasValue) {
                    var t2 = this[0];
                    foreach (var t1 in this) {
                        if (DateTime.Compare(t1, t2) > 0)
                            t2 = t1;
                    }
                    _maximumDate = t2;
                }
                return _maximumDate;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Kavand.Windows.Controls.SelectedDatesCollection"/> class.
        /// </summary>
        /// <param name="owner">The <see cref="T:Kavand.Windows.Controls.Calendar"/> associated with this collection.</param>
        public SelectedDatesCollection(Calendar owner) {
            _dispatcherThread = Thread.CurrentThread;
            _owner = owner;
            _addedItems = new Collection<DateTime>();
            _removedItems = new Collection<DateTime>();
        }

        /// <summary>
        /// Adds all the dates in the specified range, which includes the first and last dates, to the collection.
        /// </summary>
        /// <param name="start">The first date to add to the collection.</param>
        /// <param name="end">The last date to add to the collection.</param>
        public void AddRange(DateTime start, DateTime end) {
            BeginAddRange();
            if (_owner.SelectionMode == CalendarSelectionMode.SingleRange && Count > 0)
                ClearInternal();
            foreach (var dateTime in GetDaysInRange(start, end))
                Add(dateTime);
            EndAddRange();
        }

        protected override void ClearItems() {
            if (!IsValidThread())
                throw new NotSupportedException("MultiThreadedCollectionChangeNotSupported");
            _owner.HoverStart = new DateTime?();
            ClearInternal(true);
        }

        protected override void InsertItem(int index, DateTime item) {
            if (!IsValidThread())
                throw new NotSupportedException("MultiThreadedCollectionChangeNotSupported");
            if (Contains(item))
                return;
            var collection = new Collection<DateTime>();
            var flag = CheckSelectionMode();
            if (!Calendar.IsValidDateSelection(_owner, item))
                throw new ArgumentOutOfRangeException();
            if (flag)
                index = 0;
            base.InsertItem(index, item);
            UpdateMinMax(item);
            if (index == 0 && (!_owner.SelectedDate.HasValue || DateTime.Compare(_owner.SelectedDate.Value, item) != 0))
                _owner.SelectedDate = item;
            if (!_isAddingRange) {
                collection.Add(item);
                RaiseSelectionChanged(_removedItems, collection);
                _removedItems.Clear();
                var num = _owner.Engine.CompareYearMonth(item, _owner.DisplayDateInternal);
                if (num >= 2 || num <= -2)
                    return;
                _owner.UpdatePresenter();
            } else
                _addedItems.Add(item);
        }

        protected override void RemoveItem(int index) {
            if (!IsValidThread())
                throw new NotSupportedException("MultiThreadedCollectionChangeNotSupported");
            if (index >= Count) {
                base.RemoveItem(index);
                ClearMinMax();
            } else {
                var collection1 = new Collection<DateTime>();
                var collection2 = new Collection<DateTime>();
                var num = _owner.Engine.CompareYearMonth(this[index], _owner.DisplayDateInternal);
                collection2.Add(this[index]);
                base.RemoveItem(index);
                ClearMinMax();
                if (index == 0)
                    _owner.SelectedDate = Count <= 0 ? new DateTime?() : this[0];
                RaiseSelectionChanged(collection2, collection1);
                if (num >= 2 || num <= -2)
                    return;
                _owner.UpdatePresenter();
            }
        }

        protected override void SetItem(int index, DateTime item) {
            if (!IsValidThread())
                throw new NotSupportedException("CalendarCollection_MultiThreadedCollectionChangeNotSupported");
            if (Contains(item))
                return;
            var collection1 = new Collection<DateTime>();
            var collection2 = new Collection<DateTime>();
            if (index >= Count) {
                base.SetItem(index, item);
                UpdateMinMax(item);
            } else {
                if (DateTime.Compare(this[index], item) == 0 || !Calendar.IsValidDateSelection(_owner, item))
                    return;
                collection2.Add(this[index]);
                base.SetItem(index, item);
                UpdateMinMax(item);
                collection1.Add(item);
                if (index == 0 && (!_owner.SelectedDate.HasValue || DateTime.Compare(_owner.SelectedDate.Value, item) != 0))
                    _owner.SelectedDate = item;
                RaiseSelectionChanged(collection2, collection1);
                var num = _owner.Engine.CompareYearMonth(item, _owner.DisplayDateInternal);
                if (num >= 2 || num <= -2)
                    return;
                _owner.UpdatePresenter();
            }
        }

        internal void AddRangeInternal(DateTime start, DateTime end) {
            BeginAddRange();
            var dateTime1 = start;
            foreach (DateTime dateTime2 in GetDaysInRange(start, end)) {
                if (Calendar.IsValidDateSelection(_owner, dateTime2)) {
                    Add(dateTime2);
                    dateTime1 = dateTime2;
                } else if (_owner.SelectionMode == CalendarSelectionMode.SingleRange) {
                    _owner.CurrentDate = dateTime1;
                    break;
                }
            }
            EndAddRange();
        }

        internal void ClearInternal() {
            ClearInternal(false);
        }

        internal void ClearInternal(bool fireChangeNotification) {
            if (Count <= 0)
                return;
            foreach (var dateTime in this)
                _removedItems.Add(dateTime);
            base.ClearItems();
            ClearMinMax();
            if (!fireChangeNotification)
                return;
            if (_owner.SelectedDate.HasValue)
                _owner.SelectedDate = new DateTime?();
            if (_removedItems.Count > 0) {
                RaiseSelectionChanged(_removedItems, new Collection<DateTime>());
                _removedItems.Clear();
            }
            _owner.UpdatePresenter();
        }

        internal void Toggle(DateTime date) {
            if (!Calendar.IsValidDateSelection(_owner, date))
                return;
            switch (_owner.SelectionMode) {
                case CalendarSelectionMode.SingleDate:
                    if (!_owner.SelectedDate.HasValue || _owner.Engine.CompareDays(_owner.SelectedDate.Value, date) != 0) {
                        _owner.SelectedDate = date;
                        break;
                    }
                    _owner.SelectedDate = new DateTime?();
                    break;
                case CalendarSelectionMode.MultipleRange:
                    if (Remove(date))
                        break;
                    Add(date);
                    break;
            }
        }

        private void RaiseSelectionChanged(IList removedItems, IList addedItems) {
            _owner.OnSelectedDatesCollectionChanged(new SelectionChangedEventArgs(Calendar.SelectedDatesChangedEvent, removedItems, addedItems));
        }

        private void BeginAddRange() {
            _isAddingRange = true;
        }

        private void EndAddRange() {
            _isAddingRange = false;
            RaiseSelectionChanged(_removedItems, _addedItems);
            _removedItems.Clear();
            _addedItems.Clear();
            _owner.UpdatePresenter();
        }

        private bool CheckSelectionMode() {
            if (_owner.SelectionMode == CalendarSelectionMode.None)
                throw new InvalidOperationException("Calendar_OnSelectedDateChanged_InvalidOperation");
            if (_owner.SelectionMode == CalendarSelectionMode.SingleDate && Count > 0)
                throw new InvalidOperationException("Calendar_CheckSelectionMode_InvalidOperation");
            if (_owner.SelectionMode != CalendarSelectionMode.SingleRange || _isAddingRange || Count <= 0)
                return false;
            ClearInternal();
            return true;
        }

        private bool IsValidThread() {
            return Thread.CurrentThread == _dispatcherThread;
        }

        private void UpdateMinMax(DateTime date) {
            if (!_maximumDate.HasValue || date > _maximumDate.Value)
                _maximumDate = date;
            if (_minimumDate.HasValue && !(date < _minimumDate.Value))
                return;
            _minimumDate = date;
        }

        private void ClearMinMax() {
            _maximumDate = new DateTime?();
            _minimumDate = new DateTime?();
        }

        private IEnumerable<DateTime> GetDaysInRange(DateTime start, DateTime end) {
            var increment = GetDirection(start, end);
            var rangeStart = new DateTime?(start);
            do {
                yield return rangeStart.Value;
                rangeStart = _owner.Engine.AddDays(rangeStart.Value, increment);
            }
            while (rangeStart.HasValue && DateTime.Compare(end, rangeStart.Value) != -increment);
        }

        private static int GetDirection(DateTime start, DateTime end) {
            return DateTime.Compare(end, start) < 0 ? -1 : 1;
        }
    }

}