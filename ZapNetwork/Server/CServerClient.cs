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

        private CServerMain main;

        public delegate void Authenticated_Delegate(CServerClient client);
        public event Authenticated_Delegate Authenticated;

        public delegate void NetMessageReceived_Delegate(CServerClient client, CNetMessage message);
        public event NetMessageReceived_Delegate NetMessageReceived;

        private bool bValid = false;
        private bool bAuthenticated = false;

        private int iServerNumber = -1;

        public CServerClient(CServerMain _main, TcpClient _client, bool use_thread = true)
            : base(_client.Client.RemoteEndPoint.ToString()) {
            this.main = _main;
            this.client = _client;
            this.bValid = true;

            Start(use_thread);
        }

        public void Kick(string reason = null) {
            if (reason == null || reason.Length == 0)
                reason = "No reason given.";

            SendNetMessage(new msg_Kick(reason));
        }

        // For handling internal net messages.
        // We don't want our end-user to handle these, so return on each clause of the switch-case statement.
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
        }

        public override void Shutdown(string reason) {
            bAuthenticated = false;

            base.Shutdown(reason);
        }
    }
}
