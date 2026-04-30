/*
 * FILE             : Command.cs
 * PROJECT          : A05 - TCP/IP
 * PROGRAMMER       : Josh Rice | Josh Horsley
 * FIRST VERSION    : 2024-11-14
 * DESCRIPTION      :
 *  
 *      This class allows construction of Command objects which
 *      adhere to our A05 TCP/IP communication protocol.
 *      
 *      Newtonsoft.Json NuGet package was added to our solution to allow for
 *      serialization/deserialization of messages.
 *      
 *      CMD enum defines all possible commands for both server/client.
 *      
 *      Message format is fully documented in the README.
 *      Summary:
 *      CMD      - Enum specifying which command is being sent.
 *      ClientId - Used by client to send a GUID string identifying them.
 *      Message  - String argument for commands
 *      Arg2     - Integer argument for commands
 *                          - Used to get client port # upon registration
 *                          - Used to send # of words/words remaining to client
 *                          
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A05_Server
{

    public enum CMD
    {
        //Client commands
        C_START = 100,          //New client + new game
        C_GUESS = 101,          //Submitting Guess
        C_QUIT = 104,           //Request quit
        C_CONFIRM_QUIT = 105,   //Confirm quit 
        C_TIME_UP = 107,        //Player time ended
        C_RESTART = 108,        //Confirm restart game    -> can get here via timeout, OR finding all words.
        C_ERR = 111,            //Client err/unexpected shutdown

        C_SHUTDOWN_OK = 112,    //Confirm "Shutdown" message received
        C_UNBLOCK_LISTENER = 113,//Need to ping listener upon shutdown to clear AcceptTcpClient() call

        //Server commands
        S_NEW_CLIENT = 200,     //New client + new game data
        S_VALID_GUESS = 201,    //Guess was valid
        S_INVALID_GUESS = 202,  //Guess was invalid
        S_CONFIRM_QUIT = 204,   //Please confirm quit?
        S_QUIT = 205,           //Client de-registered.
        S_ABORT_QUIT = 206,     //Quit aborted
        S_PLAY_AGAIN = 207,     //Want to play again?
        S_RESTART = 208,        //Existing client + new game data
        S_ERR = 211,            //Server error
        S_SHUTDOWN = 212,       //Server shutdown as of RIGHT NOW
        S_INVALID = 222,        //Request was invalid for client game state

        NULL = int.MaxValue     //CMD uninitialized
    }
    public class Command
    {
        private CMD cmd;
        private string clientId;
        private string message;
        private int arg2;

        public CMD Cmd
        {
            get { return cmd; } 
            set { cmd = value; }
        }
        public string ClientId
        {
            get { return clientId; }
            set { clientId = value; }
        }
        public string Message
        {
            get { return message; }
            set { message = value; }
        }
        public int Arg2
        {
            get { return arg2; }
            set { arg2 = value; }
        }

        public Command(CMD comm, string id, string mess, int wc)
        {
            cmd = comm;
            clientId = id;
            message = mess;
            arg2 = wc;
        }
        public Command()
        {
            cmd = CMD.NULL;
            clientId = null;
            message = null;
            arg2 = int.MaxValue;
        }

        public Byte[] ConvertToBytes()
        {
            string jsonCommand = JsonConvert.SerializeObject(this);
            Byte[] byteCommand = Encoding.UTF8.GetBytes(jsonCommand);
            return byteCommand;
        }
        public void ReadFromBytes(Byte[] data, Int32 bytes)
        {
            Command temp = new Command();
            string jsonCommand = Encoding.ASCII.GetString(data, 0, bytes);
            temp = JsonConvert.DeserializeObject<Command>(jsonCommand);

            this.cmd = temp.cmd;
            this.clientId = temp.clientId;
            this.message = temp.message;
            this.arg2 = temp.arg2;
        }
    }
}