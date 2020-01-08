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
using System.Net.Sockets;
using System.Text;

namespace ZapNetwork.Shared {
    public class CUdpShared : CObjectBase {
        public static int RecvSz { get { return 256; } }

        private Socket sendSock = null;
        private Socket recvSock = null;

        private IPEndPoint tcpClient;
        private int sendToPort = -1;

        public delegate void RawDataReceivedDelegate(EndPoint ep, byte[] buffer, int sz);
        public event RawDataReceivedDelegate RawDataReceived;

        public CUdpShared(IPEndPoint _tcp)
            : base("UdpShared") {
            this.tcpClient = _tcp;
        }

        // Starts randomly on a port and returns the port number.
        public int SetupListener() {
            recvSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            recvSock.Bind(new IPEndPoint(IPAddress.Any, 0));

            Receive();

            return ((IPEndPoint)recvSock.LocalEndPoint).Port;
        }

        public void SetupSender(int portSendTo) {
            this.sendToPort = portSendTo;

            if(sendToPort < 0) {
                NegativeStatus("Failed to setup sender, other side's port is apparently less than 0.");
                return;
            }

            sendSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public void SendData(byte[] data, int len) {
            sendSock.BeginSendTo(data, 0, len, SocketFlags.None, new IPEndPoint(tcpClient.Address, sendToPort), new AsyncCallback(SendDataComplete), null);
        }

        public void Close() {
            try {
                if (sendSock != null) {
                    sendSock.Close();
                    sendSock = null;
                }
            } catch (Exception e) {
                ExceptionSummary(e);
            }

            try {
                if (recvSock != null) {
                    recvSock.Close();
                    recvSock = null;
                }
            } catch (Exception e) {
                ExceptionSummary(e);
            }
        }

        private void Receive() {
            try {
                byte[] buffer = new byte[RecvSz];
                EndPoint foreign = new IPEndPoint(IPAddress.Any, 0);

                if (!recvSock.IsBound /* || ShouldShutDown */) {
                    NegativeStatus("Failed to receive, recvSock is not bound ...");
                    Close();

                    return;
                }

                recvSock.BeginReceiveFrom(buffer, 0, RecvSz, SocketFlags.None, ref foreign, new AsyncCallback(DataReceived), new object[] { buffer, foreign });
            } catch (ObjectDisposedException) {
                recvSock.Close();
            }
        }

        private void DataReceived(IAsyncResult ar) {
            byte[] buffer = (byte[])((object[])ar.AsyncState)[0];
            EndPoint foreign = (EndPoint)((object[])ar.AsyncState)[1];

            int len = -1;
            try {
                len = recvSock.EndReceiveFrom(ar, ref foreign);
            } catch (ObjectDisposedException) {
                return;
            } catch (NullReferenceException) {
                return;
            }

            if (((IPEndPoint)foreign).Address == IPAddress.Any)
                return;

            if(!((IPEndPoint)foreign).Address.Equals(tcpClient.Address)) {
                NegativeStatus("Dropped packet received from unrecognised IP! (expected " + tcpClient.Address.ToString() + ", received from " + (((IPEndPoint)foreign).Address.ToString() + ")"));
                return;
            }

            if(buffer == null || foreign == null) {
                NegativeStatus("Received null buffer or null foreign client ...");
                return;
            }

            int length = BitConverter.ToInt32(buffer, 0);
            byte[] result = new byte[length];

            Buffer.BlockCopy(buffer, 4, result, 0, length);

            if (RawDataReceived != null)
                RawDataReceived(foreign, result, length);

            Receive();
        }

        private void SendDataComplete(IAsyncResult ar) {
            try {
                sendSock.EndSendTo(ar);
            } catch (ObjectDisposedException) {
                return;
            }
        }
    }
}
