using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NCMConverter
{
    /// <summary>
    /// NeteaseCrypt C# Wrapper for NCMConverter
    /// </summary>
    public class NcmCrypt
    {
        const string DLL_PATH = "libncmdump.dll";

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CreateNeteaseCrypt(IntPtr path);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern int Dump(IntPtr NeteaseCrypt, IntPtr outputPath);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void FixMetadata(IntPtr NeteaseCrypt);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void DestroyNeteaseCrypt(IntPtr NeteaseCrypt);

        private IntPtr NeteaseCryptClass = IntPtr.Zero;

        /// <summary>
        /// 创建 NeteaseCrypt 类的实例。
        /// </summary>
        /// <param name="FileName">网易云音乐 ncm 加密文件路径</param>
        public NcmCrypt(string FileName)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(FileName);

            IntPtr inputPtr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, inputPtr, bytes.Length);
            Marshal.WriteByte(inputPtr, bytes.Length, 0);

            NeteaseCryptClass = CreateNeteaseCrypt(inputPtr);
        }

        /// <summary>
        /// 启动转换过程。
        /// </summary>
        /// <param name="OutputPath">指定一个路径输出，如果为空，则输出到原路径</param>
        /// <returns>返回一个整数，指示转储过程的结果。如果成功，返回0；如果失败，返回1。</returns>
        public int Dump(string OutputPath)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(OutputPath);

            IntPtr outputPtr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, outputPtr, bytes.Length);
            Marshal.WriteByte(outputPtr, bytes.Length, 0);

            return Dump(NeteaseCryptClass, outputPtr);
        }

        /// <summary>
        /// 修复音乐文件元数据。
        /// </summary>
        public void FixMetadata()
        {
            FixMetadata(NeteaseCryptClass);
        }

        /// <summary>
        /// 销毁 NeteaseCrypt 类的实例。
        /// </summary>
        public void Destroy()
        {
            DestroyNeteaseCrypt(NeteaseCryptClass);
        }
    }

    public static class NcmConvertService
    {
        private const string DLL_FILENAME = "libncmdump.dll";

        private static string AppDirectory => AppDomain.CurrentDomain.BaseDirectory;
        private static string LocalDllPath => Path.Combine(AppDirectory, DLL_FILENAME);

        /// <summary>
        /// 异步转换NCM文件
        /// </summary>
        /// <param name="inputFile">输入的NCM文件路径</param>
        /// <param name="outputDir">输出目录</param>
        /// <param name="log">日志回调函数</param>
        /// <returns>转换是否成功</returns>
        public static async Task<bool> ConvertNcmAsync(string inputFile, string outputDir, Action<string> log)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 检查输入文件是否存在
                    if (!File.Exists(inputFile))
                    {
                        log($"错误: 输入文件不存在: {inputFile}");
                        return false;
                    }

                    // 检查DLL文件是否存在
                    if (!File.Exists(LocalDllPath))
                    {
                        log("错误: 核心库文件缺失，请重新启动程序以自动下载。");
                        return false;
                    }

                    // 确保输出目录存在
                    if (!Directory.Exists(outputDir))
                    {
                        try
                        {
                            Directory.CreateDirectory(outputDir);
                        }
                        catch (Exception ex)
                        {
                            log($"错误: 无法创建输出目录 {outputDir}。{ex.Message}");
                            return false;
                        }
                    }

                    log($"正在转换: {Path.GetFileName(inputFile)}");

                    // 使用libncmdump.dll进行转换
                    var crypt = new NcmCrypt(inputFile);
                    int result = crypt.Dump(outputDir);
                    crypt.Destroy();

                    if (result == 0)
                    {
                        log($"转换成功: {Path.GetFileName(inputFile)}");
                        return true;
                    }
                    else
                    {
                        log($"转换失败: {Path.GetFileName(inputFile)} (错误码: {result})");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    log($"转换过程中发生异常: {ex.Message}");
                    return false;
                }
            });
        }
    }
}