using System;
using System.Threading;


namespace FuelTankerSensorSimulator
{
    public class Program
    {
        //static string connectionString = "HostName=fedTankerDemo.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=bYyV8U0PYVozwqoseKG4oF8Ja8wY/aRRDrzVCG2vv/0=";
        static string deviceKey;
        static string deviceName;

        
        private static int RandomGenerator(int min, int max)
        {
            Random rnd = new Random();
            return rnd.Next(min, max);
        }


        
        static void Main(string[] args)
        {
            if(args == null)
            {
                Console.WriteLine("usage: TankerSimulation.exe <deviceName> <deviceKey>");
                return;
            }
            if (args.Length < 2)
            {
                Console.WriteLine("usage: TankerSimulation.exe < deviceName > < deviceKey > ");
                Console.WriteLine("Only detected {0} arguments", args.Length.ToString());
                return;
            }

            deviceName = args[0].ToString();
            deviceKey = args[1].ToString();

            string acTailNo = RandomGenerator(58, 65).ToString() + "-00" + RandomGenerator(15, 95).ToString();
      
            // Setup the reset event that will be notified when the tanker is Bingo Fuel
            AutoResetEvent autoEvent = new AutoResetEvent(false);

            //Launch an aircraft and set initial flight parameters

            TankerData ac01 = new TankerData(acTailNo, 200000);
            ac01.Speedkts = RandomGenerator(250, 400);
            ac01.Heading = RandomGenerator(0, 360);
            //This position is the Goldwater MOA refuelling track ARIP
            ac01.Latitude = 32.582167;
            ac01.Longitude = -114.459419;
            ac01.Altitude = RandomGenerator(25000, 40000);

            //Create a new instance of the TankerOps class to monitor flight status
            TankerOps ac01Ops = new TankerOps(ac01, deviceKey, deviceName);
            Console.WriteLine("Aircraft {0} Arrive ARIP at {1:hh:mm} Zulu", ac01.TailNumber, DateTime.UtcNow.ToString());
            Console.WriteLine("Speed {0} kts, Heading {1} degrees, Altitude {2} ft ", ac01.Speedkts, ac01.Heading, ac01.Altitude);
            Console.WriteLine();

            //Setup the callback for the timer that will be used to monitor status
            TimerCallback tcb = ac01Ops.FlightStatus;

            //Start the timer and set the interval to 10 seconds for the initial call and 1 minute for subsequent calls
            Timer flightTimer = new Timer(tcb, autoEvent, 10000, 60000);

            //Block this thread until the completion event is fired
            autoEvent.WaitOne();
            //good housekeeping
            flightTimer.Dispose();
            Console.WriteLine();
            Console.WriteLine("Mission complete for aircraft {0}", ac01.TailNumber);
            
        }
    }
}
