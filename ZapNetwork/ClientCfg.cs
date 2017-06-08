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
using System.Net;
using System.Text;
using System.Threading.Tasks;

using ZapNetwork.Shared;

namespace ZapNetwork {
    public class ClientCfg : CObjectBase {
        public IPAddress Target { get { return this.ipAddress; } }
        public int PortNumber { get { return this.iPortNumber; } }

        public string Password { get { return this.sPassword; } }

        private IPAddress ipAddress;
        private int iPortNumber;

        private string sPassword;

        public ClientCfg(IPAddress _target, int _port, string _pass)
            : base("clientcfg") {
            Construct(_target, _port, _pass);
        }

        public ClientCfg(string _ip, int _port, string _pass)
            : base("clientcfg") {
            IPAddress result = null;

            if(!IPAddress.TryParse(_ip, out result)) {
                NegativeStatus("FATAL! Corrupt IP address provided!");
                result = null;
            }

            Construct(result, _port, _pass);
        }

        private void Construct(IPAddress _target, int _port, string _pass) {
            this.ipAddress = _target;
            this.iPortNumber = _port;
            this.sPassword = _pass;
        }

        public bool IsIPAddressValid() {
            return ipAddress != null;
        }
    }
}
