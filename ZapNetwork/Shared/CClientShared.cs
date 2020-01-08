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
        protected CUdpShared udpShared = null;
        private BinSerializer serializer = null;

        public delegate bool HandleNetMsgDelegate(CNetMessage msg);
        // Return true if HANDLED.
        // Subclass will not be called.
        public event HandleNetMsgDelegate HandleNetMsg;

        public delegate bool HandlePacketDelegate(CNetMessage msg);
        // Return true if HANDLED.
        // Subclass will not be called.
        public event HandlePacketDelegate HandlePacket;

        protected bool bValid = false;
        private bool bShutdown = false;
        private bool bLocalhost = false;
        public CClientShared(string _name, bool localhost)
            : base(_name) {
            this.bLocalhost = localhost;

            if(!localhost)
                this.serializer = new BinSerializer();
        }

        // When client is connected, call Start() to initialise the actual processes.
        protected void Start(bool use_thread) {
            bValid = true;

            if (bLocalhost)
                return;

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

            // If we are on loopback, just send the message back to us.
            if(client == null && bLocalhost) {
                HandleNetMessageInternal(msg);
                return;
            }


            byte[] buffer = serializer.SerializeObject(msg);
            if (buffer == null)
                return;

            netStream.WriteBuffer(buffer);
        }

        public virtual void SendPacket(CNetMessage msg) {
            if (!bValid || udpShared == null)
                return;

            // If we are on loopback, just send the message back to us.
            if (client == null && bLocalhost) {
                HandlePacketInternal(msg);
                return;
            }

            byte[] buffer = serializer.SerializeObject(msg);
            if (buffer == null)
                return;

            if((buffer.Length + 4) > CUdpShared.RecvSz) {
                NegativeStatus("Failed to send message of name " + msg.sMessageName + "! It's too big for packet.");
                return;
            }

            try {
                byte[] real_buffer = new byte[256];

                byte[] sz = BitConverter.GetBytes(buffer.Length);
                Buffer.BlockCopy(sz, 0, real_buffer, 0, sz.Length);
                Buffer.BlockCopy(buffer, 0, real_buffer, sz.Length, buffer.Length);

                udpShared.SendData(real_buffer, real_buffer.Length);
            }catch(Exception e) {
                ExceptionSummary(e);
            }
        }

        /// <summary>
        /// DO NOT OVERRIDE UNLESS YOU'RE GOOD!!!
        /// For handling internal net messages.
        /// </summary>
        protected virtual void HandleNetMessageInternal(CNetMessage msg) {
            
        }

        /// <summary>
        /// DO NOT OVERRIDE UNLESS YOU'RE GOOD!!!
        /// For handling internal packets.
        /// </summary>
        protected virtual void HandlePacketInternal(CNetMessage msg) {

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

        private void NetStream_DataReceived(CNetStream stream, byte[] buffer, int sz) {
            try {
                CNetMessage msg = serializer.DeserializeObject<CNetMessage>(buffer, sz);
                if (msg != null) {
                    // Attempt to handle this message without subclass interference,
                    // using this we can build things like replicator/chunking systems.
                    if (HandleNetMsg != null && HandleNetMsg(msg))
                        return;

                    HandleNetMessageInternal(msg);
                    return;
                }

                NegativeStatus("FATAL! Received data is NOT a packet. Dropping packet...");
            } catch (Exception e) {
                ExceptionSummary(e);
            }
        }

        protected int CalculateChallengeResult(int input) {
            return input = (input + 5) * 10 / 2;
        }

        protected int SetupUdpInternal() {
            if (client == null || !client.Connected)
                return -1;

            this.udpShared = new CUdpShared((IPEndPoint)client.Client.RemoteEndPoint);
            udpShared.RawDataReceived += UdpShared_RawDataReceived;

            return udpShared.SetupListener();
        }

        private void UdpShared_RawDataReceived(EndPoint ep, byte[] buffer, int sz) {
            try {
                CNetMessage msg = serializer.DeserializeObject<CNetMessage>(buffer, sz);
                if(msg != null) {
                    // Attempt to handle this message without subclass interference,
                    // using this we can build things like replicator/chunking systems.
                    if (HandlePacket != null && HandlePacket(msg))
                        return;

                    HandlePacketInternal(msg);
                    return;
                }

                NegativeStatus("FATAL! Received data is NOT a packet. Dropping packet...");
            } catch (Exception e) {
                ExceptionSummary(e);
            }
        }

        public virtual void Shutdown(string reason) {
            if (bShutdown)
                return;

            bShutdown = true;

            if (udpShared != null)
                udpShared.Close();

            if (netStream != null)
                netStream.Shutdown(NetStreamClose_e.Shutdown);

            if (client != null)
                client.Close();
        }
    }
}
