using System.IO.Compression;
using System.IO;

namespace ZG
{
    public interface ICompressionFactory
    {
        public static readonly ICompressionFactory Default = new GZipFactory();

        Stream Compress(Stream stream);

        Stream Decompress(Stream stream);
    }

    public class GZipFactory : ICompressionFactory
    {
        public Stream Compress(Stream stream)
        {
            return new GZipStream(stream, CompressionMode.Compress);
        }

        public Stream Decompress(Stream stream)
        {
            return new GZipStream(stream, CompressionMode.Decompress);
        }
    }
}