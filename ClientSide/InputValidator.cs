using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

// FILE : InputValidator.cs
// PROJECT : Assignment 5 - TCP/IP
// PROGRAMMERS : Josh Horsley | Josh Rice
// FIRST VERSION : 2024-11-15
// DESCRIPTION :
// This file contains the InputValidator class. The InputValidator class provides methods to validate user input for the connection screen of the application.
// The validation includes checks for the time limit, port number, IP address and username to make sure they are appropriate values and are correctly formatted.
// If validation fails, error messages are displayed to the user using the NotificationManager class.


namespace ClientSide
{
    internal static class InputValidator
    {
        // Method: ValidateConnectionScreen
        // Details: This method validates the user inputs provided on the connection screen of our Word Finder application.
        //          This method makes sure that the time limit, port number, IP address, and username have appropriate values and are correctly formatted.
        //          If validation fails an error message is displayed.
        // Parameters:
        //  int? timeLimit - The amount of time left in the game (in seconds).
        //  port? - The port number to connect to.
        //  string ipAddress - The IP address of the server.
        //  string username - The username provided by the user.
        // Returns:
        //  A boolean indicating whether the inputs are valid or not.

        internal static bool ValidateConnectionScreen(int? timeLimit, int? port, string ipAddress, string userName)
        {
            var errors = new List<string>();

            //Validate Time Limit
            if (!timeLimit.HasValue)
            {
                errors.Add("Time limit cannot be blank.");
            }
            else if (timeLimit.Value <= 0)
            {
                errors.Add("Time limit must be greater than 0.");
            }

            //Validate Port
            if (port < 1)
            {
                errors.Add("Port must be greater than or equal to 1.");
            }
            else if (port > 65535)
            {
                errors.Add("Port must be less than or equal to 65535.");
            }
            else if (port == null)
            {
                errors.Add("Port cannot be blank.");
            }

            //Validate IP Address
            if (string.IsNullOrEmpty(ipAddress))
            {
                errors.Add("IP address cannot be null or empty.");
            }
            else if (!IPAddress.TryParse(ipAddress, out _))
            {
                errors.Add("Invalid IP address format.");
            }

            //Validate Username
            if (string.IsNullOrEmpty(userName))
            {
                errors.Add("Username cannot be blank.");
            }
            else if (userName.Length == 0 || userName.Length > 50)
            {
                errors.Add("Username must be between 1 and 50 characters long.");
            }

            //Show all errors in a single MessageBox
            if (errors.Count > 0)
            {
                NotificationManager.Error(string.Join(Environment.NewLine, errors));
                return false;
            }

            // All validations passed
            return true;
        }

    }

}
