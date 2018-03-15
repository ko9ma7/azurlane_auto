using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Configuration;

namespace Azurlane_Auto
{
    public partial class Form1 : Form
    {
        private bool is_run = false;
        private string[] template_files;
        Window window = null;
        public Form1()
        {
            InitializeComponent();
        }


        private void exit()
        {
            Application.Exit();
        }

        private string get_emu_name()
        {
            string[] emus = new string[] { "Nox", "BlueStack3" };
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

        private string read_ss_file()
        {
            try
            {
                return string.Format("{0}\\{1}\\{2}", Directory.GetCurrentDirectory(), read_config("ss_folder"), read_config("ss_file_name"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        private string[] read_template_files()
        {
            try
            {
                return Directory.GetFiles(
                    Directory.GetCurrentDirectory() + "\\" + read_config("template_folder"),
                    "*",
                    SearchOption.AllDirectories
                    );

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        private string read_config(string str)
        {
            try
            {
                return ConfigurationSettings.AppSettings[str].ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        private bool exist_ss_folder()
        {
            if (Directory.Exists(read_config("ss_folder")))
            {
                return true;
            }
            else
            {
                Directory.CreateDirectory(
                    Directory.GetCurrentDirectory() + "\\" + read_config("ss_folder")
                    );
                return false;
            }
        }

        private void capture()
        {
            Bitmap resize_img(Bitmap image, double width, double height)
            {
                double hi;
                double imagew = image.Width;
                double imageh = image.Height;

                if ((height / width) <= (imageh / imagew))
                {
                    hi = height / imageh;
                }
                else
                {
                    hi = width / imagew;
                }
                int w = (int)(imagew * hi);
                int h = (int)(imageh * hi);

                Bitmap result = new Bitmap(w, h);
                Graphics g = Graphics.FromImage(result);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, result.Width, result.Height);

                return result;
            }
            resize_img(window.CaptureImage(), 832, 498).Save(Directory.GetCurrentDirectory() + "\\" + read_config("ss_folder") + "\\" + read_config("ss_file_name"));
        }

        private void delete_ss()
        {
            //あとで
        }

        private string now()
        {
            return null;
        }

        private void add_log(string type, string str)
        {
            logBox.AppendText("[" + type + "] " + str + "\n");
        }

        private void add_encounter(int i)
        {
            encounterBox.Clear();
            encounterBox.AppendText(i.ToString());
        }

        private void set_action_label(string str)
        {
            actionLabel.ResetText();
            actionLabel.Text = str;
        }

        /*
         自動生成
         */

        private void Form1_Load(object sender, EventArgs e)
        {
            add_log("+", "プログラム起動");
            var emu_name = get_emu_name();
            if (emu_name != null)
            {
                window = new Window(emu_name);
                add_log("+", emu_name + " の起動確認");

                template_files = read_template_files();
                if (template_files != null)
                {
                    add_log("+", "テンプレートファイル読み込み");
                    if (exist_ss_folder())
                    {
                        add_log("+", "スクリーンショットフォルダ確認");
                    }
                    else
                    {
                        add_log("x", "スクリーンショットフォルダを生成");
                    }
                }
                else
                {
                    add_log("x", "テンプレートファイルが読み込めません");
                }
                
            }
            else
            {
                add_log("x", "エミュレータが起動してません");
            }
        }

        private async void start_Click(object sender, EventArgs e)
        {
            //inner func
            List<(int, int)> get_all_pos(Mat result, double match_rate = 0.3)
            {
                var t = new List<int>();
                var pos_list = new List<(int, int)>();

                for (int y = 0; y < result.Rows; y++)
                {
                    for (int x = 0; x < result.Cols; x++)
                    {
                        if (result.At<float>(y, x) > 0)
                        {
                            int d = (int)Math.Round(Math.Round(Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) / 10, 0, MidpointRounding.AwayFromZero) / 10, 1, MidpointRounding.AwayFromZero);
                            if (t.Exists(s => s == d))
                            {
                                //pass
                            }
                            else
                            {
                                pos_list.Add((x, y));
                                t.Add(d);
                            }
                        }
                    }
                }
                return pos_list;
            }

            //inner func
            List<((int, int), string)> template_matching()
            {
                Mat ss_img = new Mat(read_ss_file());
                var res = new List<((int, int), string)>();
                foreach (string file_path in read_template_files())
                {
                    Mat tmplate = new Mat(file_path);
                    double min_val, max_val;
                    OpenCvSharp.Point min_loc, max_loc;
                    string match_type = Path.GetFileNameWithoutExtension(file_path);
                    var result = new Mat();
                    Cv2.MatchTemplate(ss_img, tmplate, result, TemplateMatchModes.CCoeffNormed);
                    Cv2.MinMaxLoc(result, out min_val, out max_val, out min_loc, out max_loc, null);

                    if (max_val < 0.5) continue;

                    Cv2.Threshold(result, result, 0.4, 1.0, ThresholdTypes.Tozero);
                    List<(int, int)> pos_list = get_all_pos(result);
                    foreach ((int, int) pos in pos_list)
                    {
                        res.Add((pos, match_type));
                    }
                }
                return res;
            }

            void select_action()
            {
                //
            }

            //start start_Click
            if (!is_run)
            {
                is_run = true;
                add_log("+", "開始");

                while (is_run)
                {
                    add_log("*", "スクリーンキャプチャ");
                    //capture();

                    foreach(var s in template_matching())
                    {
                        Console.WriteLine(s);
                    }
                    
                    await Task.Delay(1000);
                    break;
                }
            }
        }

        private void stop_Click(object sender, EventArgs e)
        {
            if (is_run)
            {
                is_run = false;
                add_log("+", "停止");
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            exit();
        }

        /*
         ここまで
        */
    }
}
