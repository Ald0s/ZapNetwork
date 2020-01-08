/*
== Zap Networking ==
A simple TCP based networking library, that can be applied to a range of applications.
Still a work in progress.

By Alden Viljoen
https://github.com/ald0s
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace ZapNetwork.Shared {
    public class BinSerializer : CObjectBase {
        public BinSerializer()
            : base("BinSerializer") {

        }

        public byte[] SerializeObject(object obj) {
            try {
                byte[] buffer = null;

                using (MemoryStream ms = new MemoryStream()) {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, obj);

                    ms.Flush();
                    buffer = ms.ToArray();

                    ms.Close();
                }

                if (buffer == null || buffer.Length == 0) {
                    NegativeStatus("FATAL! failed to serialize object!");
                    return null;
                }

                return buffer;
            } catch (SerializationException a) {
                NegativeStatus("FATAL! Is your net message type marked with the Serializable attribute? '" + obj.GetType().Name + "'");
            } catch (Exception e) {
                ExceptionSummary(e);
            }

            return null;
        }

        public T DeserializeObject<T>(byte[] data, int length) {
            try {
                object o = null;
                using (MemoryStream ms = new MemoryStream(data, 0, length)) {
                    ms.Seek(0, SeekOrigin.Begin);

                    BinaryFormatter bf = new BinaryFormatter();
                    o = (object)bf.Deserialize(ms);
                }

                if (o != null && (o.GetType() == typeof(T) || o.GetType().IsSubclassOf(typeof(T)))) {
                    // Just an addition for specifically NetMessage.
                    if (o.GetType() == typeof(CNetMessage) || o.GetType().IsSubclassOf(typeof(CNetMessage)))
                        ((CNetMessage)o).Complete();

                    return (T)o;
                }

            } catch (Exception e) {
                ExceptionSummary(e);
            }

            return default(T);
        }
    }
}
