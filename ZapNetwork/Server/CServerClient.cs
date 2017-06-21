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

using System.Threading;
using System.Net;
using System.Net.Sockets;
using ZapNetwork.Shared;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ZapNetwork.Server {
    public class CServerClient : CClientShared {
        public bool Connected { get { return IsConnected(); } }
        public bool IsLocalhost { get { return this.bLocalhost; } }
        public string ConnectionInfo { get { return this.sConnectionInfo; } }
        public int ClientID { get { return this.iClientID; } }

        private CServerMain main;

        public delegate void Authenticated_Delegate(CServerClient client);
        public event Authenticated_Delegate Authenticated;

        public delegate void Disconnected_Delegate(CServerClient client, string reason);
        public event Disconnected_Delegate Disconnected;

        public delegate void NetMessageReceived_Delegate(CServerClient client, CNetMessage message);
        public event NetMessageReceived_Delegate NetMessageReceived;

        private string sConnectionInfo = null;
        private bool bAuthenticated = false;
        private bool bLocalhost = false;

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
            sConnectionInfo = this.client.Client.RemoteEndPoint.ToString();

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
                    if (bAuthenticated)
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
            if (NetMessageReceived != null && bAuthenticated) {
                NetMessageReceived(this, msg);
            }
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

            bAuthenticated = true;
            PositiveStatus("I've been authenticated!");

            CNetMessage setup = new CNetMessage("setup");
            setup.WriteInt(iClientID);
            setup.WriteString(main.Configuration.ServerName);
            setup.WriteString(main.Configuration.ServerDescription);

            SendSetupInfo(setup);
        }

        // Override this to send some default information.
        protected virtual void SendSetupInfo(CNetMessage setup) {
            SendNetMessage(setup);
        }

        public override void Shutdown(string reason) {
            bAuthenticated = false;
            base.Shutdown(reason);

            if (this.Disconnected != null) {
                Disconnected(this, reason);
            }
        }

        protected virtual void SetupReceived(CNetMessage setup) {

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
            if (client == null && !bLocalhost)
                return false;
            else if (client == null && bLocalhost)
                return true;
            else
                return client.Connected && bValid;
        }

        // Set this client as localhost.
        private void SetLocalhost() {
            sConnectionInfo = "localhost";
            iClientID = 0;

            bLocalhost = true;
            bAuthenticated = true;

            Start(false);
        }
    }
}
