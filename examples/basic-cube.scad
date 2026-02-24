// Calibration cube — 20mm on each side
// Use this to verify printer dimensional accuracy

$fn = 32;

difference() {
    cube([20, 20, 20], center=true);

    // X-axis label
    translate([0, -10.5, 0])
        rotate([90, 0, 0])
            linear_extrude(1)
                text("X", size=8, halign="center", valign="center");

    // Y-axis label
    translate([10.5, 0, 0])
        rotate([90, 0, 90])
            linear_extrude(1)
                text("Y", size=8, halign="center", valign="center");

    // Z-axis label
    translate([0, 0, 10.5])
        linear_extrude(1)
            text("Z", size=8, halign="center", valign="center");
}
