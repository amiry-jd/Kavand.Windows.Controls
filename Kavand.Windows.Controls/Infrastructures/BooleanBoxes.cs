using System;

namespace Kavand.Windows.Controls {

    internal static class BooleanBoxes {

        internal static object TrueBox = true;
        internal static object FalseBox = false;

        static BooleanBoxes() { }

        public static readonly Type Typeof = typeof(bool);

        internal static object Box(bool value) {
            return value ? TrueBox : FalseBox;
        }
    }
}