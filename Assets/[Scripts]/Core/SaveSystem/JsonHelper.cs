using System;
using System.Text;

namespace Planetarium.SaveSystem
{
    public static class JsonHelper2
    {
        /// <summary>
        /// Simple XOR encryption/decryption for save files.
        /// The same method is used for both encryption and decryption.
        /// </summary>
        public static string EncryptDecrypt(string data, string key)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                // XOR each character with the corresponding character in the key
                result.Append((char)(data[i] ^ key[i % key.Length]));
            }
            return result.ToString();
        }
    }
}
