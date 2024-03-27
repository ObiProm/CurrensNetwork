using System;

namespace CurrensNetwork
{
    public class RPC : Attribute
    {
        /*
         * Не забудь спиздить фишки RPC у годота
         */
        public RPC() { }
        public RPC(bool DoLocally) { } //Не раб, надо чтоб раб. Солнце пока высоко, так что раб должен раб
    }
}
