/*
== Zap Networking ==
A simple TCP based networking library, that can be applied to a range of applications.
Still a work in progress.

By Alden Viljoen
https://github.com/ald0s
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace ZapNetwork.Shared {
    [Serializable]
    // CRUCIAL:
    // Data MUST be read in the exact same order it was written.
    // Not adhereing to this standard will break things terribly!
    public class CNetMessage {
        public string sMessageName;
        public object[] items;
        public int item_count = 0;

        [NonSerialized]
        private int idx = 0;

        public CNetMessage() {

        }

        public string GetMessageName() {
            return sMessageName;
        }

        public CNetMessage(string _name) {
            this.sMessageName = _name;
        }

        // Override this to 'reconstruct' the object on the receiving end.
        // For example; call Read operations into existing properties.
        protected virtual void Complete() {

        }

        #region Write
        public void WriteString(string str) {
            AddObject(str);
        }

        public void WriteInt(int integer) {
            AddObject(integer);
        }

        public void WriteUInt(uint uinteger) {
            AddObject(uinteger);
        }

        public void WriteBool(bool b) {
            AddObject(b);
        }

        public void WriteDouble(double d) {
            AddObject(d);
        }

        public void WriteByte(byte b) {
            AddObject(b);
        }

        public void WriteColour(Color color) {
            WriteByte(color.A);
            WriteByte(color.R);
            WriteByte(color.G);
            WriteByte(color.B);
        }

        private void AddObject(object obj) {
            item_count++;
            Array.Resize(ref items, item_count);

            items[item_count - 1] = obj;
        }
        #endregion

        #region Read
        public string ReadString() {
            return (string)ReadObject();
        }

        public int ReadInt() {
            return (int)ReadObject();
        }

        public uint ReadUInt() {
            return (uint)ReadObject();
        }

        public bool ReadBool() {
            return (bool)ReadObject();
        }

        public double ReadDouble() {
            return (double)ReadObject();
        }

        public byte ReadByte() {
            return (byte)ReadObject();
        }

        public Color ReadColour() {
            byte[] col = new byte[4];
            for (int i = 0; i < 4; i++) {
                col[i] = ReadByte();
            }

            return Color.FromArgb(col[0], col[1], col[2], col[3]);
        }

        private object ReadObject() {
            // Nothing more to read.
            if (idx == item_count)
                return null;

            List<object> objects = items.ToList();
            object o = items[idx];
            idx++;

            return o;
        }
        #endregion
    }
}
