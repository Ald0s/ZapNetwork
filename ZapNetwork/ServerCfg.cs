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

namespace ZapNetwork {
    public class ServerCfg {
        public string ServerName { get { return this.sServerName; } }
        public string ServerDescription { get { return this.sServerDescription; } }
        public string Password { get { return this.sPassword; } }

        public int PortNumber { get { return this.iPortNumber; } }
        public int SearchPort { get { return this.iSearchPort; } } // Will be used so that clients can search LAN for our server.
        public int MaxConnections { get { return this.iMaxConnections; } }

        private string sServerName;
        private string sServerDescription;
        private string sPassword;

        private int iPortNumber;
        private int iSearchPort;
        private int iMaxConnections;

        public ServerCfg(string _name, string _desc, string _pass, int _port, int _search, int _max_conn) {
            this.sServerName = _name;
            this.sServerDescription = _desc;
            this.sPassword = _pass;

            this.iPortNumber = _port;
            this.iSearchPort = _search;
            this.iMaxConnections = _max_conn;
        }

        public bool IsUsingPassword() {
            return sPassword != null && sPassword != "";
        }
    }
}
