using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

// FILE : NotificationManager.cs
// PROJECT : Assignment 5 - TCP/IP
// PROGRAMMERS : Josh Horsley | Josh Rice
// FIRST VERSION : 2024-11-16
// DESCRIPTION :
// This file contains the NotificationManager class. The Notification class provides methods for displaying user notifications in our Word Finder application.
// It has methods to handle both error messages and confirmation dialogs. 

namespace ClientSide
{
    internal static class NotificationManager
    {
        // Method: Error
        // Details: This method displays an error message box to the user.
        // Parameters:
        //  string message - The Error message to be displayed in the MessageBox.
        //  string title - The title of the MessageBox (defaulted to Error).
        // Returns:
        //  void.

        internal static void Error(string message, string title = "Error") 
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Method: Info
        // Details: This method displays an informational message box to the user.
        // Parameters:
        //  string message - The information message to be displayed in the MessageBox.
        //  string title - The title of the MessageBox (defaulted to Server Shutdown).
        // Returns:
        //  void.

        internal static void Info(string message, string title = "Server Shutdown")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Method: PlayAgain
        // Details: This method displays a confirmation dialog to the user with "Yes" and "No" options.
        //          It is used to ask the user if they would like to play again and returns a boolean value based on their choice.
        // Parameters:
        //  string message - The message to be displayed in the dialog box.
        //  string title - The title of the MessageBox (defaulted to Play Again?).
        // Returns:
        //  bool - Returns true if the user selects Yes and false if the user selects No.

        internal static bool ConfirmAction(string message, string title = "Play Again?")
        {
            MessageBoxResult result = MessageBox.Show(message,title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
    }
}
