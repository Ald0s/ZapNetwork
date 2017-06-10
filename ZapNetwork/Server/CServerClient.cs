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
using ZapNetwork.Shared.Messages;

namespace ZapNetwork.Server {
    public class CServerClient : CClientShared {
        public bool Connected { get { return client.Connected && bValid; } }
        public string ConnectionInfo { get { return this.sConnectionInfo; } }
        public int ClientID { get { return this.iClientID; } }

        private CServerMain main;

        public delegate void Authenticated_Delegate(CServerClient client);
        public event Authenticated_Delegate Authenticated;

        public delegate void NetMessageReceived_Delegate(CServerClient client, CNetMessage message);
        public event NetMessageReceived_Delegate NetMessageReceived;

        private string sConnectionInfo = null;
        private bool bAuthenticated = false;

        private int iClientID = -1;
        private int iServerNumber = -1;

        public CServerClient(CServerMain _main, TcpClient _client, bool use_thread = true)
            : base(_client.Client.RemoteEndPoint.ToString()) {
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
            SendNetMessage(new msg_Kick(reason));
        }
        
        protected override void HandleNetMessageInternal(CNetMessage msg) {
            Random r = new Random();

            switch (msg.MessageName) {
                case "authentication":
                    if (bAuthenticated)
                        return;

                    // Very weak challenge system - this is NOT advised for large scale applications,
                    // unless maybe very strong encryption and binary obfuscation is used to obscure the challenge algorithm.
                    if (iServerNumber < 0) {
                        iServerNumber = r.Next(2 ^ 6, 2 ^ 14);
                        ((msg_Auth)msg).ServerNumber = iServerNumber;

                        SendNetMessage(msg);
                    } else {
                        VerifyClient((msg_Auth)msg);
                    }
                    return;
            }

            // Only allow the net message to end-user if we're authenticated (security.)
            if (NetMessageReceived != null && bAuthenticated) {
                NetMessageReceived(this, msg);
            }
        }

        private void VerifyClient(msg_Auth auth) {
            // First, a password check.
            if (main.Configuration.IsUsingPassword() && (auth.Password != main.Configuration.Password)) {
                Kick("Bad password.");
                return;
            }

            int client = auth.ClientResult;

            msg_Auth ours = new msg_Auth(iServerNumber);
            ours.CalculateClientResult();

            if (client != ours.ClientResult) {
                Kick("Bad challenge.");
                return;
            }

            bAuthenticated = true;

            if (Authenticated != null) {
                Authenticated(this);
            }

            PositiveStatus("I've been authenticated!");
            SendNetMessage(new msg_Auth(-48879, main.Configuration.ServerDescription));
        }

        protected override void NetStream_CloseStream(CNetStream stream, NetStreamClose_e reason) {
            main.HandleDisconnection(this, reason.ToString());
            base.NetStream_CloseStream(stream, reason);
        }

        public override void Shutdown(string reason) {
            bAuthenticated = false;

            base.Shutdown(reason);
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
    }
}
