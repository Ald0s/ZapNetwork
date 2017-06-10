using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ZapNetwork;
using ZapNetwork.Client;

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

        private void CClient_Authenticated(CClientMain client) {
            // Now that our client is authenticated, we can send custom messages.
            msg_SetInfo setinfo = new msg_SetInfo(sUsername, iHealth);
            SendNetMessage(setinfo);
        }
    }
}
