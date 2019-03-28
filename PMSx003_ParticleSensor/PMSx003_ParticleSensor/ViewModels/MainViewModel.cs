using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using PMSx003_ParticleSensor.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static PMSx003_ParticleSensor.Sensors.PMSx003_ParticleSensor.PMSx003_ParticleSensor;
//using PMSx003_ParticleSensor.Sensors;

namespace PMSx003_ParticleSensor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private Sensors.PMSx003_ParticleSensor.PMSx003_ParticleSensor _sensor;

        private const int GraphReadings = 20;
        public ObservableCollection<Reading> Readings { set; get; }
        public ObservableCollection<CountsHigh> CountsHigh { set; get; }
        public ObservableCollection<CountsLow> CountsLow { set; get; }

        private ICommand _setPassiveMode;
        private ICommand _getReading;
        private ICommand _setActiveMode;
        private ICommand _setSleepMode;
        private ICommand _setWakeMode;
        public ICommand SetPassiveMode => _setPassiveMode ?? (_setPassiveMode = new RelayCommand(setPassiveMode));
        public ICommand GetReading => _getReading ?? (_getReading = new RelayCommand(getReading));
        public ICommand SetActiveMode => _setActiveMode ?? (_setActiveMode = new RelayCommand(setActiveMode));
        public ICommand SetSleepMode => _setSleepMode ?? (_setSleepMode = new RelayCommand(setSleepMode));
        public ICommand SetWakeMode => _setWakeMode ?? (_setWakeMode = new RelayCommand(setWakeMode));

        private ICommand _hardWakeMode;
        public ICommand HardWakeMode => _hardWakeMode ?? (_hardWakeMode = new RelayCommand(hardWakeMode));

        private void hardWakeMode() => _sensor.HardwareStatus = SensorHardwareStatus.Active;

        private ICommand _hardSleepMode;
        public ICommand HardSleepMode => _hardSleepMode ?? (_hardSleepMode = new RelayCommand(hardSleepMode));

        private void hardSleepMode() => _sensor.HardwareStatus = SensorHardwareStatus.Sleep;

        private ICommand _hardReset;
        public ICommand HardReset => _hardReset ?? (_hardReset = new RelayCommand(hardReset));

        private void hardReset() => _sensor.HardResetSensor();

        private ICommand _disconnectUART;
        public ICommand DisconnectUART => _disconnectUART ?? (_disconnectUART = new RelayCommand(disconnectUART));

        private void disconnectUART() => _sensor.DisconnectUART();

        private ICommand _connectUART;
        public ICommand ConnectUART => _connectUART ?? (_connectUART = new RelayCommand(connectUART));

        private async void connectUART() => await _sensor.ConnectUART("UART0");

        private void setPassiveMode() => _sensor.Mode = SensorMode.Passive;

        private void setActiveMode() => _sensor.Mode = SensorMode.Active;

        private void getReading() => _sensor.PassiveRead();

        private void setSleepMode() => _sensor.SensorStatus = SensorOperationStatus.Sleep;

        private void setWakeMode() => _sensor.SensorStatus = SensorOperationStatus.Active;

        public MainViewModel()
        {
            DispatcherHelper.Initialize();

            Readings = new ObservableCollection<Reading>();
            CountsHigh = new ObservableCollection<CountsHigh>();
            CountsLow = new ObservableCollection<CountsLow>();

            _sensor = new Sensors.PMSx003_ParticleSensor.PMSx003_ParticleSensor(17, 27);
            _sensor.ReadingComplete += _sensor_ReadingComplete;
            ReadingInterval = _sensor.ReadingInterval;
        }

        private void _sensor_ReadingComplete(object sender, ReadingArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                ReadingDateTime = e.ReadingTime;

                PM1_0Concentration_CF1 = _sensor.PM1_0Concentration_CF1;
                PM2_5Concentration_CF1 = _sensor.PM2_5Concentration_CF1;
                PM10_0Concentration_CF1 = _sensor.PM10_0Concentration_CF1;

                PM1_0Concentration_amb = _sensor.PM1_0Concentration_amb;
                PM2_5Concentration_amb = _sensor.PM2_5Concentration_amb;
                PM10_0Concentration_amb = _sensor.PM10_0Concentration_amb;

                PM0_3Count = _sensor.PM0_3Count;
                PM0_5Count = _sensor.PM0_5Count;
                PM1_0Count = _sensor.PM1_0Count;
                PM5_0Count = _sensor.PM5_0Count;
                PM10_0Count = _sensor.PM10_0Count;

                ProductVersion = _sensor.ProductVersion;
                ErrorCodes = _sensor.StatusCodes;

                while (Readings.Count > GraphReadings)
                {
                    Readings.RemoveAt(0);
                }

                Reading reading = new Reading
                {
                    PM1_0Concentration = PM10_0Concentration_CF1,
                    PM2_5Concentration = PM2_5Concentration_CF1,
                    PM10_0Concentration = PM10_0Concentration_CF1,
                    ReadingDateTime = ReadingDateTime
                };

                Readings.Add(reading);

                while (CountsLow.Count > GraphReadings)
                {
                    CountsLow.RemoveAt(0);
                }

                CountsLow countsLow = new CountsLow
                {
                    PM2_5Count = PM2_5Count,
                    PM5_0Count = PM5_0Count,
                    PM10_0Count = PM10_0Count,
                    ReadingDateTime = ReadingDateTime
                };

                CountsLow.Add(countsLow);

                while (CountsHigh.Count > GraphReadings)
                {
                    CountsHigh.RemoveAt(0);
                }

                CountsHigh countsHigh = new CountsHigh
                {
                    PM1_0Count = PM1_0Count,
                    PM0_5Count = PM0_5Count,
                    PM0_3Count = PM0_3Count,
                    ReadingDateTime = ReadingDateTime
                };

                CountsHigh.Add(countsHigh);
            });
        }

        private uint _pm1_0Concentration_CF1;

        public uint PM1_0Concentration_CF1
        {
            get
            {
                return _pm1_0Concentration_CF1;
            }
            set
            {
                Set(ref _pm1_0Concentration_CF1, value);
            }
        }

        private uint _pm2_5Concentration_CF1;

        public uint PM2_5Concentration_CF1
        {
            get
            {
                return _pm2_5Concentration_CF1;
            }
            set
            {
                Set(ref _pm2_5Concentration_CF1, value);
            }
        }

        private uint _pm10_0Concentration_CF1;

        public uint PM10_0Concentration_CF1
        {
            get
            {
                return _pm10_0Concentration_CF1;
            }
            set
            {
                Set(ref _pm10_0Concentration_CF1, value);
            }
        }

        private uint _pm1_0Concentration_amb;

        public uint PM1_0Concentration_amb
        {
            get
            {
                return _pm1_0Concentration_amb;
            }
            set
            {
                Set(ref _pm1_0Concentration_amb, value);
            }
        }

        private uint _pm2_5Concentration_amb;

        public uint PM2_5Concentration_amb
        {
            get
            {
                return _pm2_5Concentration_amb;
            }
            set
            {
                Set(ref _pm2_5Concentration_amb, value);
            }
        }

        private uint _pm10_0Concentration_amb;

        public uint PM10_0Concentration_amb
        {
            get
            {
                return _pm10_0Concentration_amb;
            }
            set
            {
                Set(ref _pm10_0Concentration_amb, value);
            }
        }

        private uint _pm0_3Count;

        public uint PM0_3Count
        {
            get
            {
                return _pm0_3Count;
            }
            set
            {
                Set(ref _pm0_3Count, value);
            }
        }

        private uint _pm0_5Count;

        public uint PM0_5Count
        {
            get
            {
                return _pm0_5Count;
            }
            set
            {
                Set(ref _pm0_5Count, value);
            }
        }

        private uint _pm1_0Count;

        public uint PM1_0Count
        {
            get
            {
                return _pm1_0Count;
            }
            set
            {
                Set(ref _pm1_0Count, value);
            }
        }

        private uint _pm2_5Count;

        public uint PM2_5Count
        {
            get
            {
                return _pm2_5Count;
            }
            set
            {
                Set(ref _pm2_5Count, value);
            }
        }

        private uint _pm5_0Count;

        public uint PM5_0Count
        {
            get
            {
                return _pm5_0Count;
            }
            set
            {
                Set(ref _pm5_0Count, value);
            }
        }

        private uint _pm10_0Count;

        public uint PM10_0Count
        {
            get
            {
                return _pm10_0Count;
            }
            set
            {
                Set(ref _pm10_0Count, value);
            }
        }

        private byte _errorCodes;

        public byte ErrorCodes
        {
            get
            {
                return _errorCodes;
            }
            set
            {
                Set(ref _errorCodes, value);
            }
        }

        private byte _productVersion;

        public byte ProductVersion
        {
            get
            {
                return _productVersion;
            }
            set
            {
                Set(ref _productVersion, value);
            }
        }

        private int _readingInterval;

        public int ReadingInterval
        {
            get
            {
                return _readingInterval;
            }
            set
            {
                _sensor.ReadingInterval = value;
                Set(ref _readingInterval, value);
            }
        }

        private DateTime _readingDateTime;

        public DateTime ReadingDateTime
        {
            get
            {
                return _readingDateTime;
            }

            set
            {
                Set(ref _readingDateTime, value);
            }
        }
    }
}
