using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.International.Converters.PinYinConverter;
using System.Text;
using System.Linq;
using System.Threading;
using System.Management;
namespace 改错字
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        FormLoad loadWindow;
        private void Form1_Load(object sender, EventArgs e)
        {
            wv = new WordVectors();
            loadWindow = new FormLoad(wv);
            backgroundWorker1.RunWorkerAsync();
            loadWindow.ShowDialog();
            

            // Get the WMI class
            ManagementClass c = new ManagementClass(
            new ManagementPath("Win32_Processor"));
            // Get the properties in the class
            ManagementObjectCollection moc = c.GetInstances();
            threadCount = 0; //要设置的并行线程数量
            foreach (ManagementObject mo in moc)
            {
                PropertyDataCollection properties = mo.Properties;
                //获取内核数代码
                toolStripStatusLabel1.Text += "本机cpu - 物理内核数:" + properties["NumberOfCores"].Value ;
                toolStripStatusLabel1.Text += " 逻辑内核数:" + properties["NumberOfLogicalProcessors"].Value;
                textBox3.Text = properties["NumberOfLogicalProcessors"].Value.ToString();
                //其他属性获取代码
                //foreach (PropertyData property in properties)
                //{
                //    toolStripStatusLabel1.Text += property.Name + ":" + property.Value + "\r";
                //}
            }
        }

        int windowSize;
        string vectorBinFilename = "vectors.bin";
        WordVectors wv;
        MarkedText markedText;

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = false;
            //FixLength = -1;
            if (!int.TryParse(textBox2.Text, out windowSize)) { textBox3.Undo(); return; }

            markedText.setText(textBox1.Text, windowSize);

            skipWords.Clear();
            skipWordsCount.Clear();
            lastS = null;
            show();
            button1.Enabled = true;
            button2.Enabled = true;
        }

        private void show(bool markBad = false)
        {
            string s = "颜色标识： 相关度高  ->  相关度低：<br>";
            for (int i = 0; i < 20; i++)
            {
                s += "<font color='" + ColorTranslator.ToHtml(Color.FromArgb((int)((float)i / 20 * 255), 160 - (int)((float)i / 20 * 180) / 2, 0)) + "'>█</font>";
            }

            double max = 0;
            double min = 100;
            double average = 0;
            for (int i = 0; i < markedText.Words.Length; i++)
            {
                if (markedText.Values[i] == -1)
                {
                    continue;
                }
                if (max < markedText.Values[i])
                    max = markedText.Values[i];
                if (min > markedText.Values[i] && markedText.Values[i] != 0)
                    min = markedText.Values[i];
                average += markedText.Values[i];
            }
            average /= markedText.Values.Length;
            s += string.Format("<br><font size='2'>最好:{0:0.00}&nbsp 平均:{1:0.00}&nbsp 最差: {2:0.00}</font>", min, average, max);
            s += "<br/><br/>";
            for (int i = 0; i < markedText.Words.Length; i++)
            {
                double t = (markedText.Values[i] - min) / (max - min);
                if (double.IsNaN(t))
                    t = 0;
                if (markedText.Values[i] == -1)
                {
                    s += "<font id='" + i.ToString() + "'>";
                }
                else if (t <= 0)
                {
                    s += "<font id='" + i.ToString() + "' color='" + ColorTranslator.ToHtml(Color.DarkGreen) + "'>";
                }
                else if (markBad && markedText.SortedIndex[0] == i)
                {
                    s += "<font id='" + i.ToString() + "' style='background:#ffff00' color = " + ColorTranslator.ToHtml(Color.FromArgb((int)(t * 255), 160 - (int)(t * 180) / 2, 0)) + ">";
                }
                else if (markedText.Values[i] != -1)
                {
                    s += "<font id='" + i.ToString() + "' color='" + ColorTranslator.ToHtml(Color.FromArgb((int)(t * 255), 160 - (int)(t * 180) / 2, 0)) + "'>";
                }
                else
                {
                    throw new Exception("靠");
                }


                s += markedText.Words[i];
                s += "</font> ";
            }
            webBrowser1.DocumentText = s;
        }

        //int sortListIndex = 0;
        List<string> skipWords = new List<string>();
        List<int> skipWordsCount = new List<int>();
        string lastS = null;
        int lastCount;
        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = false;
            //if (sortListIndex>=markedText.SortedIndex.Length)
            //{
            //    return;
            //}
            //if (sortListIndex!=0)
            //{

            //HtmlElement ee = webBrowser1.Document.GetElementById(markedText.SortedIndex[sortListIndex - 1].ToString());
            //ee.InnerHtml = ss+" ";

            if (lastS != null)
            {
                skipWords.Add(lastS);
                skipWordsCount.Add(lastCount);
                markedText.setText(textBox1.Text, windowSize, skipWords, skipWordsCount);
            }

            lastS = markedText.Words[markedText.SortedIndex[0]];
            setSkipWordCount();

            show(true);
            //FixLength = -1;

            autoFix();
            //Update();
            //autoFix2();

            //HtmlElement element = webBrowser1.Document.GetElementById(markedText.SortedIndex[0].ToString());
            //string s = element.InnerHtml;
            //element.InnerHtml = "<font style='background:#ffff00'>" + markedText.Words[markedText.SortedIndex[0]] + "</font> ";
            //button2.Text = "-" + s + "-";
        }
        private void autoFix2() //最佳相关度匹配
        {
            listView2.Clear();
            int currentIndex = markedText.SortedIndex[0];
            string currentWord = markedText.Words[markedText.SortedIndex[0]];
            double[] values;
            long[] closestList = markedText.获取i位置上的最佳相关词集(currentIndex, currentWord.Length, 7, out values);

            for (int i = 0; i < 7; i++)
            {
                listView2.Items.Add(string.Format("{0} {1:0.000}", wv.getWord(closestList[i]), values[i]));
            }
        }
        JiebaNet.Segmenter.JiebaSegmenter _jieba = new JiebaNet.Segmenter.JiebaSegmenter();
        SortedList<double, string> _sortedFixedWords = new SortedList<double, string>();//按相关度由小到大排序的修改词集
        int _count;
        StringBuilder[] _words;
        Thread[] _threads;
        int threadCount;
        private void autoFix()  //音近匹配
        {
            _sortedFixedWords.Clear();
            _count = 0;
            listView1.Clear();

            List<char>[] charList = new List<char>[lastS.Length]; //存放搜索过的拼音的同音字
            for (int i = 0; i < charList.Length; i++)
            {
                charList[i] = new List<char>(); //每个字可能的读音的所有同音字
            }


            label6.Text = "搜索的词组：\n";
            ChineseChar cc;
            for (int i = 0; i < lastS.Length; i++)
            {
                try
                {
                    cc = new ChineseChar(lastS[i]);
                }
                catch
                {
                    break;
                }
                foreach (string py in cc.Pinyins)
                {
                    if (py == null)
                    {
                        continue;
                    }
                    string subPinyin = py.Substring(0, py.Length - 1);
                    string pinyin;
                    for (int k = 1; k < 5; k++)
                    {
                        pinyin = subPinyin + k.ToString();
                        if (!ChineseChar.IsValidPinyin(pinyin))
                        {
                            continue;
                        }

                        foreach (char c in ChineseChar.GetChars(pinyin))
                        {
                            if (!charList[i].Contains(c))
                            {
                                charList[i].Add(c);
                            }
                        }
                    }
                }
            }
            int wordsCount = 1;
            foreach (List<char> chars in charList)
            {
                wordsCount *= chars.Count;
            }
            _words = new StringBuilder[wordsCount];
            for (int i = 0; i < _words.Length; i++)
            {
                _words[i] = new StringBuilder();
            }
            int wordsIndex = 0;
            for (int i = 0; i < lastS.Length; i++)
            {
                wordsIndex = 0;
                try { wordsCount /= charList[i].Count; } catch { continue; }
                while (wordsIndex < _words.Length)
                {
                    foreach (char c in charList[i])
                    {
                        for (int j = 0; j < wordsCount; j++)
                        {
                            _words[wordsIndex].Append(c);
                            wordsIndex++;
                        }
                    }
                }
            }
            listView1.Items.Add("<正在计算...>");
            //listView1.Update();
            //多线程并行运算
            Thread monitor = new Thread(new ThreadStart(monitorThread)); //监视其他线程结束
            monitor.IsBackground = true;
            
            _threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                _threads[i] = new Thread(new ParameterizedThreadStart(search));
                _threads[i].IsBackground = true;
                _threads[i].Start(i);
            }
            monitor.Start();
        }
        private void monitorThread()
        {
            DateTime t1 = DateTime.Now;

            while (true)
            {
            start:
                Thread.Sleep(100);
                for (int i = 0; i < _threads.Length; i++)
                {
                    if (_threads[i].ThreadState != ThreadState.Stopped)
                    {
                        goto start;
                    }
                }
                break;
            }
            DateTime t2 = DateTime.Now;
            TimeSpan timespan = (t2 - t1);
            listView1.Clear();
            foreach (var item in _sortedFixedWords)
            {
                listView1.Items.Add(new ListViewItem(string.Format("{0} {1:0.000}", item.Value, item.Key)));
            }

            if (listView1.Items.Count == 0)
            {
                listView1.Items.Add("<无>");
            }
            label6.Text = string.Format("搜索的词组：\n\n  完成！共搜索词{0}个\n\n  用时：{1:0}分{2:0}秒{3:0}毫秒", _count,timespan.Minutes,timespan.Seconds,timespan.Milliseconds);
            button1.Enabled = true;
            button2.Enabled = true;
            return;

        }
        bool threadLock = false;
        private void search(object threadNum)
        {
            int startIndex = _words.Length / threadCount*(int)threadNum;
            int endIndex = _words.Length / threadCount + startIndex;
            if ((int)threadNum == threadCount-1)
            {
                endIndex = _words.Length;
            }
            string s;
            for (int i = startIndex; i < endIndex; i++)
            {
                s = _words[i].ToString();
                if (_jieba.Cut(s).Count() != 1)
                {
                    continue;
                }
                else
                {
                    _count++;
                    label6.Text = string.Format("搜索的词组：\n  {0} {1}", s, _count.ToString());
                   // label6.Update();
                    double v = markedText.计算修改词的value(s, markedText.SortedIndex[0]);
                    if (v >= 0)
                    {
                        _sortedFixedWords.Add(v, s);
                        if (threadLock == false)
                        {
                            threadLock = true;
                            //listView1.Clear();
                            //listView1.Items.Add("<正在计算...>");
                            listView1.Items.Add(new ListViewItem(string.Format("{0} {1:0.000}", s, v)));
                            listView1.Items[listView1.Items.Count - 1].EnsureVisible();
                            //listView1.Update();
                            threadLock = false;
                        }
                    }
                }
            }
        }

        private void setSkipWordCount()
        {
            lastCount = -1;
            for (int i = 0; i < markedText.Words.Length; i++)
            {
                if (markedText.Words[i] == lastS)
                {
                    lastCount++;
                    if (i == markedText.SortedIndex[0])
                    {
                        break;
                    }
                }
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox3.Text, out threadCount)) textBox3.Undo();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            wv.LoadVectorFile(vectorBinFilename);
            markedText = new MarkedText(wv, true);
            threadCount = int.Parse(textBox3.Text);
            windowSize = int.Parse(textBox2.Text);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loadWindow.Close();
        }

        //int FixLength = -1;
        //int FixTextLength = 0;
        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            HtmlElement element = webBrowser1.Document.GetElementById(markedText.SortedIndex[0].ToString());
            element.InnerHtml = "<font style='background:#ffff00'>" + textBox4.Text + "</font> ";

            //if (FixLength==-1) //第一次修改
            //{
            //    FixLength++;
            //    for (int i = 0; i < markedText.SortedIndex[0]; i++)
            //    {
            //        FixLength += markedText.Words[i].Length;
            //    }
            //    textBox1.Select(FixLength, markedText.Words[markedText.SortedIndex[0]].Length);
            //}
            //else
            //{
            //    textBox1.Select(FixLength,FixTextLength);
            //}
            //FixTextLength = textBox4.Text.Length;
            //textBox1.SelectedText = textBox4.Text;
            markedText.Words[markedText.SortedIndex[0]] = textBox4.Text;
            StringBuilder sb = new StringBuilder();
            foreach (string s in markedText.Words)
            {
                sb.Append(s);
            }
            textBox1.Text = sb.ToString();
            lastS = textBox4.Text;
            setSkipWordCount();

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count != 0)
            {
                textBox4.Text = listView1.SelectedItems[0].Text.Substring(0, lastS.Length);
            }
        }
    }
}
