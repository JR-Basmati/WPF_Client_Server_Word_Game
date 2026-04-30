/*
 * FILE             : ShutdownManager.cs
 * PROJECT          : A05 - TCP/IP
 * PROGRAMMER       : Josh Rice | Josh Horsley
 * FIRST VERSION    : 2024-11-14
 * DESCRIPTION      :
 *  
 *      This class handles the CancellationTokenSource used to shutdown Listener
 *      and notifications to all currently registered clients upon shutdown.
 *      
 *      Main shutdown functionality occurs in InitiateShutdown():
 *              - Log reason for shutdown
 *              - Send notifications to clients
 *              - Cancel server token
 *                  - Ping server to unblock AcceptTcpClient() function

 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace A05_Server
{
    //TODO
    //Exception handling

    internal class ShutdownManager
    {
        private List<ClientGame> playerList;
        private string serverIp;
        private int serverPort;


        private bool shutdownComplete;
        internal bool ShutdownComplete//READONLY
        {
            get { return shutdownComplete; }
        }



        private CancellationTokenSource cts;
        internal CancellationToken Token//READONLY
        {
            get { return cts.Token; }
        }
        internal void CancelToken()
        {
            cts.Cancel();
        }
        internal void DisposeToken()
        {
            cts.Dispose();
        }


        internal ShutdownManager(List<ClientGame> playerList, string serverIp, int serverPort)
        {
            this.playerList = playerList;
            this.serverIp = serverIp;
            this.serverPort = serverPort;
            cts = new CancellationTokenSource();
        }


        internal void InitiateShutdown(string s)
        {
            Log("Shutdown requested with message: " + s);

            //Send messages to all clients
            SendShutdownNotifications();


            CancelToken();
            DisposeToken();

            //Ping server to unblock the AcceptTcp() call
            UnblockServer();

            shutdownComplete = true;
        }



        internal void ListClients()
        {
            if (playerList.Count == 0)
            {
                Log("No players connected.");
            }
            else
            {
                foreach (ClientGame player in playerList)
                {
                    Log($"TEST||Client: {player.ClientId}");
                }

            }
        }

        internal event Action<string> WriteLog;
        private void Log(string s)
        {
            if (WriteLog != null)
            {
                WriteLog.Invoke(s);
            }
        }


        internal void UnblockServer()
        {
            try
            {
                Command shutdownCommand = new Command(CMD.S_SHUTDOWN, "ShutdownManager", "Server shutdown initiated.", 0);
                Byte[] data = shutdownCommand.ConvertToBytes();

                TcpClient client = new TcpClient(serverIp, serverPort);
                
                NetworkStream stream = client.GetStream();
                stream.Write(data, 0, data.Length);
            }
            catch (SocketException e)
            {
                Log("ERR|WARN: ShutdownManager.UnblockServer(): " + e.Message);
            }
        }


        internal void SendShutdownNotifications()
        {
            Command shutdownCmd = new Command(CMD.S_SHUTDOWN, null, "Server shutdown initiated.", 0);

            foreach (ClientGame player in playerList)
            {
                try
                {

                    shutdownCmd.ClientId = player.ClientId;

                    string message = JsonConvert.SerializeObject(shutdownCmd);
                    Byte[] data = Encoding.UTF8.GetBytes(message);


                    TcpClient client = new TcpClient(player.ClientIp, player.ClientPort);

                    NetworkStream stream = client.GetStream();
                    stream.Write(data, 0, data.Length);

                    data = new byte[1024];
                    Int32 bytes = stream.Read(data, 0, data.Length);

                    string asciiResponse = Encoding.ASCII.GetString(data, 0, bytes);

                    Command response = JsonConvert.DeserializeObject<Command>(asciiResponse);

                    stream.Close();
                    client.Close();

                    if (response.Cmd == CMD.C_SHUTDOWN_OK)
                    {
                        Log($"INFO: Shutdown confirmation receieved from clientId: {player.ClientId}");
                    }
                    else
                    {
                        Log($"ERR|WARN: Invalid confirmation of shutdown for client {player.ClientId}. Instead received a {response.Cmd}. " +
                            $"Continuing operation.");
                    }

                }
                catch (SocketException e)
                {
                    Log($"ERR|WARN: Shutdown notification failed to connect to client {player.ClientId}.\n" + e.Message);
                    continue;
                }
                catch (Exception e)
                {
                    Log($"ERR|WARN: Exception occurred during shutdown notification for client {player.ClientId}:\n" + e.Message);
                    continue;
                }


            }



        }


    }
}
