using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms.DataVisualization.Charting;
using Charting = System.Windows.Forms.DataVisualization.Charting;

namespace Chart
{
    public partial class Quotes : Form
    {
        private const int halfBufferLength = 8;
        private const int receiveBufferSize = 10485760;

        private int port;
        [Required, Range(1, 65535), Display(Name = "Port")]
        public int _port { get { return port; } set { port = value; } }
        private IPAddress groupAddress;
        [Required, Display(Name = "Group address")]
        public IPAddress _groupAddress { get { return groupAddress; } set { groupAddress = value; } }

        private int ttl;
        [Range(0, 100), Display(Name = "TTL")]
        public int _ttl { get { return ttl; } set { ttl = value; } }

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

        private int medianeInterval;
        [Required, Range(5, 1000000), Display(Name = "Mediane interval")]
        public int _medianeInterval { get { return medianeInterval; } set { medianeInterval = value; } }
        private int modeStep;
        [Required, Range(5, 1000000), Display(Name = "Mode step")]
        public int _modeStep { get { return modeStep; } set { modeStep = value; } }

        bool stopListen = false;
        private static readonly byte[] halfMessage = new byte[halfBufferLength];

        private Charting.Chart chart = new();

        public Quotes()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;

            groupAddress = IPAddress.Any;
            ReadFromRegistry();

            PortBox.DataBindings.Add("Text", this, "_port");
            TtlBox.DataBindings.Add("Text", this, "_ttl");
            MedianeIntervalBox.DataBindings.Add("Text", this, "_medianeInterval");
            ModeStepBox.DataBindings.Add("Text", this, "_modeStep");
            GroupAddressBox.DataBindings.Add("Text", this, "_groupAddress");

            chart.ChartAreas.Add(new ChartArea());
            chart.Location = new System.Drawing.Point(10, 10);
            chart.Size = new System.Drawing.Size(500, 400);
            chart.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            Controls.Add(chart);
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

        private void ReadFromRegistry()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey("Software\\ExchangeQuotes");
            if (key != null)
            {
                try
                {
                    groupAddress = IPAddress.Parse((string)key.GetValue("GroupAddress"));
                    port = int.Parse((string)key.GetValue("Port"));
                    ttl = int.Parse((string)key.GetValue("TTL"));
                    medianeInterval = int.Parse((string)key.GetValue("MedianeInterval"));
                    modeStep = int.Parse((string)key.GetValue("ModeStep"));
                }
                catch { }
            }
        }

        private void WriteToRegistry()
        {
            RegistryKey? key = Registry.CurrentUser.OpenSubKey("Software\\ExchangeQuotes", true);
            try
            {
                if (key == null)
                    key = Registry.CurrentUser.CreateSubKey("Software\\ExchangeQuotes");
                key.SetValue("GroupAddress", groupAddress.ToString());
                key.SetValue("Port", port.ToString());
                key.SetValue("TTL", ttl.ToString());
                key.SetValue("MedianeInterval", medianeInterval.ToString());
                key.SetValue("ModeStep", modeStep.ToString());
                key.Dispose();
            }
            catch { }
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
                UpdateChart();
                dt.Clear();
                Task.Run(() => UpdateText());
            }
        }

        private void Quotes_FormClosing(object sender, FormClosingEventArgs e)
        {
            stopListen = true;
            udpClient?.Close();
            udpClient?.Dispose();

            WriteToRegistry();
        }

        private void GroupAddressBox_Leave(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(GroupAddressBox.Text))
            {
                if (!IPAddress.TryParse(GroupAddressBox.Text, out groupAddress))
                     GroupAddressBox.Text = groupAddress?.ToString();
            }
            else
                groupAddress = IPAddress.Any;
        }

        private void PortBox_Leave(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(PortBox.Text))
            {
                if (!int.TryParse(PortBox.Text, out port))
                    PortBox.Text = port.ToString();
            }
            else
                port = 0;
        }

        private void TtlBox_Leave(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TtlBox.Text))
            {
                if (!int.TryParse(TtlBox.Text, out ttl))
                    TtlBox.Text = ttl.ToString();
            }
            else
                ttl = 0;
        }

        private void MedianeIntervalBox_Leave(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(MedianeIntervalBox.Text))
            {
                if (!int.TryParse(MedianeIntervalBox.Text, out medianeInterval))
                    MedianeIntervalBox.Text = medianeInterval.ToString();
            }
            else
                medianeInterval = 0;
        }

        private void ModeStepBox_Leave(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(ModeStepBox.Text))
            {
                if (!int.TryParse(ModeStepBox.Text, out modeStep))
                    ModeStepBox.Text = modeStep.ToString();
            }
            else
                modeStep = 0;
        }

        private bool CheckAll()
        {
            string valResult = "";
            List<ValidationResult> results = new();
            if (!Validator.TryValidateObject(this, new ValidationContext(this), results, true))
            {
                foreach (ValidationResult res in results)
                    valResult += res.ErrorMessage + "\r\n";
                MessageBox.Show(valResult, "Quotes", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;    
        }

        private async void StartButton_Click(object sender, EventArgs e)
        {
            if (!CheckAll())
                return;
            
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

            StartButton.Enabled = false;
            StopButton.Enabled = true;

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

        private void UpdateText()
        {
            Result.Text = "\r\n  Total messages received: " + messagesCount.ToString("n0") +
                      "\r\n  Total messages \"lost\":   " + lostMessagesCount.ToString("n0") +
                      "\r\n  Average:                 " + average.ToString("n") +
                      "\r\n  Standard deviation:      " + Math.Sqrt(deviationSum / (messagesCount + 1)).ToString("n") +
                      "\r\n  Mediane:                 " + mediane.ToString("n") +
                      "\r\n  Mode:                    " + mode.ToString("n");
        }

        private void UpdateChart()
        {
            chart.Series.Clear();
            Series series = new();
            foreach (KeyValuePair<Int64, Int64> item in dt)
                series.Points.Add(new DataPoint(item.Key, item.Value));
            chart.Series.Add(series);
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            StartButton.Enabled = true;
            StopButton.Enabled = false;

            stopListen = true;
            udpClient?.Close();
            udpClient?.Dispose();
        }

        private void GroupAddressBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                PortBox.Focus();    
        }

        private void PortBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                TtlBox.Focus();
        }

        private void TtlBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                MedianeIntervalBox.Focus();
        }

        private void MedianeIntervalBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                ModeStepBox.Focus();
        }

        private void ModeStepBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                chart.Focus();
        }
    }
}