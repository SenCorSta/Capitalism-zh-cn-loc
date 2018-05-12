/*
cap2mod is a simple mod kit for the game Capitalism 2. icnedit allows editing
the COL/ICN images in the game's 'image' directory. It was written by Adam
Milazzo on September 9th, 2013. This source code is released into the public
domain.

For more information, feel free to contact me. http://www.adammil.net/
*/

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Cap2Mod.IcnEdit
{

static class Program
{
  static unsafe int Main(string[] args)
  {
    if(args.Length != 0) args[0] = args[0].ToLowerInvariant();
    if(args.Length != 3 || args[0] != "import" && args[0] != "export")
    {
      Console.WriteLine("USAGE: icnedit {import|export} foo.icn bar.png");
      return 1;
    }

    try
    {
      if(!File.Exists(args[1]))
      {
        Console.WriteLine("ERROR: Image file does not exist: " + args[1]);
        return 2;
      }

      string palettePath = Path.Combine(Path.GetDirectoryName(args[1]), Path.GetFileNameWithoutExtension(args[1]) + ".col");
      if(!File.Exists(palettePath))
      {
        Console.WriteLine("ERROR: Missing palette file: " + palettePath);
        return 2;
      }

      if(args[0] == "export")
      {
        using(FileStream icnFile = File.OpenRead(args[1]))
        {
          int width = icnFile.ReadByte() | (icnFile.ReadByte()<<8), height = icnFile.ReadByte() | (icnFile.ReadByte()<<8);
          if(height <= 0) throw new InvalidDataException("Invalid ICN file.");

          Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
          ColorPalette palette = bmp.Palette; // get a copy of the palette
          using(FileStream paletteFile = File.OpenRead(palettePath))
          {
            paletteFile.Position = 8;
            for(int i=0; i<256; i++)
            {
              int r = paletteFile.ReadByte(), g = paletteFile.ReadByte(), b = paletteFile.ReadByte();
              if(b < 0) throw new InvalidDataException("Invalid COL file.");
              palette.Entries[i] = Color.FromArgb(r, g, b);
            }
          }
          bmp.Palette = palette;

          BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
          byte* row = (byte*)data.Scan0.ToPointer();
          for(int y=0; y<height; row += data.Stride, y++)
          {
            for(int x=0; x<width; x++)
            {
              int value = icnFile.ReadByte();
              if(value < 0) throw new InvalidDataException("Invalid ICN file.");
              row[x] = (byte)value;
            }
          }
          bmp.UnlockBits(data);

          bmp.Save(args[2], ImageFormat.Png);
          Console.WriteLine("Image exported to " + args[2]);
        }
      }
      else
      {
        Bitmap bmp = Image.FromFile(args[2]) as Bitmap;
        if(bmp == null) throw new InvalidDataException("Image is not a bitmap: " + args[2]);
        if(bmp.PixelFormat != PixelFormat.Format8bppIndexed) throw new InvalidDataException("Image is not 8-bit indexed: " + args[2]);

        using(FileStream icnFile = new FileStream(args[1], FileMode.Open, FileAccess.ReadWrite))
        {
          int width = icnFile.ReadByte() | (icnFile.ReadByte()<<8), height = icnFile.ReadByte() | (icnFile.ReadByte()<<8);
          if(width != bmp.Width || height != bmp.Height)
          {
            throw new InvalidDataException("Image size must be " + bmp.Width.ToString() + "x" + bmp.Height.ToString() + ".");
          }
          if(icnFile.Length != width*height + 4) throw new InvalidDataException("Invalid ICN file.");

          using(FileStream paletteFile = new FileStream(palettePath, FileMode.Open, FileAccess.ReadWrite))
          {
            paletteFile.Position = 8;
            foreach(Color color in bmp.Palette.Entries)
            {
              paletteFile.WriteByte(color.R);
              paletteFile.WriteByte(color.G);
              paletteFile.WriteByte(color.B);
            }
          }

          BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
          byte[] imgBytes = new byte[bmp.Width * bmp.Height];
          byte* row = (byte*)data.Scan0.ToPointer();
          for(int y=0, i=0; y<bmp.Height; row += data.Stride, y++)
          {
            for(int x=0; x<bmp.Width; i++, x++) imgBytes[i] = row[x];
          }
          bmp.UnlockBits(data);
          icnFile.Write(imgBytes, 0, imgBytes.Length);
        }
        Console.WriteLine("Image imported from " + args[2]);
      }

      return 0;
    }
    catch(Exception ex)
    {
      Console.WriteLine("ERROR: " + ex.GetType().Name + ": " + ex.Message);
      return 2;
    }
  }
}

} // namespace Cap2Mod.IcnEdit
