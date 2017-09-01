using System.IO;
using System.Reflection;

namespace ScaffoldTool
{
    internal static class Global
    {
        // 立杆横距
        public static double LGHJ = 900 / 304.8;
        // 立柱纵距
        public static double LGZJ = 1500 / 304.8;
        // 内排架距离墙体长度
        public static double NJJQ = 350 / 304.8;
        // 脚手架杆件直径
        public static double D = 48.3 / 304.8;
        // 脚手架杆件横截面厚度
        public static double T = 3.6 / 304.8;
        // 扫地杆相对底部偏移
        public static double PIPE_BASE_OFFSET = 200 / 304.8;
        // 立杆超出作业面距离
        public static double COLUMN_BEYOND_TOP = 1000 / 304.8;
        // 横杠步距
        public static double BJ = 1500 / 304.8;
        // 斜杠跨数（非等距斜杆参数）
        public static int SLANT_ROD_HORIZONTAL_SPAN = 4;
        // 斜杠步数（非等距斜杆参数）
        public static int SLANT_ROD_VERTICAL_SPAN = 5;
        // 大横杆超出距离
        public static double ROW_BEYOND_DISTANCE = 100 / 304.8;
        // 护栏横杆基于一个步距分段数
        public static int GUARDRAIL_DIVIDED_NUM = 3;
        // 连墙件跨数
        public static int LQJKS = 3;
        // 脚手板厚度
        public static double SCAFFOLD_HORIZONTAL_FLOOR_THICK = 50 / 304.8;
        // 挡脚板厚度
        public static double SCAFFOLD_VERTICAL_FLOOR_THICK = 30 / 304.8;
        // 脚手板放置相隔步距数
        public static int SCAFFOLD_FLOOR_SPAN = 2;
        // 安全网厚度
        public static double SCAFFOLD_NET_THICK = 1 / 304.8;
        // 垫板厚度（落地式）
        public static double BOTTOM_PLATE_THICK = 50 / 304.8;
        // 垫板边长（落地式）
        public static double BOTTOM_PLATE_LENGTH = 300 / 304.8;
        // 斜杆间距离（等距斜杆参数）
        public static double SLANT_ROD_EQUIDISTANCE = 6000 / 304.8;
        // 工字钢高度（悬挑式）
        public static double BOTTOM_OVERHANG_BEAM_HEIGHT = 200 / 304.8;
        // 工字钢超出距离（悬挑式）
        public static double OVERHANG_BEAM_BEYOND_DISTANCE = 300 / 304.8;
        // 悬挑式脚手架模型段间距
        public static double OVERHANG_EVERY_SECTION_OFFSET = 200 / 304.8;
        // 脚手架悬挑梁最大间距
        public static double OVERHANG_MAX_DISTANCE = 20000 / 304.8;
        // 当前工作程序集根目录
        public static string ASSEMBLY_DIRECTORY_PATH = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static double GetNextOffset()
        {
            mCurrentRowOffset = mCurrentRowOffset < D ? D : 0;
            return mCurrentRowOffset;
        }

        private static double mCurrentRowOffset = 0;
    }
}
