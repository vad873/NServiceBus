namespace NServiceBus.Config
{
    /// <summary>
    /// Possible data formats for encryption keys.
    /// </summary>
    public enum KeyFormat
    {
        /// <summary>
        /// The key data format is a ascii sequence that is converted to a byte[]
        /// </summary>
        Ascii = 0,
        /// <summary>
        /// The key data format is a base64 encoded byte[]
        /// </summary>
        Base64 = 1
    }
}