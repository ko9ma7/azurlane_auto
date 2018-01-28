using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ss
{
    class Program
    {
        static Window window = null;
        static void Main(string[] args)
        {
            Console.WriteLine("[+] Starting...");
            while (is_window())
            {
                Console.WriteLine("[-] Capture Window");
                capture();
                sleep();

                Tuple<OpenCvSharp.Point, string> match_result = template_match();
                OpenCvSharp.Point match_point = match_result.Item1;
                string match_type = match_result.Item2;
                if (match_point.X == 0 && match_point.Y == 0)
                {
                    Console.WriteLine("[-] {0}", match_type);
                }
                else
                {
                    Console.WriteLine("[*] {0}", match_type);
                    mouse_click(get_window_point().X + match_point.X, get_window_point().Y + match_point.Y);
                    //break;
                }
                //break;
                sleep();
            }
        }

        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void SetCursorPos(int X, int Y);

        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        static void sleep(int ms=1000)
        {
            System.Threading.Thread.Sleep(ms);
        }

        static void mouse_click(int x, int y)
        {
            //
            //Console.WriteLine("{0}, {1}", x, y);
            //
            SetCursorPos(x, y);
            mouse_event(0x2, 0, 0, 0, 0);
            mouse_event(0x4, 0, 0, 0, 0); 
        }

        static Rectangle get_window_size()
        {
            return window.getWindowSize();
        }

        static System.Drawing.Point get_window_point()
        {
            return window.GetWindowPoint();
        }

        static void capture(string ss_directory = "img", string ss_file_name = "ss.jpg")
        {
            var bitmap = window.CaptureImage();
            bitmap.Save(Directory.GetCurrentDirectory() + "\\" + ss_directory + "\\" + ss_file_name);
        }

        static string get_emu_name()
        {
            string[] emus = new string[] {"Nox", "BlueStack3"};
            System.Diagnostics.Process[] ps = System.Diagnostics.Process.GetProcesses();
            foreach (System.Diagnostics.Process p in ps)
            {
                if (0 <= Array.IndexOf(emus, p.ProcessName))
                {
                    return p.ProcessName;
                }
            }
            return null;
        }

        static bool is_window()
        {
            string pname = get_emu_name();
            if (pname != null)
            {
                window = new Window("Nox");
                return true;
            }
            return false;
        }

        static Tuple<OpenCvSharp.Point, string> template_match()
        {
            Mat ss_img = get_ss_file();
            foreach (string file_name in get_tmplate_files())
            {
                Mat tmplate = new Mat(file_name);
                double min_val, max_val;
                OpenCvSharp.Point min_loc, max_loc;
                string match_type = Path.GetFileNameWithoutExtension(file_name);
                var result = new Mat();

                Cv2.MatchTemplate(ss_img, tmplate, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out min_val, out  max_val, out  min_loc, out  max_loc, null);
                //
                //Console.WriteLine("max_val -> {0}, match_type -> {1}", max_val, match_type);
                //
                if (max_val > 0.5)
                {
                    switch (match_type)
                    {
                        case "check":
                        case "sortie":
                        case "touch":
                        case "avoid":
                        case "confirm":
                            return new Tuple<OpenCvSharp.Point, string>(new OpenCvSharp.Point(max_loc.X + 5, max_loc.Y + 5), match_type);

                        case "enemy1":
                        case "enemy2":
                        case "enemy3":
                            return new Tuple<OpenCvSharp.Point, string>(new OpenCvSharp.Point(max_loc.X + 15, max_loc.Y + 15), match_type);

                        case "boss":
                            return new Tuple<OpenCvSharp.Point, string>(new OpenCvSharp.Point(max_loc.X + 10, max_loc.Y + 10), match_type);

                        case "kanmusu":
                            return new Tuple<OpenCvSharp.Point, string>(new OpenCvSharp.Point(get_window_size().Width / 2, get_window_size().Height / 2), match_type);
                    }
                }
            }
            return new Tuple<OpenCvSharp.Point, string>(new OpenCvSharp.Point(), "No Match");
        }

        static Mat get_ss_file(string ss_directory="img", string ss_file_name="ss.jpg")
        {
            try
            {
                return new Mat(Directory.GetCurrentDirectory() + "\\" + ss_directory + "\\" + ss_file_name);
            }
            catch (Exception e)
            {
                Console.WriteLine("err ->{0}", e);
            }
            return null;
        }

        static string[] get_tmplate_files(string tmplate_directory="tmplate_img")
        {
            try
            {
                return Directory.GetFiles(Directory.GetCurrentDirectory() + "\\" + tmplate_directory, "*", SearchOption.AllDirectories);
            }
            catch (Exception e)
            {
                Console.WriteLine("err ->{0}", e);
            }
            return null;
        }
    }
}
