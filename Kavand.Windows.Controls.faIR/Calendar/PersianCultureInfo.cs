using System;
using System.Globalization;

namespace Kavand.Windows.Controls.faIR {

    /// <summary>
    /// CultureInfo for "FA-IR" culture, which has correct calendar information.
    /// </summary>
    public class PersianCultureInfo : CultureInfo {

        #region Fields

        private readonly PersianCalendar _calendar;
        private DateTimeFormatInfo _format;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="PersianCultureInfo"/> class.
        /// </summary>
        public PersianCultureInfo()
            : base("fa-IR", false) {
            _calendar = new PersianCalendar();
            _format = PersianDateTimeFormatInfo.GetFormatInfo(_calendar);
            base.DateTimeFormat = _format;
            NumberFormat.DigitSubstitution = DigitShapes.NativeNational;
        }

        #endregion

        #region Private Methods

        #endregion

        #region Props

        /// <summary>
        /// Gets the default calendar used by the culture.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// A <see cref="T:System.Globalization.Calendar"/> that represents the default calendar used by the culture.
        /// </returns>
        public override System.Globalization.Calendar Calendar {
            get { return _calendar; }
        }

        /// <summary>
        /// Gets the list of calendars that can be used by the culture.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// An array of type <see cref="T:System.Globalization.Calendar"/> that represents the calendars that can be used by the culture represented by the current <see cref="T:System.Globalization.CultureInfo"/>.
        /// </returns>
        public override System.Globalization.Calendar[] OptionalCalendars {
            get { return new System.Globalization.Calendar[] { _calendar }; }
        }

        /// <summary>
        /// Creates a copy of the current <see cref="T:System.Globalization.CultureInfo"/>.
        /// </summary>
        /// <returns>
        /// A copy of the current <see cref="T:System.Globalization.CultureInfo"/>.
        /// </returns>
        public override object Clone() {
            return new PersianCultureInfo();
        }

        public new bool IsReadOnly {
            get { return true; }
        }

        public override bool IsNeutralCulture {
            get { return false; }
        }

        /// <summary>
        /// Gets or sets a <see cref="T:System.Globalization.DateTimeFormatInfo"/> that defines the culturally appropriate format of displaying dates and times.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// A <see cref="T:System.Globalization.DateTimeFormatInfo"/> that defines the culturally appropriate format of displaying dates and times.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The property is set to null.
        /// </exception>
        public override DateTimeFormatInfo DateTimeFormat {
            get { return _format; }
            set {
                if (value == null)
                    throw new ArgumentNullException("value", @"value can not be null.");
                _format = value;
            }
        }

        #endregion
    }
}