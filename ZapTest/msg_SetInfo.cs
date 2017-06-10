using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ZapNetwork.Shared;

namespace ZapTest {
    [Serializable] // Remember to mark custom types as serializable.
    public class msg_SetInfo : CNetMessage {
        // To avoid binary serialization issues with versioning,
        // this type uses a read/write buffer.

        // Information must be written and read in the SAME order.
        // We can write our info in the constructor.
        public msg_SetInfo(string _name, int _health)
            :base("msg_SetInfo") { // Specify the message name.
            WriteString(_name);
            WriteInt(_health);
        }
    }
}
