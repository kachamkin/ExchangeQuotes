using Microsoft.Win32;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Windows.Forms.DataVisualization.Charting;
using Charting = System.Windows.Forms.DataVisualization.Charting;

namespace Chart
{
    public partial class Quotes : Form
    {
        private int port;
        [Required, Range(1, 65535), Display(Name = "Port")]
        public int _port { get { return port; } set { port = value; } }

        private IPAddress groupAddress;
        [Required, Display(Name = "Group address")]
        public IPAddress _groupAddress { get { return groupAddress; } set { groupAddress = value; } }

        private int ttl;
        [Range(0, 100), Display(Name = "TTL")]
        public int _ttl { get { return ttl; } set { ttl = value; } }

        private int medianeInterval;
        [Required, Range(5, 1000000), Display(Name = "Mediane interval")]
        public int _medianeInterval { get { return medianeInterval; } set { medianeInterval = value; } }

        private int modeStep;
        [Required, Range(5, 1000000), Display(Name = "Mode step")]
        public int _modeStep { get { return modeStep; } set { modeStep = value; } }

        private Charting.Chart chart = new();

        private Stats? stats;

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
                key ??= Registry.CurrentUser.CreateSubKey("Software\\ExchangeQuotes");

                key.SetValue("GroupAddress", groupAddress.ToString());
                key.SetValue("Port", port.ToString());
                key.SetValue("TTL", ttl.ToString());
                key.SetValue("MedianeInterval", medianeInterval.ToString());
                key.SetValue("ModeStep", modeStep.ToString());

                key.Dispose();
            }
            catch { }
        }

        private void Quotes_FormClosing(object sender, FormClosingEventArgs e)
        {
           stats?.Stop();
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

        private void StartButton_Click(object sender, EventArgs e)
        {
            if (!CheckAll())
                return;

            StartButton.Enabled = false;
            StopButton.Enabled = true;

            bool firstTime = stats == null;

            if (firstTime)
            {
                stats = new(groupAddress, port, ttl, medianeInterval, modeStep);
                stats.OnIntervalElapsed += OnIntervalElapsed;
            }
            stats?.SetParams(groupAddress, port, ttl, medianeInterval, modeStep);
            stats?.Start();
        }

        private void OnIntervalElapsed(Dictionary<Int64, Int64> dt, StatData data)
        {
            Task.Run(() => UpdateText(data));
            Task.Run(() => UpdateChart(dt));
        }

        private void UpdateText(StatData data)
        {
            Result.Text = "\r\n  Total messages received: " + data.messagesCount.ToString("n0") +
                      "\r\n  Total messages \"lost\":   " + data.lostMessagesCount.ToString("n0") +
                      "\r\n  Average:                 " + data.average.ToString("n") +
                      "\r\n  Standard deviation:      " + Math.Sqrt(data.deviationSum / (data.messagesCount + 1)).ToString("n") +
                      "\r\n  Mediane:                 " + data.mediane.ToString("n") +
                      "\r\n  Mode:                    " + data.mode.ToString("n");
        }

        private void UpdateChart(Dictionary<Int64, Int64> dt)
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

            stats?.Stop();
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