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

        //1byte(ID) + 1byte(chann) + byte(data)
        public static byte[] JoinHeaderBytes(byte ID , byte chann , byte[] data){
            byte[] newArray = new byte[data.Length + 2];
            data.CopyTo(newArray, 2);
            newArray[0] = ID;
            newArray[1] = chann;
            return newArray;
        }

        //1byte(ID) + 1byte(chann) + byte(data)
        public static byte[] SplitHeaderBytes(byte[] data){
            var newArray = new byte[data.Length - 2];
            Array.Copy(data, 2, newArray, 0, data.Length - 2);
            return newArray;
        }

    }
}
