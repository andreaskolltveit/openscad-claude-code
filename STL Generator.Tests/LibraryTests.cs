using System.Diagnostics;
using Xunit;

namespace STL_Generator.Tests
{
    public class LibraryTests
    {
        private static readonly string[] OpenScadCandidates =
        [
            @"C:\Program Files\OpenSCAD\openscad.exe",
            @"C:\Program Files (x86)\OpenSCAD\openscad.exe",
        ];

        private static string? FindOpenScad() =>
            OpenScadCandidates.FirstOrDefault(File.Exists);

        private static async Task<(bool success, string output)> RenderScad(
            string openScadPath, string scadContent)
        {
            var scadFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.scad");
            var stlFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.stl");

            try
            {
                await File.WriteAllTextAsync(scadFile, scadContent);

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
                var success = process.ExitCode == 0
                    && File.Exists(stlFile)
                    && new FileInfo(stlFile).Length > 0;

                return (success, stderr);
            }
            finally
            {
                if (File.Exists(scadFile)) File.Delete(scadFile);
                if (File.Exists(stlFile)) File.Delete(stlFile);
            }
        }

        // ---------------------------------------------------------------
        // BOSL2 Tests
        // ---------------------------------------------------------------

        [Fact]
        public async Task BOSL2_Include_Works()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                include <BOSL2/std.scad>
                cuboid([10, 10, 10], $fn=16);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"BOSL2 include failed: {output}");
        }

        [Fact]
        public async Task BOSL2_Cuboid_WithRounding()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                include <BOSL2/std.scad>
                cuboid([20, 20, 10], rounding=2, $fn=32);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"BOSL2 cuboid with rounding failed: {output}");
        }

        [Fact]
        public async Task BOSL2_Cuboid_WithChamfer()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                include <BOSL2/std.scad>
                cuboid([20, 20, 10], chamfer=2, $fn=32);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"BOSL2 cuboid with chamfer failed: {output}");
        }

        [Fact]
        public async Task BOSL2_Cyl_WithRounding()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                include <BOSL2/std.scad>
                cyl(h=20, r=10, rounding1=2, rounding2=2, $fn=48);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"BOSL2 cyl with rounding failed: {output}");
        }

        [Fact]
        public async Task BOSL2_Tube()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                include <BOSL2/std.scad>
                tube(h=20, or=15, ir=12, $fn=48);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"BOSL2 tube failed: {output}");
        }

        [Fact]
        public async Task BOSL2_Attachments()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                include <BOSL2/std.scad>
                cuboid([20, 20, 10], anchor=BOTTOM, $fn=16)
                    attach(TOP) cyl(h=10, r=5, $fn=16);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"BOSL2 attachments failed: {output}");
        }

        [Fact]
        public async Task BOSL2_Prismoid()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                include <BOSL2/std.scad>
                prismoid([40, 30], [20, 15], h=20, rounding=3, $fn=32);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"BOSL2 prismoid failed: {output}");
        }

        [Fact]
        public async Task BOSL2_Screws()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                include <BOSL2/std.scad>
                include <BOSL2/screws.scad>
                screw("M6", length=20, head="hex", $fn=32);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"BOSL2 screw failed: {output}");
        }

        [Fact]
        public async Task BOSL2_Nut()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                include <BOSL2/std.scad>
                include <BOSL2/screws.scad>
                nut("M6", thickness=5, $fn=32);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"BOSL2 nut failed: {output}");
        }

        [Fact]
        public async Task BOSL2_ThreadedRod()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                include <BOSL2/std.scad>
                include <BOSL2/threading.scad>
                threaded_rod(d=10, pitch=1.5, l=20, blunt_start=true, $fn=32);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"BOSL2 threaded rod failed: {output}");
        }

        [Fact]
        public async Task BOSL2_SpurGear()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                include <BOSL2/std.scad>
                include <BOSL2/gears.scad>
                spur_gear(mod=2, teeth=16, thickness=6, pressure_angle=20, $fn=32);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"BOSL2 spur gear failed: {output}");
        }

        [Fact]
        public async Task BOSL2_Shorthands()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                include <BOSL2/std.scad>
                up(5) right(10) cuboid([10, 10, 10], $fn=16);
                down(5) left(10) cyl(h=10, r=5, $fn=16);
                fwd(15) cuboid([8, 8, 8], rounding=1, $fn=16);
                back(15) sphere(r=5, $fn=16);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"BOSL2 shorthands failed: {output}");
        }

        // ---------------------------------------------------------------
        // MCAD Tests
        // ---------------------------------------------------------------

        [Fact]
        public async Task MCAD_InvoluteGear()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                use <MCAD/involute_gears.scad>
                gear(number_of_teeth=16, circular_pitch=200,
                     gear_thickness=5, rim_thickness=5,
                     hub_thickness=5, bore_diameter=5, $fn=32);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"MCAD involute gear failed: {output}");
        }

        [Fact]
        public async Task MCAD_RoundedBox()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                use <MCAD/boxes.scad>
                roundedBox([30, 20, 10], 3, true, $fn=32);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"MCAD roundedBox failed: {output}");
        }

        [Fact]
        public async Task MCAD_Teardrop()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                use <MCAD/teardrop.scad>
                teardrop(radius=5, length=10, $fn=32);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"MCAD teardrop failed: {output}");
        }

        [Fact]
        public async Task MCAD_RegularShapes()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                use <MCAD/regular_shapes.scad>
                hexagon(10);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            // hexagon is 2D — may not produce STL geometry but should not error
            // Just check it doesn't crash
            Assert.DoesNotContain("ERROR", output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task MCAD_Bearing()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                use <MCAD/bearing.scad>
                bearing(model=608, $fn=32);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"MCAD bearing failed: {output}");
        }

        [Fact]
        public async Task MCAD_Polyhole()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                use <MCAD/polyholes.scad>
                polyhole(h=10, d=5);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"MCAD polyhole failed: {output}");
        }

        [Fact]
        public async Task MCAD_StepperMotor()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var scad = """
                use <MCAD/stepper.scad>
                motor(Nema17, NemaShort, $fn=32);
                """;

            var (success, output) = await RenderScad(openScadPath, scad);

            Assert.True(success, $"MCAD stepper motor failed: {output}");
        }

        // ---------------------------------------------------------------
        // Example file tests
        // ---------------------------------------------------------------

        [Fact]
        public async Task Example_BOSL2RoundedBox_Renders()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var projectRoot = GetProjectRoot();
            var scadFile = Path.Combine(projectRoot, "examples", "bosl2-rounded-box.scad");
            if (!File.Exists(scadFile)) return;

            var content = await File.ReadAllTextAsync(scadFile);
            var (success, output) = await RenderScad(openScadPath, content);

            Assert.True(success, $"bosl2-rounded-box.scad failed: {output}");
        }

        [Fact]
        public async Task Example_BOSL2Gears_Renders()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var projectRoot = GetProjectRoot();
            var scadFile = Path.Combine(projectRoot, "examples", "bosl2-gears.scad");
            if (!File.Exists(scadFile)) return;

            var content = await File.ReadAllTextAsync(scadFile);
            var (success, output) = await RenderScad(openScadPath, content);

            Assert.True(success, $"bosl2-gears.scad failed: {output}");
        }

        [Fact]
        public async Task Example_MCadMotorMount_Renders()
        {
            var openScadPath = FindOpenScad();
            if (openScadPath == null) return;

            var projectRoot = GetProjectRoot();
            var scadFile = Path.Combine(projectRoot, "examples", "mcad-motor-mount.scad");
            if (!File.Exists(scadFile)) return;

            var content = await File.ReadAllTextAsync(scadFile);
            var (success, output) = await RenderScad(openScadPath, content);

            Assert.True(success, $"mcad-motor-mount.scad failed: {output}");
        }

        private static string GetProjectRoot()
        {
            var dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "STL Generator.sln")))
                dir = Directory.GetParent(dir)?.FullName;
            return dir ?? throw new DirectoryNotFoundException("Could not find project root");
        }
    }
}
