using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Devices.Gpio;

namespace HC_SR04_Ultrasonic_Distance_Sensor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ICommand _getGPIODistance1;
        private ICommand _getGPIODistance2;
        public ICommand GetGPIODistance => _getGPIODistance1 ?? (_getGPIODistance1 = new RelayCommand(getGPIODistance1));
        public ICommand GetGPIODistance2 => _getGPIODistance2 ?? (_getGPIODistance2 = new RelayCommand(getGPIODistance2));

        private const double ROUNDTRIPSPEEDOFSOUND = 171500; //divided by 2 for round trip mm / sec

        //GPIO setup
        const int TRIGGERPIN = 12;
        const int ECHOPIN = 16;
        private GpioPin _triggerPin;
        private GpioPin _echoPin;

        // NOTE GpioChangeReader requires a minimum of Windows 10 Creators Update (v10.0.15063.0)
        // https://docs.microsoft.com/en-us/uwp/api/windows.devices.gpio.gpiochangereader
        private GpioChangeReader _echoBackReader;

        private bool _measure;
        private bool _measured;

        private Stopwatch timeWatcher;

        public MainViewModel()
        {
            DispatcherHelper.Initialize();

            InitAll();

            timeWatcher = new Stopwatch();
        }

        private async void InitAll()
        {
            InitGPIO();
        }

        private void InitGPIO()
        {
            GpioController gpioController = GpioController.GetDefault();

            _triggerPin = gpioController.OpenPin(TRIGGERPIN, GpioSharingMode.Exclusive);
            _triggerPin.Write(GpioPinValue.Low);
            _triggerPin.SetDriveMode(GpioPinDriveMode.Output);

            _echoPin = gpioController.OpenPin(ECHOPIN, GpioSharingMode.Exclusive);
            _echoPin.Write(GpioPinValue.Low);
            _echoPin.SetDriveMode(GpioPinDriveMode.Input);

            _echoBackReader = new GpioChangeReader(_echoPin)
            {
                // we are looking for the width of a pulse sent from the HC-SR04 so we need the
                // time from the rising edge to the falling edge
                Polarity = GpioChangePolarity.Both
            };

            // this is for trying pin value change interrupt method
            //_echoPin.ValueChanged += _echoPin_ValueChanged;
        }

        public async void getGPIODistance1()
        {
            if (_echoBackReader != null)
            {
                ManualResetEvent mre = new ManualResetEvent(false);

                CancellationTokenSource source = new CancellationTokenSource();
                source.CancelAfter(TimeSpan.FromMilliseconds(250));

                _echoBackReader.Clear();
                _echoBackReader.Start();

                //Send pulse
                _triggerPin.Write(GpioPinValue.High);
                mre.WaitOne(TimeSpan.FromMilliseconds(0.01));
                _triggerPin.Write(GpioPinValue.Low);

                try
                {
                    await _echoBackReader.WaitForItemsAsync(2).AsTask(source.Token);

                    IList<GpioChangeRecord> changeRecords = this._echoBackReader.GetAllItems();

                    //foreach (var i in changeRecords)
                    //{
                    //    Debug.WriteLine(i.RelativeTime.ToString() + "  " +  (i.Edge == GpioPinEdge.FallingEdge ? "Falling" : "Rising"));
                    //}

                    TimeSpan d = changeRecords[1].RelativeTime.Subtract(changeRecords[0].RelativeTime);
                    GPIOTime = d.TotalSeconds;
                    GPIODistance = ROUNDTRIPSPEEDOFSOUND * GPIOTime;
                }
                catch (OperationCanceledException)
                {
                    GPIOTime = -1;
                    GPIODistance = -1;  //no measurement
                }
                _echoBackReader.Stop();
            }
        }

        public async void getGPIODistance2()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(TimeSpan.FromMilliseconds(250));

            try
            {
                double x = await Task.Run(() => PulseTime(source.Token), source.Token);
                GPIOTime2 = x;
                GPIODistance2 = GPIOTime2 * ROUNDTRIPSPEEDOFSOUND;
            }
            catch (OperationCanceledException)
            {
                GPIOTime2 = -1.0;
                GPIODistance2 = -1.0;  //no measurement
            }

            timeWatcher.Reset();
        }

        private double PulseTime(CancellationToken cancellationToken)
        {
            // polling isn't the best answer as its wasteful of CPU cycles and
            // could be swapped out mid poll by the OS, so really not the best
            // solution but never the less workable.
            int count = 0;
            Stopwatch timeWatcher = new Stopwatch();

            ManualResetEvent mre = new ManualResetEvent(false);
            timeWatcher.Reset();

            //Send pulse
            _triggerPin.Write(GpioPinValue.High);
            mre.WaitOne(TimeSpan.FromMilliseconds(0.01));
            _triggerPin.Write(GpioPinValue.Low);

            while ( _echoPin.Read() == GpioPinValue.Low)
            {
                if (count++ % 5000 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            timeWatcher.Start();

            while (_echoPin.Read() == GpioPinValue.High)
            {
                if (count++ % 5000 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            timeWatcher.Stop();

            return timeWatcher.Elapsed.TotalSeconds;
        }



        private void getGPIODistance3()
        {
            //using the pin change event is too slow with a full operating system but Microsoft
            //understands this and created GpioChangeReader for those situations where pin
            //timings matter.

            _measure = true;

            timeWatcher.Reset();

            ManualResetEvent mre = new ManualResetEvent(false);
            mre.WaitOne(100);

            //Send pulse
            _triggerPin.Write(GpioPinValue.High);
            mre.WaitOne(TimeSpan.FromMilliseconds(0.01));
            _triggerPin.Write(GpioPinValue.Low);

            timeWatcher.Start();

            //limit the time we wait to a quarter second
            mre.WaitOne(TimeSpan.FromMilliseconds(250));
            if (!_measured)
            {
                _measure = false;
                timeWatcher.Stop();
                timeWatcher.Reset();

                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    GPIOTime2 = -1;
                    GPIODistance2 = -1;
                });
            }
        }

        private void _echoPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            //Debug.WriteLine((args.Edge == GpioPinEdge.FallingEdge ? "Echo Falling Edge" : "Echo Rising Edge"));

            if (_measure && args.Edge == GpioPinEdge.RisingEdge)
            {
                timeWatcher.Stop();
                _measured = true;
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    GPIOTime2 = timeWatcher.Elapsed.TotalSeconds;
                    GPIODistance2 = GPIOTime2 * ROUNDTRIPSPEEDOFSOUND;
                });
            }
        }
        private double _gpioDistance;
        public double GPIODistance
        {
            get
            {
                return _gpioDistance;
            }
            set
            {
                Set(ref _gpioDistance, value);
            }
        }

        private double _gpioTime;
        public double GPIOTime
        {
            get
            {
                return _gpioTime;
            }
            set
            {
                Set(ref _gpioTime, value);
            }
        }

        private double _gpioDistance2;
        public double GPIODistance2
        {
            get
            {
                return _gpioDistance2;
            }
            set
            {
                Set(ref _gpioDistance2, value);
            }
        }

        private double _gpioTime2;
        public double GPIOTime2
        {
            get
            {
                return _gpioTime2;
            }
            set
            {
                Set(ref _gpioTime2, value);
            }
        }
    }
}
