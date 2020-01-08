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

namespace ZapNetwork {
    public class ServerCfg {
        public string ServerName { get { return this.serverName; } }
        public string ServerDescription { get { return this.serverDesc; } }
        public string Password { get { return this.password; } }

        public int PortNumber { get { return this.portNumber; } }
        public int SearchPort { get { return this.searchPort; } } // Will be used so that clients can search LAN for our server.
        public int MaxConnections { get { return this.maxConnections; } }

        public bool UdpEnabled { get { return this.enableUdp; } }

        private string serverName;
        private string serverDesc;
        private string password;

        private int portNumber;
        private int searchPort;
        private int maxConnections;

        private bool enableUdp = false;

        public ServerCfg(string _name, string _desc, string _pass, int _port, int _search, int _max_conn, bool _enable_udp) {
            this.serverName = _name;
            this.serverDesc = _desc;
            this.password = _pass;

            this.portNumber = _port;
            this.searchPort = _search;
            this.maxConnections = _max_conn;

            this.enableUdp = _enable_udp;
        }

        public bool IsUsingPassword() {
            return password != null && password != "";
        }
    }
}
