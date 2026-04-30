/*
 * FILE             : GameEngine.cs
 * PROJECT          : A05 - TCP/IP
 * PROGRAMMER       : Josh Rice | Josh Horsley
 * FIRST VERSION    : 2024-11-14
 * DESCRIPTION      :
 *  
 *      This game handles the list of connected clients & their game state.
 *      
 *      General Flow:
 *          Command is received from Listener
 *          
 *          ExecuteCommand() parses the command, and calls appropriate functions to:
 *                      - Register/deregister clients
 *                      - Assign new GameFiles to clients
 *                      - Check Client guesses against GameFile
 *                      - Receive other commands (Quit, confirm quit, etc..)
 *                      
 *          Response is then created as a Command and returned to Listener who is able
 *          to dispatch the response back to the client.
 *      
 *      
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;


namespace A05_Server
{
    internal class GameEngine
    {

        private List<ClientGame> gameList;
        private List<GameFile> gameFiles;

        internal List<ClientGame> GameList
        {
            get { return gameList; }
        }


        internal event Action<string> WriteLog;
        private void Log(string s)
        {
            if (WriteLog != null)
            {
                WriteLog.Invoke(s);
            }
        }


        internal GameEngine(List<GameFile> gameFiles)
        {
            this.gameFiles = gameFiles;
            gameList = new List<ClientGame>();
        }


        internal Command ExecuteCommand(Command c, IPEndPoint endPointInfo)
        {
            ClientGame clientGame;
            Command response;

            switch (c.Cmd)
            {
                case CMD.C_START:
                    //If client doesn't exist, register them & give them back a new game.
                    if ((clientGame = GetGame(c.ClientId)) == null)
                    {
                        Log("Registering new client..");                        
                        AddClient(c.ClientId, endPointInfo, c.Arg2);

                        //Check that newly created client exists in list
                        if ((clientGame = GetGame(c.ClientId)) == null)
                        {
                            Log("ERR|FATAL-ISH: GetGame() found null trying to retrieve newly added clientGame.");
                            return response = new Command(CMD.C_ERR, c.ClientId, "Error creating client game", 0);
                        }
                        //Give them a new gameFile
                        AssignGameFile(clientGame);

                        //Reply to client with game file
                        response = new Command(CMD.S_NEW_CLIENT, c.ClientId,
                                                clientGame.Game.gameText, clientGame.Game.wordCount);

                        return response;
                    }
                    //If they DO exist, send them to RESTART, but they shouldn't have ended up here.
                    else
                    {
                        Log($"ERR|WARN: NewGame requested by existing client {c.ClientId}. Redirecting to 'Restart' command.");
                        c.Cmd = CMD.C_RESTART;
                        return ExecuteCommand(c, endPointInfo);
                    }

                case CMD.C_GUESS:

                    //
                    if((clientGame = GetGame(c.ClientId)) == null)
                    {
                        Log("ERR|FATAL-ISH: GetGame() found no session for client requesting guess validation. Redirecting to 'Start'");
                        c.Cmd = CMD.C_START;
                        return ExecuteCommand(c, endPointInfo);
                    }
                    else if (clientGame.Game == null || clientGame.Game.gameText == null)
                    {
                        Log("ERR|FATAL-ISH: Client game was uninitialized. Redirecting to 'Restart'");
                        c.Cmd = CMD.C_RESTART;
                        return ExecuteCommand(c, endPointInfo);
                    }
                    
                    //Validate the actual guess & return CMD.S_VALID_GUESS or CMD.S_INVALID_GUESS
                    return response = ValidateGuess(clientGame, c.Message);



                case CMD.C_RESTART:

                    clientGame = GetGame(c.ClientId);
                    //If Client exists, continue as usual
                    if(clientGame != null)
                    {
                        AssignGameFile(clientGame);

                        response = new Command(CMD.S_RESTART, c.ClientId,
                                                clientGame.Game.gameText, clientGame.Game.wordCount);
                        return response;
                    }
                    //Else log warning, and redirect them to "New Client Game Start"
                    else
                    {
                        Log($"ERR|WARN: Restart requested by non-existing client {c.ClientId}. Redirecting to 'Start' command for registartion.");
                        c.Cmd = CMD.C_START;
                        return ExecuteCommand(c, endPointInfo);
                    }

                case CMD.C_TIME_UP:
                    clientGame = GetGame(c.ClientId);
                    clientGame.GameEnded = true;
                    response = new Command(CMD.S_PLAY_AGAIN, c.ClientId, "Want to play again?", 0);
                    return response;
                    

                case CMD.C_QUIT:
                    response = new Command(CMD.S_CONFIRM_QUIT, c.ClientId, "Are you sure you'd like to quit?", 0);
                    return response;
                    

                case CMD.C_CONFIRM_QUIT:
                    if (DeregisterClient(c.ClientId))
                    {
                        response = new Command(CMD.S_QUIT, c.ClientId, "Client de-registered.", 0);
                    }
                    else
                    {
                        response = ErrorResponse(c.ClientId, "Client does not exist. Failed to deregister.");
                    }
                    return response;                    
            }
            Log($"ERR|WARN: Invalid command recieved by client {c.ClientId} :: {JsonConvert.SerializeObject(c)}");
            return InvalidCommand(c.ClientId, "Command not recognized by game engine.");
        }



        internal void AddClient(string clientId, IPEndPoint endPointInfo, int clientListenerPort)
        {
            ClientGame temp = new ClientGame();
            temp.ClientId = clientId;

            temp.ClientIp = endPointInfo.Address.ToString();
            if(clientListenerPort == 0)
            {
                Log("ERR|WARN: Client did not provide listener port. No shutdown command will be sent to them upon server shutdown.");
            }
            temp.ClientPort = clientListenerPort;
            temp.GameEnded = false;
            gameList.Add(temp);
        }
        internal bool AssignGameFile(ClientGame playerGame)
        {
            if(gameFiles == null)
            {
                Log("ERR|WARN: Game Engine's GameFile List is null.");
                return false;
            }            
            //Start gameFound flag, 
            bool gameFound = false;
            Random rand = new Random(System.DateTime.Now.Second);
            while (!gameFound)
            {
                //If multiple game files, make sure we're giving them a new one
                if(gameFiles.Count > 1)
                {
                    int randIndex = rand.Next(0, gameFiles.Count);

                    //If it's an existing client, double check we're not giving them the same GameFile
                    if(playerGame.Game != null)
                    {
                        if (playerGame.Game.gameText == gameFiles[randIndex].gameText)
                        {
                            continue;
                        }
                    }

                    playerGame.Game = gameFiles[randIndex].Clone();
                    gameFound = true;
                }
                //If there's only one game, give them that one
                else
                {
                    playerGame.Game = gameFiles[0];
                    gameFound = true;
                }
            }
            return true;
        }
        internal bool DeregisterClient(string clientId)
        {
            ClientGame clientGame = GetGame(clientId);
            if(clientGame == null)
            {
                Log($"ERR|WARN: Error deregistering client {clientId}. Client session not found.");
                return false;
            }
            else
            {
                gameList.Remove(clientGame);
                return true;
            }
        }

        internal int GameTimeout(string clientId)
        {
            return 0;
        }
        internal ClientGame GetGame(string clientId)
        {
            if(!gameList.Exists(g => g.ClientId == clientId)){
                return null;
            }

            return gameList.Find(g => g.ClientId == clientId);
        }

        internal GameFile StartNewGame(string clientId)
        {
            GameFile temp = null;
            return temp;
        }
        

        internal Command ValidateGuess(ClientGame clientGame, string guess)
        {

            Command response;

            if (clientGame.TryGuess(guess))
            {
                if(clientGame.RemainingWordCount() == 0)
                {
                    response = new Command(CMD.S_PLAY_AGAIN, clientGame.ClientId, "You got them all! Want to play again?", 0);
                    clientGame.GameEnded = true;
                }
                else
                {
                    response = new Command(CMD.S_VALID_GUESS, clientGame.ClientId, "Correct!", clientGame.RemainingWordCount());
                }
            }
            else
            {
                response = new Command(CMD.S_INVALID_GUESS, clientGame.ClientId, "Invalid Guess.", clientGame.RemainingWordCount());
            }
            return response;
        }

        


        //PREMADE ERROR RESPONSES
        internal Command ErrorResponse(string clientId, string message)
        {
            Command temp = new Command();
            temp.Cmd = CMD.S_ERR;
            temp.ClientId = clientId;
            temp.Message = message;
            temp.Arg2 = 0;
            return temp;
        }
        internal Command InvalidCommand(string clientId, string message)
        {
            Command c = new Command();
            c.Cmd = CMD.S_INVALID;
            c.ClientId = clientId;
            c.Message = message;
            c.Arg2 = 0;

            return c;
        }
    }
}
