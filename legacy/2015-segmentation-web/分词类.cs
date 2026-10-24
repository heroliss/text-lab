using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace 分词程序
{
    /// <summary>
    /// FMM与BMM核心代码类
    /// </summary>
    class 分词类
    {
        #region 有关字典的静态私有变量声明
        private static int maxDictionary = 3669216;//字典中的总词数(设置选项中的匹配数量)
        private static int maxMatch = 30; //匹配最大字符数（设置选项中的匹配长度）
        private static string symbols = "`~!@#$%^&*()-_=+[{]};:\'\"\\|,<.>/?·！@#￥%…&*（）【】；：‘’“”、《》，。？";//匹配标点符号集（设置选项中的标点符号集）
        private static string[] words = new string[maxDictionary];//存放字典单词的数组
        private static string[] wordsType = new string[maxDictionary];//存放字典单词对应的单词类型的数组
        private static int i_initDictionary = 0;//词典加载进度
        #endregion
        #region 有关分词操作的实例私有变量声明
        private int i_FMM = 0; //进度（当前文本位置）
        private int i_BMM = 0;
        public int i_FMM_real = 0;
        public int i_BMM_real = 0; 
        
        private List<string> resultWords_FMM = new List<string>();//FMM单词结果集
        private List<string> resultWordsType_FMM = new List<string>();//FMM单词类型结果集
        private List<string> resultWords_BMM = new List<string>();//BMM单词结果集
        private List<string> resultWordsType_BMM = new List<string>();//BMM单词类型结果集
        #endregion
        #region FMM与BMM执行结果的统计数据
        private int count_char_FMM = 0;
        private int count_whiteSpace_FMM = 0;
        private int count_chineseWord_FMM = 0;
        private int count_number_FMM = 0;
        private int count_englishWord_FMM = 0;
        private int count_symbol_FMM = 0;
        private int count_unknow_FMM = 0;

        private int count_char_BMM = 0;
        private int count_whiteSpace_BMM = 0;
        private int count_chineseWord_BMM = 0;
        private int count_number_BMM = 0;
        private int count_englishWord_BMM = 0;
        private int count_symbol_BMM = 0;
        private int count_unknow_BMM = 0;
        #endregion
        #region 字段封装
        public static int MaxDictionary
        {
            get
            {
                return maxDictionary;
            }

            set
            {
                maxDictionary = value;
            }
        }

        public static int MaxMatch
        {
            get
            {
                return maxMatch;
            }

            set
            {
                maxMatch = value;
            }
        }

        public static string Symbols
        {
            get
            {
                return symbols;
            }

            set
            {
                symbols = value;
            }
        }

        public int Count_char_FMM
        {
            get
            {
                return count_char_FMM;
            }
        }

        public int Count_whiteSpace_FMM
        {
            get
            {
                return count_whiteSpace_FMM;
            }
        }

        public int Count_chineseWord_FMM
        {
            get
            {
                return count_chineseWord_FMM;
            }
        }

        public int Count_number_FMM
        {
            get
            {
                return count_number_FMM;
            }
        }

        public int Count_englishWord_FMM
        {
            get
            {
                return count_englishWord_FMM;
            }
        }

        public int Count_symbol_FMM
        {
            get
            {
                return count_symbol_FMM;
            }
        }

        public int Count_unknow_FMM
        {
            get
            {
                return count_unknow_FMM;
            }
        }

        public int Count_char_BMM
        {
            get
            {
                return count_char_BMM;
            }
        }

        public int Count_whiteSpace_BMM
        {
            get
            {
                return count_whiteSpace_BMM;
            }
        }

        public int Count_chineseWord_BMM
        {
            get
            {
                return count_chineseWord_BMM;
            }
        }

        public int Count_number_BMM
        {
            get
            {
                return count_number_BMM;
            }
        }

        public int Count_englishWord_BMM
        {
            get
            {
                return count_englishWord_BMM;
            }
        }

        public int Count_symbol_BMM
        {
            get
            {
                return count_symbol_BMM;
            }
        }

        public int Count_unknow_BMM
        {
            get
            {
                return count_unknow_BMM;
            }
        }
        /// <summary>
        /// FMM分词进度
        /// </summary>
        public double Schedule_FMM
        {
            get
            {
                return (double)i_FMM/count_char_FMM;
            }
        }
        /// <summary>
        /// BMM分词进度
        /// </summary>
        public double Schedule_BMM
        {
            get
            {
                return (double)i_BMM/count_char_BMM;
            }
        }
        /// <summary>
        /// 字典加载进度
        /// </summary>
        public static double Schedule_initDictionary
        {
            get
            {
                return (double)i_initDictionary/maxDictionary;
            }
        }

        public List<string> ResultWords_FMM
        {
            get
            {
                return resultWords_FMM;
            }
        }

        public List<string> ResultWordsType_FMM
        {
            get
            {
                return resultWordsType_FMM;
            }
        }

        public List<string> ResultWords_BMM
        {
            get
            {
                return resultWords_BMM;
            }
        }

        public List<string> ResultWordsType_BMM
        {
            get
            {
                return resultWordsType_BMM;
            }
        }
        #endregion

        /// <summary>
        /// 初始化字典(在执行分词前必须先初始化词典,可重复初始化)
        /// </summary>
        /// <param name="dictionaryReader">字典文件读入流</param>
        public static void initDictionary(StreamReader dictionaryReader)
        {
            string s;
            string[] sArray = new string[3];
            i_initDictionary = 0;
            while (!dictionaryReader.EndOfStream && i_initDictionary<maxDictionary)
            {
                s = dictionaryReader.ReadLine();
                sArray = s.Split('\t');
                words[i_initDictionary] = sArray[0];
                wordsType[i_initDictionary] = "<" + sArray[1] + ">";
                i_initDictionary++;
            }
            maxDictionary = i_initDictionary;
        }
        /// <summary>
        /// 判断一个字符是否为标点符号
        /// </summary>
        /// <param name="c">要判断的字符</param>
        /// <returns>若判断为标点则返回true</returns>
        private bool isSymbol(char c)
        {
            foreach (char item in symbols)
            {
                if (c == item)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 判断一个字符是否为字母
        /// </summary>
        /// <param name="c">要判断的字符</param>
        /// <returns>若判断为字母则返回true</returns>
        private bool isLetter(char c)
        {
            if ((c > 'a' && c < 'z') || (c > 'A' && c < 'Z'))
            {
                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// 执行FMM前的预处理过程（识别英文，标点，数字和空格类字符）
        /// </summary>
        /// <param name="buffer">引用字符拼接缓存区</param>
        /// <param name="s">引用要处理的字符串</param>
        /// <param name="i">引用要处理的字符串的当前位置变量</param>
        /// <returns>若识别出非中文字符则返回true（并将其后所连带的单词或数字以及单个字符加入结果集
        /// 若识别为空格则仅仅返回true），否则返回false（无动作）
        /// </returns>
        private bool pretreatment_FMM(ref StringBuilder buffer, ref string s, ref int i)
        {
            if (isSymbol(s[i]))//是标点符号
            {
                resultWords_FMM.Add(s[i].ToString());//将此标点加入结果集
                resultWordsType_FMM.Add("<标点>");//将其类型加入类型结果集
                                                //buffer.Clear();//清空字符拼接缓存区

                count_symbol_FMM++;//标点符号数量统计值加一
                return true;
            }

            else if (isLetter(s[i])) //是字母
            {
                for (; i < s.Length; i++)//从当前位置遍历原始字符串直至结尾，若是连续字母则加入拼接缓存区
                {
                    if (isLetter(s[i]))
                    {
                        buffer.Append(s[i]);
                    }
                    else//否则停止遍历，索引恢复一位
                    {
                        i--;
                        break;
                    }
                }
                resultWords_FMM.Add(buffer.ToString());//将单个字母或拼接好的英文单词信息加入结果集
                resultWordsType_FMM.Add("<英文>");
                buffer.Clear();//清空缓存区

                count_englishWord_FMM++;//英文单词统计数值加一
                return true;

            }

            else if (Char.IsDigit(s[i]))//是数字
            {
                for (; i < s.Length; i++)//从当前位置遍历原始字符串直至结尾，若是连续数字则加入拼接缓存区
                {
                    if (Char.IsDigit(s[i]))
                    {
                        buffer.Append(s[i]);
                    }
                    else//否则停止遍历，索引恢复一位
                    {
                        i--;
                        break;
                    }
                }
                resultWords_FMM.Add(buffer.ToString());//将单个数字或拼接好的一串数字加入结果集
                resultWordsType_FMM.Add("<数字>");
                buffer.Clear();//清空缓存区

                count_number_FMM++;//数字统计数值加一
                return true;
            }
            else if (Char.IsWhiteSpace(s[i]))//是空格类
            {
                count_whiteSpace_FMM++;//空格类统计数值加一
                return true;
            }
            return false;
        }

        /// <summary>
        /// 预判断字符类型（与预处理函数的判断机制相同）
        /// </summary>
        /// <param name="s">引用原始字符串</param>
        /// <param name="i">引用当前位置</param>
        /// <returns>若是非中文类型则返回true</returns>
        private bool prejudgement(ref string s, ref int i)
        {
            if (isSymbol(s[i]))//是标点符号
            {
                return true;
            }
            else if (isLetter(s[i])) //是字母
            {
                return true;
            }
            else if (Char.IsDigit(s[i]))
            {
                return true;
            }
            else if (Char.IsWhiteSpace(s[i]))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 执行BMM前的预处理过程（识别英文，标点，数字和空格类字符）（结果集与FMM的结果集顺序相反）
        /// </summary>
        /// <param name="buffer">引用字符拼接缓存区</param>
        /// <param name="s">引用要处理的字符串</param>
        /// <param name="i">引用要处理的字符串的当前位置变量</param>
        /// <returns>若识别出非中文字符则返回true（并将其后所连带的单词或数字以及单个字符加入结果集
        /// 若识别为空格则仅仅返回true），否则返回false（无动作）
        /// </returns>
        private bool pretreatment_BMM(ref StringBuilder buffer, ref string s, ref int i)
        {
            if (isSymbol(s[i]))//是标点符号
            {
                resultWords_BMM.Add(s[i].ToString());//将此标点加入结果集
                resultWordsType_BMM.Add("<标点>");//将其类型加入类型结果集
                                                //buffer.Clear();

                count_symbol_BMM++;//标点符号数量统计值加一
                return true;
            }
            else if (isLetter(s[i])) //是字母
            {
                for (; i < s.Length; i++)//从当前位置遍历原始字符串直至结尾，若是连续字母则加入拼接缓存区
                {
                    if (isLetter(s[i]))
                    {
                        buffer.Append(s[i]);
                    }
                    else//否则停止遍历，索引恢复一位
                    {
                        i--;
                        break;
                    }
                }
                resultWords_BMM.Add(new string(buffer.ToString().Reverse().ToArray()));//将单个字母或拼接好的英文单词信息加入结果集
                resultWordsType_BMM.Add("<英文>");
                buffer.Clear();//清空缓存区

                count_englishWord_BMM++;//英文单词统计数值加一
                return true;

            }

            else if (Char.IsDigit(s[i]))//是数字
            {
                for (; i < s.Length; i++)//从当前位置遍历原始字符串直至结尾，若是连续数字则加入拼接缓存区
                {
                    if (Char.IsDigit(s[i]))
                    {
                        buffer.Append(s[i]);
                    }
                    else//否则停止遍历，索引恢复一位
                    {
                        i--;
                        break;
                    }
                }
                resultWords_BMM.Add(new string(buffer.ToString().Reverse().ToArray()));//将单个数字或拼接好的一串数字加入结果集(与FMM不同，此处需要反转字符串)
                resultWordsType_BMM.Add("<数字>");
                buffer.Clear();

                count_number_BMM++;//数字统计数值加一
                return true;
            }
            else if (Char.IsWhiteSpace(s[i]))//是空格类
            {
                count_whiteSpace_BMM++;//空格类统计数值加一
                return true;
            }
            return false;
        }
        /// <summary>
        /// 正向最大匹配
        /// </summary>
        /// <param name="s">要处理的原始字符串</param>
        public void FMM(string s)
        {
            //归零所有统计数据
            count_char_FMM = s.Length;//获取原始文本的字符个数
            count_whiteSpace_FMM = 0;
            count_chineseWord_FMM = 0;
            count_number_FMM = 0;
            count_englishWord_FMM = 0;
            count_symbol_FMM = 0;
            count_unknow_FMM = 0;
            resultWordsType_FMM.Clear();
            resultWords_FMM.Clear();
            i_FMM_real = 0;

            int ti = 0;//记录开始处理原始文本时的初始位置
            StringBuilder buffer = new StringBuilder();//创建字符串缓存区，用来对读入的字符拼接组装产生中文单词，英文单词以及数字串
            for (i_FMM = 0; i_FMM < s.Length; i_FMM++)
            {
                ti = i_FMM; //记录开始进行下面判断时的第一个字符位置

                if (pretreatment_FMM(ref buffer, ref s, ref i_FMM))//预处理，如果产生了结果(发现非中文并处理)则返回真，向后继续
                {
                    continue;//goto next;
                }

                for (; (i_FMM - ti) < maxMatch && i_FMM < s.Length; i_FMM++)//向缓存区一次性读入长度为"最大匹配长度"的字符
                {
                    if (prejudgement(ref s, ref i_FMM))
                        break;
                    buffer.Append(s[i_FMM]);
                }
                i_FMM--;//此时i_FMM指向最后一个字符
                for (; (i_FMM - ti) > 0; i_FMM--)//由当前位置反向遍历至初始位置以后的所有字符
                {
                    for (int j = 0; j < maxDictionary; j++)//遍历字典查找匹配的词组，若找到则加入结果集并向下继续（到下一个文本位置）
                    {
                        if (buffer.ToString() == words[j])
                        {
                            resultWords_FMM.Add(words[j]);
                            resultWordsType_FMM.Add(wordsType[j]);
                            buffer.Clear();

                            count_chineseWord_FMM++;
                            goto next;//跳至下一次循环
                        }
                    }
                    buffer.Remove(i_FMM - ti, 1);//遍历字典后没有找到，则移除缓存区最后一个字符，重新查找
                }

                //遍历所有可能长度的缓存区字符串后无字典匹配，则向结果集加入单字
                resultWords_FMM.Add(s[ti].ToString());
                resultWordsType_FMM.Add("<单字>");

                buffer.Clear();
                count_unknow_FMM++;//未识别（单子）统计数据加一

                next:;
                i_FMM_real = i_FMM;
            }
        }
        /// <summary>
        /// 反向最大匹配（结果集与正向最大匹配顺序相反）
        /// </summary>
        /// <param name="s">要处理的原始字符串</param>
        public void BMM(string s_in)
        {
            string s = new string(s_in.Reverse().ToArray());//反转原始字符串
            //归零所有统计数据
            count_char_BMM = s.Length;//获取原始文本的字符个数
            count_whiteSpace_BMM = 0;
            count_chineseWord_BMM = 0;
            count_number_BMM = 0;
            count_englishWord_BMM = 0;
            count_symbol_BMM = 0;
            count_unknow_BMM = 0;
            resultWordsType_BMM.Clear();
            resultWords_BMM.Clear();
            i_BMM_real = 0;

            int ti = 0;//记录开始处理原始文本时的初始位置
            StringBuilder buffer = new StringBuilder();//创建字符串缓存区，用来对读入的字符拼接组装产生中文单词，英文单词以及数字串

            for (i_BMM = 0; i_BMM < s.Length; i_BMM++)
            {
                ti = i_BMM; //记录开始进行下面判断时的第一个字符位置

                if (pretreatment_BMM(ref buffer, ref s, ref i_BMM))//预处理如果产生了结果则返回真，向后继续
                {
                    continue;//goto next;
                }

                for (; (i_BMM - ti) < maxMatch && i_BMM < s.Length; i_BMM++)//向缓存区一次性读入长度为"最大匹配长度"的字符
                {
                    if (prejudgement(ref s, ref i_BMM))
                        break;
                    buffer.Insert(0, s[i_BMM]);//此处与FMM不同，字符插入头部使拼接好的字符串反转为原文本顺序
                }
                i_BMM--;//此时i_BMM指向最后一个字符
                for (; (i_BMM - ti) > 0; i_BMM--)//由当前位置反向遍历至初始位置以后的所有字符
                {

                    for (int j = 0; j < maxDictionary; j++)//遍历字典查找匹配的词组，若找到则加入结果集并向下继续（到下一个文本位置）
                    {
                        if (buffer.ToString() == words[j])
                        {
                            resultWords_BMM.Add(words[j]);
                            resultWordsType_BMM.Add(wordsType[j]);
                            buffer.Clear();

                            count_chineseWord_BMM++;
                            goto next;//跳至下一次循环
                        }
                    }
                    buffer.Remove(0, 1);//遍历字典后没有找到，则移除缓存区第一个字符，重新查找
                }

                //遍历所有可能长度的缓存区字符串后无字典匹配，则向结果集加入单字
                resultWords_BMM.Add(s[ti].ToString());
                resultWordsType_BMM.Add("<单字>");

                buffer.Clear();
                count_unknow_BMM++;//未识别（单子）统计数据加一

                next:;
                i_BMM_real = i_BMM;
            }

            resultWords_BMM.Reverse();
            resultWordsType_BMM.Reverse();
        }
        /// <summary>
        /// 为创建线程准备的FMM函数
        /// </summary>
        /// <param name="s">参数本质是string类型</param>
        public void FMM(object s)
        {
            FMM((string)s);
        }
        /// <summary>
        /// 为创建线程准备的BMM函数
        /// </summary>
        /// <param name="s">参数本质是string类型</param>
        public void BMM(object s)
        {
            BMM((string)s);
        }
        /// <summary>
        /// 为创建线程准备的初始化字典函数
        /// </summary>
        /// <param name="dictionaryReader"></param>
        public void initDictionary(object dictionaryReader)
        {
            initDictionary((StreamReader)dictionaryReader);
        }
    }
}


