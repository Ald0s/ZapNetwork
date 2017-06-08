/*
== Zap Networking ==
A simple TCP based networking library, that can be applied to a range of applications.
Still a work in progress.

By Alden Viljoen
https://github.com/ald0s
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZapNetwork.Shared {
    public enum StatusType_e {
        Success,
        Information,
        Failure,

        Other = -1
    }

    public class CObjectBase {
        private string sSource = null;

        public CObjectBase(string _name) {
            this.sSource = _name;
        }

        protected void PositiveStatus(string text) {
            Print(StatusType_e.Success, text);
        }

        protected void NeutralStatus(string text) {
            Print(StatusType_e.Information, text);
        }

        protected void NegativeStatus(string text) {
            Print(StatusType_e.Failure, text);
        }

        protected void ExceptionSummary(Exception e) {
            StringBuilder exc = new StringBuilder();
            exc.AppendLine("=== EXCEPTION HANDLER ===");
            exc.AppendLine("Unhandled exc occurred in '" + sSource + "'");
            exc.AppendLine("Exception Type: " + e.GetType().Name);
            exc.AppendLine("Exception Message: " + e.Message);
            exc.AppendLine("Exception Stack Trace: " + e.StackTrace);

            Console.Write(exc.ToString());
        }

        protected void Print(StatusType_e type, string sMessageText) {
            ConsoleColor oldColour = Console.ForegroundColor;

            Console.Write("[");

            switch (type) {
                case StatusType_e.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;

                case StatusType_e.Information:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;

                case StatusType_e.Failure:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }

            Console.Write(sSource);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] ");

            Console.WriteLine(sMessageText);
            Console.ForegroundColor = oldColour;
        }
    }
}
