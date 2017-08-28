using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace PatchingStaticServer
{
    public class PatchingFileProvider : IFileProvider
    {
        public IFileInfo GetFileInfo(string subpath)
        {
            subpath = subpath.TrimStart(Path.DirectorySeparatorChar);  // Remove leading '/'

            if (Global.Root.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return new ZipFileInfo(Global.AppDirectory, Global.Root, subpath);
            }
            else
            {
                var fullPath = Path.Combine(Global.AppDirectory, Global.Root, subpath);
                var fileInfo = new FileInfo(fullPath);
                return new PatchablePhysicalFileInfo(fileInfo);
            }
        }

		public IDirectoryContents GetDirectoryContents(string subpath) => throw new NotImplementedException();
		public IChangeToken Watch(string filter) => throw new NotImplementedException();
    }

    public class ZipFileInfo : IFileInfo
    {
        public string ZipFilename { get; set; }
        public string AppDirectory { get; }

        public ZipFileInfo(string appDirectory, string zipFilename, string path)
        {
            AppDirectory = appDirectory;
            PhysicalPath = path;
            ZipFilename = zipFilename;

            using (var file = File.OpenRead(Path.Combine(AppDirectory, ZipFilename)))
			using (var archive = new ZipArchive(file, ZipArchiveMode.Read))
			{
			    var zipFile = archive.GetEntry(PhysicalPath);
				if (zipFile != null)
				{
                    Exists = true;
                    Length = zipFile.Length;  // Add patch length?
                    Name = zipFile.Name;
                    LastModified = zipFile.LastWriteTime;
				}
                else
                {
                    Exists = false;
                }
			}
		}

        public bool Exists { get; set; }
        public long Length { get; set; }
        public string PhysicalPath { get; set; }
        public string Name { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
			using (var file = File.OpenRead(Path.Combine(AppDirectory, ZipFilename)))
			using (var archive = new ZipArchive(file, ZipArchiveMode.Read))
			{
				var zipFile = archive.GetEntry(PhysicalPath);
				if (zipFile != null)
				{
                    using (var zipEntryStream = zipFile.Open())
                    {
                        if(PhysicalPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                        {
							using (var reader = new StreamReader(zipEntryStream))
							{
								var content = reader.ReadToEnd();
                                return Patcher.PatchHtml(content);
							}
						}
                        else
                        {
							var ms = new MemoryStream();
							zipEntryStream.CopyTo(ms);
							ms.Position = 0; // rewind
							return ms;
						}
                    }
				}
			}
            throw new Exception("Failed");
		}
    }

    public class PatchablePhysicalFileInfo : IFileInfo
    {
        private readonly FileInfo m_info;
        private int m_extraLength = 0;

        public PatchablePhysicalFileInfo(FileInfo info)
        {
            m_info = info;
            if (PhysicalPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                m_extraLength = Global.Patch.Length;
        }

        public bool           Exists         => m_info.Exists;
        public long           Length         => m_info.Length + m_extraLength;
        public string         PhysicalPath   => m_info.FullName;
        public string         Name           => m_info.Name;
        public DateTimeOffset LastModified   => m_info.LastWriteTimeUtc;
        public bool           IsDirectory    => false;

        public Stream CreateReadStream()
        {
            if (PhysicalPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                var originalContent = File.ReadAllText(PhysicalPath, Encoding.UTF8);
                return Patcher.PatchHtml(originalContent);
            }
            else
            {
                var bufferSize = 1;
                return new FileStream(PhysicalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            }        
        }
	}

    public static class Patcher
    {
        public static Stream PatchHtml(string input)
        {
			string target;
			if (Global.Position == Global.PatchPosition.Start)
				target = Global.Element == Global.PatchElement.Body ? "<body[ ]*[^>]*>" : "<head>";
			else
				target = Global.Element == Global.PatchElement.Body ? "</body>" : "</head>";

            string replacement = "";
            string output;

            var regex = new Regex(target, RegexOptions.IgnoreCase);
            var match = regex.Match(input);
            if (match.Success)
            {
                replacement = Global.Position == Global.PatchPosition.Start ? match.Value + Global.Patch : Global.Patch + match.Value; 
                output = regex.Replace(input, replacement, 1);
            }
            else
            {
                output = input;
            }

			byte[] byteArray = Encoding.UTF8.GetBytes(output);
			MemoryStream stream = new MemoryStream(byteArray);

			return stream;
		}
    }
}


