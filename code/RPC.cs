using System;

namespace CurrensNetwork
{
    public class RPC : Attribute
    {
        public RPC() { }
        public RPC(bool DoLocally) { } 
    }
}
