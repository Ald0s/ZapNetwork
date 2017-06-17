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

        private StreamWriter writer = null;
        private StreamReader reader = null;

        private bool bShouldRead = false;

        private Queue<byte[]> sendQueue;

        public delegate void DataRead_Delegate(CNetStream stream, byte[] buffer, int num_read);
        public event DataRead_Delegate DataReceived;

        public delegate void ShouldClose_Delegate(CNetStream stream, NetStreamClose_e reason);
        public event ShouldClose_Delegate ShouldClose;

        public CNetStream(TcpClient _client)
            : base("netstream") {
            parent = _client;
            sendQueue = new Queue<byte[]>();
            netStream = _client.GetStream();

            reader = new StreamReader(netStream);
            writer = new StreamWriter(netStream);
            
            bShouldRead = true;
        }

        public void Start() {
            BeginReading();
        }

        public void WriteBuffer(byte[] buffer) {
            try {
                string base64 = Convert.ToBase64String(buffer);
                writer.WriteLine(base64);
                writer.Flush();
            } catch (IOException) {
                ShouldClose_Internal(NetStreamClose_e.EndOfStream);
            }
        }

        private void BeginReading() {
            try {

                while (bShouldRead) {
                    string base64 = reader.ReadLine();

                    if (base64 == null) {
                        ShouldClose_Internal(NetStreamClose_e.Corrupt);
                        break;
                    }

                    byte[] result = Convert.FromBase64String(base64);
                    if (DataReceived != null) {
                        DataReceived(this, result, result.Length);
                    }
                }
            } catch (IOException) {
                ShouldClose_Internal(NetStreamClose_e.EndOfStream);
            } catch (Exception e) {
                ExceptionSummary(e);
                ShouldClose_Internal(NetStreamClose_e.Unknown);
            }
        }

        private void ShouldClose_Internal(NetStreamClose_e reason) {
            if (ShouldClose != null) {
                ShouldClose(this, reason);
            }
        }

        public void Shutdown(NetStreamClose_e reason) {
            if(netStream != null)
                netStream.Close();

            if (writer != null)
                writer.Close();

            if (reader != null)
                reader.Close();

            bShouldRead = false;
        }
    }
}
