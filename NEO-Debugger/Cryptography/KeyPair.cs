﻿using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Debugger.Utils;
using System;
using System.Linq;

namespace NeoLux
{
    public class KeyPair 
    {
        public readonly byte[] PrivateKey;
        public readonly byte[] PublicKey;
        public readonly byte[] CompressedPublicKey;
        public readonly UInt160 PublicKeyHash;
        public readonly string address;
        public readonly string WIF;

        public readonly UInt160 signatureHash;
        public readonly string signatureScript;

        public KeyPair(byte[] privateKey)
        {
            if (privateKey.Length != 32 && privateKey.Length != 96 && privateKey.Length != 104)
                throw new ArgumentException();
            this.PrivateKey = new byte[32];
            Buffer.BlockCopy(privateKey, privateKey.Length - 32, PrivateKey, 0, 32);

            ECPoint pKey;

            if (privateKey.Length == 32)
            {
                pKey = ECCurve.Secp256r1.G * privateKey;
            }
            else
            {
                pKey = ECPoint.FromBytes(privateKey, ECCurve.Secp256r1);
            }

            var bytes = pKey.EncodePoint(true).ToArray();
            this.CompressedPublicKey = bytes;

            this.PublicKeyHash = Crypto.Default.ToScriptHash(bytes);

            this.signatureScript = CreateSignatureScript(bytes);
            signatureHash = Crypto.Default.ToScriptHash(signatureScript.HexToBytes());

            this.PublicKey = pKey.EncodePoint(false).Skip(1).ToArray();

            this.address = Crypto.Default.ToAddress(signatureHash);
            this.WIF = GetWIF();
        }

        public static string CreateSignatureScript(byte[] bytes)
        {
            return "21" + bytes.ByteToHex() + "ac";
        }
      
        private string GetWIF()
        {
            byte[] data = new byte[34];
            data[0] = 0x80;
            Buffer.BlockCopy(PrivateKey, 0, data, 1, 32);
            data[33] = 0x01;
            string wif = data.Base58CheckEncode();
            Array.Clear(data, 0, data.Length);
            return wif;
        }

        private static byte[] XOR(byte[] x, byte[] y)
        {
            if (x.Length != y.Length) throw new ArgumentException();
            return x.Zip(y, (a, b) => (byte)(a ^ b)).ToArray();
        }
    }
}
