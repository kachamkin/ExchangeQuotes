using System.Data;
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

UdpClient udpClient;
try
{
    udpClient = new(port);

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

// wait for the user's "Enter"
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
        // otherwise we should use lock on critical section "UpdateData" that would cause fast increase of memory usage that will make work of application impossible
        // (large queue of threads waiting for release of critical section)
        await Task.Run(async () => UpdateData((await udpClient.ReceiveAsync()).Buffer)); 
    }
    catch { };
}

partial class Program
{
    private const int halfBufferLength = 8;

    private static int port;
    private static IPAddress? groupAddress;
    private static int ttl; // multicast TTL

    private static int delayPeriodicity = 0;
    private static int delayDuration = 0;
    private static bool useDelay = false;
    private static System.Timers.Timer? timer;

    // stores received values and their frequencies;
    // always oredered and grouped by values;
    // increases permormance: smaller memory usage and faster data access — no need to store all large set of received data;
    // necessary only for mediane and mode calculation
    private static DataTable dt = new(); 

    private static Int64 messagesCount = 0; 
    private static Int64 lostMessagesCount = 0; 
    private static Int64 sum = 0;
    private static double average = 0;
    private static double deviationSum = 0;
    private static double mediane = 0;
    private static Int64 mode = 0;
    private static Int64 maxValueCount = 0;

    private static void Output()
    {
        if (Console.ReadKey().Key == ConsoleKey.Enter)
        {
            Console.WriteLine("\nTotal messages received: " + messagesCount);
            Console.WriteLine("Total messages lost:     " + lostMessagesCount);
            Console.WriteLine("Average:                 " + average);
            Console.WriteLine("Standard deviation:      " + Math.Sqrt(deviationSum / (messagesCount + 1)));
            Console.WriteLine("Mediane:                 " + mediane);
            Console.WriteLine("Mode:                    " + (maxValueCount > 1 ? mode + " with frequency " + maxValueCount : "none"));
        }
        Output();
    }

    private static EnumerableRowCollection<DataRow> UpdateTable(Int64 value)
    {
        // no use of "OrderBy" and "GroupBy" here because it would decrease performance for large data volumes;
        // row insertion at the right place and count increment seems more efficient

        DataRow row;
        EnumerableRowCollection<DataRow> rows = dt.AsEnumerable();
        try
        {
            // value already exists in the table
            row = rows.Where(r => r.Field<Int64>("Value") == value).First(); 
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
                dt.Rows.InsertAt(row, dt.Rows.IndexOf(rows.Where(r => r.Field<Int64>("Value") >= value).First()));
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
        messagesCount++;

        byte[] halfMessage = new byte[halfBufferLength]; // 8 bytes

        Array.Copy(rawData, halfMessage, halfBufferLength);
        lostMessagesCount = BitConverter.ToInt64(halfMessage, 0) - messagesCount; // first 8 bytes are message number

        Array.Copy(rawData, halfBufferLength, halfMessage, 0, halfBufferLength);
        Int64 value = BitConverter.ToInt64(halfMessage, 0);                       // last 8 bytes are received value

        sum += value;
        average = (double)sum / messagesCount;

        double deviation = (double)value - average;
        deviationSum += deviation * deviation;

        EnumerableRowCollection<DataRow> rows = UpdateTable(value);

        // max frequency to define mode;
        // if it equals 1 then all values in the table are unique, no mode defined
        maxValueCount = (Int64)rows.Max(r => r["Count"]); 
        if (maxValueCount > 1)                            
            mode = (Int64)rows.Where(r => r.Field<Int64>("Count") == maxValueCount).First()["Value"];

        int count = dt.Rows.Count;

        // mediane = "middle" value in ordered dataset
        mediane = count % 2 == 0 ? 
                  ((double)dt.Rows[count / 2 - 1]["Value"] + (double)dt.Rows[count / 2]["Value"]) / 2.0 :
                  (Int64)dt.Rows[(count + 1) / 2 - 1]["Value"];
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