using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices;
using Windows.Devices.I2c;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.IoT.Lightning.Providers;
using VCNL4000Adapter;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace pi_sensors_win10Core
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly ThreadPoolTimer _timer;
        private I2cDevice _proximitySensor;

        public MainPage()
        {
            this.InitializeComponent();

            if (!LightningProvider.IsLightningEnabled)
            {
                // Lightning provider is required for this sample
                Application.Current.Exit();
            }

            // set Lightning provider as the default
            LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

            var proximitySensorProvider = new VCNL4000_Provider();

            _proximitySensor = proximitySensorProvider.GetSensor();

            _timer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromMilliseconds(1000));


        }

        private void Timer_Tick(ThreadPoolTimer timer)
        {
            byte[] buffer = new byte[8];

            _proximitySensor.Read(buffer);
            
        }
    }
}
