using System;

namespace CurrensNetwork
{
    /// <summary>
    /// The attribute marks methods that will be called through methods of the RPC group of the <see cref="Networking"/> class
    /// </summary>
    public class RPC : Attribute
    {
        public RPC() { }
    }
}
