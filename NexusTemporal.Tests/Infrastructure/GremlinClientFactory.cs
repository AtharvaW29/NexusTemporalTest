using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphBinary;

public static class GremlinClientFactory
{
    public static GremlinClient Create()
    {
        var server = new GremlinServer(
            hostname: "localhost",
            port: 8182,
            enableSsl: false
        );

        var serializer = new GraphBinaryMessageSerializer();

        return new GremlinClient(
            server,
            serializer
        );
    }
}