using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Diagnostics;

namespace Chart
{
    public struct StatData
    {
        public Int64 messagesCount;
        public Int64 lostMessagesCount;
        public double average;
        public double deviationSum;
        public double mediane;
        public double mode;
    }

    internal class Stats
    {
        private const int halfBufferLength = 8;
        private const int receiveBufferSize = 10485760;

        private readonly int port;
        private readonly IPAddress groupAddress;

        private readonly int ttl;

        private UdpClient? udpClient;

        private readonly SortedDictionary<Int64, Int64> dt = new();

        private Int64 messagesCount = 0;
        private Int64 lostMessagesCount = 0;
        private Int64 sum = 0;
        private double average = 0;
        private double deviationSum = 0;
        private double mediane = 0;
        private double mode;
        private Int64 initMessageNumber = -1;

        private readonly int medianeInterval;
        private readonly int modeStep;

        bool stopListen = false;
        private static readonly byte[] halfMessage = new byte[halfBufferLength];

        public delegate void IntervalElapsed(Dictionary<Int64, Int64> pairs, StatData data);
        public event IntervalElapsed? OnIntervalElapsed;

        public Stats(IPAddress _groupAddress, int _port, int _ttl, int _medianeInterval, int _modeStep)
        {
            groupAddress = _groupAddress;
            port = _port;
            ttl = _ttl;
            medianeInterval = _medianeInterval;
            modeStep = _modeStep;
        }

        private double GetMediane()
        {
            ParallelQuery<KeyValuePair<Int64, Int64>> rows = dt.AsParallel();
            Int64 sumF = rows.Sum(r => r.Value);

            Int64 s = 0;
            KeyValuePair<Int64, Int64> row = dt.SkipWhile(r => { s += r.Value; return s < sumF / 2; }).First();

            Int64 fm0_1 = 0;
            ParallelQuery<KeyValuePair<Int64, Int64>> pq = rows.AsOrdered().Where(r => r.Key < row.Key);
            if (pq.Any())
                fm0_1 = pq.Last().Value;

            return row.Key - 0.5 * modeStep + modeStep * (0.5 * sumF - fm0_1) / row.Value;
        }

        private double GetMode()
        {
            ParallelQuery<KeyValuePair<Int64, Int64>> rows = dt.AsParallel();
            Int64 maxValueCount = rows.Max(r => r.Value);

            if (maxValueCount > 1)
            {
                KeyValuePair<Int64, Int64> row = rows.Where(r => r.Value == maxValueCount).First();
                ParallelQuery<KeyValuePair<Int64, Int64>> ordered = rows.AsOrdered();

                Int64 fm0_1 = 0;
                ParallelQuery<KeyValuePair<Int64, Int64>> pq = ordered.Where(r => r.Key < row.Key);
                if (pq.Any())
                    fm0_1 = pq.Last().Value;

                Int64 fm01 = 0;
                pq = ordered.Where(r => r.Key > row.Key);
                if (pq.Any())
                    fm01 = pq.First().Value;

                return row.Key - 0.5 * modeStep + modeStep * (row.Value - fm0_1) / (2.0 * row.Value - fm0_1 - fm01);
            }
            return 0;
        }

        private void UpdateTable(Int64 value)
        {
            if (dt.ContainsKey(value))
                dt[value]++;
            else
                dt.Add(value, 1);
        }

        private void UpdateData(byte[] rawData)
        {
            messagesCount++;

            Array.Copy(rawData, halfMessage, halfBufferLength);

            Int64 num = BitConverter.ToInt64(halfMessage, 0);
            if (initMessageNumber == -1)
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
                RaiseEvent();
                dt.Clear();
            }
        }

        private void RaiseEvent()
        {
            Dictionary<Int64, Int64> pairs = new(dt.Count);
            dt.AsParallel().ForAll(item => pairs.Add(item.Key, item.Value));
            StatData data = new() { average = this.average, deviationSum = this.deviationSum, lostMessagesCount = this.lostMessagesCount, mediane = this.mediane, messagesCount = this.messagesCount, mode = this.mode  };
            OnIntervalElapsed?.Invoke(pairs, data);
        }

        public async void Start()
        {
            try
            {
                udpClient = new(port);
                udpClient.Client.ReceiveBufferSize = receiveBufferSize;
                udpClient.JoinMulticastGroup(groupAddress, ttl);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Quotes", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            while (true)
            {
                if (stopListen)
                {
                    stopListen = false;
                    break;
                }
                try
                {
                    await Task.Run(async () => UpdateData((await udpClient.ReceiveAsync()).Buffer));
                }
                catch { };
            }
        }

        public void Stop()
        {
            stopListen = true;
            udpClient?.Close();
            udpClient?.Dispose();
        }
    }
}
