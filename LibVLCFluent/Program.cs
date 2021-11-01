using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;

namespace LibVLCFluent
{
    class Program
    {
        const string LIBVLC_3_SOURCE_URL = "https://github.com/videolan/vlc/archive/refs/heads/3.0.x.zip";
        const string SOURCE_ARCHIVE_PATH = "source.zip";
        const string SOURCE_PATH = "source";
        static async Task Main(string[] args)
        {
            if (!Directory.Exists(SOURCE_PATH))
            {
                if (!File.Exists(SOURCE_ARCHIVE_PATH))
                {
                    using var httpClient = new WebClient();
                    await httpClient.DownloadFileTaskAsync(new Uri(LIBVLC_3_SOURCE_URL), SOURCE_ARCHIVE_PATH);
                }
                ZipFile.ExtractToDirectory(SOURCE_ARCHIVE_PATH, SOURCE_PATH);
                File.Delete(SOURCE_ARCHIVE_PATH);
            }


        }
    }
}
