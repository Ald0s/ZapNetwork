using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using ZapNetwork;
using ZapNetwork.Server;

namespace ZapTest {
    public class CServer : CServerMain {
        public CServer(ServerCfg _cfg)
            :base(_cfg, true) {

        }

        protected override void NewClient(CServerMain main, TcpClient client) {
            // Instantiate our custom type, then pass that back to the server to be stored in the List.
            CUser user = new CUser(main, client);
            base.UserConnected(user);
        }
    }
}
