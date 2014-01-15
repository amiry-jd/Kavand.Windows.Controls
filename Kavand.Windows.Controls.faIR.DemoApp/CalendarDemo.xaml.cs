using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Kavand.Windows.Controls.faIR.DemoApp {
    /// <summary>
    /// Interaction logic for CalendarDemo.xaml
    /// </summary>
    public partial class CalendarDemo : Window {

        private struct CultureItem {
            public string DisplayName { get; set; }
            public int Key { get; set; }
        }

        private ObservableCollection<CultureItem> _culturesList;

        public CalendarDemo() {
            InitializeComponent();
            Loaded += WindowLoaded;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e) {
            _culturesList = new ObservableCollection<CultureItem> {
                new CultureItem {DisplayName = "new CultureInfo(en-US)",   Key = 1},
                new CultureItem {DisplayName = "new CultureInfo(fa-IR)",   Key = 2},
                new CultureItem {DisplayName = "new PersianCultureInfo()", Key = 3}
            };
            CultureSelector.ItemsSource = _culturesList;
            EngineSelector.ItemsSource = new CalendarEngine[] {
                new PersianCalendarEngine(), new GregorianCalendarEngine()
            };
        }

        private void SelectCulture(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            var key = ((CultureItem)CultureSelector.SelectedValue).Key;
            CultureInfo culture;
            switch (key) {
                case 1:
                    culture = new CultureInfo("en-US");
                    break;
                case 2:
                    culture = new CultureInfo("fa-IR");
                    break;
                default:
                    culture = new PersianCultureInfo();
                    break;
            }
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            Calendar.NotifyOfCultureChanged();
        }

    }
}
