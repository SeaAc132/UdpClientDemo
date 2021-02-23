using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UdpClientDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public static int dataInterval = 1000;
        private List<string> dataToSend = new List<string>();
        public static long connectedTimestamp;
        private List<PacketReceived> packets = new List<PacketReceived>();
        private UdpClient udpClient = null;
        private bool connected = false;
        private static long milliTime()
        {
            return Stopwatch.GetTimestamp();
        }
        private static long nanoTime()
        {
            long nano = 10000L * milliTime();
            nano /= TimeSpan.TicksPerMillisecond;
            nano *= 100L;
            return nano;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            gridLog.DataSource = new BindingList<PacketReceived>(packets);
        }
        private void appendConsole(string message)
        {
            DateTime dt = DateTime.Now;
            string dtstring = dt.ToString("yyyy-MM-dd HH:mm:sss");
            txtConsole.AppendText(string.Format("[{0}] {1}\n", dtstring, message));
        }
        private void appendLogFile(long timestamp, string message)
        {
            PacketReceived packet = new PacketReceived
            {
                Timestamp = timestamp,
                Data = message
            };
            packets.Add(packet);
            gridLog.DataSource = new BindingList<PacketReceived>(packets);
        }
        private void appendPlainText(string message)
        {
            txtPlain.AppendText(message);
        }

        private void btnClearConnection_Click(object sender, EventArgs e)
        {
            txtAddress.Clear();
            txtSendingPort.Clear();
            txtListeningPort.Clear();
        }

        private void btnClearData_Click(object sender, EventArgs e)
        {
            txtSendData.Clear();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!connected)
            {
                string ip = txtAddress.Text;
                int listeningPort;
                int sendingPort;
                if (ip == "")
                {
                    MessageBox.Show("Please enter IP address");
                    return;
                }
                bool isListening = int.TryParse(txtListeningPort.Text, out listeningPort);
                bool isSending = int.TryParse(txtSendingPort.Text, out sendingPort);
                if (!isListening && !isSending)
                {
                    MessageBox.Show("Please enter port number");
                    return;
                }
                udpClient = new UdpClient(sendingPort);
                udpClient.Connect(ip, listeningPort);
                try
                {
                    udpClient.BeginReceive(DataReceived, null);
                    connectedTimestamp = nanoTime();
                    connected = true;
                    btnConnect.Text = "Disconnect";
                    txtAddress.Enabled = false;
                    txtSendingPort.Enabled = false;
                    txtListeningPort.Enabled = false;
                    appendConsole("Connecting to " + ip + " on port " + sendingPort);
                    appendConsole("Listening on port " + listeningPort);
                    labelStatus.Text = "Open";
                }
                catch (Exception ex)
                {
                    appendConsole(ex.ToString());
                }
            } else
            {
                try
                {
                    udpClient.Close();
                    connected = false;
                    btnConnect.Text = "Connect";
                    labelStatus.Text = "Closed";
                    txtAddress.Enabled = true;
                    txtSendingPort.Enabled = true;
                    txtListeningPort.Enabled = true;
                }
                catch (Exception ex)
                {
                    appendConsole(ex.ToString());
                }
            }
        }

        private void DataReceived(IAsyncResult ar)
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
            byte[] data;
            try
            {
                data = udpClient.EndReceive(ar, ref ip);
                if (data.Length == 0)
                {
                    return;
                }
                udpClient.BeginReceive(DataReceived, null);
            }
            catch (Exception)
            {
                return;
            }
            this.BeginInvoke((Action<IPEndPoint, string>)DataReceivedUI, ip, Encoding.UTF8.GetString(data));
        }

        private void DataReceivedUI(IPEndPoint endPoint, string data)
        {
            long timestamp = nanoTime();
            appendConsole(data);
            appendConsole("This message was sent from " + endPoint.Address.ToString() + " on port " + endPoint.Port.ToString());
            appendLogFile(timestamp, data);
            appendPlainText(data);
        }
        private void sendData(string data)
        {
            byte[] sendBytes = Encoding.ASCII.GetBytes(data);
            udpClient.Send(sendBytes, sendBytes.Length);
        }
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (connected)
            {
                string data = txtSendData.Text;
                if (data == "")
                {
                    MessageBox.Show("Please enter data to send");
                    return;
                }
                sendData(data);
                appendConsole("Sending data : " + data);
            }
            else
            {
                MessageBox.Show("Please make a connection first");
                return;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPage1)
            {
                MessageBox.Show("Please choose log file or plain text");
            }
            else if (tabControl1.SelectedTab == tabPage2)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = "LogFile";
                saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
                saveFileDialog.RestoreDirectory = true;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter writer = new StreamWriter(saveFileDialog.OpenFile()))
                    {
                        writer.WriteLine("Time,Timestamp,Data");
                        foreach (PacketReceived packet in packets)
                        {
                            writer.WriteLine(packet.ToString());
                        }
                    }
                }
            }
            else if (tabControl1.SelectedTab == tabPage3)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = "PlainText";
                saveFileDialog.Filter = "Plain Text Files (*.txt)|*.txt";
                saveFileDialog.RestoreDirectory = true;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter writer = new StreamWriter(saveFileDialog.OpenFile()))
                    {
                        writer.Write(txtPlain.Text);
                    }
                }
            }
        }

        private void btnClearTab_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPage1)
            {
                txtConsole.Clear();
            }
            else if (tabControl1.SelectedTab == tabPage2)
            {
                packets.Clear();
                gridLog.DataSource = new BindingList<PacketReceived>(packets);
            }
            else if (tabControl1.SelectedTab == tabPage3)
            {
                txtPlain.Clear();
            }
        }

        private void btnChooseFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Plain Text Files (*.txt)|*.txt";
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtReadFile.Text = openFileDialog.FileName;
            }
        }
        private void ThreadSendBuffer()
        {
            while (connected && dataToSend.Count > 0)
            {
                string data = dataToSend[0];
                sendData(data);
                dataToSend.RemoveAt(0);
                Thread.Sleep(dataInterval);
            }
        }
        private List<string> splitString(string data, int bufferSize)
        {
            List<string> list = new List<string>();
            char[] arr = data.ToCharArray();
            string buffer = "";
            for (int i = 0; i < arr.Length; i++)
            {
                if (i % bufferSize == 0)
                {
                    if (i != 0) list.Add(buffer);
                    buffer = "";
                }
                buffer += arr[i];
            }
            if (buffer != "")
            {
                list.Add(buffer);
            }
            return list;
        }
        private void btnSendFile_Click(object sender, EventArgs e)
        {
            double interval;
            int size;
            if (txtReadFile.Text == "")
            {
                MessageBox.Show("Please select file");
                return;
            }
            if (!double.TryParse(txtInterval.Text, out interval))
            {
                MessageBox.Show("Please enter interval");
                return;
            }
            if (!int.TryParse(txtSize.Text, out size))
            {
                MessageBox.Show("Please enter size");
                return;
            }
            if (connected)
            {
                string data;
                using (StreamReader reader = new StreamReader(txtReadFile.Text))
                {
                    data = reader.ReadToEnd();
                    data = data.Replace(Environment.NewLine, "");
                    data = data.Replace(" ", "");
                }
                dataInterval = (int) (interval * 1000);
                dataToSend = splitString(data, size);
                Thread thread = new Thread(ThreadSendBuffer);
                thread.Start();
            }
            else
            {
                MessageBox.Show("Please make a connection first");
                return;
            }
        }

        private void richTextBox_TextChanged(object sender, EventArgs e)
        {
            RichTextBox txt = (RichTextBox)sender;
            txt.SelectionStart = txt.Text.Length;
            txt.ScrollToCaret();
        }

        private void dataGridView_DataSourceChanged(object sender, EventArgs e)
        {
            DataGridView grid = (DataGridView)sender;
            if (grid.RowCount > 0)
            {
                grid.FirstDisplayedScrollingRowIndex = grid.RowCount - 1;
            }
        }

        private void btnClearFile_Click(object sender, EventArgs e)
        {
            txtReadFile.Text = "";
            txtInterval.Text = "";
            txtSize.Text = "";
        }
    }
}
