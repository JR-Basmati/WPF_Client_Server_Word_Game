/*
 * FILE             : Program.cs
 * PROJECT          : A05 - TCP/IP
 * PROGRAMMER       : Josh Rice | Josh Horsley
 * FIRST VERSION    : 2024-11-14
 * DESCRIPTION      :
 *  
 *      Test harness for A05's Server component.
 *      
 *      Server usage is as follows:
 *          1. GameDataLoader loads App.Config settings & the .txt files used for games
 *          
 *          2. Create a GameEngine to take in those GameFiles
 *
 *          3. Create a ShutdownManager who can later notify clients & shut down server.
 *          
 *          4. Create a Listener to receive TCP/IP requests from clients.
 *              a. Start the Listener on a new thread.
 *              
 *          5. Wait for user to press [Esc] key to shutdown the server.
 *          
 *          
 *          NOTE: Most classes contain a "Log" event which is subscribed to by the logger class.
 *          Current implementation just passes the string to Console.WriteLine()
 */


using System;
using System.Threading.Tasks;
using System.Net.Http;


namespace A05_Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //Load App.Config stuff + GameFiles
            GameDataLoader dataLoader = new GameDataLoader();
            dataLoader.WriteLog += Logger.WriteLine;
            dataLoader.LoadGameData();


            //Create other components
            GameEngine engine = new GameEngine(dataLoader.GameFiles);

            ShutdownManager shutdownManager = new ShutdownManager(engine.GameList, dataLoader.IpAddress, dataLoader.Port);

            Listener list = new Listener(engine, dataLoader.Port, dataLoader.IpAddress);
            
            //Attach Console.WriteLine() to class logging functions
            list.WriteLog += Logger.WriteLine;
            shutdownManager.WriteLog += Logger.WriteLine;
            engine.WriteLog += Logger.WriteLine;


            var client = new HttpClient();
            var text = client.GetByteArrayAsync("https://example.com").Result;

            //Start Listener
            list.StartListener(shutdownManager.Token);
            ////NEW VERSION: Keep task available in order to await properly later.
            //var listenerTask = Task.Run(() => list.StartListener(shutdownManager.Token));


            //Run until user presses [Escape] key
            ConsoleKeyInfo keyPress;
            while (!shutdownManager.ShutdownComplete)
            {
                keyPress = Console.ReadKey(true);
                if (keyPress.Key == ConsoleKey.Escape)
                {
                    shutdownManager.InitiateShutdown("Triggered via console.");
                }
               
            }
            //NEW VERSION: AWAIT
            //await listenerTask;

            Logger.WriteLine("Server shutdown successful, press any key to exit.");
            Console.ReadKey();
        }
    }
}
