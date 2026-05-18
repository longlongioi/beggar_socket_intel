using System;
using System.IO;

namespace ChisFlashBurner
{
    public static class BuildTimeHelper
    {
        /// <summary>
        /// 获取当前程序集的编译日期时间（从PE头时间戳读取）
        /// </summary>
        public static DateTime GetBuildDate()
        {
            string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            using (var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] buffer = new byte[512];
                stream.Read(buffer, 0, 512);

                // PE 偏移量在 DOS 头的 0x3C 处
                int peOffset = BitConverter.ToInt32(buffer, 0x3C);

                // COFF 头在 PE 签名之后（PE 签名4字节），TimeDateStamp 在偏移 4 处
                int timestampOffset = peOffset + 4 + 4;
                int timestamp = BitConverter.ToInt32(buffer, timestampOffset);

                DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return epoch.AddSeconds(timestamp).ToLocalTime();
            }
        }
    }
}
