using System;

using HC_SR04_Ultrasonic_Distance_Sensor.ViewModels;

using Windows.UI.Xaml.Controls;

namespace HC_SR04_Ultrasonic_Distance_Sensor.Views
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
