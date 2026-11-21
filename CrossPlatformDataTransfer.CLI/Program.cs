namespace CrossPlatformDataTransfer.CLI;

internal static class Program
{
    private static int Main()
    {
        Console.Error.WriteLine("The legacy plaintext TCP listener has been removed.");
        Console.Error.WriteLine("Use the Windows desktop application, which verifies the Android TLS session before pairing.");
        return 2;
    }
}
