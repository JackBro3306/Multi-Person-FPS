using System;
using System.IO;

namespace LSGameServ.Protobuf {
    public static class ProtoTransfer {
        public static byte[] Serialize<T>(T data) {
            // 使用了using自动释放资源，如果在using中被return或者异常终止，也会继续执行dispose函数
            using (MemoryStream ms = new MemoryStream()) {
                ProtoBuf.Serializer.Serialize<T>(ms, data);
                return ms.ToArray();
            }
        }

        public static T Deserialize<T>(GameMessage buffer) where T : class, ProtoBuf.IExtensible {
            return Deserialize<T>(buffer.data);
        }

        public static T Deserialize<T>(byte[] data) where T : class, ProtoBuf.IExtensible {
            if (data == null) {
                return null;
            }
            using (MemoryStream ms = new MemoryStream(data)) {
                T t = ProtoBuf.Serializer.Deserialize<T>(ms);
                return t;
            }
        }
        
        public static GameMessage Deserialize(byte[] readbuff, int start, int length) {
            
            byte[] bytes = new byte[length];
            Array.Copy(readbuff,start, bytes, 0,length);
            GameMessage message = Deserialize<GameMessage>(bytes);

            return message;
        }
    }
}
