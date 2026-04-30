/*
 * FILE             : Listener.cs
 * PROJECT          : A05 - TCP/IP
 * PROGRAMMER       : Josh Rice | Josh Horsley
 * FIRST VERSION    : 2024-11-14
 * DESCRIPTION      :
 *  
 *      Listener is used to receive client TCP/IP requests & send them to GameEngine.
 *      
 *      Usage:
 *          Listener takes in a reference to the GameEngine upon construction.
 *          
 *          StartListener() is called on its own thread, and waits for Client TCP Requests.
 *          
 *              Received requests are deserialized from JSON, passed to GameEngine.
 *              GameEngine returns command object, which is Serialized and returned to client.
 *      
 *          Shutdown manager controls the CancellationTokenSource which breaks the StartListener() loop.
 */


using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace A05_Server
{
    internal class Listener
    {
        private string ipAddress;
        private int port;

        GameEngine engine;

        internal string IpAddress
        {
            get { return ipAddress; }
            set { ipAddress = value; }
        }
        internal int Port
        {
            get { return port; }
            set { port = value; }
        }



        internal event Action<string> WriteLog;
        private void Log(string s)
        {
            if (WriteLog != null)
            {
                WriteLog("Listener:" + s);
            }
        }

        internal Listener(GameEngine engine, int port, string ipAddress)
        {
            this.engine = engine;
            this.port = port;
            this.ipAddress = ipAddress;
        }

        async internal void StartListener(CancellationToken token)
        {
            TcpListener server = null;
            try
            {
                Log("Listener started");
                //Boot up server on port & IP specified
                
                server = new TcpListener(IPAddress.Parse(ipAddress), port);
                server.Start();

                //Listen for client requests
                while (!token.IsCancellationRequested)
                {
                    Log("Waiting for a connection... ");
                    TcpClient client = server.AcceptTcpClient();

                    //Create a thread to parse JSON request & pass to game engine
                    Log("Client connected.");
                    Task t = new Task(() => HandleRequest(client));
                    t.Start();
                }
            }
            catch (SocketException e)
            {
                Log("SocketException: " + e.Message);
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }
        }

        internal void HandleRequest(TcpClient client)
        {
            Command clientCommand = null;
            NetworkStream stream = null;
            try
            {

                //Get client data
                Byte[] bytes = new Byte[1024];
                string message = String.Empty;

                stream = client.GetStream();

                Int32 i = stream.Read(bytes, 0, bytes.Length);
                message = Encoding.ASCII.GetString(bytes, 0, i);

                //Convert to object
                clientCommand = JsonConvert.DeserializeObject<Command>(message.ToString());
                //Save client IP in case of registration
                IPEndPoint endPointInfo = (IPEndPoint)client.Client.RemoteEndPoint;

                //TROUBLESHOOTING
                Log("INFO: Message from client [" + clientCommand.ClientId + "]:");
                Log(message.ToString());
                Log("IpAddress: " + endPointInfo.Address + " Port: " + endPointInfo.Port);
                

                if(clientCommand.Cmd != CMD.S_SHUTDOWN)
                {
                    //PASS TO ENGINE AND RECEIVE RESPONSE
                    Command response = null;
                    response = engine.ExecuteCommand(clientCommand, endPointInfo);

                    //Send response to client
                    string respAscii = JsonConvert.SerializeObject(response);
                    Byte[] respData = Encoding.UTF8.GetBytes(respAscii);

                    stream.Write(respData, 0, respData.Length);

                    //TROUBLESHOOTING
                    Log("INFO: Response to client [" + response.ClientId + "]:");
                    Log(respAscii);
                }


            }
            catch (Exception e)
            {
                Log("ERR|WARN: Listener.HandleRequest() failed. " + e.Message);
                if(clientCommand != null)
                {
                    Log("Client command: " + JsonConvert.SerializeObject(clientCommand));
                }
                else
                {
                    Log("Client command returned null at time of exception.");
                }
            }
            finally
            {
                stream.Close();
                client.Close();
            }
        }
    }
}
