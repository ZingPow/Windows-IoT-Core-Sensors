using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace PMSx003_ParticleSensor.Sensors.PMSx003_ParticleSensor
{
public class PMSx003_ParticleSensor
    {

        /*
         * 
    Don't forget to update the appxmanifest to include access to the serial interface

    <DeviceCapability Name="serialcommunication">
      <Device Id="any">
        <Function Type="name:serialPort" />
      </Device>
    </DeviceCapability>
         *
         */

        //UART
        private SerialDevice _serialPort;

        private System.Threading.CancellationTokenSource _readCancellationTokenSource;
        private DataWriter _dataWriter;
        private DataReader _dataReader;
        private string _uartPort;
        private bool _listening;

        private static readonly byte[] SLEEPCMD = { 0x42, 0x4d, 0xe4, 0x00, 0x00, 0x01, 0x73 };
        private static readonly byte[] WAKECMD = { 0x42, 0x4d, 0xe4, 0x00, 0x01, 0x01, 0x74 };
        private static readonly byte[] ACTIVEMODECMD = { 0x42, 0x4d, 0xe1, 0x00, 0x01, 0x01, 0x71 };
        private static readonly byte[] PASSIVEMODECMD = { 0x42, 0x4d, 0xe1, 0x00, 0x00, 0x01, 0x70 };
        private static readonly byte[] PASSIVEREADCMD = { 0x42, 0x4d, 0xe2, 0x00, 0x00, 0x01, 0x71 };
 
        //GPIO pins
        private int _resetPin = -1;

        private GpioPin _resetGPIOPin;

        private int _sleepPin = -1;
        private GpioPin _sleepGPIOPin;

        // this seems to be a common feature in some other PMXx003 drivers I've seen
        // what it does is only reports a reading every x seconds
        // to disable it use an interval of 0
        private const int DEFAULTREADINGINTERVAL = 30;


        public PMSx003_ParticleSensor(string uartName = "UART0")
        {
            _uartPort = uartName;

            InitSensor();
        }

        public PMSx003_ParticleSensor(int resetPin, int sleepPin, string uartName = "UART0")
        {
            _resetPin = resetPin;
            _sleepPin = sleepPin;
            _uartPort = uartName;

            InitSensor();
        }

        private async Task InitSensor()
        {
            await InitGPIO();

            await ConnectUART(_uartPort);

            StartListening();
        }

        private void _serialPort2_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Debug.WriteLine(e.ToString());
        }

        private async Task InitGPIO()
        {
            try
            {
                GpioController gpioController = await GpioController.GetDefaultAsync();

                // When applying the sleep function, note that the fan stops working when you sleep and the
                // fan restart requires at least 30 Sec settling time, so to obtain accurate data,
                // the sleep wake-up after the sensor working time should not be less then 30 seconds.

                if (_sleepPin > 0)
                {
                    _sleepGPIOPin = gpioController.OpenPin(_sleepPin, GpioSharingMode.Exclusive);
                    _sleepGPIOPin.SetDriveMode(GpioPinDriveMode.Output);
                    _sleepGPIOPin.Write(GpioPinValue.High); //low is sleep mode, high is active
                }

                if (_resetPin > 0)
                {
                    _resetGPIOPin = gpioController.OpenPin(_resetPin, GpioSharingMode.Exclusive);
                    _resetGPIOPin.SetDriveMode(GpioPinDriveMode.Output);
                    _resetGPIOPin.Write(GpioPinValue.High); //low resets the sensor
                }
            }
            catch (Exception ex)
            {
                throw new Exception("UART Initialise Error", ex);
            }
        }

        public enum SensorOperationStatus { Active, Sleep };

        private SensorOperationStatus _sensorOperationStatus;

        public SensorOperationStatus SensorStatus
        {
            get
            {
                return _sensorOperationStatus;
            }
            set
            {
                switch (value)
                {
                    case SensorOperationStatus.Active:
                        WakeSensor();
                        break;

                    case SensorOperationStatus.Sleep:
                        SleepSensor();
                        break;

                    default:
                        break;
                }

                _sensorOperationStatus = value;
            }
        }

        public enum SensorHardwareStatus { Active, Sleep };

        private SensorHardwareStatus _sensorHardwareStatus;

        public SensorHardwareStatus HardwareStatus
        {
            get
            {
                return _sensorHardwareStatus;
            }

            set
            {
                if (_sleepGPIOPin != null)
                {
                    switch (value)
                    {
                        case SensorHardwareStatus.Sleep:
                            HardSleepSensor();
                            break;

                        case SensorHardwareStatus.Active:
                            HardWakeSensor();
                            break;

                        default:
                            break;
                    }

                    _sensorHardwareStatus = value;
                }
            }
        }

        public enum SensorMode { Active, Passive };

        private SensorMode _sensorMode;

        public SensorMode Mode
        {
            get
            {
                return _sensorMode;
            }
            set
            {
                switch (value)
                {
                    case SensorMode.Active:
                        ActiveModeOn();
                        break;

                    case SensorMode.Passive:
                        StopIntervalTimer();
                        PassiveModeOn();
                        break;

                    default:
                        break;
                }

                _sensorMode = value;
            }
        }

        private void SleepSensor()
        {
            StopListening();

            SendBytes(SLEEPCMD);
        }

        private void HardSleepSensor()
        {
            if (_sleepGPIOPin != null)
            {
                StopListening();

                _sleepGPIOPin.Write(GpioPinValue.Low);
            }
        }

        private void HardWakeSensor()
        {
            if (_sleepGPIOPin != null)
            {
                _sensorOperationStatus = SensorOperationStatus.Active;
                _sensorMode = SensorMode.Active;

                _sleepGPIOPin.Write(GpioPinValue.High);

                if (!_listening)
                {
                    StartListening();
                }
            }
        }

        private void WakeSensor()
        {
            SendBytes(WAKECMD);

            _sensorOperationStatus = SensorOperationStatus.Active;
            _sensorMode = SensorMode.Active;

            if (!_listening)
            {
                StartListening();
            }
        }

        public async void HardResetSensor()
        {
            if (_resetGPIOPin != null)
            {
                StopListening();

                _sensorOperationStatus = SensorOperationStatus.Active;
                _sensorMode = SensorMode.Active;

                _resetGPIOPin.Write(GpioPinValue.Low);

                await Task.Delay(100);

                _resetGPIOPin.Write(GpioPinValue.High);

                if (HardwareStatus == SensorHardwareStatus.Active)
                {
                    StartListening();
                }
            }
        }

        private void PassiveModeOn()
        {
            SendBytes(PASSIVEMODECMD);

            if (!_listening)
            {
                StartListening();
            }
            else
            {
                StartIntervalTimer(ReadingInterval);
            }
        }

        private void ActiveModeOn()
        {
            SendBytes(ACTIVEMODECMD);

            if (!_listening)
            {
                StartListening();
            }
        }

        public void PassiveRead()
        {
            if (Mode == SensorMode.Passive)
                SendBytes(PASSIVEREADCMD);
        }

        private void SetSleepModeOn()
        {
            StopListening();

            SendBytes(SLEEPCMD);
        }

        private int _readingInterval = DEFAULTREADINGINTERVAL;

        public int ReadingInterval
        {
            get
            {
                return _readingInterval;
            }
            set
            {
                StartIntervalTimer(value);
                _readingInterval = value;
            }
        }

        private void StartIntervalTimer(int interval)
        {
            StopIntervalTimer();

            if (interval > 0)
            {
                int i = interval * 1000;

                _readIntervalTimer = new Timer(async (e) =>
                {
                    SendReadEvent();
                }, null, i, i);
            }
        }

        private void StopIntervalTimer()
        {
            if (_readIntervalTimer != null)
                _readIntervalTimer.Dispose();
        }

        private async Task SendReadEvent()
        {
            if (_serialPort != null && Mode == SensorMode.Active)
            {
                ReadingComplete?.Invoke(this, new ReadingArgs(ReadingTime));
            }
        }

        private Timer _readIntervalTimer;

        public async Task ConnectUART(string UARTPort)
        {
            if (_serialPort == null)
            {
                try
                {
                    string aqs = SerialDevice.GetDeviceSelector(UARTPort); //Raspberry Pi UART Port could pass this in if you have more then 1 port available
                    DeviceInformationCollection dis = await DeviceInformation.FindAllAsync(aqs);
                    _serialPort = await SerialDevice.FromIdAsync(dis[0].Id);

                    //Configure Serial Settings
                    _serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                    _serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                    _serialPort.BaudRate = 9600;
                    _serialPort.Parity = SerialParity.None;
                    _serialPort.StopBits = SerialStopBitCount.One;
                    _serialPort.DataBits = 8;

                    //setup stream processors
                    _dataReader = new DataReader(_serialPort.InputStream)
                    {
                        InputStreamOptions = InputStreamOptions.Partial
                    };
                    _dataWriter = new DataWriter(_serialPort.OutputStream);
                }
                catch (Exception ex)
                {
                    throw new Exception("UART Initialise Error", ex);
                }
            }
        }

        public void DisconnectUART()
        {
            StopListening();

            if (_dataWriter != null)
            {
                _dataWriter.DetachStream();
                _dataWriter.Dispose();
            }

            if (_dataReader != null)
            {
                _dataReader.DetachStream();
                _dataReader.Dispose();
            }

            if (_serialPort != null)
            {
                _serialPort.Dispose();
            }
        }

        //ASYNC Method to create the listen loop
        public async void StartListening()
        {
            _readCancellationTokenSource = new CancellationTokenSource();

            if (Mode == SensorMode.Active)
                StartIntervalTimer(ReadingInterval);

            while (true)
            {
                _listening = true;
                await Listen();
                if (_readCancellationTokenSource.Token.IsCancellationRequested || (_serialPort == null) || !_listening)
                    break;
            }
            _listening = false;
        }

        public void StopListening()
        {
            StopIntervalTimer();

            if (_readCancellationTokenSource != null && _readCancellationTokenSource.Token.CanBeCanceled)
            {
                _readCancellationTokenSource.Cancel();
            }
        }

        private async Task Listen()
        {
            byte[] ReceiveData;
            UInt32 bytesRead;

            try
            {
                if (_serialPort != null)
                {
                    while (true)
                    {
                        if (_readCancellationTokenSource.Token.IsCancellationRequested || (_serialPort == null))
                            break;

                        bytesRead = await _dataReader.LoadAsync(1).AsTask(_readCancellationTokenSource.Token);
                        if (bytesRead == 1)
                        {
                            byte StartData = _dataReader.ReadByte();

                            if (!_readCancellationTokenSource.Token.IsCancellationRequested)
                            {
                                //looking for a 0x42, 0x4D as a data start marker
                                if (StartData == 0x42)
                                {
                                    bytesRead = await _dataReader.LoadAsync(1).AsTask(_readCancellationTokenSource.Token);
                                    if (bytesRead == 1)
                                    {
                                        if (!_readCancellationTokenSource.Token.IsCancellationRequested)
                                        {
                                            byte StartData2 = _dataReader.ReadByte();

                                            if (StartData2 == 0x4D)
                                            {
                                                //next 2 bytes contain the framelength
                                                bytesRead = await _dataReader.LoadAsync(2).AsTask(_readCancellationTokenSource.Token);
                                                if (bytesRead == 2)
                                                {
                                                    if (!_readCancellationTokenSource.Token.IsCancellationRequested)
                                                    {
                                                        byte[] FrameHeader = new byte[2];
                                                        _dataReader.ReadBytes(FrameHeader);

                                                        uint framelength = PMSConvert(FrameHeader[0], FrameHeader[1]);

                                                        bytesRead = await _dataReader.LoadAsync(framelength).AsTask(_readCancellationTokenSource.Token);
                                                        if (!_readCancellationTokenSource.Token.IsCancellationRequested)
                                                        {
                                                            if (bytesRead == framelength)
                                                            {
                                                                ReceiveData = new byte[framelength];
                                                                _dataReader.ReadBytes(ReceiveData);

                                                                //do checksum test remembering to add in the previous bytes
                                                                uint checksum = (uint)(0x42 + 0x4D + FrameHeader[0] + FrameHeader[1]);

                                                                for (int i = 0; i < ReceiveData.Length - 2; i++)
                                                                {
                                                                    checksum += ReceiveData[i];
                                                                }

                                                                if (!_readCancellationTokenSource.Token.IsCancellationRequested)
                                                                {
                                                                    if (ReceiveData.Length == 28) //receiving expected data
                                                                    {
                                                                        uint checksumcheck = PMSConvert(ReceiveData[framelength - 2], ReceiveData[framelength - 1]);

                                                                        if (checksum == checksumcheck)
                                                                        {
                                                                            ReadingTime = DateTime.Now;

                                                                            PM1_0Concentration_CF1 = PMSConvert(ReceiveData[0], ReceiveData[1]);
                                                                            PM2_5Concentration_CF1 = PMSConvert(ReceiveData[2], ReceiveData[3]);
                                                                            PM10_0Concentration_CF1 = PMSConvert(ReceiveData[4], ReceiveData[5]);

                                                                            PM1_0Concentration_amb = PMSConvert(ReceiveData[6], ReceiveData[7]);
                                                                            PM2_5Concentration_amb = PMSConvert(ReceiveData[8], ReceiveData[9]);
                                                                            PM10_0Concentration_amb = PMSConvert(ReceiveData[10], ReceiveData[11]);

                                                                            PM0_3Count = PMSConvert(ReceiveData[12], ReceiveData[13]);
                                                                            PM0_5Count = PMSConvert(ReceiveData[14], ReceiveData[15]);
                                                                            PM1_0Count = PMSConvert(ReceiveData[16], ReceiveData[17]);

                                                                            PM2_5Count = PMSConvert(ReceiveData[18], ReceiveData[19]);
                                                                            PM5_0Count = PMSConvert(ReceiveData[20], ReceiveData[21]);
                                                                            PM10_0Count = PMSConvert(ReceiveData[22], ReceiveData[23]);

                                                                            ProductVersion = ReceiveData[24];
                                                                            StatusCodes = ReceiveData[25];

                                                                            if (Mode == SensorMode.Passive || ReadingInterval == 0)
                                                                            {
                                                                                //raise the event that there is a new reading available
                                                                                ReadingComplete?.Invoke(this, new ReadingArgs(ReadingTime));
                                                                            }
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        Debug.Write("Some other kind of response: ");
                                                                        foreach (var item in ReceiveData)
                                                                        {
                                                                            Debug.WriteLine(" {0} ", item);
                                                                        }
                                                                        Debug.WriteLine("");
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Debug.WriteLine("Alignment issue 2 read non header character {0}", StartData2);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("Alignment issue read non header character {0}", StartData);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_readCancellationTokenSource != null)
                    _readCancellationTokenSource.Cancel();

                System.Diagnostics.Debug.WriteLine("UART ReadAsync Exception: {0}", ex.Message);
            }
        }

        private uint PMSConvert(byte high, byte low)
        {
            return (uint)(low + (high << 8));
        }

        private bool CheckSumTest(byte[] data)
        {
            int checksum = 0;

            for (int i = 0; i < data.Length - 2; i++)
            {
                checksum += data[i];
            }

            Debug.WriteLine("Checksum = {0} data[30] = {1} data[1] = {2} combined = {3}", checksum, data[30], data[31], PMSConvert(data[30], data[31]));

            return checksum == PMSConvert(data[30], data[31]);
        }

        public class ReadingArgs : EventArgs
        {
            public DateTime ReadingTime { get; private set; }

            public ReadingArgs(DateTime readingTime)
            {
                ReadingTime = readingTime;
            }
        }

        public event EventHandler<ReadingArgs> ReadingComplete;

        public async void SendBytes(byte[] TxData)
        {
            try
            {
                //Send data to UART
                _dataWriter.WriteBytes(TxData);
                await _dataWriter.StoreAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("UART Tx Error", ex);
            }
        }

        public async void SendText(string TxData)
        {
            try
            {
                //Send data to UART
                _dataWriter.WriteString(TxData);
                await _dataWriter.StoreAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("UART Tx Error", ex);
            }
        }

        public uint PM1_0Concentration_CF1 { get; private set; }

        public uint PM2_5Concentration_CF1 { get; private set; }

        public uint PM10_0Concentration_CF1 { get; private set; }

        public uint PM1_0Concentration_amb { get; private set; }

        public uint PM2_5Concentration_amb { get; private set; }

        public uint PM10_0Concentration_amb { get; private set; }

        public uint PM0_3Count { get; private set; }

        public uint PM0_5Count { get; private set; }

        public uint PM1_0Count { get; private set; }

        public uint PM2_5Count { get; private set; }

        public uint PM5_0Count { get; private set; }

        public uint PM10_0Count { get; private set; }

        public byte ProductVersion { get; private set; }

        public byte StatusCodes { get; private set; }

        public DateTime ReadingTime { get; private set; }
    }
}
