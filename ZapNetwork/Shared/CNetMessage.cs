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

namespace ZapNetwork.Shared {
    [Serializable]
    public class CNetMessage {
        public string MessageName { get { return this.sName; } }
        public int Size { get { return this.item_count; } }

        private string sName;

        private object[] items;
        private int item_count = 0;

        public CNetMessage(string _name) {
            this.sName = _name;
        }

        public virtual void Complete() {

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

        private object ReadObject() {
            List<object> objects = items.ToList();
            object o = items[0];
            objects.RemoveAt(0);

            items = objects.ToArray();
            item_count = objects.Count;

            return o;
        }
        #endregion
    }
}
