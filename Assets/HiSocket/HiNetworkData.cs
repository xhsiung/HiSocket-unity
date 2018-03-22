using System;
using System.Collections.Generic;

namespace HiSocket
{
    [Serializable]
    public class HiNetworkItem
    {
        public int ID = 0;
        public string Name = "";
        public bool IsEnable = false;
    }

    [Serializable]
    public class HiNetworkData
    {
        public Dictionary<string, HiNetworkItem> ndada = new Dictionary<string, HiNetworkItem>();
    }
}