using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpClientDemo
{
    class PacketReceived
    {
        public long Timestamp { get; set; }
        public double Time
        {
            get
            {
                return (Timestamp - Form1.connectedTimestamp) / 1000000000.0;
            }
        }
        public string Data { get; set; }
        public override string ToString()
        {
            return Time + "," + Timestamp + "," + Data;
        }
    }
}
