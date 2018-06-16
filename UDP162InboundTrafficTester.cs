using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Web;
using System.IO;
using System.Diagnostics;

namespace OpenITTools_SNMP_Listener_Dry_Tester
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                Process.Start("powershell set-service snmp -startuptype disabled");
                Process.Start("powershell spsv snmp -force");
                Process.Start("powershell spps snmp -force");
            }
            catch
            {
                Console.WriteLine("-----SNMP Kill Failed-----");
            }

            System.Threading.Thread.Sleep(2000);

            UdpClient listener;
            int port = 162;
            IPEndPoint groupEP;
            byte[] packet = new byte[1024];
            listener = new UdpClient(port);
            groupEP = new IPEndPoint(IPAddress.Any, port);

            while (true)
            {

                Console.WriteLine("Waiting for messages....");
                packet = listener.Receive(ref groupEP);
                Console.WriteLine("Processing new message...");
                if (packet.Length != 0)
                {

                    string output2 = Encoding.ASCII.GetString(packet);
                    string SNMPEP = groupEP.ToString();
                    string SNMPPK = packet + " - " + DateTime.Now;
                    string SNMPSubject = "SNMP Alert From " + SNMPEP + " " + SNMPPK;
                    string SNMPBody = "SNMP Alert From " + SNMPEP + " " + SNMPPK + " - " + output2;
                    string SNMPshortMsg = output2;
                    

                    if (packet[0] == 0xff)
                    {
                        Console.WriteLine(DateTime.Now + " - Invalid Packet");

                      
                    } 

                    Console.WriteLine("RECEIVED OK!" + packet + " - " + DateTime.Now);

                }
            }
        }


    }
}
