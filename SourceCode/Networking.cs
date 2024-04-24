using System.Collections.Generic;
using System.Net.Sockets;
using System;
using System.Text;
using System.Linq;
using System.Reflection;

namespace CurrensNetwork
{
    /// <summary>
    /// This class was created for easy-calling RPC.
    /// It also stores some data
    /// </summary>
    public static class Networking
    {
        /// <summary>
        /// UniqueID of user, host always has ID 1
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

        /// <summary>
        /// Cached client
        /// </summary>
        public static Client Client { get; private set; }
        /// <summary>
        /// Cached host
        /// </summary>
        public static Host Host { get; private set; }
        internal static TcpListener Listener { get; set; }
        internal static NetworkStream ClientStream { get; set; }

        internal static void SetIsHost(bool data) { IsHost = data; }
        internal static void SetID(ulong Id) { UniqueID = Id; }
        internal static void SetHost(Host host) { Host = host; }
        internal static void SetClient(Client client) { Client = client; }
        /// <summary>
        /// Calls a specific method with given arguments at all connected clients
        /// </summary>
        /// <param name="packet">The RPC packet to be sent.</param>
        public static void Rpc(string method, params object[] args)
        {
            if (method == null) throw new ArgumentNullException("Methodname", "Method name can't be null!");
            Packet packet = new Packet() { Name = method, Params = args.ToList() };
            CheckForLocalPerform(packet);
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
        /// Sends an RPC packet to all connected clients
        /// </summary>
        /// <param name="packet">The RPC packet to be sent.</param>
        public static void Rpc(Packet packet)
        {
            CheckForLocalPerform(packet);
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
            Packet packet = new Packet() { Name = method, Params = args.ToList(), SendTo = ID };
            CheckForLocalPerform(packet);
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
        /// <summary>
        /// Calls a specific method with given arguments at a specific client identified by ID using a <see cref="Packet"/> object.
        /// </summary>
        /// <param name="ID">The ID of the client.</param>
        /// <param name="packet">The RPC packet containing the method and arguments.</param>
        public static void RpcTo(ulong ID, Packet packet)
        {
            CheckForLocalPerform(packet);
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

        internal static void InvokeRpcMethod(Packet packet)
        {
            string methodName = packet.Name;
            object[] args = packet.Params.ToArray();
            MethodInfo method = GetMethodByName(methodName, args);

            var classInstance = Activator.CreateInstance(method.DeclaringType);
            method.Invoke(classInstance, args);
           
        }

        internal static MethodInfo GetMethodByName(string methodName, object[] args) 
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                MethodInfo[] methods = assembly.GetTypes()
                    .SelectMany(t => t.GetMethods())
                    .Where(m => m.GetCustomAttributes(typeof(RPC), false).Length > 0)
                    .ToArray();

                foreach (var method in methods)
                    if (method.Name == methodName && method.GetParameters().Length == args.Length)
                        return method;
            }
            return null;
        }

        internal static void CheckForLocalPerform(Packet packet)
        {
            MethodInfo method = GetMethodByName(packet.Name, packet.Params.ToArray());
            var attribute = (RPC)Attribute.GetCustomAttribute(method, typeof(RPC));
            if (attribute.DoLocally)
                InvokeRpcMethod(packet);
        }
    }
}