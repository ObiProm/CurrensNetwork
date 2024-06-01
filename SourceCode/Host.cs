using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CurrensNetwork
{
    /// <summary>
    /// 
    /// </summary>
    public class Host : Server
    {
        public Client Client { get; private set; }
        /// <summary>
        /// Creates a host on the specified port to accept incoming connections.
        /// </summary>
        /// <param name="Port">The port number for hosting the server.</param>
        public void Create(int Port)
        {
            Start(Port);

            Client = new Client();
            Client.Connect("127.0.0.1", Port);

            Networking.Host = this;
            Networking.NetworkState = NetworkState.Host;
            Networking.Server = this as Server;
        }
        /// <summary>
        /// Stops the host and client at the same time
        /// </summary>
        public new void Stop()
        {
            Client.Disconnect();
            (this as Server).Stop();
            Networking.Host = null;
        }
    }
}