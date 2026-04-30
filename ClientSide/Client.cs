using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Windows;
using A05_Server;
using Newtonsoft.Json;

// FILE : Client.cs
// PROJECT : Assignment 5 - TCP/IP
// PROGRAMMERS : Josh Horsley | Josh Rice
// FIRST VERSION : 2024-11-10
// DESCRIPTION :
// This file defines the Client class. It is responsible for connecting to the server and managing the exchange of commands.
// The class stores the server's IP address, port number, and ClientId. It handles the serialization of commands to JSON,
// sends them over a TCP connection, and converts server responses back into Command objects.


namespace ClientSide
{
    internal class Client
    {

        private string ipAddress;
        private int? port;
        private string clientId; // READONLY


        public string IpAddress
        {
            get { return ipAddress; }
            set { ipAddress = value; }
        }
        public int? Port
        {
            get { return port; }
            set { port = value; }
        }
        public string ClientId
        {
            get { return clientId; }
        }
      
        internal Client()
        {
            ipAddress = String.Empty;
            port = null;
            clientId = Guid.NewGuid().ToString();
        }


        internal Command SendCommand(Command c)
        {
            try
            {
                string serialMessage = JsonConvert.SerializeObject(c);

                Byte[] data = Encoding.UTF8.GetBytes(serialMessage);
                TcpClient client = new TcpClient(ipAddress, (int)port);

                NetworkStream stream = client.GetStream();
                stream.Write(data, 0, data.Length);

                data = new Byte[1024];
                Int32 bytes = stream.Read(data, 0, data.Length);

                //Bytes -> Ascii
                string asciiResponse = Encoding.ASCII.GetString(data, 0, bytes);

                Command response = null;
                response = JsonConvert.DeserializeObject<Command>(asciiResponse);

                stream.Close();
                client.Close();

                return response;
            }
            catch (Exception e) 
            {
                return null;
            }
        }
    }
}
