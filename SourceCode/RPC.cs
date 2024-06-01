using System;

namespace CurrensNetwork
{
    
    public class RPC : Attribute
    {
        /// <summary>
        /// The attribute marks methods that will be called through methods of the RPC group of the <see cref="Networking"/> class
        /// </summary>

        public RPC()
        {
        }
    }
}
