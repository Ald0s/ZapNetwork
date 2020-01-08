using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ZapNetwork;
using ZapNetwork.Client;
using ZapNetwork.Server;
using ZapNetwork.Shared;

namespace ZapTest {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("1: server <port>" + Environment.NewLine + "2: client <ip> <port>");

            string cmd = Console.ReadLine();
            ProcessCmd(cmd);
            Console.ReadLine();
        }

        private static void ProcessCmd(string command) {
            string[] frags = command.Split(' ');

            switch (frags[0]) {
                case "server":
                    Server(int.Parse(frags[1]));
                    break;

                case "client":
                    Client(frags[1], int.Parse(frags[2]));
                    break;
            }
        }

        private static void Server(int port) {
            ServerCfg cfg = new ServerCfg("Test Server", "Welcome to my server!", "", port, 4555, 32, true);
            CServer server = new CServer(cfg);

            server.StartServer();
        }

        private static void Client(string ip, int port) {
            ClientCfg cfg = new ClientCfg(ip, port, "");
            CClient client = new CClient(cfg, "aldos", 100);
            client.Connect();
        }
    }
}
