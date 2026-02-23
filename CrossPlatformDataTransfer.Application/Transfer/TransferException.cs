namespace CrossPlatformDataTransfer.Application.Transfer;

public sealed class TransferException : Exception
{
    public TransferException(string message) : base(message) { }
    public TransferException(string message, Exception inner) : base(message, inner) { }
}
