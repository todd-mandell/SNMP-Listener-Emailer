using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Security;
using System.Net.NetworkInformation;

namespace OpenITTools_SNMP_Listener_Emailer
{
    class Program
    {

        static void Main(string[] args)
        {

            try
            {    //only need this part if your chosen port is udp 162 due to conflict crash
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

            //way down at bottom sets i back to zero, will this work?
            while (true)
            {

                //begin the failover beta stuff
                bool pingable = false;
                Ping pinger = null;
                string FileName = "c:\\FAILOVER-iplist.txt"; //FIRST LINE IS MY LOCAL IP
                string FerstLineIP = File.ReadLines(FileName).Skip(0).Take(1).First();
                //ip list enumeration
                int TotalLines = File.ReadAllLines(FileName).Count();

                for (int i = 1; i < TotalLines; i++)
                {

                    string CurrantLineIP = File.ReadLines(FileName).Skip(i).Take(1).First();
                    string NextLineIP;

                    try
                    {
                        NextLineIP = File.ReadLines(FileName).Skip(i + 1).Take(1).First();
                    }
                    catch
                    {
                        NextLineIP = File.ReadLines(FileName).Skip(i).Take(1).First();
                    }

                    pingable = false;
                    pinger = null;
                    pinger = new Ping();
                    try
                    {
                        PingReply reply = pinger.Send(CurrantLineIP);
                        pingable = reply.Status == IPStatus.Success;
                    }
                    catch
                    {

                    }
                    while (pingable == false && NextLineIP == FerstLineIP)
                    {
                        Console.WriteLine("Node active and Waiting for messages....");
                        packet = listener.Receive(ref groupEP);
                        Console.WriteLine("Processing new message...");
                        if (packet.Length != 0)
                        {
                            string output2 = Encoding.ASCII.GetString(packet);
                            string SNMPEP = groupEP.ToString();
                            string SNMPPK = packet + " - " + DateTime.Now;
                            string SNMPSubject = "Alert From " + SNMPEP + " " + SNMPPK;
                            string SNMPBody = "Alert From " + SNMPEP + " " + SNMPPK + " - " + output2 + "\n ----FROM NODE " + FerstLineIP;
                            string SNMPshortMsg = output2;
                            string path = @"c:\alert-log.txt"; // Users\" + Environment.UserName + "\\alert-log.txt";
                            string bufferPath = @"c:\sendBuffer.txt";

                            // if invalid packet found do this first
                            if (packet[0] == 0xff)
                            {
                                Console.WriteLine(DateTime.Now + " - Invalid Packet");

                                //try to mail the invalid packets as well here
                                try
                                {
                                    string smtpFile = "c:\\SMTPINFO.txt";
                                    //SecureString gave some difficulties here would like to implement that though
                                    //SecureString ss = new NetworkCredential("", File.ReadLines(smtpFile).Skip(0).Take(1).First());
                                    string SMTPPORT = File.ReadLines(smtpFile).Skip(0).Take(1).First();
                                    string SMTPHOSTURL = File.ReadLines(smtpFile).Skip(1).Take(1).First();
                                    string USERNAME = File.ReadLines(smtpFile).Skip(2).Take(1).First();
                                    string PASSWORD = File.ReadLines(smtpFile).Skip(3).Take(1).First();
                                    string FROMADDRESS = File.ReadLines(smtpFile).Skip(4).Take(1).First();
                                    string TOADDRESS = File.ReadLines(smtpFile).Skip(5).Take(1).First();

                                    // SMTP Stuff Begin for invalid packet
                                    SmtpClient invalidPacket = new SmtpClient();
                                    invalidPacket.Port = Int32.Parse(SMTPPORT);
                                    invalidPacket.Host = SMTPHOSTURL;
                                    invalidPacket.EnableSsl = true;
                                    invalidPacket.Timeout = 10000;
                                    invalidPacket.DeliveryMethod = SmtpDeliveryMethod.Network;
                                    invalidPacket.UseDefaultCredentials = false;
                                    invalidPacket.Credentials = new System.Net.NetworkCredential(USERNAME, PASSWORD);

                                    MailMessage mm = new MailMessage(FROMADDRESS, TOADDRESS, DateTime.Now + " - INVALID PACKET", DateTime.Now + " - SNMP INVALID PACKET");
                                    mm.BodyEncoding = UTF8Encoding.UTF8;
                                    mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
                                    mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess;
                                    mm.DeliveryNotificationOptions = DeliveryNotificationOptions.Delay;

                                    invalidPacket.Send(mm);

                                    //log it
                                    try
                                    {
                                        // This text is added only once to the file if it doesnt exist.
                                        if (!File.Exists(path))
                                        {
                                            // Create a file to write to.
                                            using (StreamWriter sw = File.CreateText(path))
                                            {
                                                sw.WriteLine("alert-log.txt Log File Begin");
                                                sw.WriteLine("----------------------------------------------------------------------");
                                            }
                                        }

                                        // This text is always appended for a successful SNMP Email
                                        using (StreamWriter sw = File.AppendText(path))
                                        {
                                            sw.WriteLine(DateTime.Now + " ----- INVALID PACKET");
                                            sw.WriteLine("----------------------------------------------------------------------");
                                        }
                                    }
                                    catch
                                    {
                                        Console.WriteLine("Log File Failure for Invalid Packet");
                                        // create and log to the buffer file begin
                                        if (!File.Exists(bufferPath))
                                        {
                                            // Create a file to write to.
                                            using (StreamWriter sw = File.CreateText(bufferPath))
                                            {
                                                sw.WriteLine(DateTime.Now + " - INVALID PACKET");
                                                sw.WriteLine("----------------------------------------------------------------------");
                                            }
                                        }

                                        // the append outside of the if to create.
                                        using (StreamWriter sw = File.AppendText(bufferPath))
                                        {
                                            sw.WriteLine(DateTime.Now + " - INVALID PACKET");
                                            sw.WriteLine("----------------------------------------------------------------------");
                                        }

                                    }
                                    return;
                                }
                                //the invalid packet send's catch
                                catch
                                {
                                    Console.WriteLine("Email Notifier Failure for Invalid Packet");

                                    // create and log to the buffer file begin
                                    if (!File.Exists(bufferPath))
                                    {
                                        // Create a file to write to.
                                        using (StreamWriter sw = File.CreateText(bufferPath))
                                        {
                                            sw.WriteLine(DateTime.Now + " - INVALID PACKET");
                                            sw.WriteLine("----------------------------------------------------------------------");
                                        }
                                    }

                                    // the append outside of the if to create.
                                    using (StreamWriter sw = File.AppendText(bufferPath))
                                    {
                                        sw.WriteLine(DateTime.Now + " - INVALID PACKET");
                                        sw.WriteLine("----------------------------------------------------------------------");
                                    }

                                }
                            } //end the if here - since we need to be notified of invalid packets too

                            Console.WriteLine(packet + " - " + DateTime.Now);
                            //try to send the normal packet's message
                            try
                            {

                                string smtpFile = "c:\\SMTPINFO.txt";
                                //SecureString gave some difficulties here would like to implement that though
                                string SMTPPORT = File.ReadLines(smtpFile).Skip(0).Take(1).First();
                                string SMTPHOSTURL = File.ReadLines(smtpFile).Skip(1).Take(1).First();
                                string USERNAME = File.ReadLines(smtpFile).Skip(2).Take(1).First();
                                string PASSWORD = File.ReadLines(smtpFile).Skip(3).Take(1).First();
                                string FROMADDRESS = File.ReadLines(smtpFile).Skip(4).Take(1).First();
                                string TOADDRESS = File.ReadLines(smtpFile).Skip(5).Take(1).First();

                                // SMTP Stuff Begin
                                SmtpClient client = new SmtpClient();
                                client.Port = Int32.Parse(SMTPPORT);
                                client.Host = SMTPHOSTURL;
                                client.EnableSsl = true;

                                client.Timeout = 10000;
                                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                                client.UseDefaultCredentials = false;
                                client.Credentials = new System.Net.NetworkCredential(USERNAME, PASSWORD);


                                MailMessage mm = new MailMessage(FROMADDRESS, TOADDRESS, SNMPSubject, SNMPshortMsg);
                                mm.BodyEncoding = UTF8Encoding.UTF8;
                                mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
                                mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess;
                                mm.DeliveryNotificationOptions = DeliveryNotificationOptions.Delay;

                                client.Send(mm);

                                try
                                {
                                    // if the buffer file exists, send it and delete it
                                    if (File.Exists(bufferPath))
                                    {
                                        // read the file and delete it
                                        string bufferContents = File.ReadAllText(bufferPath);

                                        // byffer dump emailer Stuff Begin
                                        SmtpClient bufferDump = new SmtpClient();
                                        bufferDump.Port = Int32.Parse(SMTPPORT);
                                        bufferDump.Host = SMTPHOSTURL;
                                        bufferDump.EnableSsl = true;

                                        bufferDump.Timeout = 10000;
                                        bufferDump.DeliveryMethod = SmtpDeliveryMethod.Network;
                                        bufferDump.UseDefaultCredentials = false;
                                        bufferDump.Credentials = new System.Net.NetworkCredential(USERNAME, PASSWORD);


                                        MailMessage bufferMessage = new MailMessage(FROMADDRESS, TOADDRESS, DateTime.Now + " Offline Buffer Dump", bufferContents);
                                        bufferMessage.BodyEncoding = UTF8Encoding.UTF8;
                                        bufferMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
                                        bufferMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess;
                                        bufferMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.Delay;

                                        bufferDump.Send(bufferMessage);

                                        File.Delete(bufferPath);

                                    }

                                }
                                catch
                                {
                                    Console.WriteLine("Buffer File Send Failure!!!");
                                }

                                try
                                {

                                    // This text is added only once to the file.
                                    if (!File.Exists(path))
                                    {
                                        // Create a file to write to.
                                        using (StreamWriter sw = File.CreateText(path))
                                        {
                                            sw.WriteLine("alert-log.txt Log File Begin");
                                            sw.WriteLine("----------------------------------------------------------------------");
                                        }
                                    }

                                    // This text is always appended for a successful SNMP Email
                                    using (StreamWriter sw = File.AppendText(path))
                                    {
                                        sw.WriteLine(DateTime.Now + " ----- " + SNMPBody);
                                        sw.WriteLine("----------------------------------------------------------------------");
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("Log File Failure");

                                    // create and log to the buffer file begin
                                    if (!File.Exists(bufferPath))
                                    {
                                        // Create a file to write to.
                                        using (StreamWriter sw = File.CreateText(bufferPath))
                                        {
                                            sw.WriteLine(DateTime.Now + " ----- " + SNMPBody);
                                            sw.WriteLine("----------------------------------------------------------------------");
                                        }
                                    }

                                    // the append outside of the if to create.
                                    using (StreamWriter sw = File.AppendText(bufferPath))
                                    {
                                        sw.WriteLine(DateTime.Now + " ----- " + SNMPBody);
                                        sw.WriteLine("----------------------------------------------------------------------");
                                    }

                                }

                            }
                            catch (Exception EXCEP)
                            {

                                Console.WriteLine(DateTime.Now + " - Email Failed ----- " + EXCEP + " ----- " + SNMPBody);

                                try
                                {
                                    // create and log to the buffer file begin
                                    if (!File.Exists(bufferPath))
                                    {
                                        // Create a file to write to.
                                        using (StreamWriter sw = File.CreateText(bufferPath))
                                        {
                                            sw.WriteLine(DateTime.Now + " ----- " + SNMPBody);
                                            sw.WriteLine("----------------------------------------------------------------------");
                                        }
                                    }

                                    // the append outside of the if to create.
                                    using (StreamWriter sw = File.AppendText(bufferPath))
                                    {
                                        sw.WriteLine(DateTime.Now + " ----- " + SNMPBody);
                                        sw.WriteLine("----------------------------------------------------------------------");
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("Buffer File Failure!!!");
                                }

                                try
                                {

                                    // This text is added only once to the file.
                                    if (!File.Exists(path))
                                    {
                                        // Create a file to write to.
                                        using (StreamWriter sw = File.CreateText(path))
                                        {
                                            sw.WriteLine("alert-log.txt Log File Begin");
                                            sw.WriteLine("----------------------------------------------------------------------");
                                        }
                                    }

                                    // This text is always added, making the file longer over time
                                    // if it is not deleted.
                                    using (StreamWriter sw = File.AppendText(path))
                                    {
                                        sw.WriteLine(DateTime.Now + " ----- Email Failure ----- " + EXCEP + " ----- " + SNMPBody);
                                        sw.WriteLine("----------------------------------------------------------------------");
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("Email Notifier & Logging Failure");
                                }
                            }
                        }
                    }
                    i = 0;
                }
            }
        }
    }
}

