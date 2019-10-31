using System;

namespace Sylver.Network.Data.Internal
{
    /// <summary>
    /// Provides methods to parse incoming packets.
    /// </summary>
    internal sealed class NetPacketParser
    {
        /// <summary>
        /// Gets or sets the packet processor.
        /// </summary>
        public IPacketProcessor PacketProcessor { get; set; }

        /// <summary>
        /// Creates a new <see cref="NetPacketParser"/> instance.
        /// </summary>
        /// <param name="packetProcessor">Net packet processor used to parse the incoming data.</param>
        public NetPacketParser(IPacketProcessor packetProcessor)
        {
            this.PacketProcessor = packetProcessor;
        }

        /// <summary>
        /// Parses incoming buffer contai
        /// </summary>
        /// <param name="token">Client token information.</param>
        /// <param name="buffer">Received buffer.</param>
        /// <param name="bytesTransfered">Number of bytes transfered throught the network.</param>
        public void ParseIncomingData(NetDataToken token, byte[] buffer, int bytesTransfered)
        {
            while (token.DataStartOffset < bytesTransfered)
            {
                int headerSize = this.PacketProcessor.HeaderSize;

                if (headerSize == 0)
                {
                    token.HeaderData = Array.Empty<byte>();
                }

                if (token.ReceivedHeaderBytesCount < headerSize)
                {
                    if (token.HeaderData == null)
                        token.HeaderData = new byte[headerSize];

                    int bufferRemainingBytes = bytesTransfered - token.DataStartOffset;
                    int headerRemainingBytes = headerSize - token.ReceivedHeaderBytesCount;
                    int bytesToRead = Math.Min(bufferRemainingBytes, headerRemainingBytes);

                    Buffer.BlockCopy(buffer, token.DataStartOffset, token.HeaderData, token.ReceivedHeaderBytesCount, bytesToRead);

                    token.ReceivedHeaderBytesCount += bytesToRead;
                    token.DataStartOffset += bytesToRead;
                }

                if (token.ReceivedHeaderBytesCount == headerSize && token.HeaderData != null)
                {
                    if (!token.MessageSize.HasValue)
                        token.MessageSize = this.PacketProcessor.GetMessageLength(token.HeaderData, bytesTransfered);
                    if (token.MessageSize.Value < 0)
                        throw new InvalidOperationException("Message size cannot be smaller than zero.");

                    if (token.MessageData == null)
                        token.MessageData = new byte[token.MessageSize.Value];

                    if (token.ReceivedMessageBytesCount < token.MessageSize.Value)
                    {
                        int bufferRemainingBytes = bytesTransfered - token.DataStartOffset;
                        int messageRemainingBytes = token.MessageSize.Value - token.ReceivedMessageBytesCount;
                        int bytesToRead = Math.Min(bufferRemainingBytes, messageRemainingBytes);

                        Buffer.BlockCopy(buffer, token.DataStartOffset, token.MessageData, token.ReceivedMessageBytesCount, bytesToRead);

                        token.ReceivedMessageBytesCount += bytesToRead;
                        token.DataStartOffset += bytesToRead;
                    }
                }

                if (token.IsMessageComplete)
                {
                    break;
                }
            }

            token.DataStartOffset = 0;
        }
    }
}
