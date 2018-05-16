using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace US_100_Ultrasonic_Distance_Sensor.ViewModels
{
    // In this sample I use three different ways to use the US-100 Ultrasonic Distance Sensor to compare how
    // well they work.  To summarize I'd recommend using the UART method as it offloads the sending of the ping
    // and watching for the echo to the sensor and even has a build in temperature compensation, very nice.
    // While I'm not a fan of polling it works next best, but sometime gives incorrect results, but is better
    // then what would be the preferred on a real time system, interrupt events, but in this case the OS doesn't
    // schedule the interrupts fast enough to be useful (of course faster hardware might give you good enough
    // results, but I'm using a Raspberry Pi 2 so its about as slow as you can go with Windows IoT Core).  Of
    // course even with a full blown OS, if you chuck fast enough hardware at the problem you can get close enough
    // to real time to make it work for what you are doing, but sometimes you just want the cheapest, smallest,
    // lowest power consuming CPU you can find to solve a problem so your solution should reflect your required
    // criteria, so flexibility, creativeness and openness to different solutions matter in devices and IoT.

    public class MainViewModel : ViewModelBase
    {
        private int _events;

        private ICommand _getUARTDistance;
        private ICommand _getGPIODistance;
        private ICommand _getGPIODistance2;
        public ICommand GetUARTDistance => _getUARTDistance ?? (_getUARTDistance = new RelayCommand(getUARTDistance));
        public ICommand GetGPIODistance => _getGPIODistance ?? (_getGPIODistance = new RelayCommand(getGPIODistance));
        public ICommand GetGPIODistance2 => _getGPIODistance2 ?? (_getGPIODistance2 = new RelayCommand(getGPIODistance2));

        //GPIO setup
        const int TRIGGERPIN = 24;
        const int ECHOPIN = 23;
        private GpioPin _triggerPin;
        private GpioPin _echoPin;

        const int TRIGGERPIN2 = 16;
        const int ECHOPIN2 = 12;
        private const double ROUNDTRIPSPEEDOFSOUND = 171500; //divided by 2 for round trip mm / sec
        private GpioPin _triggerPin2;
        private GpioPin _echoPin2;
        private bool _measure;

        private Stopwatch timeWatcher;
        private Stopwatch timeWatcher2;

        //UART setup
        private SerialDevice serialPort = null;
        DataWriter dataWriterObject = null;
        DataReader dataReaderObject = null;
        private static readonly byte[] GETDISTANCE = new byte[] { 0x55 };
        private static readonly byte[] GETTEMPERATURE = new byte[] { 0x50 };

        public MainViewModel()
        {
            DispatcherHelper.Initialize();

            InitAll();
        }
        private async void InitAll()
        {
            InitGPIO();
            InitUART();

            timeWatcher = new Stopwatch();
            timeWatcher2 = new Stopwatch();
        }

        private void InitGPIO()
        {
            GpioController gpioController = GpioController.GetDefault(); // .GetControllersAsync(LightningGpioProvider.GetGpioProvider()))[0];

            _triggerPin = gpioController.OpenPin(TRIGGERPIN, GpioSharingMode.Exclusive);
            _triggerPin.SetDriveMode(GpioPinDriveMode.Output);
            _triggerPin.Write(GpioPinValue.Low);

            _echoPin = gpioController.OpenPin(ECHOPIN, GpioSharingMode.Exclusive);
            _echoPin.Write(GpioPinValue.Low);
            _echoPin.SetDriveMode(GpioPinDriveMode.Input);

            _triggerPin2 = gpioController.OpenPin(TRIGGERPIN2, GpioSharingMode.Exclusive);
            _triggerPin2.SetDriveMode(GpioPinDriveMode.Output);
            _triggerPin2.Write(GpioPinValue.Low);

            _echoPin2 = gpioController.OpenPin(ECHOPIN2, GpioSharingMode.Exclusive);
            _echoPin2.Write(GpioPinValue.Low);
            _echoPin2.SetDriveMode(GpioPinDriveMode.Input);
            _echoPin2.DebounceTimeout = TimeSpan.Zero;

            //_echoPin2.ValueChanged += _echoPin2_ValueChanged;
        }

        private async void InitUART()
        {
            try
            {
                string aqs = SerialDevice.GetDeviceSelector("UART0");
                var dis = await DeviceInformation.FindAllAsync(aqs);
                serialPort = await SerialDevice.FromIdAsync(dis[0].Id);

                //Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);    //mS before a time-out occurs when a write operation does not finish (default=InfiniteTimeout).
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);     //mS before a time-out occurs when a read operation does not finish (default=InfiniteTimeout).
                serialPort.BaudRate = 9600;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;

                // Create the DataReader object and attach to InputStream
                dataReaderObject = new DataReader(serialPort.InputStream);
                // Create the DataWriter object and attach to OutputStream
                dataWriterObject = new DataWriter(serialPort.OutputStream);
            }
            catch (Exception ex)
            {
                throw new Exception("UART Initialize Error", ex);
            }
        }

        public void getUARTDistance()
        {
            //instruct the sensor to do a distance measurement
            byte[] data = ReadData(GETDISTANCE, 2);
            UARTDistance = data[0] * 256 + data[1];

            //instruct the sensor to return a temperature reading
            byte[] data2 = ReadData(GETTEMPERATURE, 1);
            UARTTemperature = data2[0] - 45;
        }

 

        public async void getGPIODistance()
        {
            // what is nice about this method is if it times out without getting a measurement then the task is exited
            // and an exception is raised that you can handle, this just makes it a little cleaner then the other method
            // which could get caught in a polling loop.
            _measure = false; //don't use pin interrupts

            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(TimeSpan.FromMilliseconds(250));  //if we don't have a measurement in a 1/4 second bail

            try
            {
                double x = await Task.Run(() => PulseTime(source.Token), source.Token);
                GPIOTime = x;
                GPIODistance = GPIOTime * ROUNDTRIPSPEEDOFSOUND;
            }
            catch (OperationCanceledException)
            {
                GPIOTime = -1.0;
                GPIODistance = -1.0;  //no measurement
            }

            timeWatcher.Reset();
        }

        private double PulseTime(CancellationToken cancellationToken)
        {
            _triggerPin.Write(GpioPinValue.Low);

            int count = 0;
            Stopwatch timeWatcher = new Stopwatch();

            ManualResetEvent mre = new ManualResetEvent(false);
            timeWatcher.Reset();

            //Send pulse
            _triggerPin.Write(GpioPinValue.High);
            mre.WaitOne(TimeSpan.FromMilliseconds(0.01));
            _triggerPin.Write(GpioPinValue.Low);

            while (_echoPin.Read() == GpioPinValue.Low)
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

        public void getGPIODistance2()
        {

            //this is the pin polling task
            var t = Task.Run(() =>
            {
                //Wait for the measurement pulse to be sent
                while (_echoPin.Read() != GpioPinValue.High)
                {
                }
                timeWatcher.Start();

                //wait for the echo to return
                while (_echoPin.Read() == GpioPinValue.High)
                {
                }
                timeWatcher.Stop();

                return timeWatcher.Elapsed.TotalSeconds;
            });


            ManualResetEvent mre = new ManualResetEvent(false);
            //a pause to clear the air per say
            mre.WaitOne(100);

            timeWatcher.Reset();

            //Send pulse
            _triggerPin.Write(GpioPinValue.High);
            mre.WaitOne(TimeSpan.FromMilliseconds(0.01));
            _triggerPin.Write(GpioPinValue.Low);

            //start the task and expect an response within a quarter of a second
            bool didComplete = t.Wait(TimeSpan.FromMilliseconds(250)); //timeout timer
            //but note if the task didn't complete it is likely still running
            if (didComplete)
            {
                GPIOTime2 = t.Result;
                GPIODistance2 = GPIOTime2 * ROUNDTRIPSPEEDOFSOUND;
            }
            else
            {
                GPIOTime2 = -1.0;
                GPIODistance2 = -1.0;  //no measurement
            }
        }

        //public void getGPIODistance2()
        //{
        /// <summary>
        //// don't recommend this method as the event scheduler is too slow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        //    Task task = new Task(() => GetDistance2());
        //    task.Start();
        //}

        //private void GetDistance2()
        //{
        //    timeWatcher2.Reset();
        //    _events = 0;

        //    ManualResetEvent mre = new ManualResetEvent(false);
        //    mre.WaitOne(100);

        //    Debug.WriteLine((_echoPin.Read() == GpioPinValue.Low ? "Pin Low" : "Pin High"));

        //    _measure = true;


        //    //Send pulse
        //    _triggerPin2.Write(GpioPinValue.High);
        //    mre.WaitOne(TimeSpan.FromMilliseconds(0.01));
        //    _triggerPin2.Write(GpioPinValue.Low);

        //    timeWatcher2.Start();

        //    //limit the time we wait to a quarter second
        //    mre.WaitOne(TimeSpan.FromMilliseconds(250));
        //    if (_measure)
        //    {
        //        //_measure = false;
        //        //timeWatcher2.Stop();
        //        //timeWatcher2.Reset();

        //        //DispatcherHelper.CheckBeginInvokeOnUI(() =>
        //        //{
        //        //    GPIOTime2 = -1;
        //        //    GPIODistance2 = _events;// -1;
        //        //});
        //    }
        //}

        private void _echoPin2_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            //Debug.WriteLine((args.Edge == GpioPinEdge.FallingEdge ? "Falling Edge" : "Rising Edge"));

            if (args.Edge == GpioPinEdge.RisingEdge)
            {
                timeWatcher2.Stop();
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    GPIOTime2 = timeWatcher2.Elapsed.TotalSeconds;
                    GPIODistance2 = GPIOTime2 * ROUNDTRIPSPEEDOFSOUND;
                });
            }
        }

        // UART routines

        /// <summary>
        /// SendCommand
        /// Asynchronous task to write to sensor through UART
        /// </summary>
        /// <param name="command"></param>
        /// <returns>async Task</returns>
        private async Task SendCommandAsync(byte[] command)
        {
            //Launch the storeAsync task to perform the write
            Task<UInt32> storeAsyncTask;

            if (command.Length != 0)
            {
                // Load the text from the sendText input text box to the dataWriter object
                dataWriterObject.WriteBytes(command);

                // Launch an async task to complete the write operation
                storeAsyncTask = dataWriterObject.StoreAsync().AsTask();

                UInt32 bytesWritten = await storeAsyncTask;
                if (bytesWritten != command.Length)
                    throw new Exception("Bytes written does not match command length");
            }
        }

        public async Task LoadResponseAsync(uint length)
        {
            Task<UInt32> loadAsyncTask;

            loadAsyncTask = dataReaderObject.LoadAsync(length).AsTask();

            uint bytesRead = await loadAsyncTask;

            if (length != bytesRead)
                throw new Exception("ReadDataAsync timeout");
        }

        /// <summary>
        /// ReadData
        /// </summary>
        /// <param name="data">Array of data read from device</param>
        /// <returns></returns>
        private byte[] ReadData(byte[] command, uint readLength)
        {
            byte[] returnBuffer = new byte[readLength];

            SendCommandAsync(command).Wait(500);

            LoadResponseAsync(readLength).Wait(500);
            dataReaderObject.ReadBytes(returnBuffer);

            return returnBuffer;
        }

        private int _uartDistance;
        public int UARTDistance
        {
            get
            {
                return _uartDistance;
            }
            set
            {
                Set(ref _uartDistance, value);
            }
        }

        private int _uartTemperature;
        public int UARTTemperature
        {
            get
            {
                return _uartTemperature;
            }
            set
            {
                Set(ref _uartTemperature, value);
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
