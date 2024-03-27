using System;
using System.Net;

namespace CurrensNetwork
{
    /// <summary>
    /// Data which CurrensNetwork uses for transpotring
    /// </summary>
    [Serializable]
    public class Packet
    {
        /// <summary>
        /// Name of method
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Args of method
        /// </summary>
        public object[] Params { get; set; }
        /// <summary>
        /// Id of user, which get data
        /// </summary>
        public ulong SendTo = 0;
    }
}
