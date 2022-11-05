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

dt.Columns.Add("Value", typeof(Int64));
dt.Columns.Add("Count", typeof(Int64));

UdpClient udpClient = new(port);
IPEndPoint remoteIP = new(IPAddress.Any, port);

if (delayPeriodicity > 0 && delayDuration > 0)
{
    timer = new(delayPeriodicity);
    timer.Elapsed += (sender, eventArgs) => useDelay = true;
    timer.Start();
}

Task.Run(() => Output());

while (true)
{
    if (useDelay)
    {
        useDelay = false;
        Thread.Sleep(delayDuration);
    }
    try
    {
        await Task.Run(async () => FillData((await udpClient.ReceiveAsync()).Buffer));
    }
    catch { };
}

partial class Program
{
    private const int halfBufferLength = 8;

    private static int port;

    private static int delayPeriodicity = 0;
    private static int delayDuration = 0;
    private static bool useDelay = false;
    private static System.Timers.Timer? timer;

    private static DataTable dt = new();

    private static Int64 messagesCount = 0; 
    private static Int64 lostMessagesCount = 0; 
    private static Int64 sum = 0;
    private static double average = 0;
    private static double deviationSum = 0;
    private static Int64 mediana = 0;
    private static Int64 moda = 0;

    private static object locker = new();

    private static void Output()
    {
        if (Console.ReadKey().Key == ConsoleKey.Enter)
        {
            Console.WriteLine("\nTotal messages received: " + messagesCount);
            Console.WriteLine("Total messages lost: " + lostMessagesCount);
            Console.WriteLine("Average: " + average);
            Console.WriteLine("Standard deviation: " + Math.Sqrt(deviationSum / (messagesCount + 1)));
            Console.WriteLine("Mediana: " + mediana);
            Console.WriteLine("Moda: " + moda);
        }
        Output();
    }
    
    private static void FillData(byte[] rawData)
    {
        messagesCount++;

        byte[] halfMessage = new byte[halfBufferLength];

        Array.Copy(rawData, halfMessage, halfBufferLength);
        lostMessagesCount = BitConverter.ToInt64(halfMessage, 0) - messagesCount;

        Array.Copy(rawData, halfBufferLength, halfMessage, 0, halfBufferLength);
        Int64 value = BitConverter.ToInt64(halfMessage, 0);

        sum += value;
        average = (double)sum / (double)messagesCount;

        double deviation = (double)value - average;
        deviationSum += deviation * deviation;

        DataRow row;
        EnumerableRowCollection<DataRow> rows = dt.AsEnumerable();
        try
        {
            row = rows.Where(r => r.Field<Int64>("Value") == value).First();
            row["Count"] = (Int64)row["Count"] + 1;
        }
        catch
        {
            row = dt.NewRow();
            row["Value"] = value;
            row["Count"] = 1;
            try
            {
                dt.Rows.InsertAt(row, dt.Rows.IndexOf(rows.Where(r => r.Field<Int64>("Value") >= value).First()));
            }
            catch
            {
                dt.Rows.InsertAt(row, dt.Rows.Count);
            }
        }

        int count = dt.Rows.Count;
        if (count % 2 == 0)
            mediana = ((Int64)dt.Rows[count / 2 - 1]["Value"] + (Int64)dt.Rows[count / 2]["Value"]) / 2;
        else
            mediana = (Int64)dt.Rows[(count + 1) / 2 - 1]["Value"];
        moda = (Int64)rows.Where(r => r.Field<Int64>("Count") == (Int64)rows.Max(r => r["Count"])).First()["Value"];
    }

    public static bool GetSettings()
    {
        XmlDocument Settings = new();
        try
        {
            Settings.Load("Settings.xml");
            XmlElement? Element = Settings.DocumentElement;

            port = int.Parse(Element.SelectSingleNode("Port").InnerText);
            delayPeriodicity = int.Parse(Element.SelectSingleNode("DelayPeriodicity").InnerText);
            delayDuration = int.Parse(Element.SelectSingleNode("DelayDuration").InnerText);

            return true;
        }
        catch
        {
            return false;
        }
    }
}