using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace PatchingStaticServer.Controllers
{
    [Route("config")]
    public class ConfigController : Controller
    {
        string m_appDirectory;

        public ConfigController()
        {
            m_appDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        [HttpGet("config.html")]
        public object ConfigPage()
        {
            var resourceStream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("PatchingStaticServer.HTML.config.html");

            return File(resourceStream, "text/html");
        }

        [HttpGet("read")]
        public object Read()
        {
            return new
            {
                element = Global.Element.ToString().ToLowerInvariant(),
                position = Global.Position.ToString().ToLowerInvariant(),
                patch = Global.Patch,
                roots = GetRoots(),
                root = Global.Root
            };
        }

		[HttpPost("write")]
		public void Write([FromBody] ConfigDto configDto)
		{
			Global.Patch = configDto.Patch;
			if (configDto.Element == "head")
				Global.Element = Global.PatchElement.Head;
			else
				Global.Element = Global.PatchElement.Body;
			if (configDto.Position == "start")
				Global.Position = Global.PatchPosition.Start;
			else
				Global.Position = Global.PatchPosition.End;
			Global.Root = configDto.Root;
		}

        private List<string> GetRoots()
        {
            var zipFiles = Directory
                .EnumerateFiles(m_appDirectory, "*.zip", SearchOption.TopDirectoryOnly)
                .Where(s => IsValidZipFile(s));
            var folders = Directory
                .EnumerateDirectories(m_appDirectory, "*", SearchOption.TopDirectoryOnly)
                .Where(s => IsValidDirectory(s));

            return folders
                .Concat(zipFiles)
                .Select(s => Path.GetFileName(s))
                .OrderBy(s => s)
                .ToList();
        }

        bool IsValidZipFile(string path)
        {
            if (Path.GetFileName(path).StartsWith(".", StringComparison.Ordinal))
                return false;

            if (path.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        bool IsValidDirectory(string path)
        {
            if (Path.GetFileName(path).StartsWith(".", StringComparison.Ordinal))
                return false;

            if (Directory.Exists(path))
                return true;

            return false;
        }
    }

    public class ConfigDto
    {
        public string Element { get; set; }
        public string Position { get; set; }
        public string Patch { get; set; }
        public string Root { get; set; }
    }
}
