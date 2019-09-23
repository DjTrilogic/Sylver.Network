﻿namespace Sylver.Network.Server
{
    /// <summary>
    /// Provides properties to configure a new <see cref="NetServer{T}"/> instance.
    /// </summary>
    public sealed class NetServerConfiguration
    {
        /// <summary>
        /// Gets the default maximum of connections in accept queue.
        /// </summary>
        public const int DefaultBacklog = 50;

        /// <summary>
        /// Gets the default maximum number of connections simultanously.
        /// </summary>
        public const int DefaultMaximumNumberOfConnections = 100;

        /// <summary>
        /// Gets the default client buffer allocated size.
        /// </summary>
        public const int DefaultClientBufferSize = 128;

        /// <summary>
        /// Gets the server's listening host.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Gets the server's listening port.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the maximum of pending connections queue.
        /// </summary>
        public int Backlog { get; }

        /// <summary>
        /// Gets the maximum number of connections allowed on the server.
        /// </summary>
        public int MaximumNumberOfConnections { get; }

        /// <summary>
        /// Gets the handled client buffer size.
        /// </summary>
        public int ClientBufferSize { get; }

        /// <summary>
        /// Creates a new basic <see cref="NetServerConfiguration"/> instance.
        /// </summary>
        /// <param name="host">Server host address.</param>
        /// <param name="port">Server listening port.</param>
        /// <param name="backlog">Maximum of connections in accept queue.</param>
        /// <param name="maximumNumberOfConnections">Maximum number of connections on the server.</param>
        /// <param name="clientBufferSize">Allocated memory buffer per clients.</param>
        public NetServerConfiguration(string host, int port, int backlog = DefaultBacklog, int maximumNumberOfConnections = DefaultMaximumNumberOfConnections, int clientBufferSize = DefaultClientBufferSize)
        {
            this.Host = host;
            this.Port = port;
            this.Backlog = backlog;
            this.MaximumNumberOfConnections = maximumNumberOfConnections;
            this.ClientBufferSize = clientBufferSize;
        }
    }
}
