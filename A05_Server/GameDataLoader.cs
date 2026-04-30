/*
 * FILE             : GameDataLoader.cs
 * PROJECT          : A05 - TCP/IP
 * PROGRAMMER       : Josh Rice | Josh Horsley
 * FIRST VERSION    : 2024-11-14
 * DESCRIPTION      :
 *  
 *      This class loads & validates settings from App.Config:
 *         "IpMode":    <wifi> detects wifi LAN ip
 *                      <home> uses 127.0.0.1
 *                      <manual> uses the value in "IpAddress"
 *                      
 *         "IpAddress": This address is used when IpMode == "manual"
 *         
 *         "Port":      The port to start server on
 *         
 *         "GameFilesDirectory":   The directory holding the .txt files for games
 *         
 *      
 *      Validated game files are stored in an array of type GameFile.
 *      
 *      The list of valid words within a game are stored in an array of ValidWord structs
 *      which store the word along with a boolean to mark it "found" by players.
 *      
 *      Events:
 *      Action<string> WriteLog   -> Sends string with error/info summaries.
 *      
 */
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace A05_Server 
{

    internal class GameDataLoader
    {

        ////////////////
        //DataMembers//
        bool gameDataLoaded;
        private string  ipAddress;              
        private string  gameFilesDirectory;     //Directory containing Text files of proper game format
        private int   port;                   

        private List<GameFile> gameFiles;       //Array holding all loaded game files

        //Properties all READONLY
        internal bool GameDataLoaded
        {
            get { return gameDataLoaded; }
        }
        internal string IpAddress
        {
            get { return ipAddress; }
        }
        internal int Port
        {
            get { return port; }
        }
        internal List<GameFile> GameFiles
        {
            get { return gameFiles; }
        }


        ///////////
        //Events//
        internal event Action<string> WriteLog;
        private void Log(string s)
        {
            if (WriteLog != null)
            {
                WriteLog.Invoke(s);
            }
        }


        /////////////////
        //Constructors//
        internal GameDataLoader()
        {

            gameDataLoaded = false;

            ipAddress = string.Empty;
            port = 0;
            gameFilesDirectory = string.Empty;
            
            gameFiles = null;
        }


        ////////////
        //Methods//

        //Function to load AppSettings from Config file & GameFiles into array
        internal bool LoadGameData()
        {
            try
            {
                //Load and check IpAddress mode/value
                string ipMode = ConfigurationManager.AppSettings["IpMode"];
                string ipAddress = string.Empty;
                switch (ipMode)
                {
                    case "wifi":
                        ipAddress = DetectWifiIp();
                        if (ipAddress == string.Empty)
                        {
                            return false;
                        }
                        else
                        {
                            this.ipAddress = ipAddress;
                        }
                        break;

                    case "home":
                        this.ipAddress = "127.0.0.1";
                        break;

                    case "manual":
                        ipAddress = ConfigurationManager.AppSettings["IpAddress"];
                        if(ipAddress == string.Empty || !IPAddress.TryParse(ipAddress, out IPAddress parsedIp))
                        {
                            Log("ERR|FATAL: Config: Manual IP empty or failed to parse.");
                            return false;
                        }
                        this.ipAddress = parsedIp.ToString();
                        break;
                    
                    default:
                        Log("ERR|FATAL: Config: Invalid IpMode provided via App.Config file.");
                        Log("Valid options: wifi || home || manual");
                        return false;
                }

                //Detect port
                bool portParsed = Int32.TryParse(ConfigurationManager.AppSettings["Port"], out int port);
                if (!portParsed)
                {
                    Log("ERR|FATAL: AppSettings: Port failed to parse into type Int32");
                    return false;
                }
                else
                {
                    this.port = port;
                }

                Log("INFO: App settings loaded successfully.");
                Log("IpAddress: " + this.ipAddress + "  Port: " + this.port.ToString());



                //Load and check for existing gameFilesDirectory
                string gameFilesDirectory = ConfigurationManager.AppSettings["GameFilesDirectory"];

                if (gameFilesDirectory == String.Empty || !Directory.Exists(gameFilesDirectory))
                {
                    Log("ERR|FATAL: AppSettings: Game Files directory did not load, or directory does not exist.");
                    return false;
                }

                //Load game files from directory
                int filesLoaded = ParseGameFiles(gameFilesDirectory);
                //Ensure at least 1 gameFile was loaded
                if (filesLoaded == 0)
                {
                    Log("ERR|FATAL: ParseGameFiles(): No valid game files were loaded.");
                    return false;
                }
            }
            catch (ConfigurationErrorsException e)
            {
                Log("EX|FATAL: " + e.Message);
                return false;
            }

            return (gameDataLoaded = true);

        }

        //Seperated out to allow Try/Catch in main parser to
        //more gracefully handle WARNs
        private string[] GetGameFilePaths(string gameFilesDirectory)
        {
            try
            {
                string[] gameFilePaths = Directory.GetFiles(gameFilesDirectory, "*.txt");
                return gameFilePaths;
            }
            catch (Exception e)
            {
                Log("ERR|FATAL: " + e.Message);
                return null;
            }
        }

        //Parse the .txt files used to represent a "Game" into GameFile objects
        private int ParseGameFiles(string gameFilesDirectory)
        {
            
            //Retrieve all files
            string[] gameFilePaths = GetGameFilePaths(gameFilesDirectory);


            //Temp variables to store stuff
            List<GameFile> temp_GameFileList = new List<GameFile>();
            ValidWord temp_validWord = new ValidWord(String.Empty, false);
            int totalFiles = gameFilePaths.Length;
            int filesLoaded = 0;
           
            foreach (string filePath in gameFilePaths)
            {
                try
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {

                        //If any errors, skip file.

                        // Check that game text is exactly 80 chars
                        string gameText = reader.ReadLine();
                        if (gameText == null || gameText.Length != 80)
                        {
                            Log("ERR|WARN: " + filePath.ToString() + " - Main game text is " 
                                + gameText.Length + " characters instead of 80 as required.");
                            continue;
                        }

                        //Get word count
                        if (!int.TryParse(reader.ReadLine(), out int wordCount))
                        {
                            Log("ERR|WARN: " + filePath.ToString() + 
                                " - Could not parse word count to integer.");
                            continue;
                        }

                        //Read the remaining lines as ValidWords
                        List<ValidWord> words = new List<ValidWord>();
                        string word;
                        while ((word = reader.ReadLine()) != null)
                        {
                            words.Add(new ValidWord(word, false));
                        }

                        //Ensure word count matches the actual number of words
                        if (words.Count != wordCount)
                        {
                            Log("ERR|WARN: " + filePath.ToString() + 
                                " - Actual words do not match indicated count.");
                            continue;
                        }

                        temp_GameFileList.Add(new GameFile(gameText, wordCount, words.ToArray()));
                        filesLoaded++;
                    }
                }
                catch (FileNotFoundException e)
                {
                    Log("EX|WARN: File not found - " + e.Message);
                    continue;
                }
                catch (UnauthorizedAccessException e)
                {
                    Log("EX|WARN: Access exception for file - " + e.Message);
                    continue;
                }
                catch (Exception e)
                {
                    Log("EX|WARN: Error processing file - " + e.Message);
                    continue;
                }
            }

            if (filesLoaded > 0)
            {
                Log("INFO: Successfully loaded " + filesLoaded + "/" + totalFiles + " Game Files.");
                this.gameFiles = temp_GameFileList;
            }

            return filesLoaded;
        }
        
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
                Log("ERR|FATAL: Exception occured while trying to retrive wireless LAN Ip: " + e.Message);
                return string.Empty;
            }
        }

           
    }
}        



