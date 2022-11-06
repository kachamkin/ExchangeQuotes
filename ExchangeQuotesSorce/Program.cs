using System.Net;
using System.Net.Sockets;
using System.Xml;

if (!GetSettings())
{
    Console.WriteLine("Failed to read settings!");
    return;
}

if (maxValue <= minValue)
{
    Console.WriteLine("Invalid random numbers interval!");
    return;
}

using UdpClient udpClient = new();
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
byte[] buffer = new byte[bufferLength]; // buffer to send; first 8 bytes = messageNumber, last 8 bytes = random value

while (true)
{
    BitConverter.GetBytes(messageNumber).CopyTo(buffer, 0);
    BitConverter.GetBytes(random.NextInt64(minValue, maxValue)).CopyTo(buffer, halfBufferLength);
    try
    {
        await udpClient.SendAsync(buffer, bufferLength, endPoint); // await to synchronize message sending and sequential increment message number
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
    private static Int64 minValue, maxValue;

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

            return true;
        }
        catch
        {
            return false;
        }
    }
}