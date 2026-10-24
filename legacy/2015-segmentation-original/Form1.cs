using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
namespace 分词程序
{
    public partial class Form1 : Form
    {
        StringBuilder buffer = new StringBuilder();
        Thread fcThread1_FMM;
        Thread fcThread1_BMM;
        分词类 fc1;
        string sourceFile;
        string sourceFilePath;
        string dictionaryPath = "词典360万.txt";
        bool 不显示进度 = false;
        SettingForm settingForm = new SettingForm();
        Thread t/*, t2*/;
        bool? dictionaryIsReady = null;
        bool sourceFileIsReady = false;
        public void loadSourceFile(string path)
        {
            sourceFilePath = path;
            StreamReader sr;
            try
            { sr = new StreamReader(path); }
            catch
            {
                MessageBox.Show("文件<" + (string)path + ">没有找到!\n" + @"(词典文件格式：单词\t词性\n)", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                sourceFileIsReady = false;
                return;
            }

            sourceFile = sr.ReadToEnd();
            sourceFileIsReady = true;
        }
        public void loadDictionary(object path)
        {
            StreamReader sr;
            try
            { sr = new StreamReader((string)path); }
            catch
            {
                MessageBox.Show("词典文件<" + (string)path + ">没有找到!请重新指定词典文件\n" + @"(词典文件格式：单词\t词性\n)", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                dictionaryIsReady = false;
                return;
            }

            分词类.initDictionary(sr);
            dictionaryIsReady = true;
        }
        /* public void setScheduleText()
       {
         if (toolStrip1.InvokeRequired)//一种异步委托方式
           {
               Method m = new Method(setScheduleText);
               this.Invoke(m, null);
           }
           else
           {
               toolStripProgressBar1.Visible = true;
               while (!dictionaryIsReady)
               {
                   // Thread.Sleep(1000);
                   toolStripStatusLabel1.Text = "正在加载字典文件... "
           + string.Format("{0,6:f2}%", 分词类.Schedule_initDictionary * 100);
                   toolStripProgressBar1.Value = (int)(分词类.Schedule_initDictionary * 100);
               }

               if (myEvent != null)
               {
                   myEvent(this, new EventArgs());
               }

           }
       }*/
        public event EventHandler myEvent;
        public void getSetting(bool load)
        {
            dictionaryPath = settingForm.dictionaryPath;
            分词类.MaxMatch = settingForm.maxMatchCount;
            分词类.MaxDictionary = settingForm.maxCountOfWordsForSearching;
            不显示进度 = settingForm.不显示进度;
            if (load)
            {
                loadDictionary(dictionaryPath);
            }

        }
        //  public delegate void Method();//声明一个委托类型
        /* public void setScheduleText2(object obj,EventArgs e)
         {
            Method In = delegate ()//另一种异步委托方式
             {
                 if (!(bool)dictionaryIsReady)
                 { toolStripStatusLabel1.ForeColor = Color.Red; toolStripStatusLabel1.Text = "没有加载词典文件！"; }
                 else
                 { toolStripStatusLabel1.ForeColor = Color.Black; toolStripStatusLabel1.Text = "词典已就绪"; }
             };
                 this.BeginInvoke(In);  //异步执行委托
         }*/

        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "正在加载字典文件... "
            + string.Format("{0,6:f2}%", 分词类.Schedule_initDictionary * 100);
            try { toolStripProgressBar1.Value = (int)(分词类.Schedule_initDictionary * 100); }
            catch { }
            if (dictionaryIsReady != null)
            {
                if (!(bool)dictionaryIsReady)
                { toolStripStatusLabel1.ForeColor = Color.Red; toolStripStatusLabel1.Text = "没有加载词典文件！"; }
                else
                {
                    toolStripStatusLabel1.ForeColor = Color.Black; toolStripStatusLabel1.Text = "词典已就绪";
                }
                toolStripProgressBar1.Visible = false;
                timer1.Enabled = false;
            }
        }
        void loadDictionary(string s)
        {
            if (t != null)
            {
                t.Abort();
            }
            t = new Thread(new ParameterizedThreadStart(loadDictionary));
            t.Name = "加载词典线程";
            t.Start(s);
            //t2 = new Thread(setScheduleText);
            //t2.Name = "状态栏加载信息线程";
            //t2.Start();
            timer1.Enabled = true;
            toolStripProgressBar1.Visible = true;
        }
        public Form1()
        {

            settingForm.dictionaryPath = this.dictionaryPath;
            settingForm.maxCountOfWords = 3669216;
            settingForm.maxCountOfWordsForSearching = 分词类.MaxDictionary;
            settingForm.maxMatchCount = 分词类.MaxMatch;
            InitializeComponent();
            loadDictionary(dictionaryPath);
            richTextBox1.Text = "\n\n操作提示：请先从“文件”菜单中打开一个文件，然后点击“开始分词”按钮，即可同时开始BMM和FMM操作(双线程)"
                             +"\n\n\n          请打开UTF-8或Unicode格式的文本！否则请用记事本另存为utf-8格式后再打开。";

        }

        private void 选项ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settingForm.maxCountOfWordsForSearching = 分词类.MaxDictionary;//词典最大长度的反馈设置
            settingForm.init();
            DialogResult d = settingForm.ShowDialog();
            if (d == DialogResult.OK)
            {
                dictionaryIsReady = null;
                getSetting(true);
            }
            else if (d == DialogResult.Ignore)
            {
                getSetting(false);
            }

        }
        void initbuffer(int i)
        {
            if (i == 1)
            {
                buffer.Clear();
                buffer.Append("\n\t**********************分词对比信息***********************");
                buffer.AppendLine(); buffer.AppendLine();
                buffer.AppendFormat("{0,20}\t\t\t", "FMM");
                buffer.AppendFormat("{0,20}\n", "BMM");
            }
            if (i == 2)
            {
                buffer.Clear();
                buffer.Append("\n\t**********************分词进度***********************");
                buffer.AppendLine(); buffer.AppendLine();
                buffer.AppendFormat("    \t{0,10}{1,20}\n", "单词", "类型", "");
            }
        }
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            i_FMM = 0;
            j_BMM = 0;
            i_BMM_MAX = 0;
            i_FMM_MAX = 0;
            initbuffer(2);
            richTextBox1.AppendText(":\n" + sourceFilePath + ":" + buffer.ToString());
            if (dictionaryIsReady != true)
            {
                MessageBox.Show("字典文件未就绪！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            if (!sourceFileIsReady)
            {
                MessageBox.Show("请打开一个文件！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            fc1 = new 分词类();
            fcThread1_FMM = new Thread(fc1.FMM);
            fcThread1_BMM = new Thread(fc1.BMM);
            fcThread1_BMM.IsBackground = true;
            fcThread1_FMM.IsBackground = true;
            fcThread1_FMM.Start(sourceFile);
            fcThread1_BMM.Start(sourceFile);
            timer2.Enabled = true;
            toolStripProgressBar1.Visible = true;
        }

        private void 打开文本文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                loadSourceFile(openFileDialog1.FileName);
                toolStripLabel1.Text = sourceFilePath;
            }
        }
        int i_FMM_MAX = 0, i_BMM_MAX = 0;
        public void appendBufferResult()
        {
            buffer.AppendLine();
            buffer.AppendFormat("统计结果：\nFMM: 字符个数：{0} 中文词个数：{1} 英文词个数：{2} 数字个数：{3} 标点符号个数：{4} 空格类个数：{5} 单字个数：{6}\n"
                    , fc1.Count_char_FMM, fc1.Count_chineseWord_FMM, fc1.Count_englishWord_FMM, fc1.Count_number_FMM
                    , fc1.Count_symbol_FMM, fc1.Count_whiteSpace_FMM, fc1.Count_unknow_FMM);
            buffer.AppendFormat("BMM: 字符个数：{0} 中文词个数：{1} 英文词个数：{2} 数字个数：{3} 标点符号个数：{4} 空格类个数：{5} 单字个数：{6}\n"
                    , fc1.Count_char_BMM, fc1.Count_chineseWord_BMM, fc1.Count_englishWord_BMM, fc1.Count_number_BMM
                    , fc1.Count_symbol_BMM, fc1.Count_whiteSpace_BMM, fc1.Count_unknow_BMM);
            buffer.Append("----------------------------------------------------------------------------------------------------------------");
        }
        private void 分词对比信息ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fc1 == null)
                return;
            initbuffer(1);
            int i, j;
            i = 0;
            j = 0;
            i_FMM_MAX = fc1.ResultWordsType_FMM.Count;
            i_BMM_MAX = fc1.ResultWordsType_BMM.Count;
            while (true)
            {
                if (i < i_FMM_MAX)
                {
                    buffer.AppendFormat("{0,10}{1,20}\t|", fc1.ResultWords_FMM[i], fc1.ResultWordsType_FMM[i]);
                    i++;
                }
                if (j < i_BMM_MAX)
                {
                    buffer.AppendFormat("{0,10}{1,20}\n", fc1.ResultWords_BMM[j], fc1.ResultWordsType_BMM[j]);
                    j++;
                }
                if (i >= i_FMM_MAX && j >= i_BMM_MAX)
                    break;
            }
            appendBufferResult();
            richTextBox1.AppendText("\n" + sourceFilePath + ":" + buffer.ToString());
        }


        int i_FMM = 0, j_BMM = 0;

        private void 保存分词结果为可读文本ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveToFile();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            try
            {
                fcThread1_FMM.Abort();
                fcThread1_BMM.Abort();
                MessageBox.Show("分词已中断，但后台可能仍在输出已经得到的结果，请无视之,现在即可查看各项结果", "保存结果文件", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            { }
        }

        private void 分词结果ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fc1 == null)
                return;
            buffer.Clear();
            buffer.Append("\t**********************分词结果***********************");
            buffer.Append("\n文件" + sourceFilePath + "的分词结果如下：\n");
            buffer.Append("FMM：\n");
            for (int i = 0; i < fc1.ResultWords_FMM.Count; i++)
            {
                buffer.Append(fc1.ResultWords_FMM[i] + "\\");
            }
            buffer.Append("\n-----------------------------------------\n");
            buffer.Append("BMM：\n");
            for (int i = 0; i < fc1.ResultWords_BMM.Count; i++)
            {
                buffer.Append(fc1.ResultWords_BMM[i] + "\\");
            }
            buffer.Append("\n----------------------------------------------------------------------------------------------------------------");

            richTextBox1.AppendText(buffer.ToString());
        }

        private void 清屏ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = "\n";
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("作者：李旭丰\n2015,10,6");
        }

        //bool mylock = false;
        private void timer2_Tick(object sender, EventArgs e)
        {
            // if (i_FMM < i_FMM_MAX)
            //i_FMM = i_FMM_MAX;
            //  if (j_BMM < i_FMM_MAX)
            //  j_BMM = i_BMM_MAX;
            if (!不显示进度)
            {
                lock (this)
                {
                    i_FMM_MAX = fc1.ResultWordsType_FMM.Count;
                    i_BMM_MAX = fc1.ResultWordsType_BMM.Count;
                    //               while (true)
                    {
                        if (i_FMM < i_FMM_MAX /*&& mylock == false*/)
                        {
                            // buffer.AppendFormat("FMM:{2}\t{0,10}{1,20}\n", fc1.ResultWords_FMM[i_FMM], fc1.ResultWordsType_FMM[i_FMM], "[" + i_FMM + "]");
                            richTextBox1.AppendText(string.Format("FMM:{2}\t{0,10}{1,20}\n", fc1.ResultWords_FMM[i_FMM], fc1.ResultWordsType_FMM[i_FMM], "[" + i_FMM + "]"));
                            i_FMM++;//mylock = true;
                        }
                        if (j_BMM < i_BMM_MAX/* && mylock == true*/)
                        {
                            //buffer.AppendFormat("BMM:{2}\t{0,10}{1,20}\n", fc1.ResultWords_BMM[j_BMM], fc1.ResultWordsType_BMM[j_BMM], "[" + j_BMM + "]");
                            richTextBox1.AppendText(string.Format("BMM:{2}\t{0,10}{1,20}\n", fc1.ResultWords_BMM[j_BMM], fc1.ResultWordsType_BMM[j_BMM], "[" + j_BMM + "]"));
                            j_BMM++;//mylock = false;
                        }
                        //                  if (i_FMM >= i_FMM_MAX && j_BMM >= i_BMM_MAX)
                        //           break;
                    }
                    //richTextBox1.Text = buffer.ToString();
                    this.richTextBox1.SelectionStart = this.richTextBox1.TextLength;
                    this.richTextBox1.ScrollToCaret();
                }
            }
            toolStripStatusLabel1.Text = "正在分词... "
            + string.Format(" FMM: {0,6:f2}%  BMM: {1,6:f2}% ", fc1.Schedule_FMM * 100, fc1.Schedule_BMM * 100);
            try
            { toolStripProgressBar1.Value = (int)((fc1.Schedule_FMM + fc1.Schedule_BMM) / 2 * 100); }
            catch {; }
            if ((fcThread1_BMM.ThreadState == ThreadState.Stopped && fcThread1_FMM.ThreadState == ThreadState.Stopped) || fcThread1_FMM.ThreadState == ThreadState.Aborted)
            {
                toolStripStatusLabel1.Text = "分词完成！";
                toolStripProgressBar1.Visible = false;
                if (i_FMM == i_FMM_MAX && j_BMM == i_BMM_MAX)
                {
                    buffer.Clear();
                    appendBufferResult();
                    buffer.AppendLine();
                    buffer.AppendLine("分词操作完成！请点击“视图”菜单中的“查看分词结果”或“查看对比结果”");
                    richTextBox1.AppendText(buffer.ToString());
                    this.richTextBox1.SelectionStart = this.richTextBox1.TextLength;
                    this.richTextBox1.ScrollToCaret();
                    timer2.Enabled = false;
                }
            }
        }
        void saveToFile()
        {
            if (fc1 == null)
            {
                MessageBox.Show("请先进行分词操作", "保存结果文件", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string savePath = "";
            SaveFileDialog o = new SaveFileDialog();
            o.RestoreDirectory = true;
            o.DefaultExt = "txt";
            o.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (o.ShowDialog() == DialogResult.OK)
            {
                savePath = o.FileName;
            }
            else
            { return; }
            using (FileStream f = File.OpenWrite(savePath))
            {
                string txtResult = "";
                txtResult += "文件" + sourceFilePath + "的分词结果如下：\r\n\r\n";
                txtResult += "FMM：\r\n";
                for (int i = 0; i < fc1.ResultWords_FMM.Count; i++)
                {
                    txtResult += fc1.ResultWords_FMM[i] + "\\";
                }
                txtResult += "\r\n\r\n统计信息：\r\n";
                txtResult += "[字符个数：" + fc1.Count_char_FMM.ToString() + "] ";
                txtResult += "[中文词个数：" + fc1.Count_chineseWord_FMM.ToString() + "] ";
                txtResult += "[英文词个数：" + fc1.Count_englishWord_FMM.ToString() + "] ";
                txtResult += "[数字个数：" + fc1.Count_number_FMM + "] ";
                txtResult += "[标点符号个数：" + fc1.Count_symbol_FMM + "] ";
                txtResult += "[空格类个数：" + fc1.Count_whiteSpace_FMM + "]";
                txtResult += "[单字个数：" + fc1.Count_unknow_FMM + "]";
                txtResult += "\r\n---------------------------------------------------\r\n";
                txtResult += "\r\n\r\nBMM：\r\n";
                for (int i = 0; i < fc1.ResultWords_BMM.Count; i++)
                {
                    txtResult += fc1.ResultWords_BMM[i] + "\\";
                }
                txtResult += "\r\n\r\n统计信息：\r\n";
                txtResult += "[字符个数：" + fc1.Count_char_BMM.ToString() + "] ";
                txtResult += "[中文词个数：" + fc1.Count_chineseWord_BMM.ToString() + "] ";
                txtResult += "[英文词个数：" + fc1.Count_englishWord_BMM.ToString() + "] ";
                txtResult += "[数字个数：" + fc1.Count_number_BMM + "] ";
                txtResult += "[标点符号个数：" + fc1.Count_symbol_BMM + "] ";
                txtResult += "[空格类个数：" + fc1.Count_whiteSpace_BMM + "]";
                txtResult += "[单字个数：" + fc1.Count_unknow_BMM + "]";

                StreamWriter fileWriter = new StreamWriter(f);
                // Write the current date time to the file
                fileWriter.WriteLine(System.DateTime.Now.ToString());
                fileWriter.Write(txtResult.ToArray());
                fileWriter.Flush();
                fileWriter.Close();
            }
            if (File.Exists(savePath))
            {
                MessageBox.Show("结果已保存！", "保存结果文件", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("保存失败！", "保存结果文件", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
