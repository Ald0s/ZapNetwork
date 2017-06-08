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
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace ZapNetwork.Shared {
    public class CClientShared : CObjectBase {
        protected TcpClient client = null;
        protected Thread thrClient = null;
        protected CNetStream netStream = null;

        public CClientShared(string _name)
            : base(_name) {
            
        }

        // When client is connected, call Setup() to initialise the actual processes.
        protected void Start(bool use_thread) {
            this.netStream = new CNetStream(client);
            netStream.DataReceived += NetStream_DataReceived;
            netStream.CloseStream += NetStream_CloseStream;

            if (use_thread) {
                this.thrClient = new Thread(() => netStream.Start());
                this.thrClient.Start();
            } else {
                netStream.Start();
            }
        }

        public virtual void SendNetMessage(CNetMessage msg) {
            try {
                byte[] btOutgoingBuffer = null;

                using (MemoryStream ms = new MemoryStream()) {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, msg);

                    ms.Flush();
                    ms.Close();

                    btOutgoingBuffer = ms.ToArray();
                }

                if (btOutgoingBuffer == null || btOutgoingBuffer.Length == 0) {
                    NegativeStatus("FATAL! Failed to send net message, failed to serialize object!");
                    return;
                }

                netStream.WriteBuffer(btOutgoingBuffer, btOutgoingBuffer.Length);
            } catch (Exception e) {
                ExceptionSummary(e);
            }
        }

        protected virtual void HandleNetMessageInternal(CNetMessage msg) {

        }

        // Reactionary function - not to be called.
        protected virtual void NetStream_CloseStream(CNetStream stream, NetStreamClose_e reason) {
            switch (reason) {
                case NetStreamClose_e.Unknown:
                    NegativeStatus("Disconnect! An unknown error occurred!");
                    break;

                case NetStreamClose_e.EndOfStream:
                    NegativeStatus("Disconnect! End of stream!");
                    break;

                case NetStreamClose_e.Corrupt:
                    NegativeStatus("Disconnect! The stream is corrupt!");
                    break;
            }
        }

        private void NetStream_DataReceived(CNetStream stream, byte[] buffer, int num_read) {
            try {
                object o = null;

                using (MemoryStream ms = new MemoryStream(buffer, 0, num_read)) {
                    BinaryFormatter bf = new BinaryFormatter();
                    o = (object)bf.Deserialize(ms);
                }

                if (o == null || o.GetType().IsSubclassOf(typeof(CNetMessage))) {
                    HandleNetMessageInternal((CNetMessage)o);
                    return;
                }

                NegativeStatus("FATAL! Received data is NOT a net message. Dropping message...");
            } catch (Exception e) {
                ExceptionSummary(e);
            }
        }

        public virtual void Shutdown(string reason) {
            if(netStream != null)
                netStream.Shutdown(NetStreamClose_e.Shutdown);

            if (client != null)
                client.Close();
        }
    }
}
