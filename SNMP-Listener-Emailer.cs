using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.IO;

namespace SNMP_Listener_Emailer
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 162;
            UdpClient listener;
            IPEndPoint groupEP;
            byte[] packet = new byte[1024];
            int commlength, miblength, datatype, datalength, datastart, Objecttype, Objectlength;
            int objectstart;
            string objectid;
            string output;

            Console.WriteLine("Initializing SNMP Listener on Port:" + port + "...");

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

                    if (packet[0] == 0xff)
                    {
                        Console.WriteLine("Invalid Packet" + " - " + DateTime.Now);
                        return;
                    }
                    Console.WriteLine(packet + " - " + DateTime.Now);

                    try
                    {
                        // SMTP Stuff Begin - This is built for TLS over AWS SES
                        SmtpClient client = new SmtpClient();
                        client.Port = 587;
                        client.Host = "email-smtp.us-east-1.amazonaws.com";
                        client.EnableSsl = true;

                        client.Timeout = 10000;
                        client.DeliveryMethod = SmtpDeliveryMethod.Network;
                        client.UseDefaultCredentials = false;
                        client.Credentials = new System.Net.NetworkCredential("AWS SES SMTP UN", "AWS SES SMTP PW");

                        MailMessage mm = new MailMessage("SES SMTP VERIFIED EMAIL SENDER", "SMTP EMAIL RECIPIENT", SNMPSubject, SNMPBody);
                        mm.BodyEncoding = UTF8Encoding.UTF8;
                        mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                        client.Send(mm);
                    }
                    catch (Exception EXCEP)
                    {
                        Console.WriteLine(DateTime.Now + " - SNMP Email Failed");


                        DateTime now = DateTime.Now;


                        try
                        {
                            string path = @"c:\snmp-log.txt";
                            // This text is added only once to the file.
                            if (!File.Exists(path))
                            {
                                // Create a file to write to.
                                using (StreamWriter sw = File.CreateText(path))
                                {
                                    sw.WriteLine("snmp-log.txt Log File Begin");
                                }
                            }

                            // This text is always added, making the file longer over time
                            // if it is not deleted.
                            using (StreamWriter sw = File.AppendText(path))
                            {

                                sw.WriteLine(now + " - " + EXCEP);

                            }
                        }
                        catch
                        {
                            Console.WriteLine("Email Notifier & Log File Failure");
                        }
                    }
                }
            }
        }
    }
}