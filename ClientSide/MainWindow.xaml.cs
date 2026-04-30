using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Timers;
using System.Net.Sockets;
using A05_Server;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;

// FILE : MainWindow.xaml.cs
// PROJECT : Assignment 5 - TCP/IP
// PROGRAMMERS : Josh Horsley | Josh Rice
// FIRST VERSION : 2024-11-10
// DESCRIPTION :
// This file contains the MainWindow class, which serves as the primary interface for our Word Finder application.
// The MainWindow class manages user interactions, game state, and communication with the server. It handles the connection setup, 
// gameplay mechanics, and user feedback, ensuring smooth interaction between the client and server.

namespace ClientSide
{
    public partial class MainWindow : Window
    {
        CSListener csListener = null;
        Client client = null;
        GameData gameData = null;

        private DispatcherTimer feedbackTimer;
        private System.Timers.Timer countdownTimer;
        private int? initialTimeLimit;
        Task listenerTask;
        public MainWindow()
        {
            InitializeComponent();
            InitializeFeedbackTimer();


            //Instantiate client.
            //"Connect" button will init the properties
            client = new Client();

            //Init gameData + UI bindings
            gameData = new GameData();
            this.DataContext = gameData;

            csListener = new CSListener();
            csListener.PortUpdate += gameData.UpdateClientPort;


            //Create listener & subscribe to events.
            csListener = new CSListener();
            csListener.PortUpdate += gameData.UpdateClientPort;
            csListener.ServerShutdownInitiated += ServerShutdownReceived;
        }

        private void LocalConnectionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            txtbxIpAddress.Text = "127.0.0.1";
            txtbxIpAddress.IsReadOnly = true;

        }

        private void LocalConnectionCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            txtbxIpAddress.Text = string.Empty;
            txtbxIpAddress.IsReadOnly = false;
        }

        //////////////////////
        //CONNECT TO SERVER//
        ////////////////////
        private void connectButton_Click(object sender, RoutedEventArgs e)
        {

            if (InputValidator.ValidateConnectionScreen(gameData.TimeLimit, gameData.Port, gameData.IpAddress, gameData.UserName))
            {
                //Set client IP/Port to user input
                client.IpAddress = gameData.IpAddress;
                client.Port = gameData.Port;

                //Set either "127.0.0.1" if checkbox was checked, else use config file to set Ip
                csListener.SetIp(gameData.LocalConnectionCheckbox);
                gameData.ClientSidePort = csListener.FindPort();



                //Request start of game
                Command startCommand = new Command(CMD.C_START, client.ClientId, "", gameData.ClientSidePort);
                Command response = client.SendCommand(startCommand);
                if (response == null)
                {
                    NotificationManager.Error("Connection request failed to send.");
                    return;
                }
                if (response.Cmd != CMD.S_NEW_CLIENT)
                {
                    NotificationManager.Error("Invalid command received from server.");
                    return;
                }

                //Start listener on new thread if registration successful
                listenerTask = new Task(csListener.StartListener, TaskCreationOptions.LongRunning);
                listenerTask.Start();

                gameData.ServerActive = true;
                gameData.GameString = response.Message;
                gameData.WordCount = response.Arg2;
                initialTimeLimit = gameData.TimeLimit;


                //Flip game to next screen & start the timer
                gameData.UserName = txtbxUserName.Text.Trim();
                ConnectionPanel.Visibility = Visibility.Collapsed;
                ConnectBorder.Visibility = Visibility.Collapsed;
                GamePanel.Visibility = Visibility.Visible;
                GameBorder.Visibility = Visibility.Visible;

                StartCountdown();
            }
        }


        /////////////////
        //SUBMIT GUESS//
        ///////////////
        private void SubmitWordButton_Click(object sender, RoutedEventArgs e)
        {

            Command submit = new Command(CMD.C_GUESS, client.ClientId, txtbxGuess.Text, 0);
            submit = client.SendCommand(submit);

            if (submit == null)
            {
                NotificationManager.Error("Submission failed to send.");
                return;
            }

            if (submit.Cmd == CMD.S_VALID_GUESS)
            {
                gameData.WordCount = submit.Arg2;

                gameData.FeedbackMessage = "Correct Guess!";
                gameData.FeedbackColour = new SolidColorBrush(Colors.Green);
                feedbackTimer.Stop();
                feedbackTimer.Start();
            }
            else if (submit.Cmd == CMD.S_INVALID_GUESS)
            {
                gameData.FeedbackMessage = "Incorrect guess, Try again.";
                gameData.FeedbackColour = new SolidColorBrush(Colors.Red);
                feedbackTimer.Stop();
                feedbackTimer.Start();
            }
            else if (submit.Cmd == CMD.S_PLAY_AGAIN)
            {
                gameData.FeedbackMessage = "Congratulations! You guessed all the words!";
                gameData.FeedbackColour = new SolidColorBrush(Colors.Green);
                countdownTimer.Stop();


                ResetGame();
            }
        }

        ///////////////
        //RESET GAME//
        /////////////
        private void ResetGame()
        {
            bool playAgain = NotificationManager.ConfirmAction("Would you like to play again?", "Play again");

            if (playAgain)
            {
                gameData.FeedbackMessage = string.Empty;

                //Send command
                Command gameReset = new Command(CMD.C_RESTART, client.ClientId, "", gameData.WordCount);
                Command response = client.SendCommand(gameReset);

                if (response == null)
                {
                    NotificationManager.Error("Restart command failed to send.");
                    return;
                }

                if (response.Cmd != CMD.S_RESTART)
                {
                    NotificationManager.Error("Invalid command received from server.");
                    return;
                }

                gameData.WordCount = response.Arg2;
                gameData.GameString = response.Message;
                gameData.TimeLimit = initialTimeLimit;
                StartCountdown();
            }

            else
            {
                Application.Current.Shutdown();
            }
        }


        ///////////
        // TIMER //
        //////////
        private void StartCountdown()
        {
            countdownTimer = new System.Timers.Timer(1000);
            countdownTimer.Elapsed += ElapsedTime;
            countdownTimer.Start();
            TimerTextBlock.Text = gameData.TimeLimit.ToString();
        }
        private void ShutdownTimers()
        {
            if (countdownTimer != null)
            {
                countdownTimer.Stop();
                countdownTimer.Dispose();
                countdownTimer = null;
            }

            if (feedbackTimer != null)
            {
                feedbackTimer.Stop();
                feedbackTimer = null;
            }
        }

        private void ElapsedTime(object sender, ElapsedEventArgs e)
        {
            gameData.TimeLimit--;

            Dispatcher.Invoke(() =>
            {
                if (gameData.TimeLimit >= 0)
                {
                    TimerTextBlock.Text = gameData.TimeLimit.ToString();
                }
                else
                {
                    countdownTimer.Stop();
                    gameData.FeedbackMessage = "Game Over : You ran out of time!";
                    gameData.FeedbackColour = new SolidColorBrush(Colors.Red);

                    Command gameOver = new Command(CMD.C_TIME_UP, client.ClientId, "Time up for player.", 0);
                    Command response = client.SendCommand(gameOver);
                    if (response == null)
                    {
                        NotificationManager.Error("End game command failed to send.");
                        return;
                    }
                    if (response.Cmd != CMD.S_PLAY_AGAIN)
                    {
                        NotificationManager.Error("Expected response code 208 - server sent " + response.Cmd.ToString() +
                            "\nContinuing with end game logic.");
                    }
                    //I mean, even if it's the wrong code
                    //We should just try restarting and if that works then no real harm..
                    ResetGame();
                }
            });

        }

        private void InitializeFeedbackTimer()
        {
            feedbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            feedbackTimer.Tick += FeedbackTimer_Tick;
        }

        private void FeedbackTimer_Tick(object sender, EventArgs e)
        {
            gameData.FeedbackMessage = string.Empty;
            feedbackTimer.Stop();
        }


        ///////////////
        //EXIT LOGIC//
        /////////////


        protected override void OnClosing(CancelEventArgs e)
        {
            if (gameData.ServerActive)
            {
                bool? confirmed = ConfirmQuit();

                if (confirmed == false)
                {
                    e.Cancel = true;
                    return;
                }
            }

            //Will be null if game was never full started
            if (listenerTask != null)
            {
                if (!listenerTask.IsCompleted)
                {
                    //Gracefully shutdown listener thread
                    csListener.ShutdownListener();
                    listenerTask.Wait();
                }
            }
            //If server is active ensure client is deregistered.
            if (gameData.ServerActive)
            {
                Command deregisterClient = new Command(CMD.C_CONFIRM_QUIT, client.ClientId, "Client shutting down.", 0);
                client.SendCommand(deregisterClient);
            }

            //Close down timers
            ShutdownTimers();
        }

        private void EndGameButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private bool? ConfirmQuit()
        {
            //If time left on their game, pause.
            if (gameData.TimeLimit > 0)
            {
                countdownTimer.Stop();
            }
            Command exit = new Command(CMD.C_QUIT, client.ClientId, "", gameData.WordCount);
            Command response = client.SendCommand(exit);

            if (response == null)
            {
                NotificationManager.Error("End game command failed to send.");
                return true;
            }
            if (response.Cmd != CMD.S_CONFIRM_QUIT)
            {
                NotificationManager.Error($"Unexpected server response. Expected: 204, Received: {response.Cmd}.\nContinuing with quit logic.");
            }

            bool? confirmQuit = NotificationManager.ConfirmAction("Are you sure you'd like to quit?", "Exit game");


            if (gameData.TimeLimit > 0 && confirmQuit == false)
            {
                countdownTimer.Start();
            }

            return confirmQuit;
        }
        private void ServerShutdownReceived(string s)
        {
            //String available to write to somewhere
            Thread.Sleep(100);
            gameData.GameString = "Server shutdown.";
            NotificationManager.Info("Server shutdown detected.");
            ShutdownTimers();
            gameData.ServerActive = false;
        }
    }
}
