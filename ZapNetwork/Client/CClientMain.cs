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
using ZapNetwork.Shared.Messages;

namespace ZapNetwork.Client {
    public class CClientMain : CClientShared {
        public bool Connected { get { return client.Connected; } }

        public ClientCfg Configuration { get { return this.config; } }
        private ClientCfg config;

        public delegate void Authenticated_Delegate(CClientMain client);
        public event Authenticated_Delegate Authenticated;

        public delegate void NetMessageReceived_Delegate(CClientMain client, CNetMessage message);
        public event NetMessageReceived_Delegate NetMessageReceived;

        public delegate void Disconnected_Delegate(string reason);
        public event Disconnected_Delegate Disconnected;

        private bool bAuthenticated = false;

        public CClientMain(ClientCfg _cfg)
            :base("localhost-client") {
            this.config = _cfg;
        }

        public void Connect(bool use_thread = true) {
            if (!ConnectToServer(use_thread)) {
                return;
            }
        }

        // For handling internal net messages.
        // We don't want our end-user to handle these, so return on each clause of the switch-case statement.
        protected override void HandleNetMessageInternal(CNetMessage msg) {
            Random r = new Random();

            switch (msg.MessageName) {
                case "authentication":
                    if (((msg_Auth)msg).ServerNumber == -48879) {
                        string desc = ((msg_Auth)msg).Password;
                        PositiveStatus("We've been authenticated! The server says; " + desc);

                        if (Authenticated != null) {
                            Authenticated(this);
                        }
                    } else {
                        if (bAuthenticated)
                            return;

                        ((msg_Auth)msg).CalculateClientResult();
                        SendNetMessage(msg);
                    }
                    return;

                case "kick_user":
                    Shutdown(((msg_Kick)msg).Reason);
                    return;
            }

            if (NetMessageReceived != null && bAuthenticated) {
                NetMessageReceived(this, msg);
            }
        }

        private bool ConnectToServer(bool use_thread) {
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
            this.client.BeginConnect(config.Target, config.PortNumber, new AsyncCallback(Connect_Callback), use_thread);

            return true;
        }

        private void Connect_Callback(IAsyncResult ar) {
            try {
                client.EndConnect(ar);
                if (!Connected) {
                    Shutdown("Failed to properly connect.");
                    return;
                }

                PositiveStatus("Connected to target server! Waiting for authentication...");

                Start((bool)ar.AsyncState);
                SendNetMessage(new msg_Auth(0, config.Password));
            } catch (SocketException) {
                NegativeStatus("Failed to connect to the given host. Is it alive?");
                Shutdown("host-down");
            }
        }

        public override void Shutdown(string reason) {
            bAuthenticated = false;
            base.Shutdown(reason);

            if(Disconnected != null) {
                Disconnected(reason);
            }

            NegativeStatus("Disconnected: " + reason);
        }
    }
}
