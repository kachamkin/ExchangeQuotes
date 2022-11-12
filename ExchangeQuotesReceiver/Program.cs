using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Xml;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (!GetSettings())
{
    Console.WriteLine("Failed to read settings!");
    return;
}

if (medianeInterval <= 0 || modeStep <= 0 )
{
    Console.WriteLine("Invalid parameters for mediane / mode calculation!");
    return;
}

if (delayPeriodicity < 0)
{
    Console.WriteLine("Invalid delay parameters!");
    return;
}

try
{
    udpClient = new(port);
    udpClient.Client.ReceiveBufferSize = receiveBufferSize; 
    udpClient.JoinMulticastGroup(groupAddress, ttl); 
}
catch
{
    Console.WriteLine("Invalid IP address or port values!");
    return;
}

Console.WriteLine("\nPlease use \"Q\" to exit, correctly free network resources and avoid side effects\n");
Task.Run(() => Output());

while (true)
{
    try
    {
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
    private static int ttl; 
    private static UdpClient? udpClient;

    private static int delayPeriodicity = 0;

    private static readonly SortedDictionary<Int64, Int64> dt = new();

    private static Int64 messagesCount = 0; 
    private static Int64 lostMessagesCount = 0; 
    private static Int64 sum = 0;
    private static double average = 0;
    private static double deviationSum = 0;
    private static double mediane = 0;
    private static double mode;
    private static Int64 initMessageNumber = - 1;
    private static int medianeInterval;
    private static int modeStep;

    private static bool drawChart = false;

    private static readonly byte[] halfMessage = new byte[halfBufferLength]; 

    private static void Output()
    {
        ConsoleKey keyPressed = Console.ReadKey(true).Key;
        if (keyPressed == ConsoleKey.Enter)
        {
            Console.WriteLine("\nTotal messages received: " + messagesCount.ToString("n0"));
            Console.WriteLine("Total messages \"lost\":   " + lostMessagesCount.ToString("n0"));
            Console.WriteLine("Average:                 " + average.ToString("n"));
            Console.WriteLine("Standard deviation:      " + Math.Sqrt(deviationSum / (messagesCount + 1)).ToString("n"));
            Console.WriteLine("Mediane:                 " + mediane.ToString("n"));
            Console.WriteLine("Mode:                    " + mode.ToString("n"));
        }
        else if (keyPressed == ConsoleKey.P)
            drawChart = true;
        else if (keyPressed == ConsoleKey.Q)
        {
            udpClient?.Close();
            udpClient?.Dispose();

            Process.GetCurrentProcess().Kill();
        }
        Output();
    }

    public static void Print()
    {
        drawChart = false;

        double max = Console.BufferWidth / (dt.Max(x => x.Value) + 24.0);
        Console.Write("\n");
        foreach (KeyValuePair<Int64, Int64> item in dt)
        {
            Console.Write(item.Key + " ");
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            for (double i = 0; i <= item.Value * max; i++)
                Console.Write("●");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write("\n");
        }
    }

    private static double GetMediane()
    {
        ParallelQuery<KeyValuePair<Int64, Int64>> rows = dt.AsParallel();

        Int64 sumF = rows.Sum(r => r.Value);

        ParallelQuery<KeyValuePair<Int64, Int64>> ordered = rows.AsOrdered();

        double s = 0;
        KeyValuePair<Int64, Int64> row = ordered.SkipWhile(r => { s += r.Value; return s < sumF / 2.0; }).First();
        Int64 fm0 = row.Value;
        Int64 key = row.Key;

        Int64 fm0_1 = 0;
        ParallelQuery<KeyValuePair<Int64, Int64>> pq = ordered.Where(r => r.Key < key);
        if (pq.Count() != 0)
            fm0_1 = pq.Last().Value;

        return key - 0.5 * modeStep + modeStep * (0.5 * sumF - fm0_1) / fm0;
    }

    private static double GetMode()
    {
        ParallelQuery<KeyValuePair<Int64, Int64>> rows = dt.AsParallel();
        Int64 maxValueCount = rows.Max(r => r.Value);

        if (maxValueCount > 1)
        {
            KeyValuePair<Int64, Int64> row = rows.Where(r => r.Value == maxValueCount).First();
            Int64 fm0 = row.Value;
            Int64 key = row.Key;

            ParallelQuery<KeyValuePair<Int64, Int64>> ordered = rows.AsOrdered();

            Int64 fm0_1 = 0;
            ParallelQuery<KeyValuePair<Int64, Int64>> pq = ordered.Where(r => r.Key < key);
            if (pq.Count() != 0)
                fm0_1 = pq.Last().Value; 

            Int64 fm01 = 0;
            pq = ordered.Where(r => r.Key > key);
            if (pq.Count() != 0)
                fm01 = pq.First().Value;

            return key - 0.5 * modeStep + modeStep * (fm0 - fm0_1) / (2.0 * fm0 - fm0_1 - fm01);
        }
        return 0;
    }

    private static void UpdateTable(Int64 value)
    {
        if (dt.ContainsKey(value))
            dt[value]++;
        else
            dt.Add(value, 1);
    }

    private static void UpdateData(byte[] rawData)
    {
        messagesCount++;

        Array.Copy(rawData, halfMessage, halfBufferLength);

        Int64 num = BitConverter.ToInt64(halfMessage, 0);
        if (initMessageNumber == - 1)
            initMessageNumber = num - 1;
        lostMessagesCount = num - initMessageNumber - messagesCount;

        Array.Copy(rawData, halfBufferLength, halfMessage, 0, halfBufferLength);
        Int64 value = BitConverter.ToInt64(halfMessage, 0);

        sum += value;
        average = (double)sum / messagesCount;

        double deviation = (double)value - average;
        deviationSum += deviation * deviation;

        UpdateTable(((value % modeStep >= modeStep / 2 ? value + modeStep : value) / modeStep) * modeStep);

        if (messagesCount >= medianeInterval && messagesCount % medianeInterval == 0)
        {
            Int64 diff = messagesCount - medianeInterval;
            mediane = (diff * mediane + medianeInterval * GetMediane()) / messagesCount;
            mode = (diff * mode + medianeInterval * GetMode()) / messagesCount;
            if (drawChart)
                Print();
            dt.Clear();
        }
    }

    public static bool GetSettings()
    {
        XmlDocument settings = new();
        try
        {
            settings.Load("Settings.xml");
            XmlElement? element = settings.DocumentElement;
            if (element == null)
                return false;

            groupAddress = IPAddress.Parse(element.SelectSingleNode("GroupAddress").InnerText);
            port = int.Parse(element.SelectSingleNode("Port").InnerText);
            ttl = int.Parse(element.SelectSingleNode("TTL").InnerText);
            delayPeriodicity = int.Parse(element.SelectSingleNode("DelayPeriodicity").InnerText);
            medianeInterval = int.Parse(element.SelectSingleNode("MedianeInterval").InnerText);
            modeStep = int.Parse(element.SelectSingleNode("ModeStep").InnerText);

            return true;
        }
        catch
        {
            return false;
        }
    }
}