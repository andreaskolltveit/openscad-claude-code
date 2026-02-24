using System.Diagnostics;
using Xunit;

namespace STL_Generator.Tests
{
    public class RenderPipelineTests
    {
        private static readonly string[] OpenScadCandidates =
        [
            @"C:\Program Files\OpenSCAD\openscad.exe",
            @"C:\Program Files (x86)\OpenSCAD\openscad.exe",
        ];

        private static string? FindOpenScad() =>
            OpenScadCandidates.FirstOrDefault(File.Exists);

        private static string GetProjectRoot()
        {
            var dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "STL Generator.sln")))
                dir = Directory.GetParent(dir)?.FullName;
            return dir ?? throw new DirectoryNotFoundException("Could not find project root");
        }

        private static async Task<(int exitCode, string stdout, string stderr)> RunProcess(
            string fileName, string arguments, int timeoutMs = 60_000)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(timeoutMs))
            {
                process.Kill(entireProcessTree: true);
                return (-1, "", $"Process timed out after {timeoutMs}ms");
            }

            return (process.ExitCode, await stdoutTask, await stderrTask);
        }

        [Fact]
        public async Task EndToEnd_WriteScadAndRenderStl()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var tempDir = Path.Combine(Path.GetTempPath(), $"scad_e2e_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            var scadFile = Path.Combine(tempDir, "test-model.scad");
            var stlFile = Path.Combine(tempDir, "test-model.stl");

            try
            {
                // Step 1: Write .scad file
                var scadContent = """
                    $fn = 32;
                    difference() {
                        cube([20, 20, 10], center=true);
                        translate([0, 0, 2])
                            cube([16, 16, 10], center=true);
                    }
                    """;
                await File.WriteAllTextAsync(scadFile, scadContent);
                Assert.True(File.Exists(scadFile));

                // Step 2: Render to STL
                var (exitCode, _, stderr) = await RunProcess(
                    openScadPath, $"-o \"{stlFile}\" \"{scadFile}\"");

                Assert.Equal(0, exitCode);

                // Step 3: Verify STL output
                Assert.True(File.Exists(stlFile), "STL file should exist");
                var stlInfo = new FileInfo(stlFile);
                Assert.True(stlInfo.Length > 0, "STL file should not be empty");
                Assert.True(stlInfo.Length > 100, "STL file should contain meaningful geometry");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public async Task EndToEnd_ParametricDesign()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var tempDir = Path.Combine(Path.GetTempPath(), $"scad_e2e_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            var scadFile = Path.Combine(tempDir, "parametric.scad");
            var stlFile = Path.Combine(tempDir, "parametric.stl");

            try
            {
                var scadContent = """
                    $fn = 32;
                    wall = 2;
                    width = 30;
                    depth = 20;
                    height = 15;

                    module box(w, d, h, t) {
                        difference() {
                            cube([w, d, h]);
                            translate([t, t, t])
                                cube([w - 2*t, d - 2*t, h]);
                        }
                    }

                    box(width, depth, height, wall);
                    """;
                await File.WriteAllTextAsync(scadFile, scadContent);

                var (exitCode, _, _) = await RunProcess(
                    openScadPath, $"-o \"{stlFile}\" \"{scadFile}\"");

                Assert.Equal(0, exitCode);
                Assert.True(File.Exists(stlFile));
                Assert.True(new FileInfo(stlFile).Length > 100);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public async Task ExampleFiles_AllRenderSuccessfully()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var projectRoot = GetProjectRoot();
            var examplesDir = Path.Combine(projectRoot, "examples");

            if (!Directory.Exists(examplesDir)) return;

            var scadFiles = Directory.GetFiles(examplesDir, "*.scad");
            Assert.NotEmpty(scadFiles);

            foreach (var scadFile in scadFiles)
            {
                var stlFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.stl");

                try
                {
                    var (exitCode, _, stderr) = await RunProcess(
                        openScadPath, $"-o \"{stlFile}\" \"{scadFile}\"");

                    Assert.True(exitCode == 0,
                        $"Example {Path.GetFileName(scadFile)} failed to render: {stderr}");
                    Assert.True(File.Exists(stlFile),
                        $"Example {Path.GetFileName(scadFile)} produced no output");
                    Assert.True(new FileInfo(stlFile).Length > 0,
                        $"Example {Path.GetFileName(scadFile)} produced empty STL");
                }
                finally
                {
                    if (File.Exists(stlFile)) File.Delete(stlFile);
                }
            }
        }

        [Fact]
        public async Task RenderPng_ProducesImageFile()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scadFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.scad");
            var pngFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

            try
            {
                await File.WriteAllTextAsync(scadFile, "cube([10, 10, 10]);");

                var (exitCode, _, stderr) = await RunProcess(
                    openScadPath, $"-o \"{pngFile}\" --imgsize=512,512 \"{scadFile}\"");

                Assert.Equal(0, exitCode);
                Assert.True(File.Exists(pngFile), $"PNG not created. stderr: {stderr}");
                Assert.True(new FileInfo(pngFile).Length > 0);
            }
            finally
            {
                if (File.Exists(scadFile)) File.Delete(scadFile);
                if (File.Exists(pngFile)) File.Delete(pngFile);
            }
        }
        [Fact]
        public async Task AnalyzeStl_ProducesReport()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var projectRoot = GetProjectRoot();
            var analyzeScript = Path.Combine(projectRoot, "scripts", "analyze-stl.py");
            if (!File.Exists(analyzeScript)) return;

            // Generate a test STL first
            var scadFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.scad");
            var stlFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.stl");

            try
            {
                await File.WriteAllTextAsync(scadFile, "cube([20, 15, 10]);");

                var (renderExit, _, renderErr) = await RunProcess(
                    openScadPath, $"-o \"{stlFile}\" \"{scadFile}\"");
                Assert.Equal(0, renderExit);

                // Run analyze script
                var (exitCode, stdout, stderr) = await RunProcess(
                    "python", $"\"{analyzeScript}\" \"{stlFile}\"");

                Assert.Equal(0, exitCode);
                Assert.Contains("Bounding box", stdout);
                Assert.Contains("Triangles", stdout);
                Assert.Contains("20.00", stdout); // width
                Assert.Contains("15.00", stdout); // height
                Assert.Contains("10.00", stdout); // depth
                Assert.Contains("Face normals", stdout);
                Assert.Contains("Z-level distribution", stdout);
                Assert.Contains("ASCII cross-sections", stdout);
                Assert.Contains("Floor surfaces", stdout);
                Assert.Contains("Ceiling surfaces", stdout);
                Assert.Contains("Pocket/void detection", stdout);
            }
            finally
            {
                if (File.Exists(scadFile)) File.Delete(scadFile);
                if (File.Exists(stlFile)) File.Delete(stlFile);
            }
        }
    }
}
