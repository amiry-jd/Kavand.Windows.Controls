using System;
using System.Windows;

namespace Kavand.Windows.Controls {
    public class ElementPartNotFoundException : Exception {
        public ElementPartNotFoundException(FrameworkElement elemment, string elementPartName, Type typeofPart)
            : base(string.Format(
            "The framework element {0} needs a part named {1} of type {2} in its template which cannot be found.",
            elemment, elementPartName, typeofPart)) { }
    }
}