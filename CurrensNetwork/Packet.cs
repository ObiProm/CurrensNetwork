using System;

namespace CurrensNetwork
{
    [Serializable]
    internal class Packet
    {
        public string Name; 
        public object[] Params;
    }
}
