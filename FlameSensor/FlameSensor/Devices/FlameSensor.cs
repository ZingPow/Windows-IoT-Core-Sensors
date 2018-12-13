using System;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace FlameSensor.Devices
{
    /// <summary>
    /// Class that represents the Flame Sensor
    /// </summary>
    public class FlameSensor
    {
        private int _digitalPin;
        private GpioPin _digialGPIOPin;

        private int _fickerTimeOut = 10;
        public int FlickerTimeOut
        {
            get
            {
                return _fickerTimeOut;
            }
            set
            {
                if (value != _fickerTimeOut)
                {
                    _fickerTimeOut = value;
                    _digialGPIOPin.DebounceTimeout = new TimeSpan(0, 0, 0, 0, _fickerTimeOut);
                    Flame = _digialGPIOPin.Read() == GpioPinValue.High ? false : true;
                }
            }
        }

        /// <summary>
        /// A bool if flame is current dectected or not. 
        /// </summary>
        public bool Flame { get; private set; }


        /// <summary>
        /// Event args which contain inforamtion about the flame state change.
        /// </summary>
        public class FlameArgs : EventArgs
        {
            /// <summary>
            /// bool containing the current sate of the flame, ie if flame exists or not  <see cref="FlameArgs"/>.
            /// </summary>
            public bool Flame { get; private set; }
            /// <summary>
            /// The DateTime of the flame state change  <see cref="FlameArgs"/>.
            /// </summary>
            public DateTime EventTime { get; private set; }

            public FlameArgs(bool flame)
            {
                Flame = flame;
                EventTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Event raised if the state of the flame changes. 
        /// </summary>
        public event EventHandler<FlameArgs> FlameChange;

        public void OnFlameChange(bool flame)
        {
            FlameChange?.Invoke(this, new FlameArgs(flame));
        }

        /// <summary>
        /// Ctor. Gets the Digital Pin as a parameter. 
        /// </summary>
        /// <param name="DigitalPin"></param>
        public FlameSensor(int digitalPin)
        {
            _digitalPin = digitalPin;

            InitGPIO();
        }

        /// <summary>
        /// Returns a bool if flame is current dectected or not. 
        /// </summary>
        public bool IsFlame()
        {
            return _digialGPIOPin.Read() == GpioPinValue.High ? false : true;
        }

        private async Task InitGPIO()
        {
            GpioController GPIOController = await GpioController.GetDefaultAsync();

            _digialGPIOPin = GPIOController.OpenPin(_digitalPin, GpioSharingMode.Exclusive);
            _digialGPIOPin.SetDriveMode(GpioPinDriveMode.Input);
            _digialGPIOPin.DebounceTimeout = new TimeSpan(0, 0, 0, 0, FlickerTimeOut);

            Flame = _digialGPIOPin.Read() == GpioPinValue.High ? false : true;

            _digialGPIOPin.ValueChanged += _digialGPIOPin_ValueChanged;
        }

        private void _digialGPIOPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            Flame = args.Edge == GpioPinEdge.FallingEdge;
            OnFlameChange(Flame);
        }
    }
}
