﻿using Sylver.Network.Common;
using Sylver.Network.Data;
using Sylver.Network.Infrastructure;
using Sylver.Network.Server.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Sylver.Network.Server
{
    /// <summary>
    /// Provides an abstraction to manage a TCP server.
    /// </summary>
    /// <typeparam name="TClient">The client type that the server will manage.</typeparam>
    public class NetServer<TClient> : NetConnection, INetServer
        where TClient : class, INetServerClient
    {
        private readonly ConcurrentDictionary<Guid, TClient> _clients;
        private readonly NetServerClientFactory<TClient> _clientFactory;
        private readonly NetServerAcceptor _acceptor;
        private readonly INetReceiver _receiver;
        private readonly INetSender _sender;

        private IPacketProcessor _packetProcessor;
        private NetServerConfiguration _serverConfiguration;

        /// <inheritdoc />
        public bool IsRunning { get; private set; }

        /// <inheritdoc />
        public IPacketProcessor PacketProcessor
        {
            get => this._packetProcessor;
            set
            {
                if (this.IsRunning)
                    throw new InvalidOperationException("Cannot update packet processor when server is running.");
                this._packetProcessor = value;
                this._receiver.SetPacketProcessor(this._packetProcessor);
            }
        }

        /// <inheritdoc />
        public NetServerConfiguration ServerConfiguration
        {
            get => this._serverConfiguration;
            protected set
            {
                if (this.IsRunning)
                    throw new InvalidOperationException("Cannot update configuration when server is running.");
                this._serverConfiguration = value;
            }
        }

        /// <summary>
        /// Gets the list of connected clients.
        /// </summary>
        protected virtual IEnumerable<TClient> Clients => this._clients.Values;

        /// <summary>
        /// Creates a new <see cref="NetServer{TUser}"/> instance.
        /// </summary>
        protected NetServer()
            : this(new NetServerConfiguration(default, default))
        {
        }

        /// <summary>
        /// Creates a new <see cref="NetServer{TUser}"/> instance.
        /// </summary>
        public NetServer(NetServerConfiguration configuration)
        {
            this.ServerConfiguration = configuration;
            this._packetProcessor = new NetPacketProcessor();
            this._clientFactory = new NetServerClientFactory<TClient>();
            this._clients = new ConcurrentDictionary<Guid, TClient>();
            this._acceptor = new NetServerAcceptor(this);
            this._acceptor.OnClientAccepted += this.OnClientAccepted;

            this._sender = new NetServerSender();
            this._receiver = new NetServerReceiver(this);
        }

        /// <inheritdoc />
        public void Start()
        {
            if (this.IsRunning)
                throw new InvalidOperationException("Server is already running.");

            this.OnBeforeStart();

            this.Socket = new NetSocket(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            this.Socket.Bind(NetHelper.CreateIpEndPoint(this.ServerConfiguration.Host, this.ServerConfiguration.Port));
            this.Socket.Listen(this.ServerConfiguration.Backlog);

            this.IsRunning = true;
            this._sender.Start();
            this._acceptor.StartAccept();
            this.OnAfterStart();
        }

        /// <inheritdoc />
        public void Stop()
        {
            this.OnBeforeStop();
            this._sender.Stop();

            if (this.Socket != null)
            {
                this.Socket.Dispose();
                this.Socket = null;
            }

            this.IsRunning = false;
            this.OnAfterStop();
        }

        /// <inheritdoc />
        public void DisconnectClient(Guid clientId)
        {
            if (!this._clients.TryRemove(clientId, out TClient client))
            {
                // TODO: error; cannot find client by id.
                return;
            }

            this.OnClientDisconnected(client);
            client.Dispose();
        }

        /// <inheritdoc />
        public void SendTo(INetConnection connection, INetPacketStream packet)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            this._sender.Send(new NetMessageData(connection, packet.Buffer));
        }

        /// <inheritdoc />
        public void SendTo(IEnumerable<INetConnection> connections, INetPacketStream packet)
        {
            if (connections == null)
            {
                throw new ArgumentNullException(nameof(connections));
            }

            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            byte[] messageData = packet.Buffer;

            foreach (INetConnection connection in connections)
            {
                this._sender.Send(new NetMessageData(connection, messageData));
            }
        }

        /// <inheritdoc />
        public void SendToAll(INetPacketStream packet) => this.SendTo(this._clients.Values, packet);

        /// <summary>
        /// Executes the child business logic before starting the server.
        /// </summary>
        protected virtual void OnBeforeStart() { }

        /// <summary>
        /// Executes the child business logic after the server starts.
        /// </summary>
        protected virtual void OnAfterStart() { }

        /// <summary>
        /// Executes the child business logic before stoping the server.
        /// </summary>
        protected virtual void OnBeforeStop() { }

        /// <summary>
        /// Executes the child business logic after the server stops.
        /// </summary>
        protected virtual void OnAfterStop() { }

        /// <summary>
        /// Triggers a child logic when a new client connects to the server.
        /// </summary>
        /// <param name="client"></param>
        protected virtual void OnClientConnected(TClient client) { }

        /// <summary>
        /// Triggers a child logic when a client disconnects from the server.
        /// </summary>
        /// <param name="client"></param>
        protected virtual void OnClientDisconnected(TClient client) { }

        /// <summary>
        /// Fired when a client is accepted to the server.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Accepted client socket async event arguments.</param>
        private void OnClientAccepted(object sender, SocketAsyncEventArgs e)
        {
            TClient newClient = this._clientFactory.CreateClient(e.AcceptSocket, this);

            if (!this._clients.TryAdd(newClient.Id, newClient))
            {
                // TODO: send error.
            }

            this.OnClientConnected(newClient);
            this._receiver.Start(newClient);
        }
    }
}
