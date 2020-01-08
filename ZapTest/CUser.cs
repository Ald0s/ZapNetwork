/*
YOUR CUSTOM TYPE TO DERIVE FROM CServerClient

This is the class that will represent a user (serverside.)
Extend the functionality here to add things such as usernames, health, ranks etc.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Sockets;
using ZapNetwork.Server;
using System.Drawing;
using ZapNetwork.Shared;

namespace ZapTest {
    public class CUser : CServerClient {
        private string sUsername;
        private int iHealth;

        public CUser(CServerMain _server, TcpClient _client)
            :base(_server, _client, true) {

            // Subscribe to these events in your custom type, then react here.
            this.Authenticated += CUser_Authenticated;
            this.UdpNowActive += CUser_UdpNowActive;
            this.NetMessageReceived += CUser_NetMessageReceived;
        }

        private void CUser_UdpNowActive() {
            CNetMessage msg = new CNetMessage("test");

            SendPacket(msg);
        }

        private void CUser_Authenticated(CServerClient client) {
            
        }

        private void CUser_NetMessageReceived(CServerClient client, ZapNetwork.Shared.CNetMessage message) {
            CUser user = (CUser)client; // Cast to your type.

            switch (message.GetMessageName()) {
                case "setinfo":
                    // Read the intended information.
                    string name = message.ReadString();
                    int health = message.ReadInt();
                    Color col = message.ReadColour();

                    // Examples.
                    user.SetUsername(name);
                    user.SetHealth(health);

                    Console.WriteLine("Received color: " + col.ToString());

                    WelcomeUser(user);
                    break;
            }
        }

        private void WelcomeUser(CUser user) {
            string describer = "";
            if (iHealth < 50) {
                describer = "bad!";
            } else if (iHealth > 50 && iHealth < 100) {
                describer = "ok.";
            } else {
                describer = "good!";
            }

            Console.WriteLine("Hello, " + sUsername + "! Your health is " + describer);
        }

        public void SetUsername(string _username) {
            this.sUsername = _username;
        }

        public void SetHealth(int _health) {
            this.iHealth = _health;
        }
    }
}
