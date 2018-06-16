using System;
using System.Net;
using System.Text;
using System.Net.Sockets;


namespace SNMP_Event_Sender
{
    class Program
    {

        public static void Main(string[] args)
        {
            Console.WriteLine("Enter Target IP Address of SNMP Receiver to be tested");
            string IPAddr = Console.ReadLine();

            begin1:

                //send the event data over udp to pre-specified message receiver at ipadd.parse address and port below
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
                ProtocolType.Udp);
                IPAddress serverAddr = IPAddress.Parse(IPAddr);
                IPEndPoint endPoint = new IPEndPoint(serverAddr, 162);
                string messedge = "SENDING TEST DATA TO RECEIVER AT IP: " + IPAddr;
                byte[] send_buffer = Encoding.ASCII.GetBytes(messedge);
                sock.SendTo(send_buffer, endPoint);
                sock.Close();

                Console.WriteLine(DateTime.Now + " " + messedge);
                Console.WriteLine("Press Enter for another test sequence...");
                Console.ReadLine();    
                goto begin1;
            
        }
    }
}

