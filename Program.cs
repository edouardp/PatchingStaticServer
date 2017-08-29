using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace PatchingStaticServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseConfiguration(new ConfigurationBuilder().AddCommandLine(args).Build())  // --urls "http://*:1234"
                .Build();
    }

    public static class Global
    {
		public enum PatchElement { Head, Body };
		public enum PatchPosition { Start, End };

        public static string AppDirectory { get; } = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Content");

		public static string Patch { get; set; } = "<script>alert('Bong!')</script>";
		public static PatchElement Element { get; set; } = PatchElement.Body;
		public static PatchPosition Position { get; set; } = PatchPosition.End;
        public static string Root { get; set; } = "wwwroot";
    }
}
