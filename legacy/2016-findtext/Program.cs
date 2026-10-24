using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FindText
{
    class Program
    {
        static void Main(string[] args)
        {
            try { Console.WindowWidth = 120; }
            catch {; }
            string currentDirectory = Directory.GetCurrentDirectory();
            Console.WriteLine("当前搜索目录：" + currentDirectory);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("提示：搜索中，可以按空格来暂停操作");
            Console.ResetColor();
            Console.Write("请输入要搜索的文件后缀名：");
            extension = Console.ReadLine();
            if (extension != "")
                extension = '.' + extension;
            Console.Write("请输入要搜索的关键词：");
            while (keyWord == "")
                keyWord = Console.ReadLine();
            DirectoryInfo d = new DirectoryInfo(currentDirectory);
            for (int i = 0; i < Console.WindowWidth; i++)
            {
                separationLine += "-";
            }
            Console.WriteLine(separationLine);
            Thread thread = new Thread(new ParameterizedThreadStart(GetAll));
            thread.IsBackground = true;
            thread.Start(d);
            finishMonitor(thread);

            //GetAll(d);

            //Console.SetCursorPosition(0, currentCursorTop);
            //string space = "";
            //for (int i = 0; i < aa; i++)
            //{
            //    space += " ";
            //}
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("搜索完成！从所有" + allNumber + "个文件中，共找到" + number_openFile + "个" + (extension == "" ? "无后缀名的" : extension)
                + "文件，其中包含关键词的文件有" + number + "个。（输入quit退出）" /*+ space*/);
            Console.ResetColor();
            while ("quit" != Console.ReadLine()) {; }

        }
        static int sleepTime = 50; //暂停时循环监听的睡眠时间间隔
        static string separationLine = "";
        static string extension = "";
        static string keyWord = "";
        //static int number_findKeyWordFile = 0; //找到包含关键词的文件个数
        static int number_openFile = 0;//后缀名匹配的文件个数
        static int number = 0;//找到包含关键词的文件序号
        static int allNumber = 0;//扫描过的文件数
        //static int currentCursorTop;
        //static int aa = 0, bb = 0;
        //static ArrayList FileList = new ArrayList();
        static void GetAll(object dir_in)//搜索文件夹中的文件
        {
            DirectoryInfo dir = dir_in as DirectoryInfo;
            FileInfo[] allFile;
            try { allFile = dir.GetFiles(); }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("错误信息：" + e.Message + "\n");
                Console.ResetColor();
                return;
            }
            //if (allFile.Length != 0)
            //{
            //    aa = allFile[0].Name.Length;
            //}
            foreach (FileInfo fi in allFile)
            {
                allNumber++;
                int currentCursorTop = Console.CursorTop;
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.Write("请稍等，正在搜索第 " + allNumber + " 个文件...");
                Console.ResetColor();
                Console.SetCursorPosition(0, currentCursorTop);
                //bb = aa - fi.Name.Length;
                //aa = fi.Name.Length;
                //string space = "";
                //for (int i = 0; i < bb; i++)
                //{
                //    space += " ";
                //}
                //currentCursorTop = Console.CursorTop;
                //Console.Write("正在搜索文件 " + fi.Name + space);
                //Console.SetCursorPosition(0, currentCursorTop);
                if (fi.Extension == extension)
                {
                    number_openFile++;
                    //Console.Write("正在该文件中搜索关键词：" + fi.Name + space);
                    //Console.SetCursorPosition(0, currentCursorTop);

                    //if ()
                    //{
                    //    number_findKeyWordFile++;
                    //}
                    findKeyWord(fi, Encoding.GetEncoding("gb2312"));
                    findKeyWord(fi, Encoding.UTF8);
                }
            }

            DirectoryInfo[] allDir = dir.GetDirectories();
            foreach (DirectoryInfo d in allDir)
            {
                GetAll(d);
            }
        }
        static bool findKeyWord(FileInfo f, Encoding coding) //3个暂停点都在这个函数里
        {
            pausePoint();
            bool theFileHasKeyword = false;
            FileStream s;
            try
            {
                s = f.OpenRead();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("错误信息：" + e.Message + "\n");
                Console.ResetColor();
                return theFileHasKeyword;
            }
            StreamReader r = new StreamReader(s, coding);
            int rowIndex = 0;
            while (true)
            {
                string line = r.ReadLine();
                rowIndex++;
                if (line == null)
                {
                    break;
                }
                if (line.Contains(keyWord))
                {
                    pausePoint();
                    int index = 0;
                    int startIndex = 0;
                    if (!theFileHasKeyword) //第一次发现文件包含关键词
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine((++number) + ". " + coding.EncodingName + " " + f.FullName + "：\n");
                        Console.ResetColor();
                    }
                    theFileHasKeyword = true;
                    int start;
                    if (line.Length > 1000)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("  行数:{0:D3}: ", rowIndex);
                        Console.Write("（提示：由于该行内容超过1000个文字，因此下面每行将仅显示关键词前后20个字符，注意：前后文可能会有重复）↓ \n");
                        Console.ResetColor();
                        while ((index = line.IndexOf(keyWord, startIndex)) > -1)
                        {
                            pausePoint();
                            start = startIndex < index - 20 ? index - 20 : startIndex;
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            Console.Write("  → ");
                            Console.ResetColor();
                            Console.Write(line.Substring(start, index - start));
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write(line.Substring(index, keyWord.Length));
                            Console.ResetColor();
                            Console.WriteLine(line.Substring(index + keyWord.Length, keyWord.Length + 20 + index > line.Length ? line.Length - index - keyWord.Length : 20));
                            startIndex = index + keyWord.Length;
                        }
                        //Console.WriteLine(line.Substring(startIndex) + '\n');
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("  行数:{0:D3}: ", rowIndex);
                        Console.ResetColor();
                        while ((index = line.IndexOf(keyWord, startIndex)) > -1)
                        {
                            //pausePoint();
                            Console.Write(line.Substring(startIndex, index - startIndex));
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write(line.Substring(index, keyWord.Length));
                            Console.ResetColor();
                            startIndex = index + keyWord.Length;
                        }
                        Console.WriteLine(line.Substring(startIndex) + '\n');
                    }
                }
            }
            if (theFileHasKeyword)
            {
                Console.WriteLine(separationLine);
            }
            r.Close();
            return theFileHasKeyword;
        }

        private static void pausePoint()
        {
            if (pauseNow) //暂停点
            {
                recivePause = true;
                while (true)
                {
                    Thread.Sleep(sleepTime);
                    if (!pauseNow)
                    {
                        recivePause = false;
                        break;
                    }
                }
            }
        }

        static bool pauseNow = false;//发个主线程的暂停信号
        static bool recivePause = false;//主线程是否接受到暂停信号
        private static void pause(object thread_in)
        {
            Thread thread = thread_in as Thread;
            string s = "-----------搜索已暂停！-----------";
            if (Console.ReadKey(true).Key == ConsoleKey.Spacebar)
            {
                //try
                //{
                //    thread.Suspend();
                //}
                //catch { pause(thread); return; }
                pauseNow = true;
                while (!recivePause)
                    Thread.Sleep(sleepTime);
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.Write(s);
                Console.ResetColor();
                while (true)
                {
                    Thread.Sleep(sleepTime);
                    if (Console.ReadKey(true).Key == ConsoleKey.Spacebar)
                    {
                        for (int i = 0; i < s.Length + 6; i++)
                        {
                            Console.Write('\b');
                        }
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.Write("-----------开始继续搜索-----------");
                        Console.ResetColor();
                        break;
                    }
                }
                for (int i = 0; i < s.Length + 6; i++)
                {
                    Console.Write('\b');
                }
                //thread.Resume();
                pauseNow = false;
                while (recivePause)
                    Thread.Sleep(sleepTime);
                pause(thread);
            }
            else
            {
                pause(thread);
            }
        }
        static void finishMonitor(Thread thread)
        {
            //Thread thread = thread_in as Thread;
            Thread t = new Thread(new ParameterizedThreadStart(pause));
            t.IsBackground = true;
            t.Start(thread);
            while (thread.ThreadState != ThreadState.Stopped)
            {
                Thread.Sleep(sleepTime);
            }
            t.Abort();
        }
    }
}
