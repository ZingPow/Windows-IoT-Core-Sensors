using System;

using PMSx003_ParticleSensor.ViewModels;

using Windows.UI.Xaml.Controls;

namespace PMSx003_ParticleSensor.Views
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
