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

namespace OpenITTools_SNMP_Listener_Emailer
{
    class Program
    {
        
        static void Main(string[] args)
        {

            Process [] localByName = Process.GetProcessesByName("snmp*");
            foreach(Process p in localByName)
            {
                 p.Kill();
            }
        
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
                    string path = @"c:\snmp-log.txt";

                    if (packet[0] == 0xff)
                    {
                        Console.WriteLine(DateTime.Now + " - Invalid Packet");

                        //add the log file and emailer to this part too since invalid packets would be important
                        try
                        {

                            // SMTP Stuff Begin
                            SmtpClient client = new SmtpClient();
                            client.Port = 587;
                            client.Host = "smtpServerAddress";
                            client.EnableSsl = true;

                            client.Timeout = 10000;
                            client.DeliveryMethod = SmtpDeliveryMethod.Network;
                            client.UseDefaultCredentials = false;
                            client.Credentials = new System.Net.NetworkCredential("smtpUser", "smtpPassword");


                            MailMessage mm = new MailMessage("emailFromAddress", "emailToAddress", DateTime.Now + " - SNMP INVALID PACKET", DateTime.Now + " - SNMP INVALID PACKET");
                            mm.BodyEncoding = UTF8Encoding.UTF8;
                            mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                            client.Send(mm);

                            try
                            {

                                // This text is added only once to the file.
                                if (!File.Exists(path))
                                {
                                    // Create a file to write to.
                                    using (StreamWriter sw = File.CreateText(path))
                                    {
                                        sw.WriteLine("snmp-log.txt Log File Begin");
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
                                Console.WriteLine("Email Notifier & Log File Failure for Invalid Packet");
                            }


                            return;

                        }
                        catch
                        {
                            Console.WriteLine("Email Notifier Failure for Invalid Packet");
                        }
                    } //end the if here - since we need to be notified of invalid packets too

                    Console.WriteLine(packet + " - " + DateTime.Now);

                    try
                    {
                        
                        // SMTP Stuff Begin
                        SmtpClient client = new SmtpClient();
                        client.Port = 587;
                        client.Host = "snmpServerAddress";
                        client.EnableSsl = true;

                        client.Timeout = 10000;
                        client.DeliveryMethod = SmtpDeliveryMethod.Network;
                        client.UseDefaultCredentials = false;
                        client.Credentials = new System.Net.NetworkCredential("smtpUser", "smtpPassword");


                        MailMessage mm = new MailMessage("emailFromAddress", "emailToAddress", SNMPSubject, SNMPshortMsg);
                        mm.BodyEncoding = UTF8Encoding.UTF8;
                        mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                        client.Send(mm);

                        try
                        {

                            // This text is added only once to the file.
                            if (!File.Exists(path))
                            {
                                // Create a file to write to.
                                using (StreamWriter sw = File.CreateText(path))
                                {
                                    sw.WriteLine("snmp-log.txt Log File Begin");
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
                            Console.WriteLine("Email Notifier & Log File Failure");
                        }

                    }
                    catch (Exception EXCEP)
                    {
                        
                        Console.WriteLine(DateTime.Now + " - SNMP Email Failed ----- " + EXCEP + " ----- " + SNMPBody);
                        
                        try
                        {
                            
                            // This text is added only once to the file.
                            if (!File.Exists(path))
                            {
                                // Create a file to write to.
                                using (StreamWriter sw = File.CreateText(path))
                                {
                                    sw.WriteLine("snmp-log.txt Log File Begin");
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
                            Console.WriteLine("Email Notifier Failure");
                        }
                    }
                }
            }
        } 
    }
}
