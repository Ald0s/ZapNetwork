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
    public class msg_Kick : CNetMessage {
        public string Reason { get { return this.sReason; } }
        private string sReason;

        public msg_Kick(string reason)
            : base("kick_user") {
            this.sReason = reason;
        }
    }
}
