using System.Diagnostics;
using Xunit;

namespace STL_Generator.Tests
{
    public class ScadValidationTests
    {
        private static readonly string[] OpenScadCandidates =
        [
            @"C:\Program Files\OpenSCAD\openscad.exe",
            @"C:\Program Files (x86)\OpenSCAD\openscad.exe",
        ];

        private static string? FindOpenScad() =>
            OpenScadCandidates.FirstOrDefault(File.Exists);

        private static async Task<(bool valid, string output)> ValidateScad(
            string openScadPath, string scadFile)
        {
            var tempStl = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.stl");

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = openScadPath,
                        Arguments = $"-o \"{tempStl}\" \"{scadFile}\"",
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
                return (process.ExitCode == 0, stderr);
            }
            finally
            {
                if (File.Exists(tempStl)) File.Delete(tempStl);
            }
        }

        [Fact]
        public async Task ValidCube_PassesValidation()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scadFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.scad");

            try
            {
                await File.WriteAllTextAsync(scadFile, "cube([10, 10, 10]);");

                var (valid, _) = await ValidateScad(openScadPath, scadFile);

                Assert.True(valid);
            }
            finally
            {
                if (File.Exists(scadFile)) File.Delete(scadFile);
            }
        }

        [Fact]
        public async Task SyntaxError_FailsValidation()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scadFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.scad");

            try
            {
                await File.WriteAllTextAsync(scadFile, "cube([10, 10, 10);"); // missing bracket

                var (valid, output) = await ValidateScad(openScadPath, scadFile);

                Assert.False(valid);
                Assert.False(string.IsNullOrEmpty(output));
            }
            finally
            {
                if (File.Exists(scadFile)) File.Delete(scadFile);
            }
        }

        [Fact]
        public async Task ComplexValidScad_PassesValidation()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scadFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.scad");

            try
            {
                var scad = """
                    $fn = 32;
                    difference() {
                        cube([20, 20, 20], center=true);
                        cylinder(h=25, r=5, center=true);
                        rotate([90, 0, 0])
                            cylinder(h=25, r=5, center=true);
                        rotate([0, 90, 0])
                            cylinder(h=25, r=5, center=true);
                    }
                    """;
                await File.WriteAllTextAsync(scadFile, scad);

                var (valid, _) = await ValidateScad(openScadPath, scadFile);

                Assert.True(valid);
            }
            finally
            {
                if (File.Exists(scadFile)) File.Delete(scadFile);
            }
        }

        [Fact]
        public async Task ModuleDefinition_PassesValidation()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scadFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.scad");

            try
            {
                var scad = """
                    module rounded_cube(size, radius) {
                        minkowski() {
                            cube(size - [radius*2, radius*2, radius*2], center=true);
                            sphere(r=radius, $fn=16);
                        }
                    }
                    rounded_cube([20, 20, 20], 2);
                    """;
                await File.WriteAllTextAsync(scadFile, scad);

                var (valid, _) = await ValidateScad(openScadPath, scadFile);

                Assert.True(valid);
            }
            finally
            {
                if (File.Exists(scadFile)) File.Delete(scadFile);
            }
        }

        [Fact]
        public async Task UndefinedModule_ProducesWarning()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scadFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.scad");

            try
            {
                await File.WriteAllTextAsync(scadFile, "nonexistent_module();");

                var (_, output) = await ValidateScad(openScadPath, scadFile);

                Assert.Contains("WARNING", output, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(scadFile)) File.Delete(scadFile);
            }
        }
    }
}
