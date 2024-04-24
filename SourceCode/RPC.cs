using System;

namespace CurrensNetwork
{
    
    public class RPC : Attribute
    {
        public bool DoLocally { get; }
        /// <summary>
        /// The attribute marks methods that will be called through methods of the RPC group of the <see cref="Networking"/> class
        /// </summary>

        public RPC(bool doLocally = false)
        {
            DoLocally = doLocally;
        }
    }
}
