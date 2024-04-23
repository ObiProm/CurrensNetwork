using System.Collections.Generic;
using System.Net.Sockets;
using System;
using System.Text;
using System.Linq;

namespace CurrensNetwork
{
    /// <summary>
    /// This class was created for easy-calling RPC.
    /// It also stores some data
    /// </summary>
    public static class Networking
    {
        /// <summary>
        /// UniqueID, host always has ID 1
        /// </summary>
        public static ulong UniqueID { get; private set; }
        /// <summary>
        /// Contains data boolean is host
        /// </summary>
        public static bool IsHost { get; private set; }
        /// <summary>
        /// Contains all connectedClients
        /// </summary>
        /// <returns>Dictionary or null(if client)</returns>
        internal static Dictionary<ulong, TcpClient> ConnectedClients = new Dictionary<ulong, TcpClient>();
        internal static TcpListener Host { get; set; }
        internal static NetworkStream ClientStream { get; set; }

        internal static void SetIsHost(bool data){ IsHost = data; }
        internal static void SetID(ulong Id) { UniqueID = Id; }
        /// <summary>
        /// Calls a specific method with given arguments at all connected clients
        /// </summary>
        /// <param name="packet">The RPC packet to be sent.</param>
        public static void Rpc(string method, params object[] args)
        {
            if (method == null) throw new ArgumentNullException("Methodname", "Method name can't be null!");
            if (IsHost)
            {
                foreach (var stream in ConnectedClients.Values)
                {
                    Packet packet = new Packet() { Name = method, Params = args.ToList() };
                    var _stream = stream.GetStream();
                    _stream.Write(Encoding.ASCII.GetBytes(packet.Pack()), 0, packet.Pack().Length);
                    _stream.Flush();
                }
            }
            else
            {
                Packet packet = new Packet() { Name = method, Params = args.ToList() };
                ClientStream.Write(Encoding.ASCII.GetBytes(packet.Pack()), 0, packet.Pack().Length);
                ClientStream.Flush();
            }
        }
        /// <summary>
        /// Sends an RPC packet to all connected clients
        /// </summary>
        /// <param name="packet">The RPC packet to be sent.</param>
        public static void Rpc(Packet packet)
        {
            if (IsHost)
            {
                foreach (var stream in ConnectedClients.Values)
                {
                    var _stream = stream.GetStream();
                    _stream.Write(Encoding.ASCII.GetBytes(packet.Pack()), 0, packet.Pack().Length);
                    _stream.Flush();
                }
            }
            else
            {
                ClientStream.Write(Encoding.ASCII.GetBytes(packet.Pack()), 0, packet.Pack().Length);
                ClientStream.Flush();
            }
        }
        /// <summary>
        /// Calls a specific method with given arguments at a specific client identified by ID.
        /// </summary>
        /// <param name="ID">The ID of the client.</param>
        /// <param name="method">The method to be called.</param>
        /// <param name="args">The arguments for the method.</param>
        public static void RpcTo(ulong ID, string method, params object[] args)
        {
            if (method == null) throw new ArgumentNullException("Methodname", "Method name can't be null!");
            if (IsHost)
            {
                Packet packet = new Packet() { Name = method, Params = args.ToList(), SendTo = ID };
                var _stream = ConnectedClients[ID].GetStream();
                _stream.Write(Encoding.ASCII.GetBytes(packet.Pack()), 0, packet.Pack().Length);
                _stream.Flush();
            }
            else
            {
                Packet packet = new Packet() { Name = method, Params = args.ToList(), SendTo = ID };
                ClientStream.Write(Encoding.ASCII.GetBytes(packet.Pack()), 0, packet.Pack().Length);
                ClientStream.Flush();
            }
        }
        /// <summary>
        /// Calls a specific method with given arguments at a specific client identified by ID using a <see cref="Packet"/> object.
        /// </summary>
        /// <param name="ID">The ID of the client.</param>
        /// <param name="packet">The RPC packet containing the method and arguments.</param>
        public static void RpcTo(ulong ID, Packet packet)
        {
            if (IsHost)
            {
                var _stream = ConnectedClients[ID].GetStream();
                _stream.Write(Encoding.ASCII.GetBytes(packet.Pack()), 0, packet.Pack().Length);
                _stream.Flush();
            }
            else
            {
                ClientStream.Write(Encoding.ASCII.GetBytes(packet.Pack()), 0, packet.Pack().Length);
                ClientStream.Flush();
            }
        }

    }
}