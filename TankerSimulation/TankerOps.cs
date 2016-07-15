using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace FuelTankerSensorSimulator
{
    public class TankerOps
    {
        private int minFuel;
        private int totalFlightTime;
        private double previousLat;
        private double previousLon;
        private DateTime previousStatusTime;
        private double fuelRemaining;
        private TankerData aircraft;

        private static string deviceKey;
        private static string deviceName;
        private static string iotHubURI;

        /// <summary>
        /// There are some transient issues with Raspberry Pi certificates, even after importing them using the mozroots utility
        /// (sudo mozroots --import --ask-remove --sync --quiet) so this callback examines the certificate chain.
        /// Typically you would inform the user of an issue here, but I am simply ignoring the problem as we know that the
        /// ssl communication is with Azure IoT and not a site where information could be compromised. However, if we don't use
        /// this callback routine, the call to DeviceClient will fail with an "Unable to Decrypt" message in some cases.
        /// </summary>
        /// <param name="sender">The DeviceClient method</param>
        /// <param name="certificate">The requested certificate</param>
        /// <param name="chain">The chain of authority</param>
        /// <param name="sslPolicyErrors">Any policy errors that might be present</param>
        /// <returns></returns>
        public static bool RemoteCertificateValidationCallback(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool isOk = true;
            // If there are errors in the certificate chain, look at each error to determine the cause.
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                for (int i = 0; i < chain.ChainStatus.Length; i++)
                {
                    if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
                    {
                        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                        chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                        bool chainIsValid = chain.Build((X509Certificate2)certificate);
                        if (!chainIsValid)
                        {
                            isOk = false;
                        }
                    }
                }
            }
            return isOk;
        }

        public TankerOps(TankerData plane, string dk, string dn)
        {
            minFuel = 3000;
            totalFlightTime = 0;
            previousLat = plane.Latitude;
            previousLon = plane.Longitude;
            previousStatusTime = plane.LaunchTime;
            fuelRemaining = plane.FuelLoad;
            aircraft = plane;
            deviceKey = dk;
            iotHubURI = "fedTankerDemo.azure-devices.net";
            deviceName = dn;
        }
        public void FlightStatus(Object stateinfo)
        {
            double flightMinutes = (DateTime.UtcNow - previousStatusTime).TotalMinutes;
            previousStatusTime = DateTime.UtcNow;
            totalFlightTime = totalFlightTime + (int)flightMinutes;



            if (flightMinutes >= 1)
            {
                Random rnd = new Random();
                //KC-135 max transfer is 6500lbs / minute -- Randomize that value for this instance
                int transferRate = rnd.Next(0, 6500);
                //How much fuel did we transfer
                fuelRemaining = aircraft.FuelLoad - (transferRate * flightMinutes); 
                aircraft.FuelLoad = fuelRemaining;
                //How far did we travel
                double distance = (aircraft.Speedkmh / 60) * flightMinutes;
                //Calculate the new Lat/Lon based on distance travelled and heading
                GlobalCoordinates newPosition = newLoc(aircraft.Latitude, aircraft.Longitude, distance * 1000, aircraft.Heading);

                aircraft.Latitude = newPosition.Latitude.Degrees;
                aircraft.Longitude = newPosition.Longitude.Degrees;
                Console.WriteLine("Checking Aircraft {0} status at {1}", aircraft.TailNumber, System.DateTime.UtcNow.ToString());
                Console.WriteLine("Current Position: {0:0.0000} by {1:0.0000}", aircraft.Latitude, aircraft.Longitude);
                Console.WriteLine("Elapsed Flight Time {0} minutes", totalFlightTime);
                Console.WriteLine("Transfer Fuel Remaining: {0:0.00} lbs", fuelRemaining);
                Console.WriteLine();
            }

            //Send the aircraft telemetry to Azure
            SendDeviceToCloudMessageAsync(aircraft);

            AutoResetEvent autoevent = (AutoResetEvent)stateinfo;       
            //When the fuel load reaches the minimum, terminate the mission and the application
            if (fuelRemaining <= minFuel)
            {
                Console.WriteLine("Aircraft {0} is Bingo, Returning to base", aircraft.TailNumber);
                autoevent.Set();
            }

        }

        //Calculate the new Lat/Lon based on the WGS84 Ellipsoid
        static GlobalCoordinates newLoc(double lat, double lon, double distance, int heading)
        {
            GeoCalc gc = new GeoCalc();
            Ellipsoid model = Ellipsoid.WGS84;
            GlobalCoordinates startLoc = new GlobalCoordinates(new Angle(lat), new Angle(lon));

            Angle startHeading = new Angle(heading);
            Angle endHeading;

            GlobalCoordinates dest = gc.CalculateEndingGlobalCoordinates(model, startLoc, startHeading, distance, out endHeading);

            return dest;

        }

        static async void SendDeviceToCloudMessageAsync(TankerData ac)
        {
            // As mentioned above,  this is to handle transient issues with ssl certificates that occur when using
            //mono on the Raspberry Pi
            ServicePointManager.ServerCertificateValidationCallback = RemoteCertificateValidationCallback;

            // Setup the Azure iot Client
            try
                {
                    var deviceClient = DeviceClient.Create(iotHubURI,
                         Microsoft.Azure.Devices.Client.AuthenticationMethodFactory.CreateAuthenticationWithRegistrySymmetricKey(deviceName, deviceKey),
                         Microsoft.Azure.Devices.Client.TransportType.Http1);


                    var telemetryDataPoint = new
                    {
                        tailNo = ac.TailNumber,
                        speed = ac.Speedkts,
                        heading = ac.Heading,
                        altitude = ac.Altitude,
                        remainingFuel = ac.FuelLoad,
                        currentLat = ac.Latitude,
                        currentLon = ac.Longitude,
                        timestamp = DateTime.UtcNow.ToString()
                    };

                    var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                    var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));

                    await deviceClient.SendEventAsync(message);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to communicate with Azure. The message is: {0}", ex.Message.ToString());
                }
            }
        }
    }

 

