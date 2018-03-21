using System;
using System.Collections.Generic;

namespace HiSocket
{
    [Serializable]
    public class HINetworkItem
    {
        public int ID = 0;
        public string Name = "";
        public bool IsEnable = false;
    }

    [Serializable]
    public class HINetworkData
    {
        public Dictionary<string, HINetworkItem> ndada = new Dictionary<string, HINetworkItem>();
    }
}