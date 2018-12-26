using System;
using System.Collections.Generic;
using System.Text;

namespace MMSLibrary
{
    public class DatagramItem
    {
        public string BatchId { get; set; }
        public int ID { get; set; }
        public string Payload { get; set; }
    }

    public class Datagram
    {
        public List<DatagramItem> Items { get; set; }
    }
}
