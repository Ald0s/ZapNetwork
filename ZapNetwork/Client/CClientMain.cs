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

using System.Net;
using System.Net.Sockets;
using ZapNetwork.Shared;

namespace ZapNetwork.Client {
    public class CClientMain : CClientShared {
        public bool Connected { get { return client.Connected; } }
        public string ServerName { get { return this.sServerName; } }
        public string ServerDescription { get { return this.sServerDescription; } }
        public int ClientID { get { return this.iClientID; } }

        public ClientCfg Configuration { get { return this.config; } }
        private ClientCfg config;

        public delegate void Authenticated_Delegate(CClientMain client);
        public event Authenticated_Delegate Authenticated;

        public delegate void NetMessageReceived_Delegate(CClientMain client, CNetMessage message);
        public event NetMessageReceived_Delegate NetMessageReceived;

        public delegate void Disconnected_Delegate(string reason);
        public event Disconnected_Delegate Disconnected;

        private string sServerName;
        private string sServerDescription;
        private int iClientID = -1;

        private bool bAuthenticated = false;

        public CClientMain(ClientCfg _cfg)
            :base("localhost-client") {
            this.config = _cfg;
        }

        // Supply config here if its ever-changing.
        public void Connect(ClientCfg cfg = null, bool use_thread = true) {
            if (cfg != null)
                config = cfg;

            if (!ConnectToServer(use_thread)) {
                return;
            }
        }

        // For handling internal net messages.
        // We don't want our end-user to handle these, so return on each clause of the switch-case statement.
        protected override void HandleNetMessageInternal(CNetMessage msg) {
            Random r = new Random();

            switch (msg.GetMessageName()) {
                case "authentication":
                    if (bAuthenticated)
                        return;

                    int server = msg.ReadInt();
                    ReplyChallenge(server);
                    return;

                case "kick_user":
                    string reason = msg.ReadString();
                    Shutdown(reason);
                    return;

                case "setup":
                    iClientID = msg.ReadInt();
                    sServerName = msg.ReadString();
                    sServerDescription = msg.ReadString();

                    ReceivedSetupData(msg);
                    PositiveStatus("We've been authenticated! The server says; " + sServerDescription);
                    bAuthenticated = true;

                    if (Authenticated != null) {
                        Authenticated(this);
                    }
                    
                    SendSetupData(new CNetMessage("setup"));
                    return;
            }
            if (NetMessageReceived != null && bAuthenticated) {
                NetMessageReceived(this, msg);
            }
        }

        private bool ConnectToServer(bool use_thread) {
            try {
                int port = config.PortNumber;
                if (port < 0 || port > 65535) {
                    NegativeStatus("FATAL! Failed to connect to server; port number is invalid (" + port + ")");
                    return false;
                }

                if (!config.IsIPAddressValid()) {
                    NegativeStatus("FATAL! Failed to connect to server; ip address is invalid (" + port + ")");
                    return false;
                }

                this.client = new TcpClient();
                this.client.Connect(new IPEndPoint(config.Target, config.PortNumber));

                if (!Connected) {
                    Shutdown("Failed to properly connect.");
                    return false;
                }

                PositiveStatus("Connected to target server! Waiting for authentication...");

                Start(use_thread);
                CNetMessage auth = new CNetMessage("authentication");
                auth.WriteInt(-1);

                SendNetMessage(auth);

                return true;
            } catch (SocketException) {
                NegativeStatus("Failed to connect to the given host. Is it alive?");
                Shutdown("The foreign host actively refused the connection!");

                return false;
            }
        }

        // Override this to write data to the setup net message.
        // This is received directly after authentication.
        protected virtual void SendSetupData(CNetMessage setup) {
            SendNetMessage(setup);
        }

        protected virtual void ReceivedSetupData(CNetMessage setup) {

        }

        private void ReplyChallenge(int server) {
            int result = CalculateChallengeResult(server);

            CNetMessage reply = new CNetMessage("authentication");
            reply.WriteString(config.Password);
            reply.WriteInt(result);

            SendNetMessage(reply);
        }

        public override void Shutdown(string reason) {
            bAuthenticated = false;
            base.Shutdown(reason);

            if(Disconnected != null) {
                Disconnected(reason);
            }

            iClientID = -1;
            sServerName = null;
            sServerDescription = null;

            NegativeStatus("Disconnected: " + reason);
        }
    }
}
