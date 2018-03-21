using System;
using System.Collections.Generic;

namespace HiSocket
{
    public class HIUtils
    {
        public HIUtils()
        {
        }

        public static byte[] ToByteArray(object source)
        {
            var Formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using (var stream = new System.IO.MemoryStream())
            {
                Formatter.Serialize(stream, source);
                return stream.ToArray();
            }
        }

        public static object ToObject(byte[] source)
        {
            var Formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using (var stream = new System.IO.MemoryStream(source))
            {
                return Formatter.Deserialize(stream);
            }
        }

        //1byte(Action) + 1byte(chann) + 1byte(ID) + byte(data)
        public static byte[] JoinHeaderBytes(byte action , byte chann , byte[] ids, byte[] data){
            int offset = 4;
            byte[] newArray = new byte[data.Length + offset];
            data.CopyTo(newArray, offset);
            newArray[0] = action;
            newArray[1] = chann;
            newArray[2] = ids[0];
            newArray[3] = ids[1];
            return newArray;
        }

        //1byte(Action) + 1byte(chann) + 1byte(ID) + byte(data)
        public static byte[] SplitHeaderBytes(byte[] data){
            int offset = 4;
            var newArray = new byte[data.Length - offset];
            Array.Copy(data, offset , newArray, 0, data.Length - offset);
            return newArray;
        }

    }
}
