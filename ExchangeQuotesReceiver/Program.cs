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

//dt.Columns.Add("Value", typeof(Int64));
//dt.Columns.Add("Count", typeof(Int64));

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
    private static Int64 maxValueCount = 0;
    private static Int64 initMessageNumber = - 1;
    private static int medianeInterval;
    private static int modeStep;
    private static List<Int64> interValues = new();

    private static readonly byte[] halfMessage = new byte[halfBufferLength]; 

    private static void Output()
    {
        ConsoleKey keyPressed = Console.ReadKey().Key;
        if (keyPressed == ConsoleKey.Enter)
        {
            Console.WriteLine("\nTotal messages received: " + messagesCount.ToString("n0"));
            Console.WriteLine("Total messages \"lost\":   " + lostMessagesCount.ToString("n0"));
            Console.WriteLine("Average:                 " + average.ToString("n"));
            Console.WriteLine("Standard deviation:      " + Math.Sqrt(deviationSum / (messagesCount + 1)).ToString("n"));
            Console.WriteLine("Mediane:                 " + mediane.ToString("n"));
            Console.WriteLine("Mode:                    " + mode.ToString("n"));
        }
        else if (keyPressed == ConsoleKey.Q)
        {
            udpClient?.Close();
            udpClient?.Dispose();

            Process.GetCurrentProcess().Kill();
        }
        Output();
    }

    private static double GetExactMediane()
    {
        interValues = interValues.AsParallel().OrderBy(x => x).ToList();

        int count = interValues.Count;

        return count % 2 == 0 ?
                      ((double)interValues[count / 2 - 1] + (double)interValues[count / 2]) / 2.0 :
                      interValues[(count + 1) / 2 - 1];
    }
    private static double? GetMode()
    {
        ParallelQuery<KeyValuePair<Int64, Int64>> rows = dt.AsParallel();
        if (dt.Count != 0)
            maxValueCount = rows.Max(r => r.Value);
        else
            return null;

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
        return null;
    }

    private static double GetExactMode()
    {
        dt.Clear();
        foreach (Int64 v in interValues)
            UpdateTable(((v % modeStep >= modeStep / 2 ? v + modeStep : v) / modeStep) * modeStep);
        
        double? exMode = GetMode();
        return exMode == null ? 0 : exMode.Value; 
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
        if (initMessageNumber == - 1)
            initMessageNumber = BitConverter.ToInt64(halfMessage, 0) - 1;
        lostMessagesCount = BitConverter.ToInt64(halfMessage, 0) - initMessageNumber - messagesCount;

        Array.Copy(rawData, halfBufferLength, halfMessage, 0, halfBufferLength);
        Int64 value = BitConverter.ToInt64(halfMessage, 0);
        interValues.Add(value);

        sum += value;
        average = (double)sum / messagesCount;

        double deviation = (double)value - average;
        deviationSum += deviation * deviation;

        if (messagesCount >= medianeInterval && messagesCount % medianeInterval == 0)
        {
            mediane = ((messagesCount - medianeInterval) * mediane + medianeInterval * GetExactMediane()) / messagesCount;
            mode = ((messagesCount - medianeInterval) * mode + medianeInterval * GetExactMode()) / messagesCount;
            interValues.Clear();
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