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

using System.Net.Sockets;
using System.IO;

namespace ZapNetwork.Shared {
    public enum NetStreamClose_e {
        Unknown,
        EndOfStream,
        Corrupt,
        Shutdown
    }

    public class CNetStream : CObjectBase {
        private TcpClient parent = null;
        private NetworkStream netStream = null;

        private bool bShouldRead = false;

        public delegate void DataRead_Delegate(CNetStream stream, byte[] buffer, int num_read);
        public event DataRead_Delegate DataReceived;

        public delegate void CloseStream_Delegate(CNetStream stream, NetStreamClose_e reason);
        public event CloseStream_Delegate CloseStream;

        public CNetStream(TcpClient _client)
            : base("netstream") {
            parent = _client;
            netStream = _client.GetStream();

            bShouldRead = true;
        }

        public void Start() {
            BeginReading();
        }

        public void WriteBuffer(byte[] buffer, int len) {
            try {
                byte[] btLength = BitConverter.GetBytes(buffer.Length);

                byte[] btBuffer = new byte[btLength.Length + len];
                Buffer.BlockCopy(btLength, 0, btBuffer, 0, btLength.Length);
                Buffer.BlockCopy(buffer, 0, btBuffer, btLength.Length, len);

                netStream.BeginWrite(btBuffer, 0, btBuffer.Length, new AsyncCallback(WriteBuffer_Callback), null);
            } catch (IOException) {
                Shutdown(NetStreamClose_e.EndOfStream);
            }
        }

        private void WriteBuffer_Callback(IAsyncResult result) {
            netStream.EndWrite(result);
        }

        private void BeginReading() {
            try {
                while (bShouldRead) {
                    byte[] sz = new byte[sizeof(int)];
                    int len = netStream.Read(sz, 0, sizeof(int));

                    if (len == 0) {
                        Shutdown(NetStreamClose_e.EndOfStream);
                        return;
                    }

                    if (len < sizeof(int) || len > sizeof(int)) {
                        Shutdown(NetStreamClose_e.Corrupt);
                        return;
                    }

                    int result_len = BitConverter.ToInt32(sz, 0);
                    byte[] result = new byte[result_len];

                    len = netStream.Read(result, 0, result_len);
                    if (len == 0) {
                        Shutdown(NetStreamClose_e.EndOfStream);
                        return;
                    }

                    if (DataReceived != null) {
                        DataReceived(this, result, result_len);
                    }
                }
            } catch (IOException) {
                Shutdown(NetStreamClose_e.EndOfStream);
            } catch (Exception e) {
                ExceptionSummary(e);
                Shutdown(NetStreamClose_e.Unknown);

                return;
            }
        }

        public void Shutdown(NetStreamClose_e reason) {
            bShouldRead = false;

            if (CloseStream != null) {
                CloseStream(this, reason);
            }
        }
    }
}
