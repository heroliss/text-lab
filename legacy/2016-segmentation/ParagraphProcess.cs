using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;


namespace 分词工具
{
    [StructLayout(LayoutKind.Explicit)]
    public struct result_t
    {
        [FieldOffset(0)]
        public int start;
        [FieldOffset(4)]
        public int length;
        [FieldOffset(8)]
        public int sPos1;
        [FieldOffset(12)]
        public int sPos2;
        [FieldOffset(16)]
        public int sPos3;
        [FieldOffset(20)]
        public int sPos4;
        [FieldOffset(24)]
        public int sPos5;
        [FieldOffset(28)]
        public int sPos6;
        [FieldOffset(32)]
        public int sPos7;
        [FieldOffset(36)]
        public int sPos8;
        [FieldOffset(40)]
        public int sPos9;
        [FieldOffset(44)]
        public int sPos10;
        //[FieldOffset(12)] public int sPosLow;
        [FieldOffset(48)]
        public int POS_id;
        [FieldOffset(52)]
        public int word_ID;
        [FieldOffset(56)]
        public int word_type;
        [FieldOffset(60)]
        public int weight;
    }
    /*
    struct result_t{
  int start; //start position,词语在输入句子中的开始位置
  int length; //length,词语的长度
  char  sPOS[POS_SIZE];//word type，词性ID值，可以快速的获取词性表
  int	iPOS;//词性标注的编号
  int word_ID; //该词的内部ID号，如果是未登录词，设成0或者-1
  int word_type; //区分用户词典;1，是用户词典中的词；0，非用户词典中的词
  int weight;//word weight,read weight
 };*/


    static class ParagraphProcess
    {
        static public string dataFilePath = "";
        const string path = @"NLPIR.dll";//设定dll的路径
        //对函数进行申明
        [DllImport(path, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi, EntryPoint = "NLPIR_Init")]
        static extern bool NLPIR_Init(String sInitDirPath, int encoding, String sLicenseCode);

        //特别注意，C语言的函数NLPIR_API const char * NLPIR_ParagraphProcess(const char *sParagraph,int bPOStagged=1);必须对应下面的申明
        [DllImport(path, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi, EntryPoint = "NLPIR_ParagraphProcess")]
        static extern IntPtr NLPIR_ParagraphProcess(String sParagraph, int bPOStagged = 0);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_Exit")]
        static extern bool NLPIR_Exit();

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_GetParagraphProcessAWordCount")]
        static extern int NLPIR_GetParagraphProcessAWordCount(String sParagraph);

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static public string exec(string s)
        {

            if (!NLPIR_Init(dataFilePath, 0, ""))//给出Data文件所在的路径，注意根据实际情况修改。
            {
                throw new ApplicationException("Data文件路径错误!");
            }
            int count = NLPIR_GetParagraphProcessAWordCount(s);//先得到结果的词数
            System.Console.WriteLine("NLPIR_GetParagraphProcessAWordCount success!");

            IntPtr intPtr = NLPIR_ParagraphProcess(s, 0);//切分结果保存为IntPtr类型
            string str = Marshal.PtrToStringAnsi(intPtr);//将切分结果转换为string
            NLPIR_Exit();
            return str;
        }
    }
}
