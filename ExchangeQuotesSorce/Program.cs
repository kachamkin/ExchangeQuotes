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
        udpClient.SendAsync(buffer, bufferLength, endPoint);
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
        XmlDocument Settings = new();
        try
        {
            Settings.Load("Settings.xml");
            XmlElement? Element = Settings.DocumentElement;

            groupAddress = IPAddress.Parse(Element.SelectSingleNode("GroupAddress").InnerText);
            port = int.Parse(Element.SelectSingleNode("Port").InnerText);
            minValue = Int64.Parse(Element.SelectSingleNode("MinValue").InnerText);
            maxValue = Int64.Parse(Element.SelectSingleNode("MaxValue").InnerText);

            return true;
        }
        catch
        {
            return false;
        }
    }
}