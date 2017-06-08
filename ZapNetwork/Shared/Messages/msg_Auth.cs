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

namespace ZapNetwork.Shared.Messages {
    [Serializable]
    public class msg_Auth : CNetMessage {
        public string Password { get { return this.sPassword; } }

        public int ServerNumber { get; set; }
        public int ClientResult { get; set; }

        private string sPassword;

        public msg_Auth(int _server, string _pass = "")
            : base("authentication") {
            this.ServerNumber = _server;
            this.sPassword = _pass;
        }

        public void CalculateClientResult() {
            ClientResult = (ServerNumber + 5) * 10 / 2;
        }
    }
}
