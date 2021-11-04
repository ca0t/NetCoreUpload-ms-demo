using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization;
namespace WebAPI.Comm
{
    [DataContract]
    public class ResultMessage
    {
        [DataMember]
        public int code;

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public object Data = default;

        [DataMember]
        public string msg;

        public ResultMessage() { }

        public ResultMessage(int status, string message, object content)
        {
            this.code = status;
            this.Data = content;
            this.msg = message;
        }
    }
}
