namespace NServiceBus.Encryption
{
    interface IEncryptionServiceInternal : IEncryptionService
    {
        /// <summary>
        /// Decrypts the given EncryptedValue object returning the source string using the specified key.
        /// </summary>
        string Decrypt(EncryptedValue encryptedValue, string keyIdentifier);

        /// <summary>
        /// Returns the encryption key identifier for the key used when calling <see cref="IEncryptionService.Encrypt(string)"/>.
        /// </summary>
        string EncryptionKeyIdentifier { get; }
    }
}