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

using System.Net;
using System.Net.Sockets;
using ZapNetwork.Shared;

namespace ZapNetwork.Client {
    public class CClientMain : CClientShared {
        public bool Connected { get { return (client == null) ? false : client.Connected; } }
        public string ServerName { get { return this.serverName; } }
        public string ServerDescription { get { return this.serverDesc; } }
        public int ClientID { get { return this.clientID; } }

        public ClientCfg Configuration { get { return this.config; } }
        private ClientCfg config;

        public delegate void Authenticated_Delegate(CClientMain client);
        public event Authenticated_Delegate Authenticated;

        public delegate void FullyConnectedDelegate(CClientMain client, CNetMessage msg);
        public event FullyConnectedDelegate FullyConnected;

        public delegate void NetMessageReceived_Delegate(CClientMain client, CNetMessage message);
        public event NetMessageReceived_Delegate NetMessageReceived;

        public delegate void Disconnected_Delegate(string reason);
        public event Disconnected_Delegate Disconnected;

        private string serverName;
        private string serverDesc;
        private int clientID = -1;

        private bool authenticated = false;
        private bool udpActive = false;

        public CClientMain(ClientCfg _cfg)
            :base("localhost-client", false) {
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
                    if (authenticated)
                        return;

                    int server = msg.ReadInt();
                    ReplyChallenge(server);
                    return;

                case "kick_user":
                    string reason = msg.ReadString();
                    Shutdown(reason);
                    return;

                case "setup":
                    clientID = msg.ReadInt();
                    serverName = msg.ReadString();
                    serverDesc = msg.ReadString();
                    int udp_port = msg.ReadInt();

                    int our_listen_port = -1;
                    if (udp_port > 0) {
                        our_listen_port = SetupUdpInternal();

                        PositiveStatus("Udp requested by server; listener initialized on " + our_listen_port + ", we're sending to " + udp_port);
                        udpShared.SetupSender(udp_port);
                    }

                    ReceivedSetupData(msg);
                    PositiveStatus("We've been authenticated! The server says; " + serverDesc);
                    authenticated = true;

                    if (Authenticated != null) {
                        Authenticated(this);
                    }

                    CNetMessage setup = new CNetMessage("setup");
                    SendSetupData(setup);

                    setup.WriteInt(our_listen_port);
                    return;

                case "connected":
                    // User is connected, authenticated and setup.
                    udpActive = true;

                    if (FullyConnected != null)
                        FullyConnected(this, msg);
                    break;
            }

            if (NetMessageReceived != null && authenticated)
                NetMessageReceived(this, msg);
        }

        protected override void HandlePacketInternal(CNetMessage msg) {
            base.HandlePacketInternal(msg);
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

        public override void SendPacket(CNetMessage msg) {
            if (!authenticated || !udpActive)
                return;

            base.SendPacket(msg);
        }

        public override void Shutdown(string reason) {
            authenticated = false;
            udpActive = false;

            base.Shutdown(reason);

            if(Disconnected != null) {
                Disconnected(reason);
            }

            clientID = -1;
            serverName = null;
            serverDesc = null;

            NegativeStatus("Disconnected: " + reason);
        }
    }
}
