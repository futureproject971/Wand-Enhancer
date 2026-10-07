using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace WandEnhancer.Core.Auth
{
    internal static class HardwareId
    {
        public static string Get()
        {
            string machineGuid = ReadMachineGuid();
            string volumeSerial = ReadSystemVolumeSerial();
            string processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? Environment.ProcessorCount.ToString();

            string raw = machineGuid + "|" + volumeSerial + "|" + processor;
            byte[] digest;

            using (var sha = SHA256.Create())
                digest = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));

            string hex = ToHex(digest).ToUpperInvariant();
            return "WG-" +
                   hex.Substring(0, 8) + "-" +
                   hex.Substring(8, 8) + "-" +
                   hex.Substring(16, 8) + "-" +
                   hex.Substring(24, 8);
        }

        private static string ReadMachineGuid()
        {
            try
            {
                object value = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography",
                    "MachineGuid",
                    null);

                string text = value as string;
                if (!string.IsNullOrWhiteSpace(text))
                    return text.Trim();
            }
            catch
            {
            }

            return Environment.MachineName;
        }

        private static string ReadSystemVolumeSerial()
        {
            try
            {
                string root = System.IO.Path.GetPathRoot(Environment.SystemDirectory);
                if (string.IsNullOrWhiteSpace(root))
                    root = "C:\\";

                uint serial;
                uint maxComponentLength;
                uint fileSystemFlags;
                var volumeName = new StringBuilder(261);
                var fileSystemName = new StringBuilder(261);

                if (GetVolumeInformation(
                    root,
                    volumeName,
                    volumeName.Capacity,
                    out serial,
                    out maxComponentLength,
                    out fileSystemFlags,
                    fileSystemName,
                    fileSystemName.Capacity))
                {
                    return serial.ToString("X8");
                }
            }
            catch
            {
            }

            return "NOVOLUME";
        }

        private static string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (byte value in bytes)
                builder.Append(value.ToString("x2"));

            return builder.ToString();
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetVolumeInformation(
            string rootPathName,
            StringBuilder volumeNameBuffer,
            int volumeNameSize,
            out uint volumeSerialNumber,
            out uint maximumComponentLength,
            out uint fileSystemFlags,
            StringBuilder fileSystemNameBuffer,
            int fileSystemNameSize);
    }
}
