using System;
using System.Text;
using System.IO;
using DiscUtils.Fat;

namespace DiscUtils.Vfat
{
    internal class VfatFileName : FileName
    {
        // Each part contains 10+12+4 = 26 bytes.
        private const int partSize = 26;

        private byte[] _bytes;
        private byte _lastPart;

        public static VfatFileName FromPath(string path, VfatFileSystem system)
        {
            return FromName(Utilities.GetFileFromPath(path), system, 1);
        }

        public static VfatFileName FromName(string name, VfatFileSystem system,  int index)
        {
            var options = (VfatFileSystemOptions)system.FatOptions;

            //var ext = Utilities.GetExtFromPath(name);
            var ext = Path.GetExtension(name);

            var primaryName = string.Format("{0}~{1}{2}", name.Substring(0, 6).ToUpperInvariant(), index, ext.ToUpperInvariant());

            return new VfatFileName(primaryName, name, options.PrimaryEncoding, options.SecondaryEncoding);
        }

        public VfatFileName(string primaryName, string secondaryName, Encoding primaryEncoding, Encoding secondaryEncoding)
            : base(primaryName, primaryEncoding)
        {
            byte[] bytes = secondaryEncoding.GetBytes(secondaryName + "\u0000");

            // Integer division intentional
            _lastPart = (byte)(bytes.Length / partSize);

            // We want the array bigger than we need
            _bytes = new byte[(_lastPart + 1) * partSize];

            for (int i = 0; i < _bytes.Length; ++i) _bytes[i] = 0xff;

            Array.Copy(bytes, _bytes, bytes.Length);
        }

        public byte LastPart { get { return _lastPart; } }

        public void GetPart1(int part, byte[] data, int offset)
        {
            var pos = part * partSize;
            Array.Copy(_bytes, pos, data, offset, 10);
        }
        public void GetPart2(int part, byte[] data, int offset)
        {
            var pos = part * partSize;
            Array.Copy(_bytes, pos + 10, data, offset, 12);
        }

        public void GetPart3(int part, byte[] data, int offset)
        {
            var pos = part * partSize;
            Array.Copy(_bytes, pos + 22, data, offset, 4);
        }

        public byte Checksum()
        {
            byte sum = 0;
            foreach (byte b in Raw)
                sum = (byte)(((sum & 1) << 7) + (sum >> 1) + b);

            return sum;
        }
    }
}
