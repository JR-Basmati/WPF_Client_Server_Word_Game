/*
 * FILE             : ClientGame.cs
 * PROJECT          : A05 - TCP/IP
 * PROGRAMMER       : Josh Rice | Josh Horsley
 * FIRST VERSION    : 2024-11-14
 * DESCRIPTION      :
 *  
 *      This object represents a single client's connection info and game state.
 *      
 *      This includes the ip/port to reach them at when the ShutdownManager sends
 *      out notifications.
 *      
 *      The game engine holds a list of ClientGames for all clients currently connected.
 *      
 *      ShutdownManager holds a reference to GameEngine's list of clients so it can
 *      send notifications out.
 *      
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A05_Server
{
    internal class ClientGame
    {

        private string clientId;
        private GameFile game;

        private string clientIp;
        private int clientPort;

        private bool gameEnded;

        internal string ClientId
        {
            get { return clientId; }
            set { clientId = value; }
        }

        internal GameFile Game
        {
            get { return game; }
            set { game = value; }
        }
        internal string ClientIp
        {
            get { return clientIp; }
            set { clientIp = value; }
        }

        internal int ClientPort
        {
            get { return clientPort; }
            set { clientPort = value; }
        }

        internal bool GameEnded
        {
            get { return gameEnded; }
            set { gameEnded = value; }
        }

        internal ClientGame()
        {
            clientId = null;
            game = null;
            clientIp = null;
            clientPort = 0;
            gameEnded = false;
        }

        internal ClientGame(string id, GameFile game, string ip, int port)
        {
            this.clientId = id;
            this.game = game;
            this.clientIp = ip;
            this.clientPort = port;
        }

        internal int RemainingWordCount()
        {
            int count = 0;
            foreach(ValidWord word in Game.words)
            {
                if(!word.found)
                {
                    count++;
                }
            }
            return count;
        }
        internal bool TryGuess(string guess)
        {
            bool result = false;
            for(int i = 0; i < game.words.Length; i++)
            {
                if (game.words[i].word.ToLower() == guess.ToLower())
                {
                    result = true;
                    game.words[i].found = true;
                }
            }
            return result;
        }

        //Command ParseCommand(Command cmd)
        //Do stuff
        //Return the command I need to send to user


        //Start new game (GameFile newGameFile)
        //All bools = false; I think?
        //If we're awaiting quit confirm and they say something else, is that an issue?

        //Check if word exists
        //If yes, mark it found
        //If that's the last word, I need to know
        //If no, say that

        //


    }
}
