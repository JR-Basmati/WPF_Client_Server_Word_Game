/*
 * FILE             : CSListener.cs
 * PROJECT          : A05 - TCP/IP
 * PROGRAMMER       : Josh Rice | Josh Horsley
 * FIRST VERSION    : 2024-11-14
 * DESCRIPTION      :
 *  
 *      Client-Side Listener used to receive shutdown notifications from server.
 *      
 *      Main AcceptTcpClient() loop runs similarly to Listener() class via server.
 *          - Once server shutdown message is received, triggers an event allowing the UI
 *          to notify user
 *      
 *      
 *      Port is auto-detected via FindPort() function.
 *              - Basically just tries ports, if it throws an exception, try the next one.
 *      
 *      IP settings are a combination of:
 *          - Checkbox in ui saying "Use Local Connection"
 *                  - If this is checked, IP defaults to "127.0.0.1"
 *          - App.Config
 *                  - Allows users to select "wifi" or "manual"
 *                          - "wifi" auto-detects current Wifi LAN Ip
 *                          - "manual" uses the ip specified in App.Config
 */


using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Newtonsoft.Json;
using A05_Server;


namespace ClientSide
{

    internal class CSListener
    {
        const string homeIp = "127.0.0.1";
        const int startingPort = 13001;
        const int maxAllowablePort = 13050;//Arbitrary #, I'm assuming 50 ports in a row won't be full.

        private string ipAddress;
        private int port;
        private bool? useHomeIp;

        private bool serverShutdownReceived;

        internal event Action<int?> PortUpdate;
        internal event Action<string> ServerShutdownInitiated;

        internal CSListener()
        {
            this.ipAddress = string.Empty;
            this.port = 0;
            this.useHomeIp = null;
            this.serverShutdownReceived = false;
        }

        private void SendPortUpdate(int? p)
        {
            if (PortUpdate != null)
            {
                PortUpdate.Invoke(p);
            }
        }
        private void RaiseServerShutdown(string message)
        {
            if (ServerShutdownInitiated != null)
            {
                //NEED TO START THESE ASYNCHRONOUSLY SINCE Application.Close() happens here!!!
                //Omg, I'd assumed event.Invoke was a new thread by default.
                            //Welp, I'm not gonna forget that now.
                foreach(Action<string> handler in ServerShutdownInitiated.GetInvocationList())
                {
                    Task.Run(() => handler.Invoke(message));
                }
            }
        }

        internal string IpAddress
        {
            get { return ipAddress; }
        }
        internal int? Port
        {
            get { return port; }
        }


        internal void StartListener()
        {            
            try
            {
                //If IP not set, that's bad.
                if(ipAddress == string.Empty)
                {
                    throw new Exception("No IP address provided.");
                }

                //Boot up listener
                TcpListener clientListener = new TcpListener(IPAddress.Parse(ipAddress), port);
                clientListener.Start();


                //Wait for "Server shutdown" message
                while (!serverShutdownReceived)
                {
                    
                    TcpClient serverConn = clientListener.AcceptTcpClient();
                    NetworkStream stream = serverConn.GetStream();

                    Byte[] bytes = new byte[1024];

                    int i = stream.Read(bytes, 0, bytes.Length);


                    string message = Encoding.ASCII.GetString(bytes, 0, i);
                    Command c = JsonConvert.DeserializeObject<Command>(message);


                    Command response;
                    Byte[] data;
                    //Parse incoming command

                    if (c.Cmd == CMD.S_SHUTDOWN)
                    {
                        response = new Command(CMD.C_SHUTDOWN_OK, " ", "Shutdown confirmed.", 0);
                        data = response.ConvertToBytes();
                        stream.Write(data, 0, data.Length);


                        RaiseServerShutdown("");
                        serverShutdownReceived = true;                        
                    }
                    else if (c.Cmd == CMD.C_UNBLOCK_LISTENER)
                    {
                        //No need to respond
                    }
                    else
                    {
                        response = new Command(CMD.C_ERR, " ", "Invalid command received, but probably server shutdown...", 0);
                        data = response.ConvertToBytes();
                        stream.Write(data, 0, data.Length);
                    }
                    stream.Close();
                    serverConn.Close();
                }
            }
            catch (SocketException e)
            {
                NotificationManager.Error("Listener socket exception: " + e.Message);
                //Raise event saying "Listener is messed up"
                SendPortUpdate(null);
            }
            catch (Exception e)
            {
                NotificationManager.Error("Listener exception: " + e.Message);

                //SET ERROR TO BAD
            }

        }
        internal int FindPort()
        {
            bool openPortFound = false;
            int tempPort = startingPort;
            TcpListener testServer = null;

            while(!openPortFound && tempPort <= maxAllowablePort)
            {
                try
                {

                    testServer = new TcpListener(IPAddress.Parse(ipAddress), tempPort);
                    testServer.Start();
                    
                    port = tempPort;
                    openPortFound = true;
                    testServer.Stop();
                }
                catch (SocketException e)
                {
                    tempPort++;
                }
            }
            if(tempPort == maxAllowablePort)
            {
                return 0;
            }
            else
            {
                return port;
            }
        }

        //Detect combination of app.config/GameData.LocalConnectionCheckbox to set listener IP
        internal void SetIp(bool? useHomeIp)
        {
            if (useHomeIp == true)
            {
                ipAddress = homeIp;
            }
            else
            {
                string ipMode = ConfigurationManager.AppSettings["IpMode"];
                string ipAddress = string.Empty;
                switch (ipMode)
                {
                    case "wifi":
                        this.ipAddress = DetectWifiIp();
                        break;
                    case "manual":
                        this.ipAddress = ConfigurationManager.AppSettings["IpAddress"];
                        break;
                    default:
                        this.ipAddress = string.Empty;
                        break;
                }
            }
        }


        //Same as in GameDataLoader
        private string DetectWifiIp()
        {
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var netInterface in networkInterfaces)
                {
                    //Check if the interface is wireless and operational
                    if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
                        netInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        //Get the IP properties
                        var properties = netInterface.GetIPProperties();

                        //Find the IPv4 address
                        foreach (var address in properties.UnicastAddresses)
                        {
                            if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return address.Address.ToString();
                            }
                        }
                    }
                }

                return string.Empty;
            }
            catch (Exception e)
            {
                SendPortUpdate(null);
                return string.Empty;
            }
        }
        internal void ShutdownListener()
        {
            //Cancel loop
            serverShutdownReceived = true;
            //Send a ping to listener to unblock AcceptTcpClient()
            try
            {
                Command shutdownCommand = new Command(CMD.C_UNBLOCK_LISTENER, "ShutdownManager", "Server shutdown initiated.", 0);
                Byte[] data = shutdownCommand.ConvertToBytes();

                TcpClient client = new TcpClient(ipAddress, (int)port);

                NetworkStream stream = client.GetStream();
                stream.Write(data, 0, data.Length);

                stream.Close();
                client.Close();
            }
            catch (SocketException e)
            {
                NotificationManager.Error("Error unblocking listener.");
            }
        }
    }

   
}
