namespace Mehrsam_Darou.ViewModel
{


    public class DashboardViewModel
    {
        // Data for first chart (Conversions Radial)
        public double ConversionRate { get; set; }
        public string ConversionLabel { get; set; }
        public string[] ConversionColors { get; set; }

        public string ThisWeekConversions { get; set; }
        public string LastWeekConversions { get; set; }

        // Data for second chart (Performance Bar & Area)
        public List<int> PageViews { get; set; }
        public List<int> Clicks { get; set; }
        public List<string> PersianMonths { get; set; }
    }
}
