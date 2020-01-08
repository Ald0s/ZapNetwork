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

using System.Threading;
using System.Net;
using System.Net.Sockets;
using ZapNetwork.Shared;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ZapNetwork.Server {
    public class CServerClient : CClientShared {
        public bool Authed { get { return this.authenticated; } }
        public bool Connected { get { return IsConnected(); } }
        public bool IsLocalhost { get { return this.localhost; } }
        public bool UdpActive { get { return this.udpActive; } }
        public string ConnectionInfo { get { return this.connectionInfo; } }

        public int ClientID { get { return this.iClientID; } }

        private CServerMain main;

        public delegate void Authenticated_Delegate(CServerClient client);
        public event Authenticated_Delegate Authenticated;

        public delegate void Disconnected_Delegate(CServerClient client, string reason);
        public event Disconnected_Delegate Disconnected;

        public delegate void NetMessageReceived_Delegate(CServerClient client, CNetMessage message);
        public event NetMessageReceived_Delegate NetMessageReceived;

        public delegate void UdpNowActiveDelegate();
        public event UdpNowActiveDelegate UdpNowActive;

        private string connectionInfo = null;
        private bool authenticated = false;
        private bool localhost = false;
        private bool udpActive = false;

        private int iClientID = -1;
        private int iServerNumber = -1;

        public CServerClient(CServerMain _main, TcpClient _client, bool use_thread = true)
            : base((_client == null) ? "localhost" : _client.Client.RemoteEndPoint.ToString(), (_main == null && _client == null) ? true : false) {
            if(_main == null || _client == null) {
                SetLocalhost();
                return;
            }

            this.main = _main;
            this.client = _client;
            connectionInfo = this.client.Client.RemoteEndPoint.ToString();

            CreateID();
            Start(use_thread);
        }

        public void Kick(string reason = null) {
            if (reason == null || reason.Length == 0)
                reason = "No reason given.";

            NegativeStatus("I've been kicked. (" + reason + ")");

            CNetMessage kick = new CNetMessage("kick");
            kick.WriteString(reason);
            SendNetMessage(kick);

            Shutdown(reason);
        }

        protected override void HandleNetMessageInternal(CNetMessage msg) {
            switch (msg.GetMessageName()) {
                case "authentication":
                    if (authenticated)
                        return;

                    // Very weak challenge system - this is NOT advised for large scale applications,
                    // unless maybe very strong encryption and binary obfuscation is used to obscure the challenge algorithm.
                    if (iServerNumber < 0) {
                        SendChallenge();
                    } else {
                        VerifyClient(msg);
                    }
                    return;

                case "setup":
                    SetupReceived(msg);
                    if (Authenticated != null) {
                        Authenticated(this);
                    }
                    return;
            }

            // Only allow the net message to end-user if we're authenticated (security.)
            if (NetMessageReceived != null && authenticated) {
                NetMessageReceived(this, msg);
            }
        }

        protected override void HandlePacketInternal(CNetMessage msg) {
            base.HandlePacketInternal(msg);
        }

        private void VerifyClient(CNetMessage auth) {
            string password = auth.ReadString();
            int client = auth.ReadInt();

            // First, a password check.
            if (main.Configuration.IsUsingPassword() && (password != main.Configuration.Password)) {
                Kick("Bad password.");
                return;
            }

            int our_result = CalculateChallengeResult(iServerNumber);
            if (client != our_result) {
                Kick("Bad challenge.");
                return;
            }

            authenticated = true;
            PositiveStatus("I've been authenticated!");

            CNetMessage setup = new CNetMessage("setup");

            setup.WriteInt(iClientID);
            setup.WriteString(main.Configuration.ServerName);
            setup.WriteString(main.Configuration.ServerDescription);

            int port_to_write = -1;
            if (main.Configuration.UdpEnabled) {
                int port = SetupUdpInternal();
                PositiveStatus("Udp requested; listener initialized on " + port);

                if (port > 0)
                    port_to_write = port;
            }
            setup.WriteInt(port_to_write);

            SendSetupInfo(setup);
        }

        // Override this to send some default information.
        protected virtual void SendSetupInfo(CNetMessage setup) {
            SendNetMessage(setup);
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

            if (this.Disconnected != null) {
                Disconnected(this, reason);
            }
        }

        protected virtual void SetupReceived(CNetMessage setup) {
            int client_udp_port = setup.ReadInt();
            if (client_udp_port > 0) {
                udpShared.SetupSender(client_udp_port);

                PositiveStatus("Client accepts udp request, we're sending to " + client_udp_port);

                udpActive = true;

                if (UdpNowActive != null)
                    UdpNowActive();
            }

            SendFullyConnected(new CNetMessage("connected"));
        }

        protected virtual void SendFullyConnected(CNetMessage msg) {
            this.SendNetMessage(msg);
        }

        private void SendChallenge() {
            Random r = new Random();
            iServerNumber = r.Next(2 ^ 6, 2 ^ 14);

            CNetMessage auth = new CNetMessage("authentication");
            auth.WriteInt(iServerNumber);

            SendNetMessage(auth);
        }

        private void CreateID() {
            Random r = new Random();
            while (true) {
                int id = r.Next(1, int.MaxValue - 1);
                if (main.GetClient(id) != null)
                    continue;

                iClientID = id;
                break;
            }
        }

        private bool IsConnected() {
            if (client == null && !localhost)
                return false;
            else if (client == null && localhost)
                return true;
            else
                return client.Connected && bValid;
        }

        // Set this client as localhost.
        private void SetLocalhost() {
            connectionInfo = "localhost";
            iClientID = 0;

            localhost = true;
            authenticated = true;
            udpActive = main.Configuration.UdpEnabled;

            Start(false);
        }
    }
}
