using UAssetManager.Pak.Readers;

namespace UAssetManager.Pak.Exceptions;
public class UnknownCompressionMethodException : ParserException
{
    public UnknownCompressionMethodException(string? message = null, Exception? innerException = null) : base(message, innerException) { }
    
    public UnknownCompressionMethodException(FArchive reader, string? message = null, Exception? innerException = null) : base(reader, message, innerException) { }
}