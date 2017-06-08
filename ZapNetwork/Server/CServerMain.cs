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

        private ServerCfg config = null;

        List<CServerClient> clients = null;
        private TcpListener listener = null;

        private Thread thrThread = null;

        private bool shouldAccept = false;

        public CServerMain(ServerCfg cfg, bool use_thread = true)
            : base("servermain") {
            config = cfg;
            this.clients = new List<CServerClient>();

            this.shouldAccept = true;

            if (use_thread) {
                this.thrThread = new Thread(() => StartServer());
                this.thrThread.Start();
            } else {
                StartServer();
            }
        }

        private void StartServer() {
            if (!CreateServer()) {
                ShutdownServer();
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

                return true;
            } catch (Exception e) {
                ExceptionSummary(e);
                return false;
            }
        }

        private void AcceptClients() {
            try {
                while (shouldAccept) {
                    TcpClient client = listener.AcceptTcpClient();
                    string remote_addr = client.Client.RemoteEndPoint.ToString();

                    if (!client.Connected) {
                        NegativeStatus("Refused connection from " + remote_addr + ": not connected");
                        return;
                    }

                    PositiveStatus(remote_addr + " connected!");
                    CServerClient user = new CServerClient(this, client);

                    if (!user.Connected) {
                        NegativeStatus("Refused connection from " + remote_addr + ": couldn't build client!");
                        return;
                    }

                    clients.Add(user);
                }
            }catch(Exception e) {
                ExceptionSummary(e);
            }
        }

        public void HandleDisconnection(CServerClient client, string reason) {
            clients.Remove(client);
        }

        public void ShutdownServer() {
            try {
                shouldAccept = false;

                if (listener != null)
                    listener.Stop();

                for (int i = 0; i < clients.Count; i++) {
                    clients[i].Shutdown("Server shutting down.");
                }

                clients.Clear();
            }catch(Exception e) {
                ExceptionSummary(e);
            }
        }

        public int UserCount() {
            return clients == null ? 0 : clients.Count;
        }
    }
}
