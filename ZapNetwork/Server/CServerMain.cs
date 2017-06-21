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

using System.Net.Sockets;
using ZapNetwork.Shared;
using System.Net;
using System.Threading;

namespace ZapNetwork.Server {
    public class CServerMain : CObjectBase {
        public bool Listening { get { return shouldAccept; } }
        public ServerCfg Configuration { get { return this.config; } }
        public List<CServerClient> Clients { get { return this.clients; } }

        private ServerCfg config = null;

        List<CServerClient> clients = null;
        private TcpListener listener = null;

        private Thread thrThread = null;

        private bool bUseThread = true;
        private bool shouldAccept = false;

        public delegate void ServerStarted_Delegate();
        public event ServerStarted_Delegate ServerStarted;

        public delegate void ServerStopped_Delegate(string reason);
        public event ServerStopped_Delegate ServerStopped;

        public CServerMain(ServerCfg cfg, bool use_thread = true)
            : base("servermain") {
            config = cfg;
            bUseThread = use_thread;

            this.clients = new List<CServerClient>();
        }

        // Supply configuration here if its ever-changing.
        public void StartServer(ServerCfg cfg = null) {
            if (cfg != null)
                config = cfg;

            this.shouldAccept = true;

            if (bUseThread) {
                this.thrThread = new Thread(() => StartServer_Internal());
                this.thrThread.Start();
            } else {
                StartServer_Internal();
            }
        }

        private void StartServer_Internal() {
            if (!CreateServer()) {
                ShutdownServer("An error occurred while launching the server!");
                return;
            }

            AcceptClients();
        }

        private bool CreateServer() {
            try {
                int port = config.PortNumber;
                if (port < 0 || port > 65535) {
                    NegativeStatus("FATAL! Failed to create server; port number is invalid (" + port + ")");
                    return false;
                }

                listener = new TcpListener(IPAddress.Any, port);
                listener.Start(64);
                PositiveStatus("'" + config.ServerName + "' started on port " + port + "!");

                if (ServerStarted != null)
                    ServerStarted();

                return true;
            } catch (Exception e) {
                ExceptionSummary(e);
                return false;
            }
        }

        private void AcceptClients() {
            try {
                listener.BeginAcceptTcpClient(new AsyncCallback(AcceptClient), null);
            }catch(Exception e) {
                ExceptionSummary(e);
            }
        }

        private void AcceptClient(IAsyncResult ar) {
            try {
                TcpClient client = listener.EndAcceptTcpClient(ar);
                string remote_addr = client.Client.RemoteEndPoint.ToString();

                if (!client.Connected) {
                    NegativeStatus("Refused connection from " + remote_addr + ": not connected");
                    return;
                }

                PositiveStatus(remote_addr + " connected!");
                NewClient(this, client);

                AcceptClients();
            } catch (ObjectDisposedException) {
                return;
            } catch (Exception e) {
                ExceptionSummary(e);
            }
        }

        /// <summary>
        /// Override this function to instantiate your own derived type of CServerClient.
        /// DO NOT CALL BASE!!!!!!! Instead, call UserConnected() with your new type.
        /// </summary>
        protected virtual void NewClient(CServerMain main, TcpClient client) {
            UserConnected(new CServerClient(this, client));
        }

        protected void UserConnected(CServerClient client) {
            if (!client.Connected) {
                NegativeStatus("Refused connection from " + client.ConnectionInfo + ": couldn't build client!");
                client.Kick("Unknown serverside error.");
                return;
            }
            client.Disconnected += HandleDisconnection;

            clients.Add(client);
        }

        public CServerClient GetClient(int id) {
            foreach(CServerClient client in clients) {
                if (client.ClientID == id)
                    return client;
            }
            return null;
        }

        public void HandleDisconnection(CServerClient client, string reason) {
            clients.Remove(client);
        }

        public void ShutdownServer(string reason = "Server shutting down.") {
            try {
                shouldAccept = false;

                if (listener != null)
                    listener.Stop();

                for (int i = 0; i < clients.Count; i++) {
                    clients[i].Shutdown(reason);
                }

                clients.Clear();

                NegativeStatus(reason);

                if (ServerStopped != null)
                    ServerStopped(reason);
            } catch(Exception e) {
                ExceptionSummary(e);
            }
        }

        public int UserCount() {
            return clients == null ? 0 : clients.Count;
        }
    }
}
