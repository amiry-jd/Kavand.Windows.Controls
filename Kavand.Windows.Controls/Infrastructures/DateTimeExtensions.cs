using System;

namespace Kavand.Windows.Controls {

    public static class DateTimeExtensions {

        /// <summary>
        /// Discards the time parts from the given nullable date-time
        /// <para>returns <code>dateTime.HasValue ? dateTime.Value.Date : new DateTime?()</code></para>
        /// </summary>
        /// <param name="dateTime">The nullable date-time to be discarded</param>
        /// <returns></returns>
        public static DateTime? DiscardTime(this DateTime? dateTime) {
            return dateTime.HasValue ? dateTime.Value.Date : new DateTime?();
        }

        /// <summary>
        /// Discards the time parts from the given date-time
        /// <para>returns <code>dateTime.Date</code></para>
        /// </summary>
        /// <param name="dateTime">The date-time to be discarded</param>
        /// <returns></returns>
        public static DateTime DiscardTime(this DateTime dateTime) {
            return dateTime.Date;
        }

    }
}