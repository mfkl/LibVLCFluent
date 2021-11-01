using CppAst;
using System;
using System.Diagnostics;
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
        const string SOURCE_MAJOR_VERSION = "vlc-3.0.x";
        const string VLC = "vlc";
        const string INCLUDE = "include";
        const string VLC_ARRAYS = "vlc_arrays.h";
        const string VLC_FIXUPS = "vlc_fixups.h";
        const string SRC = "src";
        const string LIBVLC_MEDIA = "libvlc_media.h";
        const string LIBVLC_MODULE = "libvlc-module.c";

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


            var vlcDir = Path.Combine(SOURCE_PATH, SOURCE_MAJOR_VERSION);
            PatchHeader(VLC_ARRAYS);
            PatchHeader(VLC_FIXUPS);

            var r = Parse(vlcDir);
            if (r.Diagnostics.HasErrors)
            {
                foreach (var m in r.Diagnostics.Messages)
                {
                    if (m.Type != CppLogMessageType.Error) continue;
                    Debug.WriteLine($"{nameof(m.Location)}: {m.Location}");
                    Debug.WriteLine(m.Text);
                }
            }
            else
                Debug.WriteLine("no error");
        }

        private static void PatchHeader(string header)
        {
            var fileToPatch = Path.Combine(SOURCE_PATH, SOURCE_MAJOR_VERSION, INCLUDE, header);
            var lines = File.ReadAllText(fileToPatch);

            if (lines[0] == '/') // first char of the non patched file.
            {
                const string patch = @"#include <limits.h>
#include <stddef.h>
#if SIZE_MAX == UINT_MAX
typedef int ssize_t;        /* common 32 bit case */
#elif SIZE_MAX == ULLONG_MAX
typedef long long ssize_t;  /* windows 64 bits */
#endif";
                File.WriteAllText(fileToPatch, patch + lines);
            }

            // comment out
            // // void *lfind( const void *key, const void *base, size_t *nmemb,
            //              size_t size, int(*cmp)(const void *, const void *) )
            // in vlc_fixups.h
        }

        private static CppCompilation Parse(string vlcDir)
        {
            var parserOptions = new CppParserOptions();

            parserOptions.Defines.Add("HAVE_FDOPENDIR");
            parserOptions.Defines.Add("HAVE_DIRFD");
            parserOptions.Defines.Add("HAVE_LLDIV");
            parserOptions.Defines.Add("HAVE_SWAB");
            parserOptions.Defines.Add("HAVE_STRUCT_POLLFD");
            parserOptions.Defines.Add("HAVE_STRUCT_TIMESPEC");
            parserOptions.Defines.Add("HAVE_REALPATH");
            parserOptions.Defines.Add("HAVE_GETPID"); 

            parserOptions.IncludeFolders.Add(Path.Combine(vlcDir, INCLUDE));

            return CppParser.ParseFiles(new System.Collections.Generic.List<string>
            {
                Path.Combine(vlcDir, INCLUDE, "vlc_fixups.h"),
                Path.Combine(vlcDir, SRC, LIBVLC_MODULE)
            }, parserOptions);

            //return CppParser.ParseFile(Path.Combine(vlcDir, INCLUDE, "vlc_fixups.h"), parserOptions);
            //return CppParser.ParseFile(Path.Combine(vlcDir, SRC, LIBVLC_MODULE), parserOptions);
        }
    }    
}
