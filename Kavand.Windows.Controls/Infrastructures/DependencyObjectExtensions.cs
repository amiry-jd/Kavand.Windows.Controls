using System;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace Kavand.Windows.Controls {

    public static class DependencyObjectExtensions {

        /// <summary>
        /// Retrieve CultureInfo property from specified element. 
        /// </summary>
        /// <param name="element">The element to retrieve CultureInfo from</param>
        /// <returns></returns>
        public static CultureInfo GetCultureInfo(this  DependencyObject element) {
            var language = (XmlLanguage)element.GetValue(FrameworkElement.LanguageProperty);
            try {
                return language.GetSpecificCulture();
            } catch (InvalidOperationException) {
                // We default to en-US if no part of the language tag is recognized.
                return CultureInfoHelper.InvariantEnglishUS;
            }
        }
    }
}