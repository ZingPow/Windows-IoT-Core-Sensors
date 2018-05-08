using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.UI.Xaml;

namespace ML8511_Ultraviolet_Light_Sensor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        //MCP3008 ADC
        private const string SPI_CONTROLLER_NAME = "SPI0";
        private const Int32 SPI_CHIP_SELECT_LINE = 0;
        private SpiDevice SpiADC;

        private readonly byte StartByte = 0x01;
        private readonly byte Channel0 = 0x80;
        private readonly byte Channel1 = 0x90;

        private const int ML8511ENABLE_PIN = 23;
        private static GpioPin ML8511EnablePin;

        private ICommand _getUVIReading;
        private ICommand _toggleTimer;
        public ICommand GetUVIReading => _getUVIReading ?? (_getUVIReading = new RelayCommand(execute: GetUVI2));
        public ICommand ToggleTimer => _toggleTimer ?? (_toggleTimer = new RelayCommand(execute: _ToggleTimer));

        private DispatcherTimer _timer;

        public MainViewModel()
        {
            DispatcherHelper.Initialize();

            InitAll();

            //Used to show ML8511 in continuous mode
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            _timer.Tick += _timer_Tick;
        }

        private async void InitAll()
        {
            var gpio = GpioController.GetDefault();

            if (gpio == null)
            {
                DisplayStatus("There is no GPIO controller on this device.");
                return;
            }

            //Used to toggle low power sleep mode
            ML8511EnablePin = gpio.OpenPin(ML8511ENABLE_PIN);
            ML8511EnablePin.SetDriveMode(GpioPinDriveMode.Output);
            ML8511EnablePin.Write(GpioPinValue.Low); // when pin is low the sensor is in low power sleep mode

            try
            {
                await InitSPI();    /* Initialize the SPI bus for communicating with the ADC */
            }
            catch (Exception ex)
            {
                DisplayStatus("GPIO Initialization Failed: " + ex.Message);
                return;
            }
            DisplayStatus("Running");
        }

        private async Task InitSPI()
        {
            try
            {
                var spiSettings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE); //we are using line 0
                spiSettings.ClockFrequency = 500000;   /* 0.5MHz clock rate                                        */
                spiSettings.Mode = SpiMode.Mode0;      /* The ADC expects idle-low clock polarity so we use Mode0  */

                var controller = await SpiController.GetDefaultAsync();
                SpiADC = controller.GetDevice(spiSettings);
            }

            /* If initialization fails, display the exception and stop running */
            catch (Exception ex)
            {
                DisplayStatus("SPI Initialization Failed: " + ex.Message);
            }
        }

        public double ReadMCP3008ADC(byte channel)
        {
            double volts = 0;

            byte[] readBuffer = new byte[3]; /* Buffer to hold read data*/
            byte[] writeBuffer = new byte[3] { StartByte, channel, 0x00 };

            SpiADC.TransferFullDuplex(writeBuffer, readBuffer); /* Read data from the ADC                           */
            int adcValue = convertToInt(readBuffer);            /* Convert the returned bytes into an integer value */
            volts = adcValue * (3.3 / 1023.0);

            return volts;
        }

        /* Convert the raw ADC bytes to an integer for MCP3008 */
        private int convertToInt(byte[] data)
        {
            int result = 0;
            //bit bashing is inevitable when you play at this level 
            result = data[1] & 0x03;
            result <<= 8;
            result += data[2];

            return result;
        }

        private void _timer_Tick(object sender, object e)
        {
            GetUVI1();
        }

        private void GetUVI1()
        {
            //no need to wake the ML8511 since the EN pin is connected to 3.3v (ie high)
            Voltage1 = ReadMCP3008ADC(Channel0);

            UVIntensity1a = UVIntensity(Voltage1);
            UVIndex1a = UVIndex(UVIntensity1a);

            //popular Arduino linear fit call
            UVIntensity1b = MapFloat(Voltage2, 0.99, 2.9, 0.0, 15.0);
            UVIndex1b = UVIndex(UVIntensity1b);
        }

        private void GetUVI2()
        {
            //wake ML8511 from low power sleep mode
            ML8511EnablePin.Write(GpioPinValue.High);
            //takes a millisecond to wake, but I gave it 2 milliseconds
            Task.Delay(2);
            //Get a reading
            Voltage2 = ReadMCP3008ADC(Channel1);
            //return the ML8511 to low power sleep mode
            ML8511EnablePin.Write(GpioPinValue.Low);

            UVIntensity2a = UVIntensity(Voltage2);
            UVIndex2a = UVIndex(UVIntensity2a);

            //popular Arduino linear fit call
            UVIntensity2b = MapFloat(Voltage2, 0.99, 2.9, 0.0, 15.0);
            UVIndex2b = UVIndex(UVIntensity2b);
        }

        private double UVIntensity(double voltage)
        {
            return 12.49 * voltage - 12.49;
        }

        private double UVIndex(double uvIntensity)
        {
            return uvIntensity * 0.4;
        }

        // popular technique in Arduino world, really just a linear fit equation
        private double MapFloat(double x, double in_min, double in_max, double out_min, double out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        private void _ToggleTimer()
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
                ButtonText = "Start";
            }
            else
            {
                ButtonText = "Stop";
                _timer.Start();
            }
        }

        private void DisplayStatus(string msg)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                Status = msg;
            });
        }

        private double _voltage1;
        public double Voltage1
        {
            get
            {
                return _voltage1;
            }
            set
            {
                Set(ref _voltage1, value);
            }
        }

        private double _UVIntensity1a;
        public double UVIntensity1a
        {
            get
            {
                return _UVIntensity1a;
            }
            set
            {
                Set(ref _UVIntensity1a, value);
            }
        }

        private double _UVIntensity1b;
        public double UVIntensity1b
        {
            get
            {
                return _UVIntensity1b;
            }
            set
            {
                Set(ref _UVIntensity1b, value);
            }
        }

        private double _uvIndex1a;
        public double UVIndex1a
        {
            get
            {
                return _uvIndex1a;
            }
            set
            {
                Set(ref _uvIndex1a, value);
            }
        }

        private double _uvIndex1b;
        public double UVIndex1b
        {
            get
            {
                return _uvIndex1b;
            }
            set
            {
                Set(ref _uvIndex1b, value);
            }
        }

        private double _voltage2;
        public double Voltage2
        {
            get
            {
                return _voltage2;
            }
            set
            {
                Set(ref _voltage2, value);
            }
        }

        private double _UVIntensity2a;
        public double UVIntensity2a
        {
            get
            {
                return _UVIntensity2a;
            }
            set
            {
                Set(ref _UVIntensity2a, value);
            }
        }

        private double _UVIntensity2b;
        public double UVIntensity2b
        {
            get
            {
                return _UVIntensity2b;
            }
            set
            {
                Set(ref _UVIntensity2b, value);
            }
        }

        private double _uvIndex2a;
        public double UVIndex2a
        {
            get
            {
                return _uvIndex2a;
            }
            set
            {
                Set(ref _uvIndex2a, value);
            }
        }

        private double _uvIndex2b;
        public double UVIndex2b
        {
            get
            {
                return _uvIndex2b;
            }
            set
            {
                Set(ref _uvIndex2b, value);
            }
        }

        private string _buttonText = "Start";
        public string ButtonText
        {
            get
            {
                return _buttonText;
            }
            set
            {
                Set(ref _buttonText, value);
            }
        }

        private string _status;
        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                Set(ref _status, value);
            }
        }
    }
}
