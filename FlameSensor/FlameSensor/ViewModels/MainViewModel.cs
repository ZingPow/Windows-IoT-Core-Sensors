using FlameSensor.Devices.I2c.ADS1115;
using FlameSensor.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;

namespace FlameSensor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private Devices.FlameSensor FlameSensor;
        private ADS1115Sensor _adc;
        private ADS1115SensorSetting _settingFlameSensor;
        public ObservableCollection<Reading> ChartData { set; get; }

        private DispatcherTimer dispatchTimer;

        public MainViewModel()
        {
            DispatcherHelper.Initialize();

            ChartData = new ObservableCollection<Reading>();

            InitializeADC();

            FlameSensor = new Devices.FlameSensor(17);
            FlameSensor.FlameChange += FlameSensor_FlameChange;

            dispatchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };

            dispatchTimer.Tick += DispatchTimer_Tick;
            dispatchTimer.Start();
        }

        private async void InitializeADC()
        {
            _adc = new ADS1115Sensor(AdcAddress.GND);
            await _adc.InitializeAsync();

            //Flame Sensor
            _settingFlameSensor = new ADS1115SensorSetting();
            _settingFlameSensor.Mode = AdcMode.SINGLESHOOT_CONVERSION;
            _settingFlameSensor.Input = AdcInput.A0_SE;
            _settingFlameSensor.DataRate = AdcDataRate.SPS475;
            _settingFlameSensor.Pga = AdcPga.G1;
        }

        private void DispatchTimer_Tick(object sender, object e)
        {
            GetReading(FlameSensor.Flame, DateTime.Now);
        }


        private void FlameSensor_FlameChange(object sender, Devices.FlameSensor.FlameArgs e)
        {
            GetReading(e.Flame, e.EventTime);
        }

        private void GetReading(bool flame, DateTime t)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                ADS1115SensorData x = await _adc.readSingleShot(_settingFlameSensor);

                ReadingTime = t;

                FlameLevel = x.VoltageValue; //the higher the voltage returned the less flame is detected so we invert this to give it a more flame, higher reading like most people would expect.

                if (flame)
                {
                    FlameStatus = "Flame On";
                }
                else
                {
                    FlameStatus = "Flame Off";
                }

                Reading reading = new Reading
                {
                    ReadingDateTime = t,
                    FlameOn = (flame) ? 1 : 0,
                    FlameLevel = FlameLevel
                };

                while (ChartData.Count > 60)
                {
                    ChartData.RemoveAt(0);
                }

                ChartData.Add(reading);

            });
        }

        private string _flameStatus;
        public string FlameStatus
        {
            get
            {
                return _flameStatus;
            }

            set
            {
                Set(ref _flameStatus, value);
            }

        }

        private double _flameLevel;
        public double FlameLevel
        {
            get
            {
                return _flameLevel;
            }

            set
            {
                Set(ref _flameLevel, value);
            }

        }
        private DateTime _readingTime;
        public DateTime ReadingTime
        {
            get
            {
                return _readingTime;
            }
            set
            {
                Set(ref _readingTime, value);
            }
        }
    }
}

