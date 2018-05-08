using System;

using ML8511_Ultraviolet_Light_Sensor.ViewModels;

using Windows.UI.Xaml.Controls;

namespace ML8511_Ultraviolet_Light_Sensor.Views
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
