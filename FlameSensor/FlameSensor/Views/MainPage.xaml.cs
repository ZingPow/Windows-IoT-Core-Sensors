using System;

using FlameSensor.ViewModels;

using Windows.UI.Xaml.Controls;

namespace FlameSensor.Views
{
    public sealed partial class MainPage : Page
    {
        private MainViewModel ViewModel
        {
            get { return ViewModelLocator.Current.MainViewModel; }
        }

        public MainPage()
        {
            InitializeComponent();
        }
    }
}
