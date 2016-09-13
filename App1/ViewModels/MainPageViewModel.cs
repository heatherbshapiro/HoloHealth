using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Band;
using System.Diagnostics;
using Microsoft.Azure.Devices.Client;
using Windows.Devices.Geolocation;
using System.Runtime.Serialization;
using Windows.ApplicationModel.Core;
using Windows.UI.Popups;
using Newtonsoft.Json;
using Windows.UI.Core;

namespace App1.ViewModels
{
    class MainPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string statusMessage = "Pair a Microsoft Band with your device and click Run.";
        public string StatusMessage
        {
            get { return statusMessage; }
            set
            {
                statusMessage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusMessage)));    
            }
        }
        //List<object> samples = new List<object>();

        public int samplesReceived = 0;



        [DataContract]
        internal class DeviceProperties
        {
            [DataMember]
            internal string DeviceID;

            [DataMember]
            internal bool HubEnabledState;

            [DataMember]
            internal string CreatedTime;

            [DataMember]
            internal string DeviceState;

            [DataMember]
            internal string UpdatedTime;

            [DataMember]
            internal string Manufacturer;

            [DataMember]
            internal string ModelNumber;

            [DataMember]
            internal string SerialNumber;

            [DataMember]
            internal string FirmwareVersion;

            [DataMember]
            internal string Platform;

            [DataMember]
            internal string Processor;

            [DataMember]
            internal string InstalledRAM;

            [DataMember]
            internal double Latitude;

            [DataMember]
            internal double Longitude;

        }

        [DataContract]
        internal class Thermostat
        {
            [DataMember]
            internal DeviceProperties DeviceProperties;

            [DataMember]
            internal Command[] Commands;

            [DataMember]
            internal bool IsSimulatedDevice;

            [DataMember]
            internal string Version;

            [DataMember]
            internal string ObjectType;
        }
        [DataContract]
        internal class TelemetryData
        {
            [DataMember]
            internal string DeviceId;

            [DataMember]
            internal double HeartRate;

        }
        [DataContract]

        internal class Command
        {
            [DataMember]
            internal string Name;

            [DataMember]
            internal CommandParameter[] Parameters;
        }
        [DataContract]
        internal class CommandParameter
        {
            [DataMember]
            internal string Name;

            [DataMember]
            internal string Type;
        }

        private string deviceId= "75e015c1-393c-4b94-a1d6-84e30b9fcf48";
        private string hostName = "HoloHealth.azure-devices.net";

        private string deviceKey = "5gwhFKJ6Ci5RH1w65Ee0Tqdy+lbzvA+wwdJHzZdb364=";
        private string connectionString = "HostName=HoloHealth.azure-devices.net;DeviceId=75e015c1-393c-4b94-a1d6-84e30b9fcf48;SharedAccessKey=5gwhFKJ6Ci5RH1w65Ee0Tqdy+lbzvA+wwdJHzZdb364=";

        private bool SendDataToAzureIoTHub = true;
        private double HeartRate;

        private DeviceClient deviceClient;
        Task ReceivingTask;

        public void connectHub()
        {
            Task.Run(SendDataToAzure);
        }
        private async void connectToIoTSuite()
        {
            //connectionString = "HostName=" + hostName + ";DeviceId=" + deviceId + ";SharedAccessKey=" + deviceKey;
            try
            {
                deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Http1);

                await deviceClient.OpenAsync();
                sendDeviceMetaData();
                ReceivingTask = Task.Run(ReceiveDataFromAzure);
            }
            catch
            {
                Debug.Write("Error while trying to connect to IoT Hub");
                deviceClient = null;
            }
        }
        private async void disconnectFromIoTSuite()
        {
            if (deviceClient != null)
            {
                try
                {
                    await deviceClient.CloseAsync();
                    deviceClient = null;
                }
                catch
                {
                    Debug.Write("Error while trying close the IoT Hub connection");
                }
            }
        }
        private async Task ReceiveDataFromAzure()
        {

            while (true)
            {
                Message message = await deviceClient.ReceiveAsync();
                if (message != null)
                {
                    try
                    {
                        dynamic command = DeSerialize(message.GetBytes());
                        if (command.Name == "TriggerAlarm")
                        {
                            // Received a new message, display it
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            async () =>
                            {
                                var dialogbox = new MessageDialog("Received message from Azure IoT Hub: " + command.Parameters.Message.ToString());
                                await dialogbox.ShowAsync();
                            });
                            // We received the message, indicate IoTHub we treated it
                            await deviceClient.CompleteAsync(message);
                        }
                    }
                    catch
                    {
                        await deviceClient.RejectAsync(message);
                    }
                }
            }
        }
        private dynamic DeSerialize(byte[] data)
        {
            string text = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject(text);
        }

        //private async Task SendDataToAzure()
        //{

        //    while (true)
        //    {
        //        if (SendDataToAzureIoTHub)
        //        {
        //            connectToIoTSuite();
        //            //sendDeviceTelemetryData();
        //        }
        //        await Task.Delay(1000);
        //    }
        //}
        private async void sendDeviceMetaData()
        {
            DeviceProperties device = new DeviceProperties();
            Thermostat thermostat = new Thermostat();

            thermostat.ObjectType = "DeviceInfo";
            thermostat.IsSimulatedDevice = false;
            thermostat.Version = "1.0";

            device.HubEnabledState = true;
            device.DeviceID = deviceId;
            device.Manufacturer = "Microsoft";
            device.ModelNumber = "Lumia950";
            device.SerialNumber = "5849735293875";
            device.FirmwareVersion = "10";
            device.Platform = "Windows 10";
            device.Processor = "SnapDragon";
            device.InstalledRAM = "3GB";
            device.DeviceState = "normal";

            //Geolocator geolocator = new Geolocator();
            //Geoposition pos = await geolocator.GetGeopositionAsync();

            //device.Latitude = (float)pos.Coordinate.Point.Position.Latitude;
            //device.Longitude = (float)pos.Coordinate.Point.Position.Longitude;

            thermostat.DeviceProperties = device;

            Command TriggerAlarm = new Command();
            TriggerAlarm.Name = "TriggerAlarm";
            CommandParameter param = new CommandParameter();
            param.Name = "Message";
            param.Type = "String";
            TriggerAlarm.Parameters = new CommandParameter[] { param };

            thermostat.Commands = new Command[] { TriggerAlarm };

            try
            {
                var msg = new Message(Serialize(thermostat));
                if (deviceClient != null)
                {
                    await deviceClient.SendEventAsync(msg);
                }
            }
            catch (System.Exception e)
            {
                Debug.Write("Exception while sending device meta data :\n" + e.Message.ToString());
            }

            Debug.Write("Sent meta data to IoT Suite\n" + hostName);

        }
        private byte[] Serialize(object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(json);

        }
        //private void sendDeviceTelemetryData()
        //{

        //    updateMessage();

        //    //TelemetryData data = new TelemetryData();
        //    //data.DeviceId = deviceId;
        //    //data.HeartRate = HeartRate;

        //    //try
        //    //{
        //    //    var msg = new Message(Serialize(data));
        //    //    if (deviceClient != null)
        //    //    {
        //    //        await deviceClient.SendEventAsync(msg);
        //    //    }
        //    //}
        //    //catch (System.Exception e)
        //    //{
        //    //    Debug.Write("Exception while sending device telemetry data :\n" + e.Message.ToString());
        //    //}
        //    //Debug.Write("Sent telemetry data to IoT Suite\nHeartRate=" + string.Format("{0:0.00}", 76));

        //}
        public async Task SendDataToAzure()
        {
            //StatusMessage = "Gavin is okay";
            try
            {
                // Get the list of Microsoft Bands paired to the phone.
                IBandInfo[] pairedBands = await BandClientManager.Instance.GetBandsAsync();
                if (pairedBands.Length < 1)
                {
                    StatusMessage = "This sample app requires a Microsoft Band paired to your device. Also make sure that you have the latest firmware installed on your Band, as provided by the latest Microsoft Health app.";
                    return;
                }

                // Connect to Microsoft Band.
                using (IBandClient bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
                {
                    bool heartRateConsentGranted;

                    // Check whether the user has granted access to the HeartRate sensor.
                    if (bandClient.SensorManager.HeartRate.GetCurrentUserConsent() == UserConsent.Granted)
                    {
                        heartRateConsentGranted = true;
                    }
                    else
                    {
                        heartRateConsentGranted = await bandClient.SensorManager.HeartRate.RequestUserConsentAsync();
                    }

                    if (!heartRateConsentGranted)
                    {
                        StatusMessage = "Access to the heart rate sensor is denied.";
                    }
                    else
                    {
                        //int samplesReceived = 0; // the number of HeartRate samples received
                        //List<object> samples = new List<object>();

                        // Subscribe to HeartRate data.
                        //bandClient.SensorManager.HeartRate.ReadingChanged += (s, args) => { samplesReceived++; HeartRate = args.SensorReading.HeartRate; };
                        //await Task.Run(SendDataToAzure);
                        
                        while (true)
                        {
                            if (SendDataToAzureIoTHub)
                            {
                                connectToIoTSuite();
                                //sendDeviceTelemetryData();
                                bandClient.SensorManager.HeartRate.ReadingChanged +=sendTelemetryData;
                                await bandClient.SensorManager.HeartRate.StartReadingsAsync();

                                // Receive HeartRate data for a while, then stop the subscription.
                                await Task.Delay(TimeSpan.FromSeconds(5));
                                await bandClient.SensorManager.HeartRate.StopReadingsAsync();
                            }
                            await Task.Delay(1000);
                        }
                        
                        

                        //StatusMessage = string.Format("Done. {0} HeartRate samples were received.", samplesReceived);
                        //Debug.WriteLine(bandClient.SensorManager.HeartRate);
                        
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = ex.ToString();
            }

        }

        public async void updateMessage()
        {
            await Task.Run(SendDataToAzure);
        }

        private async void sendTelemetryData(object sender, Microsoft.Band.Sensors.BandSensorReadingEventArgs<Microsoft.Band.Sensors.IBandHeartRateReading> e)
        {
            
            //HeartRate = e.SensorReading.HeartRate;
            TelemetryData data = new TelemetryData();
            data.DeviceId = deviceId;
            data.HeartRate = e.SensorReading.HeartRate;
            //StatusMessage = string.Format("Done. {0} HeartRate samples were received.{1}", samplesReceived++,data.HeartRate );

            try
            {
                var msg = new Message(Serialize(data));
                if (deviceClient != null)
                {
                    await deviceClient.SendEventAsync(msg);
                }
            }
            catch (System.Exception ex)
            {
                Debug.Write("Exception while sending device telemetry data :\n" + ex.Message.ToString());
            }
            Debug.Write("Sent telemetry data to IoT Suite\nHeartRate=" + string.Format("{0:0.00}", 76));
        }
    }

}