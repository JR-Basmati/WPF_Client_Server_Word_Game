/*
 * FILE             : GameFile.cs
 * PROJECT          : A05 - TCP/IP
 * PROGRAMMER       : Josh Rice | Josh Horsley
 * FIRST VERSION    : 2024-11-14
 * DESCRIPTION      :
 *  
 *      GameFiles hold a single Text File worth of game info:
 *          - 80 char game string
 *          - # of words to be found
 *          - An array of ValidWords[] representing the words to find
 *      
 *      ValidWord struct is also defined here, which is a single
 *      word paired with a bool to indicate whether it is found/not found
 *      by a player.
 *      
 *      Method Clone() is used to return a copy of this object.
 *      
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A05_Server
{
    //All components of a game's .txt file
    //Array of these are loaded on startup.

    //Valid words are stored with a bool to mark as "Found" by playr.
    internal struct ValidWord
    {
        internal string word;
        internal bool found;

        internal ValidWord(string word, bool found)
        {
            this.word = word;
            this.found = found;
        }
    }

    internal class GameFile
    {
        internal string gameText;
        internal int wordCount;
        internal ValidWord[] words;

        internal GameFile()
        {
            gameText = String.Empty;
            wordCount = 0;
            words = null;
        }
        internal GameFile(string gText, int wc, ValidWord[] vWords)
        {
            gameText = gText;
            wordCount = wc;
            words = vWords;
        }

        internal GameFile Clone()
        {
            ValidWord[] clonedWords = null;
            if(words != null)
            {
                clonedWords = new ValidWord[words.Length];
                for (int i = 0; i < words.Length; i++) {
                    clonedWords[i] = new ValidWord(words[i].word, words[i].found);
                }
            }
            GameFile clone = new GameFile(this.gameText, this.wordCount, clonedWords);

            return clone;
        }
    }
}
