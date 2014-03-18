using System.IO;
using NMSD.Cronus.DomainModelling;
using NMSD.Protoreg;

namespace NMSD.Cronus.Pipeline
{
    public static class EndpointMessageExtensions
    {
        public static EndpointMessage AsEndpointMessage(this IMessage message, ProtoregSerializer serializer)
        {
            byte[] body;
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, message);
                body = stream.ToArray();
            }
            return new EndpointMessage(body);
        }
    }
}
