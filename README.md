# NickDisk
#### C# Commandline Utility For Formatting/Copying To Raw FAT32 Disks

This is a small tool I built for dealing with DOS and Windows raw image files as used when running virtual machines in QEMU, DosBox, etc.
I wanted a tool that could easily build disks, make them bootable where need be, and most importantly allow me to copy files to them easily.
```
NickDisk <Command> <Param1> <Param2> ...

Commands:
CreateFloppy floppy.img [/BOOTDISK] [/LABEL:MyLabel]
CreateHD hd.img 100M [/BOOTDISK] [/LABEL:MyLabel] [/DONOTFORMAT] (100M = 100 Mb, 4G = 4 Gb, etc)
CreateISO disk.iso PathToDirectory [/BOOTIMAGE:FloppyDisk.img]
Copy PathToFileOrDirectory imageFile.img:/path/to/copy/to [/S] (/S = copy subdirectories too)
```

## Examples
#### Example 1:
````
CreateFloppy floppy.img /BOOTDISK /LABEL:MYDISK
````
Will create a new 1.44mb floppy disk image with  Windows 98 DOS boot files and the label MYDISK
#### Example 2:
````
CreateHD hd.img 5G /DONOTFORMAT
````
Will create a blank file (all zeroes internally that's 5Gb big). If /DONOTFORMAT is not included it will be forwarded to FAT16 or FAT32 depending on the size.
#### Example 3:
````
CreateISO newISO.iso c:\somedirectory /BOOTIMAGE:Floppy.img
````
Will create a new ISO file with all the files and directories from some directory. Bootimage should be a bootable 1.44mb floppy disk image.
#### Example 4:
````
Copy c:\directory\*.txt myhd.img:\TEXTDIR /S
````
Copies all files in the directory c:\directory (and its sub directories) that are txt files to the sub directory \TEXTDIR in the image


## Credit
All the clever parts of this come from the DiscUtils C# project. This is just a very simple and quickly coded wrapper around that excellent project.
It uses code from the repository here: https://github.com/quamotion/discutils but the latest version can be found here: https://github.com/DiscUtils/DiscUtils
