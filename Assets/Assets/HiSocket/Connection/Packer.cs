using System;
using System.Collections.Generic;

namespace HiSocket
{
    public class Packer : IPackage
    {
        public void Unpack(IByteArray reader, Queue<byte[]> receiveQueue)
        {
            //get head length or id
            //while (reader.Length >= 1)
            //{
            //    byte bodyLength = reader.Read(1)[0];

            //    if (reader.Length >= bodyLength)
            //    {
            //        var body = reader.Read(bodyLength);
            //        receiveQueue.Enqueue(body);
            //    }
            //}

            receiveQueue.Enqueue(reader.Read(reader.Length));
        }
        public void Pack(Queue<byte[]> sendQueue, IByteArray writer)
        {
            //add head length or id
            //byte[] head = new Byte[1] { 4 };
            //writer.Write(head, head.Length);
            //var body = sendQueue.Dequeue();

            var test = sendQueue.Dequeue();
            writer.Write(test, test.Length);
        }
    }
}
    