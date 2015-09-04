namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Config;
    using Encryption.Rijndael;
    using NServiceBus.Encryption;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static partial class ConfigureRijndaelEncryptionService
    {
        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        public static void RijndaelEncryptionService(this BusConfiguration config)
        {
            RegisterEncryptionService(config, context =>
            {
                var section = context.Build<Configure>()
                    .Settings
                    .GetConfigSection<RijndaelEncryptionServiceConfig>();
                return ConvertConfigToRijndaelService(section);
            });
        }

        internal static IEncryptionService ConvertConfigToRijndaelService(RijndaelEncryptionServiceConfig section)
        {
            if (section == null)
            {
                throw new Exception("No RijndaelEncryptionServiceConfig defined. Please specify a valid 'RijndaelEncryptionServiceConfig' in your application's configuration file.");
            }
            if (string.IsNullOrWhiteSpace(section.Key))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has an empty 'Key' property.");
            }

            if (!IsValidKey(section.Key, section.KeyFormat))
            {
                throw new Exception("Invalid 'Key' value.");
            }

            var expiredKeys = ExtractExpiredKeysFromConfigSection(section);

            VerifyKeys(expiredKeys, section.KeyFormat);

            var key = ConvertKey(section.Key, section.KeyFormat);
            var keys = expiredKeys.ConvertAll(x => ConvertKey(x, section.KeyFormat));

            var instance = BuildRijndaelEncryptionService(key, keys);

            return instance;
        }

        static byte[] ConvertKey(string keyData, KeyFormat keyFormat)
        {
            switch (keyFormat)
            {
                case KeyFormat.Base64:
                    return Convert.FromBase64String(keyData);
                case KeyFormat.Ascii:
                    return Encoding.ASCII.GetBytes(keyData);
                default:
                    throw new InvalidOperationException("Key format is unsupported.");
            }
        }

        internal static List<string> ExtractExpiredKeysFromConfigSection(RijndaelEncryptionServiceConfig section)
        {
            if (section.ExpiredKeys == null)
            {
                return new List<string>();
            }
            var encryptionKeys = section.ExpiredKeys
                .Cast<RijndaelExpiredKey>()
                .Select(x => x.Key)
                .ToList();
            if (encryptionKeys.Any(string.IsNullOrWhiteSpace))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has a 'ExpiredKeys' property defined however some keys have no data.");
            }
            if (encryptionKeys.Any(x => x == section.Key))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has a 'Key' that is also defined inside the 'ExpiredKeys'.");
            }

            if (encryptionKeys.Count != encryptionKeys.Distinct().Count())
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has overlapping ExpiredKeys defined. Please ensure that no keys overlap in the 'ExpiredKeys' property.");
            }

            return encryptionKeys;
        }

        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        public static void RijndaelEncryptionService(this BusConfiguration config, string encryptionKey, List<string> expiredKeys = null)
        {
            RijndaelEncryptionService(config, encryptionKey, KeyFormat.Ascii, expiredKeys);
        }

        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher.
        /// </summary>
        public static void RijndaelEncryptionService(this BusConfiguration config, string encryptionKey, KeyFormat keyFormat, List<string> expiredKeys = null)
        {
            if (string.IsNullOrWhiteSpace(encryptionKey))
            {
                throw new ArgumentNullException("encryptionKey");
            }

            if (!IsValidKey(encryptionKey, keyFormat))
            {
                throw new ArgumentException("Invalid encryption key. Check its format and make sure it converts to 128, 192, or 256 bits.", "encryptionKey");
            }

            if (expiredKeys == null)
            {
                expiredKeys = new List<string>();
            }
            else
            {
                VerifyKeys(expiredKeys, keyFormat);
            }

            byte[] key;

            try
            {
                key = ConvertKey(encryptionKey, keyFormat);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("encryptionKey", string.Format("Invalid {0} data for encryption key.", keyFormat), ex);
            }

            var keys = expiredKeys.ConvertAll(x => ConvertKey(x, keyFormat));


            RegisterEncryptionService(config, context => BuildRijndaelEncryptionService(key, keys));
        }


        internal static void VerifyKeys(List<string> expiredKeys, KeyFormat keyFormat)
        {
            if (expiredKeys.Count != expiredKeys.Distinct().Count())
            {
                throw new ArgumentException("Overlapping keys defined. Please ensure that no keys overlap.", "expiredKeys");
            }
            for (var index = 0; index < expiredKeys.Count; index++)
            {
                var expiredKey = expiredKeys[index];

                if (string.IsNullOrWhiteSpace(expiredKey))
                {
                    throw new ArgumentException(string.Format("Empty expired key detected in position {0}.", index), "expiredKeys");
                }

                if (!IsValidKey(expiredKey, keyFormat))
                {
                    throw new ArgumentException(string.Format("Invalid data format for expired key at position {0}. Check its format and make sure it converts to 128, 192, or 256 bits.", index), "expiredKeys");
                }
            }
        }

        static RijndaelEncryptionService BuildRijndaelEncryptionService(byte[] encryptionKey, List<byte[]> expiredKeys)
        {
            var keys = expiredKeys.ToDictionary(GetKeyIdentifier, x => x);
            var encryptionKeyIdentifier = GetKeyIdentifier(encryptionKey);
            keys.Add(encryptionKeyIdentifier, encryptionKey);
            return new RijndaelEncryptionService(encryptionKeyIdentifier, keys);
        }


        /// <summary>
        /// Register a custom <see cref="IEncryptionService"/> to be used for message encryption.
        /// </summary>
        public static void RegisterEncryptionService(this BusConfiguration config, Func<IBuilder, IEncryptionService> func)
        {
            config.Settings.Set("EncryptionServiceConstructor", func);
        }

        internal static bool GetEncryptionServiceConstructor(this ReadOnlySettings settings, out Func<IBuilder, IEncryptionService> func)
        {
            return settings.TryGet("EncryptionServiceConstructor", out func);
        }

        static string GetKeyIdentifier(byte[] key)
        {
            var hash = GetHash(key);
            return Convert.ToBase64String(hash, 0, 4); // Key is 16, 24 or 32 bytes. Grabbing the 1/4, 1/6, or 1/8 of the hash.
        }

        static byte[] GetHash(byte[] data)
        {
            using (var algo = new SHA256Managed()) // SHA256Man.Create() is *slower*
            {
                return algo.ComputeHash(data, 0, data.Length);
            }
        }

        static bool IsValidKey(string encryptionKey, KeyFormat keyFormat)
        {
            try
            {
                var data = ConvertKey(encryptionKey, keyFormat);

                return IsValidKey(data);
            }
            catch (Exception)
            {
                return false;
            }
        }

        static bool IsValidKey(byte[] key)
        {
            using (var rijndael = new RijndaelManaged()) // Rijndael.Create is *slower*
            {
                var bitLength = key.Length * 8;
                return rijndael.ValidKeySize(bitLength);
            }
        }
    }
}
