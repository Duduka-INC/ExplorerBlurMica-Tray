namespace ExplorerBlurMicaTray.Core;

public enum EbmError
{
    Unknown,
    ReleaseNotFound,
    UntrustedRelease,
    ChecksumMismatch,
    RegistrationFailed,
    ElevationDeclined,
}

public sealed class EbmException(EbmError error, string message, Exception? inner = null) : Exception(message, inner)
{
    public EbmError Error { get; } = error;
}
