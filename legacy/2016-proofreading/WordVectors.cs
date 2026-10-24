using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 改错字
{

    public class WordVectors
    {
        string[] words;//存放单词的数组
        public float[] vectors;//存放所有单词向量的数组
        long loadIndex=0;
        long vocabularyNum; //词向量文件中的总词量
        long dimension; //词向量文件的维数
        public float LoadProcess
        {
            get { return (float)loadIndex / vocabularyNum; }
        }

        #region 封装字段（vocabularyNum，dimension）
        //public string[] Words
        //{
        //    get
        //    {
        //        return words;
        //    }
        //}

        //public float[] Vectors
        //{
        //    get
        //    {
        //        return vectors;
        //    }
        //}

        public long VocabularyNum
        {
            get
            {
                return vocabularyNum;
            }
        }

        public long Dimension
        {
            get
            {
                return dimension;
            }
        }
        #endregion

        #region 构造词向量相关函数
        BinaryReader reader;
        StringBuilder stringBuilder = new StringBuilder();
        public WordVectors()
        {
            
        }
        public void LoadVectorFile(string filename)
        {
            reader = new BinaryReader(new FileStream(filename, FileMode.Open), Encoding.UTF8, false);
            vocabularyNum = long.Parse(readWord());
            dimension = long.Parse(readWord());
            words = new string[vocabularyNum];
            vectors = new float[vocabularyNum * dimension];
            buildWordVectors();
            reader.Close();
        }
        /// <summary>
        /// 读取一个词向量条目（单词+float向量）
        /// </summary>
        void buildWordVectors()
        {
            for (loadIndex = 0; loadIndex < vocabularyNum; loadIndex++)
            {
                words[loadIndex] = readWord();
                if (words[loadIndex] == "")
                {
                    loadIndex--;
                    continue;
                }
                for (long j = 0; j < dimension; j++)
                {
                    vectors[loadIndex * dimension + j] = BitConverter.ToSingle(reader.ReadBytes(sizeof(float)), 0);
                }
            }
        }

        /// <summary>
        /// 从BinaryReader中读取一个用空格分开的单词
        /// </summary>
        string readWord()
        {
            stringBuilder.Clear();
            char c;
            while (true)
            {
                c = reader.ReadChar();
                if (c == ' ' || c == '\n')
                {
                    break;
                }
                stringBuilder.Append(c);
            }
            return stringBuilder.ToString();
        }
        #endregion

        #region 归一化
        /// <summary>
        /// 词向量归一化
        /// </summary>
        public void normalizeVectors()
        {
            double len = 0;
            for (long i = 0; i < vocabularyNum; i++)
            {
                len = 0;
                for (long j = 0; j < dimension; j++)
                {
                    len += vectors[i * dimension + j] * vectors[i * dimension + j];
                }
                len = Math.Sqrt(len);
                for (long j = 0; j < dimension; j++)
                {
                    vectors[i * dimension + j] /= (float)len;
                }
            }
        }
        #endregion

        #region 查询与转换
        /// <summary>
        /// 查询单词，存在则返回索引，否则返回-1
        /// </summary>
        public long getIndex(string word)
        {
            for (long i = 0; i < vocabularyNum; i++)
            {
                if (word == words[i])
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 通过索引得到单词
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string getWord(long index)
        {
            return words[index];
        }

        /// <summary>
        /// 通过索引得到向量
        /// </summary>
        public float[] getVector(long index)
        {
            float[] v = new float[dimension];
            Array.Copy(vectors, index * dimension, v, 0, dimension);
            return v;
        }

        /// <summary>
        /// 通过单词得到向量
        /// </summary>
        public float[] getVector(string word)
        {
            long index = getIndex(word);
            return getVector(index);
        }
        #endregion

        #region 距离函数
        public double distance(float[] a, float[] b)
        {
            double len = 0;
            for (long i = 0; i < dimension; i++)
            {
                len += (double)(a[i] - b[i]) * (a[i] - b[i]);
            }
            return Math.Sqrt(len);
        }
        public double distance(long index_a, long index_b)
        {
            double len = 0;
            for (long i = 0; i < dimension; i++)
            {
                len += (double)(vectors[index_a * dimension + i] - vectors[index_b * dimension + i]) * (vectors[index_a * dimension + i] - vectors[index_b * dimension + i]);
            }
            return Math.Sqrt(len);
        }
        public double distance(string word1, string word2)
        {
            return distance(getIndex(word1), getIndex(word2));
        }
        #endregion

        #region 最近列表函数
        /// <summary>
        /// 获取与word距离最近的单词列表
        /// </summary>
        /// <param name="index">单词索引</param>
        /// <param name="quantity">列表大小</param>
        /// <param name="value">返回对应的向量列表</param>
        /// <returns>返回最近单词列表</returns>
        public long[] getClosest(long index, int quantity, out double[] value)
        {
            long[] indexList = new long[quantity];
            double[] distanceList = new double[quantity];
            for (int i = 0; i < quantity; i++) //初始化
            {
                distanceList[i] = 100;
            }
            double d;
            int end = quantity - 1;
            for (long i = 0; i < vocabularyNum; i++)
            {
                d = distance(index, i);
                if (d < distanceList[end])
                {
                    int j;
                    for (j = end - 1; j >= 0; j--)
                    {
                        if (d >= distanceList[j])
                        {
                            break;
                        }
                        else
                        {
                            distanceList[j + 1] = distanceList[j];
                            indexList[j + 1] = indexList[j];
                        }
                    }
                    distanceList[j + 1] = d;
                    indexList[j + 1] = i;
                }
            }
            value = distanceList;
            return indexList;
        }
        /// <summary>
        /// 获取与word距离最近的单词列表
        /// </summary>
        /// <param name="word"></param>
        /// <param name="quantity">列表大小</param>
        /// <param name="value">返回对应的向量列表</param>
        /// <returns>返回最近单词列表</returns>
        public long[] getClosest(string word, int quantity, out double[] value)
        {
            return getClosest(getIndex(word), quantity, out value);
        }

        /// <summary>
        /// 参照一个单词索引表获取最近距离的单词表(索引为-1则跳过)
        /// </summary>
        /// <param name="indexs">参照的单词索引表</param>
        /// <param name="quantity">要得到的最近距离单词数量</param>
        /// <param name="value">得到的平均距离值表</param>
        /// <returns>返回最近单词列表</returns>
        public long[] getClosest(long[] indexs,int charCount, int quantity, out double[] value)
        {
            if (indexs.Length==0)
            {
                value = null;
                return null;
            }
            long[] indexList = new long[quantity];
            double[] distanceList = new double[quantity];
            for (int i = 0; i < quantity; i++) //初始化
            {
                distanceList[i] = 100;
            }
            double d=0;
            int end = quantity - 1;
            for (long i = 0; i < vocabularyNum; i++)
            {
                if (getWord(i).Length!=charCount)
                {
                    continue;
                }
                d = 0;
                int count = 0;
                foreach (long index in indexs)
                {
                    if (index==-1)
                    {
                        continue;
                    }
                    d += distance(index, i);
                    count++;
                }
                d /= count;
                if (d < distanceList[end])
                {
                    int j;
                    for (j = end - 1; j >= 0; j--)
                    {
                        if (d >= distanceList[j])
                        {
                            break;
                        }
                        else
                        {
                            distanceList[j + 1] = distanceList[j];
                            indexList[j + 1] = indexList[j];
                        }
                    }
                    distanceList[j + 1] = d;
                    indexList[j + 1] = i;
                }
            }
            value = distanceList;
            return indexList;
        }

        /// <summary>
        /// 参照给定的单词表获取距离最近的单词列表
        /// </summary>
        /// <param name="words">参照的单词表</param>
        /// <param name="quantity">列表大小</param>
        /// <param name="value">返回对应的向量列表</param>
        /// <returns>返回最近单词列表</returns>
        public long[] getClosest(string[] words,int charCount, int quantity, out double[] value)
        {
            long[] indexs = new long[words.Length];
            for (int i = 0; i < indexs.Length; i++)
            {
                indexs[i] = getIndex(words[i]);
            }   
            return getClosest(indexs,charCount, quantity, out value);
        }
        #endregion

    }

}

