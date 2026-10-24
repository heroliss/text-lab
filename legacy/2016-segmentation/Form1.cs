using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 分词工具
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;//不检查跨线程的调用是否合法
        }
        List<string> filePathList = new List<string>(50000);

        private List<DirectoryInfo> GetAll(DirectoryInfo dir)//搜索文件夹中的文件
        {
            List<DirectoryInfo> DirectoryList = new List<DirectoryInfo>();

            FileInfo[] allFile = dir.GetFiles();
            foreach (FileInfo NextFile in allFile)
            {
                if (NextFile.Extension == "." + textBox3.Text || textBox3.Text == "")
                {
                    filePathList.Add(NextFile.FullName);
                }
            }

            DirectoryInfo[] allDir = dir.GetDirectories();
            foreach (DirectoryInfo d in allDir)
            {
                GetAll(d);
            }
            return DirectoryList;
        }
        string selectedPath;
        private void button1_Click(object sender, EventArgs e)
        {
            filePathList.Clear();
            count = 0;
            count_total = 0;
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (DialogResult.OK != dialog.ShowDialog())
                return;
            selectedPath = dialog.SelectedPath;
            GetAll(new DirectoryInfo(selectedPath));
            MessageBox.Show("共打开了" + filePathList.Count + "个"+textBox3.Text+"文件", "打开原文件"
                , MessageBoxButtons.OK, MessageBoxIcon.Information);
            button2.Enabled = true;
            button1.Enabled = false;
            count_total = filePathList.Count;
            label4.Text = "0/" + count_total;
        }
        int count_total;
        int count = 0;
        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            timer1.Start();
            backgroundWorker1.RunWorkerAsync();
            button3.Enabled = true;
        }
        private static StringBuilder readFile(string filename)//读文件
        {
            StringBuilder sb = new StringBuilder();
            System.Text.Encoding encode = System.Text.Encoding.GetEncoding("GB2312");
            FileStream fs = null;
            try
            {
                fs = File.OpenRead(@filename);
            }
            catch
            {
                return null;
            }
            byte[] arr = new byte[5000];
            // UTF8Encoding data = new UTF8Encoding();
            try
            {
                while (fs.Read(arr, 0, arr.Length) > 0)
                {
                    sb.Append(encode.GetString(arr));
                }
                return sb;
            }
            catch
            {
                return null;
            }
            finally
            {
                fs.Close();
            }
        }
        void firstCase(DoWorkEventArgs e)
        {
            FileInfo fi = new FileInfo(textBox1.Text);
            DirectoryInfo di = fi.Directory;
            if (!di.Exists)
                di.Create();

            StreamWriter sw = new StreamWriter(textBox1.Text, true
                , checkBox2.Checked ? Encoding.UTF8 : Encoding.Default);
            {
                foreach (string filePath in filePathList)
                {
                    if (!checkBox1.Checked)//不勾选为gb2312
                        sw.WriteLine(ParagraphProcess.exec(readFile(filePath).ToString()));
                    else
                        sw.WriteLine(ParagraphProcess.exec(File.ReadAllText(filePath)));
                    count++;
                    if (backgroundWorker1.CancellationPending)

                    {
                        e.Cancel = true;
                        sw.Close();
                        return;
                    }
                }
                sw.Close();
            }
        }
        void secondCase(DoWorkEventArgs e)
        {
            foreach (string filePath in filePathList)
            {
                FileInfo fi = new FileInfo(filePath);
                DirectoryInfo di = fi.Directory;
                string filename = fi.Name;
                string saveFolder = textBox2.Text + di.ToString().Substring(selectedPath.Length);
                DirectoryInfo di2 = new DirectoryInfo(saveFolder);
                if (!di2.Exists)
                    di2.Create();
                string savePath = saveFolder + "\\" + filename;//拼接路径的字符串

                StreamWriter sw = new StreamWriter(savePath
                    , false, checkBox2.Checked ? Encoding.UTF8 : Encoding.Default);

                if (!checkBox1.Checked)//不勾选为gb2312
                    sw.WriteLine(ParagraphProcess.exec(readFile(filePath).ToString()));
                else
                    sw.WriteLine(ParagraphProcess.exec(File.ReadAllText(filePath)));
                count++;
                sw.Close();
                if (backgroundWorker1.CancellationPending)

                {
                    e.Cancel = true;
                    return;
                }
            }
        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            if (radioButton1.Checked)
            {
                firstCase(e);
            }
            else if (radioButton2.Checked)
            {
                secondCase(e);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label4.Text = count + "/" + count_total;
            progressBar1.Value = (int)((double)count / count_total * 1000.0);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("分词完成！");
            timer1.Stop();
            label4.Text = count + "/" + count_total;
            progressBar1.Value = (int)((double)count / count_total * 1000.0);
            button2.Enabled = false;
            button1.Enabled = false;
            button1.Text = "请关闭重开";
            button4.Visible = true;
            button4.Enabled = true;
            button4.BringToFront();
            button3.Enabled = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (DialogResult.Yes
                == MessageBox.Show("确定要取消？", "are you sure?"
                , MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                backgroundWorker1.CancelAsync();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Restart(); //重启当前程序
        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            if (DialogResult.OK != dialog.ShowDialog())
                return;
            textBox1.Text = dialog.FileName;

        }

        private void button6_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (DialogResult.OK != dialog.ShowDialog())
                return;
            textBox2.Text = dialog.SelectedPath;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            new AboutBox1().ShowDialog();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            MessageBox.Show("该工具可以将一目录及其子目录下的所有指定后缀名的文本文件内容分词。\n若有其他疑问请联系QQ:1047495372");
        }
    }
}
