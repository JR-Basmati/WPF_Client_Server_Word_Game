using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows;
using System.Deployment.Internal;

// FILE : GameData.cs
// PROJECT : Assignment 5 - TCP/IP
// PROGRAMMERS : Josh Horsley | Josh Rice
// FIRST VERSION : 2024-11-12
// DESCRIPTION :
// This file contains the GameData class, which handles the game's data and keeps the UI updated with any changes.
// The class uses INotifyPropertyChanged to enable data binding, making sure the user interface 
// reflects the latest game state in real time.

namespace ClientSide
{//anything on screen you want to change lives here
    public class GameData : INotifyPropertyChanged
    {
        private string ipAddress;
        private int? port;
        private string userName;
        private int? timeLimit;
        private bool localConnectionCheckbox;
        private int clientSidePort;

        private string playerGuess;
        private string gameString;
        private int wordCount;

        private bool serverActive;

        public bool ServerActive
        {
            get { return serverActive; }
            set {
                if (serverActive != value)
                {
                    serverActive = value;
                    OnPropertyChanged(nameof(ServerActive));
                }
            }
        }

        private string feedbackMessage;
        private Brush feedbackColour;


       


        public string UserName
        {
            get
            {
                return userName;
            }
            set
            {
                if (userName != value)
                {
                    userName = value;
                    OnPropertyChanged(nameof(UserName));
                }
            }
        }
        public string IpAddress
        {
            get
            {
                return ipAddress;
            }
            set
            {
                if (ipAddress != value)
                {
                    ipAddress = value;
                    OnPropertyChanged(nameof(IpAddress));
                }
            }
        }

        public int? Port
        {
            get
            {
                return port;
            }
            set
            {
                if (port != value)
                {
                    port = value;
                    OnPropertyChanged(nameof(Port));
                }
            }
        }

        public bool LocalConnectionCheckbox
        {
            get { return localConnectionCheckbox; }
            set { localConnectionCheckbox = value; }
        }

        public int? TimeLimit
        {
            get
            {
                return timeLimit;
            }
            set
            {
                if (timeLimit != value)
                {
                    timeLimit = value;
                    OnPropertyChanged(nameof(TimeLimit));
                }
            }
        }

        public string PlayerGuess
        {
            get
            {
                return playerGuess;
            }
            set
            {
                playerGuess = value;
            }
        }
        public string GameString
        {
            get
            {
                return gameString;
            }
            set
            {
                if(gameString != value)
                {
                    gameString = value;
                    OnPropertyChanged(nameof(GameString));
                }
            }
        }

        public int WordCount
        {
            get
            {
                return wordCount;
            }
            set
            {
                if (wordCount != value)
                {
                    wordCount = value; ;
                    OnPropertyChanged(nameof(WordCount));
                }
            }
        }

        public int ClientSidePort
        {
            get
            {
                return clientSidePort;
            }
            set
            {
                if (clientSidePort != value)
                {
                    clientSidePort = value;
                    OnPropertyChanged(nameof(ClientSidePort));
                }
            }
            //set is triggered via subscribing to CSListener.PortUpdate event
        }


        public string FeedbackMessage
        {
            get
            {
                return feedbackMessage;
            }
            set
            {
                if (feedbackMessage != value)
                {
                    feedbackMessage = value;
                    OnPropertyChanged(nameof(FeedbackMessage));
                }
            }
        }

        public Brush FeedbackColour
        {
            get
            {
                return feedbackColour;
            }
            set
            {
                if (feedbackColour != value)
                {
                    feedbackColour = value;
                    OnPropertyChanged(nameof(FeedbackColour));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal void UpdateClientPort(int? portNum)
        {
             
            
            ClientSidePort = (int)portNum;
        }
        internal void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
