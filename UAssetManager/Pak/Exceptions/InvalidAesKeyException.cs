using UAssetManager.Pak.Readers;

namespace UAssetManager.Pak.Exceptions
{
    public class InvalidAesKeyException : ParserException
    {
        public InvalidAesKeyException(string? message = null, Exception? innerException = null) : base(message, innerException) { }
        
        public InvalidAesKeyException(FArchive reader, string? message = null, Exception? innerException = null) : base(reader, message, innerException) { }
    }
}