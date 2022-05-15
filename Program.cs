﻿using DiscUtils;
using DiscUtils.Fat;
using DiscUtils.Raw;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NickDisk
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            using (var f = File.OpenRead(@"C:\development\NickDisk\NickDisk\bin\Debug\3g_gd.img"))
            {
                byte[] bytes = new byte[512];
                f.Read(bytes, 0, 512);
                using (var fout = File.Create(@"C:\ChampMan\cm9798\QEMU\Win98HDMBR.bin"))
                    fout.Write(bytes, 0, 512);
            }
            */
            /*
            var bootBytes = new byte[512 * 3];
            using (var goodDisk = new Disk(@"C:\development\NickDisk\NickDisk\bin\Debug\freshblank_good.img"))
            {
                using (var part0 = goodDisk.Partitions[0].Open())
                {
                    part0.Position = 0;
                    part0.Read(bootBytes, 0, bootBytes.Length);
                    using (var fout = File.Create(@"C:\development\NickDisk\NickDisk\bin\Debug\Win98BootSectors_FAT32.bin"))
                        fout.Write(bootBytes, 0, bootBytes.Length);
                }
            }
            */

            if (args.Length < 2)
            {
                Console.WriteLine("NickDisk <Command> <Param1> <Param2> ...");
                Console.WriteLine();
                Console.WriteLine("Commands:");
                Console.WriteLine("CreateFloppy floppy.img [/BOOTDISK] [/LABEL:MyLabel]");
                Console.WriteLine("CreateHD hd.img 100M [/BOOTDISK] [/LABEL:MyLabel] (100M = 100 Megabytes, 4G = 4 Gigabytes, etc)");
                return;
            }

            bool createBootDisk = args.Where(x => x.ToUpper().Contains("/BOOTDISK")).Count() > 0;
            string label = "NICK";
            if (args.Where(x => x.ToUpper().StartsWith("/LABEL:")).Count() > 0)
                label = args.First(x => x.ToUpper().StartsWith("/LABEL:")).Substring("/LABEL:".Length);

            if (label.Length > 8)
            {
                Console.WriteLine("Label {0} is too long. Label should be 8 characters or less");
                return;
            }

            switch  (args[0].ToUpper())
            {
                case "CREATEFLOPPY":
                    if (args.Length >= 2)
                    {
                        using (FileStream fs = File.Open(args[1], FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            using (var floppy = FatFileSystem.FormatFloppy(fs, DiscUtils.FloppyDiskType.HighDensity, label))
                            {
                                if (createBootDisk)
                                {
                                    var oldPos = fs.Position;
                                    fs.Position = 0;
                                    var sector = ReadEmbeddedFile(@"Win98FloppyBootSector.bin");
                                    fs.Write(sector, 0, sector.Length);
                                    fs.Position = oldPos;

                                    WriteBytesToFATFile(floppy, "IO.SYS", ReadEmbeddedFile("IO.SYS"));
                                    floppy.SetAttributes("IO.SYS", FileAttributes.System | FileAttributes.Hidden | FileAttributes.ReadOnly);
                                    WriteBytesToFATFile(floppy, "MSDOS.SYS", ReadEmbeddedFile("MSDOS.SYS"));
                                    floppy.SetAttributes("MSDOS.SYS", FileAttributes.System | FileAttributes.Hidden | FileAttributes.ReadOnly);
                                    WriteBytesToFATFile(floppy, "COMMAND.COM", ReadEmbeddedFile("COMMAND.COM"));

                                    floppy.SetLabel(label);
                                }
                            }
                        }
                        Console.WriteLine("{1}FAT Formatted Floppy Image {0} Created", args[1], createBootDisk ? "Bootable" : "");
                    }
                    else
                        Console.WriteLine("CreateFloppy commands needs more arguments");
                    break;
                case "CREATEHD":
                    if (args.Length >= 3)
                    {
                        string sizeString = args[2];
                        long multiplier = 1;
                        if (sizeString[sizeString.Length - 1].ToString().ToUpper() == "M")
                        {
                            multiplier = 1024 * 1024;
                            sizeString = sizeString.Substring(0, sizeString.Length - 1);
                        }
                        if (sizeString[sizeString.Length - 1].ToString().ToUpper() == "G")
                        {
                            multiplier = 1024 * 1024 * 1024;
                            sizeString = sizeString.Substring(0, sizeString.Length - 1);
                        }
                        long size;
                        if (long.TryParse(sizeString, out size))
                        {
                            size *= multiplier;
                            using (FileStream fs = File.Open(args[1], FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                            {
                                var geometry = Geometry.FromCapacity(size);
                                geometry = geometry.TranslateToBios(GeometryTranslation.Lba);       // Doing Large doesn't work for 5gig??
                                using (var hd = Disk.Initialize(fs, Ownership.None, size, geometry))
                                {
                                    var partitionTable = DiscUtils.Partitions.BiosPartitionTable.Initialize(hd);
                                    partitionTable.Create(DiscUtils.Partitions.WellKnownPartitionType.WindowsFat, true);
                                    
                                    if (createBootDisk)
                                    {
                                        var bytes = ReadEmbeddedFile("Win98HDMBR.bin");
                                        var currentMBR = hd.GetMasterBootRecord();
                                        for (int i = 0; i < 446; i++)
                                            currentMBR[i] = bytes[i];
                                        hd.SetMasterBootRecord(currentMBR);
                                        hd.Signature = new Random().Next();
                                    }

                                    using (var ffs = FatFileSystem.FormatPartition(hd, 0, label))
                                    {
                                        if (createBootDisk)
                                        {

                                            byte[] bytes = ReadEmbeddedFile("Win98BootSectors.bin");
                                            
                                            using (Stream partitionStream = hd.Partitions[0].Open())
                                            {
                                                var oldpos = partitionStream.Position;
                                                int partOffset = 62;
                                                int sectorsToCopy = 1;

                                                if (hd.Partitions[0].TypeAsString.Contains("FAT32"))
                                                {
                                                    bytes = ReadBytes("Win98BootSectors_FAT32.bin");
                                                    partOffset = 90;
                                                    partitionStream.Position = 1;
                                                    partitionStream.Write(new byte[] { 0x58 }, 0, 1);
                                                    //partitionStream.Position = 0x1A;
                                                    //partitionStream.Write(new byte[] { 0x80 }, 0, 1);   // <--- Set Heads Per Cylinder to 0x80 (not needed with Large geometry)
                                                    sectorsToCopy = 3;
                                                }

                                                partitionStream.Position = partOffset;
                                                partitionStream.Write(bytes, partOffset, (512 * sectorsToCopy) - partOffset);

                                                // Set our FAT32 Next and Free Cluster to unknown
                                                if (hd.Partitions[0].TypeAsString.Contains("FAT32"))
                                                {
                                                    partitionStream.Position = 512 + 488;
                                                    partitionStream.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, 0, 8);
                                                }
                                            }

                                            WriteBytesToFATFile(ffs, "IO.SYS", ReadEmbeddedFile("IO.SYS"));
                                            ffs.SetAttributes("IO.SYS", FileAttributes.System | FileAttributes.Hidden | FileAttributes.ReadOnly);
                                            WriteBytesToFATFile(ffs, "MSDOS.SYS", ReadEmbeddedFile("MSDOS.SYS"));
                                            
                                            ffs.SetAttributes("MSDOS.SYS", FileAttributes.System | FileAttributes.Hidden | FileAttributes.ReadOnly);
                                            WriteBytesToFATFile(ffs, "COMMAND.COM", ReadEmbeddedFile("COMMAND.COM"));
                                        }
                                        ffs.SetLabel(label);
                                    }
                                }
                            }
                            Console.WriteLine("{1}FAT Formatted Hard Disk Image {0} Created", args[1], createBootDisk ? "Bootable" : "");
                        }
                        else
                            Console.WriteLine("CreateFloppy command needs more arguments");
                    }
                    break;
                case "COPY":
                    if (args.Length >= 3)
                    {
                        var srcPath = args[1];
                        var dstPath = args[2];
                        bool recurseDirectories = args.Where(x => x.ToUpper() == "/S").Count() > 0;
                        bool sourceIsDrive = (srcPath.LastIndexOf(':') > 1);
                        bool destIsDrive = (dstPath.LastIndexOf(':') > 1);
                        
                        if (!sourceIsDrive && destIsDrive)
                        {
                            // Copy local to a disk drive
                            CopyLocalToDrive(srcPath, dstPath, recurseDirectories);
                        }

                        /*
                        using (var src = File.Open(args[1], FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var dst = File.Open(args[2], FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {

                        }*/
                    }
                    else
                        Console.WriteLine("Copy requires a Source and a Destination");
                    break;
            }
        }

        static void CopyLocalToDrive(string srcPath, string dstPath, bool recurseDirectories)
        {
            IEnumerable<string> filesToCopy;
            string srcPathRoot, searchPattern, imgFile, imgPath;

            if (System.IO.Directory.Exists(srcPath))
            {
                srcPathRoot = srcPath;
                searchPattern = "*.*";
            }
            else
            {
                srcPathRoot = Path.GetDirectoryName(srcPath);
                if (string.IsNullOrEmpty(srcPathRoot))
                    srcPathRoot = ".";
                searchPattern = Path.GetFileName(srcPath);
            }

            filesToCopy = GetFiles(srcPathRoot, searchPattern, recurseDirectories).ToList();

            if (filesToCopy.Count() > 0)
            {
                SplitImagePath(dstPath, out imgFile, out imgPath);

                using (var dstImg = File.Open(imgFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    // Use this to test if it's a floppy drive
                    DiscFileSystem dfs;
                    VolumeManager volMgr = new VolumeManager();
                    volMgr.AddDisk(dstImg);
                    var volInfo = volMgr.GetLogicalVolumes()[0];

                    var fileSystems = FileSystemManager.DetectDefaultFileSystems(volInfo);
                    if (fileSystems.Length > 0)
                    {
                        var fsInfo = FileSystemManager.DetectDefaultFileSystems(volInfo)[0];
                        dfs = fsInfo.Open(volInfo);
                    }
                    else
                    {
                        // Assume a floppy and load directly
                        dfs = new FatFileSystem(dstImg);
                    }
                     
                    using (dfs)
                    {
                        // If path does not exist and copying multiple files create a directory
                        if (!dfs.Exists(imgPath) && filesToCopy.Count() > 1)
                            dfs.CreateDirectory(imgPath);
                        bool imgPathIsDir = (dfs.Exists(imgPath) && (dfs.GetAttributes(imgPath) & FileAttributes.Directory) == FileAttributes.Directory);
                        
                        foreach (var fileToCopy in filesToCopy)
                        {
                            using (var src = File.Open(fileToCopy, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                var cleanFileToCopy = fileToCopy.Substring(srcPathRoot.Length);
                                var remoteDir = (imgPathIsDir ? imgPath : Path.GetDirectoryName(imgPath)) + Path.GetDirectoryName(cleanFileToCopy);
                                var remoteFile = imgPathIsDir ? Path.GetFileName(cleanFileToCopy) : Path.GetFileName(imgPath);
                                var remoteFullPath = remoteDir + "\\" + remoteFile;

                                // Clean the Remote Path
                                remoteFullPath = remoteFullPath.Replace("\\\\", "\\").TrimEnd('\\');

                                Console.WriteLine("Copying file {0} -> {1}", fileToCopy, remoteFullPath);

                                // Ensure directory exists
                                dfs.CreateDirectory(remoteDir);

                                using (var dst = dfs.OpenFile(remoteFullPath, FileMode.Create, FileAccess.ReadWrite))
                                    Utilities.PumpStreams(src, dst);
                            }
                        }
                    }
                }
            }
            else
                Console.WriteLine("No files found in directory {0} filter {1} to copy", srcPathRoot, Path.GetFileName(srcPath));
        }

        static IEnumerable<string> GetFiles(string path, string searchPatten, bool recurseDirectories)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                if (recurseDirectories)
                {
                    try
                    {
                        foreach (string subDir in System.IO.Directory.GetDirectories(path))
                        {
                            queue.Enqueue(subDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex);
                    }
                }
                string[] files = null;
                try
                {
                    files = System.IO.Directory.GetFiles(path, searchPatten);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        yield return files[i];
                    }
                }
            }
        }

        static void SplitImagePath(string path, out string imgFile, out string imgPath)
        {
            int idx = path.LastIndexOf(':');
            imgFile = path.Substring(0, idx);
            imgPath = path.Substring(idx + 1);
        }

        static byte[] ReadBytes(string file, int pos = 0, int size = 0)
        {
            using (var f = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var br = new BinaryReader(f))
            {
                f.Position = pos;
                if (size == 0)
                    size = (int)(f.Length - f.Position);
                return br.ReadBytes(size);
            }
        }

        static byte[] ReadEmbeddedFile(string file)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("DOS.zip"));
            using (var arr = assembly.GetManifestResourceStream(resourceName))
            using (var zs = ZipStorer.Open(arr, FileAccess.Read))
            {
                using (var ms = new MemoryStream())
                {
                    zs.ExtractFile(zs.ReadCentralDir().First(x => x.FilenameInZip.Contains(file)), ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms.ToArray();
                }
            }
        }

        static void WriteBytesToFATFile(FatFileSystem ffs, string filePath, byte [] bytes)
        {
            using (var io = ffs.OpenFile(filePath, FileMode.Create, FileAccess.ReadWrite))
                io.Write(bytes, 0, bytes.Length);
        }
    }
}