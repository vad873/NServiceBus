namespace NServiceBus.Core.Tests.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using NServiceBus.Encryption.Rijndael;
    using NUnit.Framework;

    [TestFixture]
    public class RijndaelEncryptionServiceTest
    {
        [Test]
        public void Should_encrypt_and_decrypt()
        {
            var keys = new Dictionary<string, byte[]>
            {
                {"id",Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")}
            };

            var service = new RijndaelEncryptionService("id", keys);
            var encryptedValue = service.Encrypt("string to encrypt");
            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);
            var decryptedValue = service.Decrypt(encryptedValue);
            Assert.AreEqual("string to encrypt", decryptedValue);
        }

        [Test]
        public void Should_encrypt_and_decrypt_for_expired_key()
        {
            var keys = new Dictionary<string, byte[]>
            {
                {"old",Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")},
                {"new",Encoding.ASCII.GetBytes("vznkynwuvateefgduvsqjsufqfrrfcya")}
            };

            var service1 = new RijndaelEncryptionService("old", keys);
            var encryptedValue = service1.Encrypt("string to encrypt");
            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);

            var service2 = new RijndaelEncryptionService("new", keys);
            var decryptedValue = service2.Decrypt(encryptedValue);
            Assert.AreEqual("string to encrypt", decryptedValue);
        }

        [Test]
        public void Should_throw_when_no_valid_key_to_decrypt()
        {
            var keys = new Dictionary<string, byte[]>
            {
                {"valid",Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")},
                {"invalid",Encoding.ASCII.GetBytes("adDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")}
            };

            var service1 = new RijndaelEncryptionService("valid", keys);
            var encryptedValue = service1.Encrypt("string to encrypt");

            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);

            keys["valid"] = Encoding.ASCII.GetBytes("xdDbqRpqdRbTs3mhdZh9qCaDaxJXl");

            var service2 = new RijndaelEncryptionService("invalid", keys);

            var exception = Assert.Throws<AggregateException>(() => service2.Decrypt(encryptedValue));
            Assert.AreEqual("Could not decrypt message. Tried 2 keys.", exception.Message);
            Assert.AreEqual(2, exception.InnerExceptions.Count);

            foreach (var inner in exception.InnerExceptions)
            {
                Assert.IsInstanceOf<CryptographicException>(inner);
            }
        }

        [Test]
        public void Should_throw_when_no_matching_key_identifier()
        {
            var keys = new Dictionary<string, byte[]>
            {
                {"valid", Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")},
            };

            var service1 = new RijndaelEncryptionService("valid", keys);
            var encryptedValue = service1.Encrypt("string to encrypt");

            Assert.Throws<ArgumentException>(() => service1.Decrypt(encryptedValue, "invalid"), "Invalid decryption key identifier");
        }


        [Test]
        public void Should_encrypt_and_decrypt_with_key_identifier()
        {
            var keys = new Dictionary<string, byte[]>
            {
                {"valid", Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")},
            };

            var service = new RijndaelEncryptionService("valid", keys);
            var data = "string to encrypt";
            var encryptedValue = service.Encrypt(data);
            var decryptedValue = service.Decrypt(encryptedValue, "valid");

            Assert.AreNotEqual(data, encryptedValue);
            Assert.AreEqual(data, decryptedValue);
        }

        [Test]
        public void Should_have_correct_key_identier_reference()
        {
            var keys = new Dictionary<string, byte[]>
            {
                {"valid",Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")},
                {"invalid",Encoding.ASCII.GetBytes("adDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")}
            };

            var service = new RijndaelEncryptionService("valid", keys);

            Assert.AreEqual("valid", service.EncryptionKeyIdentifier);
        }

        [Test]
        public void Should_throw_argument_exception_for_invalid_encryption_key_identifier()
        {
            var keys = new Dictionary<string, byte[]>
            {
                {"valid",Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")},
            };

            Assert.Throws<ArgumentException>(() => new RijndaelEncryptionService("invalid", keys), "Invalid encryption key identifier");
        }
    }
}