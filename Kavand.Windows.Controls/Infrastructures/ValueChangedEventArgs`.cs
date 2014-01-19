using System;

namespace Kavand.Windows.Controls {

    public class ValueChangedEventArgs<TValue> : EventArgs {

        private readonly TValue _oldValue;
        private readonly TValue _newValue;

        public ValueChangedEventArgs() : this(default(TValue), default(TValue)) { }

        public ValueChangedEventArgs(TValue oldValue, TValue newValue) {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public TValue OldValue {
            get { return _oldValue; }
        }

        public TValue NewValue {
            get { return _newValue; }
        }
    }
}