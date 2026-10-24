using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JiebaNet.Segmenter;
using JiebaNet.Analyser;
using Microsoft.International.Converters.PinYinConverter;
namespace 改错字
{
    class MarkedText
    {
        private double[,] distanceTable;
        private string[] words;
        private double[] values;
        private bool normalize = false;
        private WordVectors wv;
        private JiebaSegmenter jieba;
        private int[] sortedIndex;
        #region 字段封装
        public string[] Words
        {
            get
            {
                return words;
            }
        }
        public double[] Values
        {
            get
            {
                return values;
            }
        }
        public bool Normalize
        {
            get
            {
                return normalize;
            }
        }
        public int[] SortedIndex
        { get { return sortedIndex; } }
        #endregion

        public MarkedText(WordVectors wv, bool normalize = false)
        {
            this.normalize = normalize;
            this.wv = wv;
            if (normalize)
            {
                wv.normalizeVectors();
            }
            jieba = new JiebaSegmenter();
        }
        public void setText(string text, int windowSize, List<string> skipWords = null, List<int> skipWordCount = null)
        {

            IEnumerable<string> t = jieba.Cut(text);
            IEnumerator<string> e = t.GetEnumerator();
            int wordsCount = t.Count();
            values = new double[wordsCount];
            words = new string[wordsCount];
            //初始化单词距离表
            distanceTable = new double[wordsCount, wordsCount];
            for (int i = 0; i < wordsCount; i++)
            {
                for (int j = 0; j < wordsCount; j++)
                {
                    distanceTable[i, j] = -1;
                }
            }
            ////

            for (int i = 0; i < t.Count(); i++)
            {
                e.MoveNext();
                words[i] = e.Current;
            }

            bool[] skipWordIndex = null;
            if (skipWords != null)
            {
                skipWordIndex = new bool[wordsCount];
                if (skipWordCount != null)
                {
                    for (int k = 0; k < skipWords.Count; k++)
                    {
                        int count = -1;
                        for (int m = 0; m < words.Length; m++)
                        {
                            if (words[m] == skipWords[k])
                            {
                                count++;
                                if (count == skipWordCount[k])
                                {
                                    skipWordIndex[m] = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            计算中心词的value(windowSize, wordsCount, skipWordIndex);

            sortedIndex = new int[wordsCount];
            for (int i = 0; i < wordsCount; i++)
            {
                sortedIndex[i] = i;
            }
            Array.Sort(sortedIndex, (int a, int b) => { return Math.Sign(values[b] - values[a]); });
        }

        private void 计算中心词的value(int windowSize, int wordsCount, bool[] skipWordIndex)
        {
            this.windowSize = windowSize;
            this.wordsCount = wordsCount;
            this.skipWordIndex = skipWordIndex;

            int addCount = 0;
            long index_a, index_b;
            for (int i = 0; i < wordsCount; i++)
            {
                int start = i - windowSize;
                if (start < 0)
                {
                    start = 0;
                }
                int end = i + windowSize;
                if (end >= wordsCount)
                {
                    end = wordsCount - 1;
                }
                addCount = 0;
                for (int j = start; j <= end; j++)
                {
                    try
                    {
                        if (skipWordIndex != null)
                        {
                            if (skipWordIndex[j] == true || skipWordIndex[i] == true)
                            {
                                values[i] += 0;
                                addCount++;
                                continue;
                            }
                        }
                        if (distanceTable[i, j] < 0)
                        {
                            index_a = wv.getIndex(words[j]);
                            index_b = wv.getIndex(words[i]);
                            distanceTable[i, j] = distanceTable[j, i] = wv.distance(index_a, index_b);
                        }
                        values[i] += distanceTable[i, j];
                        addCount++;
                    }
                    catch
                    {
                    }
                }
                if (addCount == 0)
                {
                    values[i] = -1;
                }
                else
                    values[i] = values[i] / addCount;
            }
        }
        int windowSize;
        int wordsCount;
        bool[] skipWordIndex;
        public double 计算修改词的value(string fixedWord, int i)
        {
            int addCount = 0;
            long index_a, index_b;
            double v = -1;
            //for (int i = 0; i < wordsCount; i++)
            {
                int start = i - windowSize;
                if (start < 0)
                {
                    start = 0;
                }
                int end = i + windowSize;
                if (end >= wordsCount)
                {
                    end = wordsCount - 1;
                }
                addCount = 0;
                for (int j = start; j <= end && j != i; j++)
                {
                    if (skipWordIndex != null)
                    {
                        if (skipWordIndex[j] == true)
                        {
                            v += 0;
                            addCount++;
                            continue;
                        }
                    }

                    index_a = wv.getIndex(words[j]);
                    index_b = wv.getIndex(fixedWord);
                    if (index_b == -1||index_a == -1)
                    {
                        return -2;//没有该词(index_b为-1时) 或者 其他错误
                    }
                    v += wv.distance(index_a, index_b);
                    addCount++;
                }

            }
            if (addCount == 0)
            {
                return -1; //没有前后文
            }
            else
                v = v / addCount;

            return v;
        }

        public long[] 获取i位置上的最佳相关词集(int i,int charCount,int count,out double[] values)
        {
            List<string> wordsList = new List<string>();//参照的单词列表

            int start = i - windowSize;
            if (start < 0)
            {
                start = 0;
            }
            int end = i + windowSize;
            if (end >= wordsCount)
            {
                end = wordsCount - 1;
            }

            for (int j = start; j <= end && j != i; j++)
            {
                if (skipWordIndex != null)
                {
                    if (skipWordIndex[j] == true)
                    {
                        continue;
                    }
                }
                wordsList.Add(words[j]);
            }

            return wv.getClosest(wordsList.ToArray(),charCount, count,out values);
        }
    }
}

//long[] list;
//float[] vector;
//double[] values;
//try
//{


/* while (true)
 {
     try
     {
         Console.Write("输入一个词：");
         string s = Console.ReadLine();

         list = wv.getClosest(s, 20, out values);
         for (long i = 0; i < 20; i++)
         {
             Console.WriteLine(i+" "+list[i]+" " + wv.getWord(list[i]) + ": " + values[i]);
         }
         Console.WriteLine( "---"+wv.getWord(list[18])+wv.distance(wv.getVector(list[18]), wv.getVector(s)));
     }
     catch (Exception e)
     {
         Console.WriteLine(e.Message);
     }
 }*/
//}
//catch
//{
//    Console.WriteLine("读入词向量文件失败！");
//}

/*
StreamReader reader = new StreamReader(filePath, Encoding.UTF8);
string text = reader.ReadToEnd();
JiebaSegmenter jieba = new JiebaSegmenter();
IEnumerable<string> words = jieba.Cut(text);
foreach (string item in words)
{
    Console.Write("(");
    foreach (char c in item)
    {
        ChineseChar ch;
        try
        {
            ch = new ChineseChar(c);
        }
        catch { continue; }
        foreach (string pinyin in ch.Pinyins)
        {
            Console.Write(pinyin + ",");
        }
        Console.Write("|");
    }
    Console.Write(")");
    Console.Write(item + "\n");
}
Console.WriteLine();
TextRankExtractor extractor = new TextRankExtractor();
TfidfExtractor extractor2 = new TfidfExtractor();
IEnumerable<WordWeightPair> keyWords = extractor.ExtractTagsWithWeight(text);
IEnumerable<WordWeightPair> keyWords2 = extractor2.ExtractTagsWithWeight(text);
foreach (WordWeightPair item in keyWords)
{
    Console.WriteLine("关键词："+item.Word + " " + item.Weight);
}
foreach (WordWeightPair item in keyWords2)
{
    Console.WriteLine("关键词2："+item.Word + " " + item.Weight);
}
*/

