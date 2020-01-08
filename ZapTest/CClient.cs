using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ZapNetwork;
using ZapNetwork.Client;
using ZapNetwork.Shared;

namespace ZapTest {
    public class CClient : CClientMain {
        private string sUsername = "";
        private int iHealth = -1;

        public CClient(ClientCfg _cfg, string _name, int _health)
            :base(_cfg) {
            this.Authenticated += CClient_Authenticated;

            this.sUsername = _name;
            this.iHealth = _health;
        }

        protected override void HandlePacketInternal(CNetMessage msg) {
            Console.WriteLine("Packet received " + msg.sMessageName.ToString());

            base.HandlePacketInternal(msg);
        }

        private void CClient_Authenticated(CClientMain client) {
            // Now that our client is authenticated, we can send custom messages.
            CNetMessage setinfo = new CNetMessage("setinfo");
            setinfo.WriteString(sUsername);
            setinfo.WriteInt(iHealth);
            setinfo.WriteColour(Color.Red);

            SendNetMessage(setinfo);
        }
    }
}
