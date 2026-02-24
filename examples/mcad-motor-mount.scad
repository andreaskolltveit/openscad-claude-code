// NEMA 17 stepper motor mounting bracket using MCAD
// Demonstrates MCAD motor module and practical bracket design

use <MCAD/stepper.scad>

$fn = 48;

wall = 4;
bracket_width = 50;
bracket_depth = 50;
bracket_height = 8;
nema17_hole_spacing = 31;  // NEMA 17 mounting holes: 31mm apart
shaft_hole_d = 23;         // NEMA 17 center boss diameter + clearance

module motor_bracket() {
    difference() {
        // Base plate
        translate([-bracket_width/2, -bracket_depth/2, 0])
            cube([bracket_width, bracket_depth, bracket_height]);

        // Center hole for motor shaft/boss
        translate([0, 0, -1])
            cylinder(h=bracket_height+2, d=shaft_hole_d);

        // NEMA 17 mounting holes (M3, 31mm spacing)
        for (x = [-1, 1], y = [-1, 1])
            translate([x * nema17_hole_spacing/2, y * nema17_hole_spacing/2, -1])
                cylinder(h=bracket_height+2, d=3.4);  // M3 clearance

        // Mounting slots on sides
        for (x = [-1, 1])
            translate([x * (bracket_width/2 - 6), 0, -1])
                hull() {
                    cylinder(h=bracket_height+2, d=4.5);
                    translate([0, 8, 0])
                        cylinder(h=bracket_height+2, d=4.5);
                }
    }
}

// Bracket
motor_bracket();

// Ghost motor for reference (transparent)
%translate([0, 0, bracket_height])
    motor(Nema17);
