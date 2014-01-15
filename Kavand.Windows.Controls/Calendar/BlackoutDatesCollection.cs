﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace Kavand.Windows.Controls {

    /// <summary>
    /// Represents a collection of DateTimeRanges. 
    /// </summary> 
    public sealed class BlackoutDatesCollection : ObservableCollection<DateRange> {

        #region Data

        private readonly Thread _dispatcherThread;
        private readonly Calendar _owner;

        #endregion Data

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Kavand.Windows.Controls.BlackoutDatesCollection"/> class. 
        /// </summary>
        /// <param name="owner"></param>
        public BlackoutDatesCollection(Calendar owner) {
            _owner = owner;
            _dispatcherThread = Thread.CurrentThread;
        }

        #region Public Methods

        /// <summary>
        /// Dates that are in the past are added to the BlackoutDates.
        /// </summary> 
        public void AddDatesInPast() {
            Add(new DateRange(DateTime.MinValue, DateTime.Today.AddDays(-1)));
        }

        /// <summary>
        /// Checks if a DateTime is in the Collection
        /// </summary>
        /// <param name="date"></param> 
        /// <returns></returns>
        public bool Contains(DateTime date) {
            return null != GetContainingDateRange(date);
        }

        /// <summary>
        /// Checks if a Range is in the collection
        /// </summary> 
        /// <param name="start"></param>
        /// <param name="end"></param> 
        /// <returns></returns> 
        public bool Contains(DateTime start, DateTime end) {
            DateTime rangeStart, rangeEnd;
            var n = Count;

            if (DateTime.Compare(end, start) > -1) {
                rangeStart = start.DiscardTime();
                rangeEnd = end.DiscardTime();
            } else {
                rangeStart = end.DiscardTime();
                rangeEnd = start.DiscardTime();
            }

            for (var i = 0; i < n; i++)
                if (DateTime.Compare(this[i].Start, rangeStart) == 0 && DateTime.Compare(this[i].End, rangeEnd) == 0)
                    return true;
            return false;
        }

        /// <summary>
        /// Returns true if any day in the given DateTime range is contained in the BlackOutDays. 
        /// </summary>
        /// <param name="range">DateRange that is searched in BlackOutDays</param>
        /// <returns>true if at least one day in the range is included in the BlackOutDays</returns>
        public bool ContainsAny(DateRange range) {
            foreach (var item in this)
                if (item.ContainsAny(range))
                    return true;
            return false;
        }

        /// <summary>
        /// This finds the next date that is not blacked out in a certian direction. 
        /// </summary>
        /// <param name="requestedDate"></param>
        /// <param name="dayInterval"></param>
        /// <returns></returns> 
        internal DateTime? GetNonBlackoutDate(DateTime? requestedDate, int dayInterval) {
            Debug.Assert(dayInterval != 0);

            var currentDate = requestedDate;
            DateRange range;

            if (requestedDate == null) {
                return null;
            }

            if ((range = GetContainingDateRange((DateTime)currentDate)) == null) {
                return requestedDate;
            }

            do {
                if (dayInterval > 0) {
                    // Moving Forwards.
                    // The DateRanges require start <= end 
                    currentDate = _owner.Engine.AddDays(range.End, dayInterval);

                } else {
                    //Moving backwards. 
                    currentDate = _owner.Engine.AddDays(range.Start, dayInterval);
                }

            } while (currentDate != null && ((range = GetContainingDateRange((DateTime)currentDate)) != null));



            return currentDate;
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// All the items in the collection are removed. 
        /// </summary>
        protected override void ClearItems() {
            if (!IsValidThread()) {
                throw new NotSupportedException("MultiThreadedCollectionChangeNotSupported");
            }

            foreach (DateRange item in Items) {
                UnRegisterItem(item);
            }

            base.ClearItems();
            _owner.UpdatePresenter();
        }

        /// <summary> 
        /// The item is inserted in the specified place in the collection.
        /// </summary> 
        /// <param name="index"></param> 
        /// <param name="item"></param>
        protected override void InsertItem(int index, DateRange item) {
            if (!IsValidThread()) {
                throw new NotSupportedException("MultiThreadedCollectionChangeNotSupported");
            }

            if (IsValid(item)) {
                RegisterItem(item);
                base.InsertItem(index, item);
                _owner.UpdatePresenter();
            } else {
                throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// The item in the specified index is removed from the collection.
        /// </summary>
        /// <param name="index"></param> 
        protected override void RemoveItem(int index) {
            if (!IsValidThread()) {
                throw new NotSupportedException("MultiThreadedCollectionChangeNotSupported");
            }

            if (index >= 0 && index < Count) {
                UnRegisterItem(Items[index]);
            }

            base.RemoveItem(index);
            _owner.UpdatePresenter();
        }

        /// <summary>
        /// The object in the specified index is replaced with the provided item. 
        /// </summary>
        /// <param name="index"></param> 
        /// <param name="item"></param> 
        protected override void SetItem(int index, DateRange item) {
            if (!IsValidThread()) {
                throw new NotSupportedException("MultiThreadedCollectionChangeNotSupported");
            }

            if (IsValid(item)) {
                DateRange oldItem = null;
                if (index >= 0 && index < Count) {
                    oldItem = Items[index];
                }

                base.SetItem(index, item);

                UnRegisterItem(oldItem);
                RegisterItem(Items[index]);

                _owner.UpdatePresenter();
            } else {
                throw new ArgumentOutOfRangeException();
            }
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary> 
        /// Registers for change notification on date ranges
        /// </summary> 
        /// <param name="item"></param> 
        private void RegisterItem(DateRange item) {
            if (item == null)
                return;
            item.Changing += Item_Changing;
            item.PropertyChanged += Item_PropertyChanged;
        }

        /// <summary>
        /// Un registers for change notification on date ranges 
        /// </summary>
        private void UnRegisterItem(DateRange item) {
            if (item == null)
                return;
            item.Changing -= Item_Changing;
            item.PropertyChanged -= Item_PropertyChanged;
        }

        /// <summary>
        /// Reject date range changes that would make the blackout dates collection invalid
        /// </summary> 
        /// <param name="sender"></param>
        /// <param name="e"></param> 
        private void Item_Changing(object sender, DateRangeChangingEventArgs e) {
            var item = sender as DateRange;
            if (item == null)
                return;
            if (!IsValid(e.Start, e.End))
                throw new ArgumentOutOfRangeException();
        }

        /// <summary>
        /// Update the calendar view to reflect the new blackout dates
        /// </summary>
        /// <param name="sender"></param> 
        /// <param name="e"></param>
        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (sender is DateRange)
                _owner.UpdatePresenter();
        }

        /// <summary>
        /// Tests to see if a date range is not already selected 
        /// </summary> 
        /// <param name="item">date range to test</param>
        /// <returns>True if no selected day falls in the given date range</returns> 
        private bool IsValid(DateRange item) {
            return IsValid(item.Start, item.End);
        }

        /// <summary> 
        /// Tests to see if a date range is not already selected 
        /// </summary>
        /// <param name="start">First day of date range to test</param> 
        /// <param name="end">Last day of date range to test</param>
        /// <returns>True if no selected day falls between start and end</returns>
        private bool IsValid(DateTime start, DateTime end) {
            foreach (var child in _owner.SelectedDates) {
                DateTime? day = child;
                Debug.Assert(day != null);
                if (_owner.Engine.InRange(day.Value, start, end))
                    return false;
            }
            return true;
        }

        private bool IsValidThread() {
            return Thread.CurrentThread == _dispatcherThread;
        }

        /// <summary>
        /// Gets the DateRange that contains the date. 
        /// </summary> 
        /// <param name="date"></param>
        /// <returns></returns> 
        private DateRange GetContainingDateRange(DateTime date) {
            for (var i = 0; i < Count; i++)
                if (_owner.Engine.InRange(date, this[i]))
                    return this[i];
            return null;
        }
        #endregion Private Methods

    }
}