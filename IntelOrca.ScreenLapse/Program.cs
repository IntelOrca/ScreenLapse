using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace IntelOrca.ScreenLapse
{
    internal class Program
    {
        #region Windows API

        [DllImport("user32")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        #endregion

        private static Version Version { get; set; }

        private static int _numCaptures;
        private static int _outputIndex;
        private static BitmapGraphics _screenBG;
        private static BitmapGraphics _saveBG;

        private static int Main(string[] args)
        {
            Version = Assembly.GetEntryAssembly().GetName().Version;

            if (args.Length == 0)
            {
                Console.WriteLine("ScreenLapse {0}.{1}", Version.Major, Version.Minor);
                Console.WriteLine("Copyright Ted John 2014");
                Console.WriteLine();
                Console.WriteLine("timelapse <config path>");
                return 0;
            }

            string configPath = args[0];
            if (!File.Exists(configPath))
            {
                Console.Error.WriteLine("{0} does not exist.", configPath);
                return 1;
            }

            string configJson;
            try
            {
                configJson = File.ReadAllText(configPath);
            }
            catch
            {
                Console.Error.WriteLine("Unable to read {0}.", configPath);
                return 1;
            }

            var json = new JavaScriptSerializer();
            CaptureConfig config = json.Deserialize<CaptureConfig>(configJson);

            // Check output format
            if (String.IsNullOrEmpty(config.Output))
            {
                Console.Error.WriteLine("No output format specified.");
                return 1;
            }

            try
            {
                if (config.Output.Count(x => x == '{') != 1)
                    throw new Exception();
                config.Output = config.Output.Replace("{", "{0:");
                String.Format(config.Output, "0");
            }
            catch
            {
                Console.Error.WriteLine("Invalid output format specified.");
                return 1;
            }

            // Defaults
            if (config.Interval == 0)
            {
                Console.Error.WriteLine("No interval specified.");
                return 1;
            }

            bool finish = false;

            // Trap CTRL-C
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                finish = true;
            };

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            while (!finish)
            {
                int msLeft = (int)(config.Interval - stopWatch.ElapsedMilliseconds);
                if (msLeft > 0)
                {
                    if (msLeft > 5)
                        Thread.Sleep(msLeft - 1);
                    continue;
                }
                stopWatch.Restart();

                Process process = GetActiveProcess();
                if (process != null && IsProcessGoodForCapture(process, config))
                    Capture(config);

                WriteStatistics();
            }

            if (_screenBG != null)
                _screenBG.Dispose();
            if (_saveBG != null)
                _saveBG.Dispose();

            return 0;
        }

        private static void WriteStatistics()
        {
            Console.Clear();
            Console.WriteLine("ScreenLapse");
            Console.WriteLine("Captures: {0}, Index: {1}", _numCaptures, _outputIndex);
        }

        private static void Capture(CaptureConfig config)
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;

            // Calculate destination image size
            Size saveImageSize = new Size(config.Width, config.Height);
            if (saveImageSize.Width == 0 && saveImageSize.Height == 0)
            {
                saveImageSize = bounds.Size;
            }
            else if (saveImageSize.Width != 0 && saveImageSize.Height == 0)
            {
                double aspectRatio = (double)bounds.Height / bounds.Width;
                saveImageSize.Height = (int)(saveImageSize.Width * aspectRatio);
            }
            else if (saveImageSize.Width == 0 && saveImageSize.Height != 0)
            {
                double aspectRatio = (double)bounds.Width / bounds.Height;
                saveImageSize.Width = (int)(saveImageSize.Height * aspectRatio);
            }

            // Ensure bitmaps are created
            if (_screenBG == null)
                _screenBG = new BitmapGraphics(new Bitmap(bounds.Width, bounds.Height));
            if (_saveBG == null)
                _saveBG = new BitmapGraphics(new Bitmap(saveImageSize.Width, saveImageSize.Height));

            // Capture screen
            _screenBG.Graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);

            _saveBG.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            _saveBG.Graphics.DrawImage(
                _screenBG.Bitmap,
                new Rectangle(0, 0, saveImageSize.Width, saveImageSize.Height),
                new Rectangle(0, 0, bounds.Width, bounds.Height),
                GraphicsUnit.Pixel
            );

            string nextPath = GetNextOutputPath(config);
            try
            {
                switch (Path.GetExtension(nextPath).ToLower())
                {
                    case ".jpg":
                    case ".jpeg":
                        _saveBG.Bitmap.Save(nextPath, ImageFormat.Jpeg);
                        break;
                    case "png":
                        _saveBG.Bitmap.Save(nextPath, ImageFormat.Png);
                        break;
                    default:
                        _saveBG.Bitmap.Save(nextPath);
                        break;
                }
            }
            catch { }
            _numCaptures++;
        }

        private static string GetNextOutputPath(CaptureConfig config)
        {
            while (true)
            {
                string path = String.Format(config.Output, _outputIndex);
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                if (File.Exists(path))
                    _outputIndex++;
                else
                    return path;
            }
        }

        private static bool IsProcessGoodForCapture(Process process, CaptureConfig config)
        {
            string processName = process.ProcessName;
            bool include = true;
            bool exclude = false;

            if (!config.Include.IsNullOrEmpty())
                include = config.Include.Any(includePattern => Regex.IsMatch(process.ProcessName, includePattern));
            if (!config.Exclude.IsNullOrEmpty())
                exclude = config.Exclude.Any(excludePattern => Regex.IsMatch(process.ProcessName, excludePattern));

            return include && !exclude;
        }

        private static Process GetActiveProcess()
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
                return null;

            int processId;
            GetWindowThreadProcessId(hWnd, out processId);

            return Process.GetProcessById(processId);
        }
    }
}
