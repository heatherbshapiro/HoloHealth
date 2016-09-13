using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Band;
using System.Diagnostics;

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

        public async void updateMessage()
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
                        int samplesReceived = 0; // the number of HeartRate samples received
                        List<object> samples = new List<object>();

                        // Subscribe to HeartRate data.
                        bandClient.SensorManager.HeartRate.ReadingChanged += (s, args) => { samplesReceived++;samples.Add(args.SensorReading.HeartRate); };
                        await bandClient.SensorManager.HeartRate.StartReadingsAsync();

                        // Receive HeartRate data for a while, then stop the subscription.
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        await bandClient.SensorManager.HeartRate.StopReadingsAsync();

                        StatusMessage = string.Format("Done. {0} HeartRate samples were received. {1} ", samplesReceived, samples[0]);
                        //Debug.WriteLine(bandClient.SensorManager.HeartRate);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = ex.ToString();
            }

        }
    }
}
