using System;
namespace FuelTankerSensorSimulator
{

    public class TankerData
    {
        int speed;
        public string TailNumber { get; set; }

        public DateTime LaunchTime { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        /// <summary>
        /// Current transfer fuel capacity in lbs
        /// </summary>
        public double FuelLoad { get; set; }
        /// <summary>
        /// Current speed in kts
        /// </summary>
        public int Speedkts
        {
            get { return speed; }
            set
            {
                speed = value;
                Speedkmh = value / 0.539956803456;
                Speedmph = value * 1.15077945;
            }
        }
        /// <summary>
        /// Current speed in kilometers per hour
        /// </summary>
        public double Speedkmh { get; set; }
        /// <summary>
        /// Current speed in miles per hour
        /// </summary>
        public double Speedmph { get; set; }
        /// <summary>
        /// Current alittude in ft.
        /// </summary>
        public int Altitude { get; set; }
        /// <summary>
        /// Current heading in magnetic degrees
        /// </summary>
        public int Heading { get; set; }
        /// <summary>
        /// The Tanker Class contains all the information related to a single instance of an aerial tanker (KC-135)
        /// You initialize the class with the Tail Number and Initial Fuel Load. This constructor will initialize all
        /// other properties to 0, indicating that the tanker is on the ground and has not yet begun operations
        /// </summary>
        /// <param name="tailNumber">A string value representing the tail number of a specific tanker</param> 
        /// <param name="initialFuel">A string value representing the tail number of a specific tanker starts with</param> 
        public TankerData(string tailNumber, int initialFuel)
        {
            TailNumber = tailNumber;
            LaunchTime = DateTime.UtcNow;
            FuelLoad = initialFuel;
            Latitude = 0;
            Longitude = 0;
            Altitude = 0;
            Heading = 0;
            Speedkts = 0;
        }
        /// <summary>
        /// This constructor is used when the aircraft is in flight in order to update volatile data such as lat/lon, fuel, speed, etc...
        /// </summary>
        /// <param name="tailNumber">A string value representing the tail number of a specific tanker</param>
        /// <param name="fuelLoad">An integer value representing the amount of transferrable fuel (in lbs) remaining</param>
        /// <param name="latitude">A double value representing the current latitude of the tanker</param>
        /// <param name="longitude">A double value representing the current longitude of the tanker</param>
        /// <param name="altitude">An integer value representing the current altitude (in feet) of the tanker</param>
        /// <param name="heading">An integer value representing the current magnetic heading of the tanker</param>
        /// <param name="speed">An integer value representing the current speed (in kts) of the tanker</param>
        public TankerData(string tailNumber, int fuelLoad, double latitude, double longitude, int altitude, int heading, int speed )
        {
            TailNumber = tailNumber;
            LaunchTime = DateTime.UtcNow;
            FuelLoad = fuelLoad;
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
            Heading = heading;
            Speedkts = speed;            
        }
    }
}
