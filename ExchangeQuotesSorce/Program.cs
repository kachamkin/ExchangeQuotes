using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Xml;

if (!GetSettings())
{
    Console.WriteLine("Failed to read settings!");
    return;
}

if (!gaussian && maxValue <= minValue || gaussian && maxValue <= 0)
{
    Console.WriteLine("Invalid random numbers interval!");
    return;
}

IPEndPoint endPoint;
try
{
    endPoint = new(groupAddress, port);
}
catch
{
    Console.WriteLine("Invalid IP address or port values!");
    return;
}

Int64 messageNumber = 1;
Random random = new();
byte[] buffer = new byte[bufferLength]; 

Console.WriteLine("\nPlease use \"Q\" to exit, correctly free network resources and avoid side effects\n");

// wait for the user's "Q"
// use "Q" to free network resources and avoid side effects
Task.Run(() => Output());

while (true)
{
    BitConverter.GetBytes(messageNumber).CopyTo(buffer, 0);
    try
    {
        if (gaussian)
            BitConverter.GetBytes((Int64)Math.Round(GetGaussian(random, minValue, maxValue), 1, MidpointRounding.ToEven)).CopyTo(buffer, halfBufferLength);
        else
            BitConverter.GetBytes(random.NextInt64(minValue, maxValue)).CopyTo(buffer, halfBufferLength);
        await udpClient.SendAsync(buffer, bufferLength, endPoint);
        messageNumber++;
    }
    catch { };
}

partial class Program
{
    private const int halfBufferLength = 8;
    private const int bufferLength = 16;

    private static IPAddress? groupAddress;
    private static int port;
    private static readonly UdpClient udpClient = new();

    private static Int64 minValue, maxValue;
    private static bool gaussian;

    private static double GetGaussian(Random rand, double mean, double stdDev)
    {
        return mean + stdDev * Math.Sqrt(-2.0 * Math.Log(1.0 - rand.NextDouble())) * Math.Sin(2.0 * Math.PI * (1.0 - rand.NextDouble()));
    }

    // use "Q" to free network resources and avoid side effects
    private static void Output()
    {
        if (Console.ReadKey().Key == ConsoleKey.Q)
        {
            udpClient?.Close();
            udpClient?.Dispose();
            Process.GetCurrentProcess().Kill();
        }
        Output();
    }

    private static bool GetSettings()
    {
        XmlDocument settings = new();
        try
        {
            settings.Load("Settings.xml");
            XmlElement? element = settings.DocumentElement;

            groupAddress = IPAddress.Parse(element.SelectSingleNode("GroupAddress").InnerText);
            port = int.Parse(element.SelectSingleNode("Port").InnerText);
            minValue = Int64.Parse(element.SelectSingleNode("MinValue").InnerText);
            maxValue = Int64.Parse(element.SelectSingleNode("MaxValue").InnerText);
            gaussian = element.SelectSingleNode("DistributionType").InnerText == "Gaussian";

            return true;
        }
        catch
        {
            return false;
        }
    }
}