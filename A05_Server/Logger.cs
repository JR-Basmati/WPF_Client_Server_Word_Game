/*
 * FILE             : Logger.cs
 * PROJECT          : A05 - TCP/IP
 * PROGRAMMER       : Josh Rice | Josh Horsley
 * FIRST VERSION    : 2024-11-14
 * DESCRIPTION      :
 *  
 *      This is a static class providing a method for Console-based IO.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A05_Server
{
    static internal class Logger
    {
        static internal void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
