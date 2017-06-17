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
using System.Runtime.Serialization;
using System.Reflection;
using System.Xml.Serialization;

namespace ZapNetwork.Shared {
    public class CClientShared : CObjectBase {
        protected TcpClient client = null;
        protected Thread thrClient = null;
        protected CNetStream netStream = null;

        protected bool bValid = false;
        private bool bShutdown = false;

        public CClientShared(string _name)
            : base(_name) {
            
        }

        // When client is connected, call Start() to initialise the actual processes.
        protected void Start(bool use_thread) {
            bValid = true;

            this.netStream = new CNetStream(client);
            netStream.DataReceived += NetStream_DataReceived;
            netStream.ShouldClose += NetStream_ShouldClose;

            if (use_thread) {
                this.thrClient = new Thread(() => netStream.Start());
                this.thrClient.Start();
            } else {
                netStream.Start();
            }
        }

        public virtual void SendNetMessage(CNetMessage msg) {
            if (!bValid)
                return;
            
            try {
                byte[] btOutgoingBuffer = null;

                using (MemoryStream ms = new MemoryStream()) {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, msg);

                    ms.Flush();
                    btOutgoingBuffer = ms.ToArray();

                    ms.Close();
                }
                
                if (btOutgoingBuffer == null || btOutgoingBuffer.Length == 0) {
                    NegativeStatus("FATAL! Failed to send net message, failed to serialize object!");
                    return;
                }

                netStream.WriteBuffer(btOutgoingBuffer);
            } catch (SerializationException a) {
                NegativeStatus("FATAL! Is your net message type marked with the Serializable attribute? '" + msg.GetType().Name + "'");
            } catch (Exception e) {
                ExceptionSummary(e);
            }
        }

        /// <summary>
        /// DO NOT OVERRIDE UNLESS YOU'RE GOOD!!!
        /// For handling internal net messages.
        /// </summary>
        protected virtual void HandleNetMessageInternal(CNetMessage msg) {
            
        }

        // Reactionary function - not to be called.
        protected virtual void NetStream_ShouldClose(CNetStream stream, NetStreamClose_e reason) {
            string strReason = null;
            switch (reason) {
                case NetStreamClose_e.Unknown:
                    strReason = "Disconnect! An unknown error occurred!";
                    break;

                case NetStreamClose_e.EndOfStream:
                    strReason = "Disconnect! End of stream!";
                    break;

                case NetStreamClose_e.Corrupt:
                    strReason = "Disconnect! The stream is corrupt!";
                    break;
            }

            bValid = false;
            NegativeStatus(strReason);

            Shutdown(strReason);
        }

        private void NetStream_DataReceived(CNetStream stream, byte[] buffer, int num_read) {
            try {
                object o = null;

                using (MemoryStream ms = new MemoryStream(buffer, 0, num_read)) {
                    ms.Seek(0, SeekOrigin.Begin);

                    BinaryFormatter bf = new BinaryFormatter();
                    o = (object)bf.Deserialize(ms);
                }

                if (o != null && (o.GetType() == typeof(CNetMessage) || o.GetType().IsSubclassOf(typeof(CNetMessage)))) {
                    HandleNetMessageInternal((CNetMessage)o);
                    return;
                }
                
                NegativeStatus("FATAL! Received data is NOT a net message. Dropping message...");
            } catch (Exception e) {
                ExceptionSummary(e);
            }
        }

        protected int CalculateChallengeResult(int input) {
            return input = (input + 5) * 10 / 2;
        }

        public virtual void Shutdown(string reason) {
            if (bShutdown)
                return;

            bShutdown = true;

            if (netStream != null)
                netStream.Shutdown(NetStreamClose_e.Shutdown);

            if (client != null)
                client.Close();
        }
    }
}
