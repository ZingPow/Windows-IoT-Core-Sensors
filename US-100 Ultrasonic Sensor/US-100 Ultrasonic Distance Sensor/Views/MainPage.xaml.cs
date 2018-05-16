using System;

using US_100_Ultrasonic_Distance_Sensor.ViewModels;

using Windows.UI.Xaml.Controls;

namespace US_100_Ultrasonic_Distance_Sensor.Views
{
    public sealed partial class MainPage : Page
    {
        private MainViewModel ViewModel
        {
            get { return DataContext as MainViewModel; }
        }

        public MainPage()
        {
            InitializeComponent();
        }
    }
}
