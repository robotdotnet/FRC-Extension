using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RobotDotNet.FRC_Extension.MonoCode
{
    public class MonoFile
    {
        public static byte[] CalculateMd5Sum(string fileName)
        {
            if (File.Exists(fileName))
            {
                using (FileStream stream = new FileStream(fileName, FileMode.Open))
                {
                    using (MD5 md5 = new MD5CryptoServiceProvider())
                    {
                        return md5.ComputeHash(stream);
                    }
                }
            }
            else
            {
                return null;
            }
        }

        public static string Md5SumToString(byte[] fileMd5Sum)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var b in fileMd5Sum)
            {
                builder.Append(b);
            }
            return builder.ToString();
        }

        public static bool CheckMd5Sum(string fileMd5Sum, string checkMd5Sum)
        {
            return fileMd5Sum.Equals(checkMd5Sum);
        }
    }
}
