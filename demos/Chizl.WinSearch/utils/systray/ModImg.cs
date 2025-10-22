using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Chizl.Graphix
{
    public class ModImg
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);
        private static Image GetImage(string path)
        {
            if (!File.Exists(path))
                throw new IOException("File not found.\n$" + path);

            FileInfo fileInfo = new FileInfo(path);
            try
            {
                string text = fileInfo.Extension.ToLower();
                if (text == ".exe" || text == ".ico")
                    return (Image)GetEXEImage<Image>(path);

                return Image.FromFile(path);
            }
            catch
            {
                throw new IOException("Invalid filetype.");
            }
        }
        private static Icon GetIcon(string path, Size reSize)
        {
            if (!File.Exists(path))
                throw new IOException("File not found.\n$" + path);

            FileInfo fileInfo = new FileInfo(path);
            Icon result;
            try
            {
                string text = fileInfo.Extension.ToLower();
                if (text == ".exe" || text == ".ico")
                    result = (Icon)GetEXEImage<Icon>(path);
                else
                {
                    Bitmap bitmap = (Bitmap)Image.FromFile(path);
                    if (bitmap != null)
                    {
                        if (reSize == Size.Empty && (bitmap.Width > 512 || bitmap.Height > 512))
                        {
                            reSize = new Size(512, 512);
                        }

                        bitmap = ResizeBitmap(bitmap, reSize);
                        IntPtr hicon = bitmap.GetHicon();
                        result = (Icon)Icon.FromHandle(hicon).Clone();
                        DestroyIcon(hicon);
                    }
                    else
                        result = null;
                }
            }
            catch
            {
                throw new IOException("Invalid filetype.");
            }

            return result;
        }
        private static object GetEXEImage<T>(string path)
        {
            string text = typeof(T).Name.ToLower();
            try
            {
                Icon icon = Icon.ExtractAssociatedIcon(path);
                if (text == "icon")
                    return icon;

                return icon.ToBitmap();
            }
            catch
            {
                return null;
            }
        }
        private static Bitmap ResizeBitmap(Bitmap img, Size sz)
        {
            float num = sz.Width;
            float num2 = sz.Height;
            SolidBrush brush = new SolidBrush(Color.Transparent);
            float num3 = Math.Min(num / (float)img.Width, num2 / (float)img.Height);
            Bitmap bitmap = new Bitmap(sz.Width, sz.Height);
            Graphics graphics = Graphics.FromImage(bitmap);
            int num4 = (int)((float)img.Width * num3);
            int num5 = (int)((float)img.Height * num3);
            RectangleF rect = new RectangleF(((int)num - num4) / 2, ((int)num2 - num5) / 2, num4, num5);
            graphics.FillRectangle(brush, rect);
            graphics.DrawImage(img, rect);
            return bitmap;
        }

        /// <summary>
        /// This allows an image object to be passed in and replace or remove a specific color.
        /// </summary>
        /// <param name="bitmap">Image object</param>
        /// <param name="color">Add / Remove properties within ReplaceColor() class.</param>
        /// <param name="tolerance">Images fade colors to be help with transition.  Tolerance allows the helps in replacing those faded pixels.</param>
        /// <returns></returns>
        public static Bitmap ChangeImgColors(Bitmap bitmap, ReplaceColor color, int tolerance)
        {
            Bitmap bitmap2 = new Bitmap(bitmap);
            for (int num = bitmap2.Size.Width - 1; num >= 0; num--)
            {
                for (int num2 = bitmap2.Size.Height - 1; num2 >= 0; num2--)
                {
                    Color pixel = bitmap2.GetPixel(num, num2);
                    if (Math.Abs(color.Delete.R - pixel.R) < tolerance && Math.Abs(color.Delete.G - pixel.G) < tolerance && Math.Abs(color.Delete.B - pixel.B) < tolerance)
                        bitmap2.SetPixel(num, num2, color.Add);
                }
            }

            return bitmap2;
        }
        /// <summary>
        /// Converts Image to an Icon w/ self cleanup for memory.<br/>
        /// Size defaults will be (w:256, h:256).
        /// </summary>
        /// <param name="img">Image object to convert</param>
        /// <returns>Icon object</returns>
        public static Icon ImgToIco(Image img)=> ImgToIco(img, new Size(256, 256));
        /// <summary>
        /// Converts Image to an Icon with resize and self cleanup for memory.
        /// </summary>
        /// <param name="img">Image object to convert</param>
        /// <param name="sz">Size of Icon to return</param>
        /// <returns>Icon object</returns>
        public static Icon ImgToIco(Image img, Size sz)
        {
            IntPtr hicon = new Bitmap(img, sz).GetHicon();
            Icon result = (Icon)Icon.FromHandle(hicon).Clone();
            DestroyIcon(hicon);
            return result;
        }
        /// <summary>
        /// Convert Icon to Image
        /// </summary>
        /// <param name="ico">Icon to convert</param>
        /// <returns>Image object</returns>
        public static Image IcoToImg(Icon ico)=> IcoToBmp(ico);
        /// <summary>
        /// Convert Icon to Bitmap
        /// </summary>
        /// <param name="ico">Icon to convert</param>
        /// <returns>Bitmap object</returns>
        public static Bitmap IcoToBmp(Icon ico)=> ico.ToBitmap();
        /// <summary>
        /// Reize an image with ratio.  It will be the exact image, only resized.
        /// </summary>
        /// <param name="img">Image to resize</param>
        /// <param name="bgColor">Background color, required to help fill in lost pixels</param>
        /// <param name="Width">Width of new Image</param>
        /// <param name="Height">Height of new Image</param>
        /// <returns>Image resized</returns>
        public static Image ResizeWithRatio(Image img, Color bgColor, int Width, int Height)
        {
            int width = img.Width;
            int height = img.Height;
            int x = 0;
            int y = 0;
            int x2 = 0;
            int y2 = 0;
            float num = (float)Width / (float)width;
            float num2 = (float)Height / (float)height;
            float num3;
            if (num2 < num)
            {
                num3 = num2;
                x2 = Convert.ToInt16(((float)Width - (float)width * num3) / 2f);
            }
            else
            {
                num3 = num;
                y2 = Convert.ToInt16(((float)Height - (float)height * num3) / 2f);
            }

            int width2 = (int)((float)width * num3);
            int height2 = (int)((float)height * num3);
            
            Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
            bitmap.SetResolution(img.HorizontalResolution, img.VerticalResolution);
            
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.Clear(bgColor);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(img, new Rectangle(x2, y2, width2, height2), new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
            graphics.Dispose();

            return bitmap;
        }
        /// <summary>
        /// Takes Image Object and Dims the image as if it's disabled.
        /// </summary>
        /// <typeparam name="T">Image type to return.  (Icon, Image, Bitmap)</typeparam>
        /// <typeparam name="T">
        /// Type of image type to return.  (Icon, Image, Bitmap)<br/>
        /// Example: var ico = DisableImage<Icon>(MyIcon);
        /// </typeparam>
        /// <param name="img"></param>
        /// <returns></returns>
        public static T DisableImage<T>(Image img)
        {
            string text = typeof(T).Name.ToLower();
            T result = default(T);

            object obj = ToolStripRenderer.CreateDisabledImage(img);
            if (text.Equals("image"))
                return (T)obj;

            if (text.Equals("bitmap") || text.Equals("image"))
                return (T)Convert.ChangeType((Image)obj, typeof(T));

            return result;
        }
        /// <summary>
        /// Self build and release resources while doing so.
        /// </summary>
        /// <param name="text">Text to use in image.</param>
        /// <param name="font">Font to use for Text</param>
        /// <param name="fgColor">Text Color</param>
        /// <param name="bgColor">Background color of text</param>
        /// <returns>New Image with Text in the middle.</returns>
        public static Image TxtToImg(string text, Font font, Color fgColor, Color bgColor)
        {
            Image image = new Bitmap(1, 1);
            Graphics graphics = Graphics.FromImage(image);
            SizeF sizeF = graphics.MeasureString(text, font);
            image.Dispose();
            graphics.Dispose();

            image = new Bitmap((int)sizeF.Width, (int)sizeF.Height);
            Graphics graphics2 = Graphics.FromImage(image);
            graphics2.Clear(bgColor);
            
            Brush brush = new SolidBrush(fgColor);
            graphics2.TextRenderingHint = TextRenderingHint.AntiAlias;
            graphics2.DrawString(text, font, brush, 0f, 0f);
            graphics2.Save();
            
            brush.Dispose();
            graphics2.Dispose();
            
            return new Bitmap(image);
        }
        /// <summary>
        /// Generic to load any graphic and convert it to a BMP or Icon.
        /// </summary>
        /// <typeparam name="T">
        /// Type of object to return.  (Icon, Image, Bitmap)<br/>
        /// Example: var ico = ImageFromFile<Icon>("MyImage.png", new Size(16,16));
        /// </typeparam>
        /// <param name="path">Path to image file</param>
        /// <param name="sz">Size of image to return</param>
        /// <returns>Icon, Image, or Bitmap</returns>
        public static T ImageFromFile<T>(string path, Size sz)
        {
            string text = typeof(T).Name.ToLower();
            T val = default(T);
            switch (text)
            {
                case "icon":
                    if (GetIcon(path, sz) != null)
                        return (T)Convert.ChangeType(GetIcon(path, sz), typeof(T));

                    val = default(T);
                    break;
                case "image":
                case "bitmap":
                    {
                        Image image = GetImage(path);
                        if (image != null)
                            return (T)Convert.ChangeType(image, typeof(T));

                        val = default(T);
                        break;
                    }
                default:
                    val = default(T);
                    break;
            }

            return val;
        }
    }
}
