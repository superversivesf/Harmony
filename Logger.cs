using System;

namespace Audio_Convertor
{
    internal class Logger
    {
        private bool quietMode;
        private int spinnerPos;
        //private string spinnerString = "/-\\|";
        private string spinnerString = ".oO0Oo";
        //private string spinnerString = "<^>v";
        //private string spinnerString = "└┘┐┌";
        //private string spinnerString = "▄▀";
        //private string spinnerString = "#■.";
        //private string spinnerString = "+x";
        //private string spinnerString = "1234567890";
        //private string spinnerString = ",.oO0***0Oo.,";
        //private string spinnerString = ",.!|T";


        public Logger(bool quietMode)
        {
            this.quietMode = quietMode;
            this.spinnerPos = 0;
        }

        internal void WriteLine(string v)
        {
            if (!this.quietMode)
            {
                Console.WriteLine(v);
            }
        }

        internal void Write(string v)
        {
            if (!this.quietMode)
            {
                Console.Write(v);
            }
        }

        internal void AdvanceSpinnder()
        {
            Console.Write("\b" + this.spinnerString[this.spinnerPos++ % this.spinnerString.Length]);
        }
    }
}