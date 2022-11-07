using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Xml;

if (!GetSettings())
{
    Console.WriteLine("Failed to read settings!");
    return;
}

if (delayPeriodicity != 0 && delayDuration >= delayPeriodicity)
{
    Console.WriteLine("Invalid delay parameters!");
    return;
}

try
{
    udpClient = new(port);
    
    // buffer should be large enough to avoid or reduce packets loss
    udpClient.Client.ReceiveBufferSize = receiveBufferSize; 

    // multicast reduces reliability;
    // direct sending of UDP packets to the receiver's IP causes less packets loss;
    // TCP of course would be more reliable (and slower)
    udpClient.JoinMulticastGroup(groupAddress, ttl); 
}
catch
{
    Console.WriteLine("Invalid IP address or port values!");
    return;
}

dt.Columns.Add("Value", typeof(Int64));
dt.Columns.Add("Count", typeof(Int64));

if (delayPeriodicity > 0 && delayDuration > 0) // force messages loss
{
    timer = new(delayPeriodicity);
    timer.Elapsed += (sender, eventArgs) => useDelay = true;
    timer.Start();
}

Console.WriteLine("\nPlease use \"Q\" to exit, correctly free network resources and avoid side effects\n");

// wait for the user's "Enter" or "Q"
// use "Q" to free network resources and avoid side effects
Task.Run(() => Output());

while (true)
{
    if (useDelay)
    {
        useDelay = false;
        Thread.Sleep(delayDuration); // force messages loss
    }
    try
    {
        // awaiting may cause some additional messages loss;
        // otherwise we could get fast increase of memory usage that will make work of application impossible
        // (large queue of threads waiting for release of critical section)
        await Task.Run(async () => UpdateData((await udpClient.ReceiveAsync()).Buffer)); 
    }
    catch { };
}

partial class Program
{
    private const int halfBufferLength = 8;
    private const int receiveBufferSize = 10485760;

    private static int port;
    private static IPAddress? groupAddress;
    private static int ttl; // multicast TTL
    private static UdpClient? udpClient;

    private static int delayPeriodicity = 0;
    private static int delayDuration = 0;
    private static bool useDelay = false;
    private static System.Timers.Timer? timer;

    // stores received values and their frequencies;
    // always oredered and grouped by values;
    // increases permormance: smaller memory usage and faster data access — no need to store all large set of received data;
    // necessary only for mediane and mode calculation
    private static readonly DataTable dt = new(); 

    private static Int64 messagesCount = 0; 
    private static Int64 lostMessagesCount = 0; 
    private static Int64 sum = 0;
    private static double average = 0;
    private static double deviationSum = 0;
    private static double mediane = 0;
    private static Int64 mode = 0;
    private static Int64 maxValueCount = 0;
    private static Int64 initMessageNumber = - 1;

    private static readonly object locker = new();

    private static void Output()
    {
        ConsoleKey keyPressed = Console.ReadKey().Key;
        if (keyPressed == ConsoleKey.Enter)
        {
            // lock data access while output to avoid asynchrony side effects
            lock (locker)
            {
                Console.WriteLine("\nTotal messages received: " + messagesCount.ToString("n0"));
                Console.WriteLine("Total messages \"lost\":   " + lostMessagesCount.ToString("n0"));
                Console.WriteLine("Average:                 " + average.ToString("n"));
                Console.WriteLine("Standard deviation:      " + Math.Sqrt(deviationSum / (messagesCount + 1)).ToString("n"));
                Console.WriteLine("Mediane:                 " + mediane.ToString("n0"));
                Console.WriteLine("Mode:                    " + (maxValueCount > 1 ? mode.ToString("n0") + " with frequency " + maxValueCount.ToString("n0") : "none"));
            }
        }
        // use "Q" to free network resources and avoid side effects
        else if (keyPressed == ConsoleKey.Q)
        {
            if (timer != null)
                if (timer.Enabled)
                {
                    timer.Stop();
                    timer.Dispose();
                }

            udpClient?.Close();
            udpClient?.Dispose();

            Process.GetCurrentProcess().Kill();
        }
        Output();
    }

    private static EnumerableRowCollection<DataRow> UpdateTable(Int64 value)
    {
        // no use of "OrderBy" and "GroupBy" here because it would reduce performance for large data volumes;
        // row insertion at the right place and count increment seems more efficient
        // especially using LINQ Parallel to distribute search between two processor cores or more

        DataRow row;
        EnumerableRowCollection<DataRow> rows = dt.AsEnumerable();
        try
        {
            // value already exists in the table
            row = rows.AsParallel().Where(r => r.Field<Int64>("Value") == value).First(); 
            row["Count"] = (Int64)row["Count"] + 1; // increment keeps the table grouped by values
        }
        catch
        {
            // new value not found in the table
            row = dt.NewRow();
            row["Value"] = value;
            row["Count"] = 1;

            try
            {
                // insert before row with the first value which is >= than new one to keep the table ordered by values
                dt.Rows.InsertAt(row, dt.Rows.IndexOf(rows.AsParallel().AsOrdered().Where(r => r.Field<Int64>("Value") >= value).First()));
            }
            catch
            {
                // new row with the greatest value, should be last in the table
                dt.Rows.InsertAt(row, dt.Rows.Count);
            }
        }
        return rows;
    }

    private static void UpdateData(byte[] rawData)
    {
        // lock to guarantee sequential access to data in critical section while processing to avoid asynchrony side effects
        lock (locker) 
        {
            messagesCount++;

            byte[] halfMessage = new byte[halfBufferLength]; // 8 bytes

            Array.Copy(rawData, halfMessage, halfBufferLength);
            if (initMessageNumber == - 1)
                initMessageNumber = BitConverter.ToInt64(halfMessage, 0) - 1; // number of the first received message

            // lost messages count can be negative (!) if the packet received "too late"
            // (messages with greater numbers received earlier so total count of received messages is greater than current message number);
            // it's possible due to asynchronous nature of data sending, receiving and processing;
            // could be watched if random interval is small enough and random generation is fast;
            // because of this it's very rough estimate — "lost" packets can be received later and estimate will decrease in such case
            lostMessagesCount = BitConverter.ToInt64(halfMessage, 0) - initMessageNumber - messagesCount; // first 8 bytes are message number

            Array.Copy(rawData, halfBufferLength, halfMessage, 0, halfBufferLength);
            Int64 value = BitConverter.ToInt64(halfMessage, 0);                       // last 8 bytes are received value

            sum += value;
            average = (double)sum / messagesCount;

            double deviation = (double)value - average;
            deviationSum += deviation * deviation;

            EnumerableRowCollection<DataRow> rows = UpdateTable(value);

            // further using LINQ Parallel enables to distribute search between two processor cores or more

            // max frequency to define mode;
            // if it equals 1 then all values in the table are unique, no mode defined
            maxValueCount = (Int64)rows.AsParallel().Max(r => r["Count"]);
            if (maxValueCount > 1)
                mode = (Int64)rows.AsParallel().Where(r => r.Field<Int64>("Count") == maxValueCount).First()["Value"];

            int count = dt.Rows.Count;

            // mediane = "middle" value in ordered dataset
            mediane = count % 2 == 0 ?
                      ((double)dt.Rows[count / 2 - 1]["Value"] + (double)dt.Rows[count / 2]["Value"]) / 2.0 :
                      (Int64)dt.Rows[(count + 1) / 2 - 1]["Value"];
        }
    }

    public static bool GetSettings()
    {
        XmlDocument settings = new();
        try
        {
            settings.Load("Settings.xml");
            XmlElement? element = settings.DocumentElement;

            groupAddress = IPAddress.Parse(element.SelectSingleNode("GroupAddress").InnerText);
            port = int.Parse(element.SelectSingleNode("Port").InnerText);
            ttl = int.Parse(element.SelectSingleNode("TTL").InnerText);
            delayPeriodicity = int.Parse(element.SelectSingleNode("DelayPeriodicity").InnerText);
            delayDuration = int.Parse(element.SelectSingleNode("DelayDuration").InnerText);

            return true;
        }
        catch
        {
            return false;
        }
    }
}