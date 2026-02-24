using System.Diagnostics;
using Xunit;

namespace STL_Generator.Tests
{
    public class OpenScadServiceTests
    {
        private static readonly string[] OpenScadCandidates =
        [
            @"C:\Program Files\OpenSCAD\openscad.exe",
            @"C:\Program Files (x86)\OpenSCAD\openscad.exe",
        ];

        private static string? FindOpenScad() =>
            OpenScadCandidates.FirstOrDefault(File.Exists);

        private static async Task<(bool success, string error)> RunOpenScad(
            string openScadPath, string scadFile, string stlFile)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = openScadPath,
                    Arguments = $"-o \"{stlFile}\" \"{scadFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var stderrTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(60_000))
            {
                process.Kill(entireProcessTree: true);
                return (false, "Process timed out after 60s");
            }

            var stderr = await stderrTask;
            var success = process.ExitCode == 0 && File.Exists(stlFile) && new FileInfo(stlFile).Length > 0;
            return (success, stderr);
        }

        [Fact]
        public void FindOpenScad_ReturnsValidPath()
        {
            var path = FindOpenScad();
            if (path == null) return;

            Assert.True(File.Exists(path));
        }

        [Fact]
        public async Task RenderCube_ProducesStlFile()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scadFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.scad");
            var stlFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.stl");

            try
            {
                await File.WriteAllTextAsync(scadFile, "cube([10, 10, 10]);");

                var (success, error) = await RunOpenScad(openScadPath, scadFile, stlFile);

                Assert.True(success, $"OpenSCAD failed: {error}");
                Assert.True(File.Exists(stlFile));
                Assert.True(new FileInfo(stlFile).Length > 0);
            }
            finally
            {
                if (File.Exists(scadFile)) File.Delete(scadFile);
                if (File.Exists(stlFile)) File.Delete(stlFile);
            }
        }

        [Fact]
        public async Task RenderSphere_ProducesStlFile()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scadFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.scad");
            var stlFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.stl");

            try
            {
                await File.WriteAllTextAsync(scadFile, "sphere(r=5, $fn=32);");

                var (success, error) = await RunOpenScad(openScadPath, scadFile, stlFile);

                Assert.True(success, $"OpenSCAD failed: {error}");
                Assert.True(File.Exists(stlFile));
                Assert.True(new FileInfo(stlFile).Length > 0);
            }
            finally
            {
                if (File.Exists(scadFile)) File.Delete(scadFile);
                if (File.Exists(stlFile)) File.Delete(stlFile);
            }
        }

        [Fact]
        public async Task RenderDifference_ProducesStlFile()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scadFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.scad");
            var stlFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.stl");

            try
            {
                var scad = """
                    difference() {
                        cube([20, 20, 20], center=true);
                        cylinder(h=25, r=5, center=true, $fn=32);
                    }
                    """;
                await File.WriteAllTextAsync(scadFile, scad);

                var (success, error) = await RunOpenScad(openScadPath, scadFile, stlFile);

                Assert.True(success, $"OpenSCAD failed: {error}");
                Assert.True(new FileInfo(stlFile).Length > 0);
            }
            finally
            {
                if (File.Exists(scadFile)) File.Delete(scadFile);
                if (File.Exists(stlFile)) File.Delete(stlFile);
            }
        }

        [Fact]
        public async Task InvalidScad_FailsGracefully()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scadFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.scad");
            var stlFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.stl");

            try
            {
                await File.WriteAllTextAsync(scadFile, "this is not valid openscad code !!!");

                var (success, error) = await RunOpenScad(openScadPath, scadFile, stlFile);

                Assert.False(success);
                Assert.False(string.IsNullOrEmpty(error));
            }
            finally
            {
                if (File.Exists(scadFile)) File.Delete(scadFile);
                if (File.Exists(stlFile)) File.Delete(stlFile);
            }
        }

        [Fact]
        public async Task EmptyScadFile_ProducesNoOutput()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scadFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.scad");
            var stlFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.stl");

            try
            {
                await File.WriteAllTextAsync(scadFile, string.Empty);

                var (success, _) = await RunOpenScad(openScadPath, scadFile, stlFile);

                Assert.False(success);
            }
            finally
            {
                if (File.Exists(scadFile)) File.Delete(scadFile);
                if (File.Exists(stlFile)) File.Delete(stlFile);
            }
        }

        [Fact]
        public async Task NonExistentScadFile_Fails()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var fakeScadFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.scad");
            var stlFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.stl");

            try
            {
                var (success, error) = await RunOpenScad(openScadPath, fakeScadFile, stlFile);

                Assert.False(success);
                Assert.False(string.IsNullOrEmpty(error));
            }
            finally
            {
                if (File.Exists(stlFile)) File.Delete(stlFile);
            }
        }
    }
}
