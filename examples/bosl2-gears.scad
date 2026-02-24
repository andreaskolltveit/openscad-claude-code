// Meshing spur gears using BOSL2
// Demonstrates gear module, attachment, and parametric design

include <BOSL2/std.scad>
include <BOSL2/gears.scad>

$fn = 64;

mod = 2;             // metric module
teeth_drive = 12;
teeth_driven = 24;
thickness = 8;
bore = 5;

// Calculate center distance for meshing
pitch_r1 = mod * teeth_drive / 2;
pitch_r2 = mod * teeth_driven / 2;
center_dist = pitch_r1 + pitch_r2;

// Drive gear
difference() {
    spur_gear(mod=mod, teeth=teeth_drive, thickness=thickness, pressure_angle=20);
    cyl(h=thickness+1, d=bore, center=true);
}

// Driven gear — offset and rotated to mesh
right(center_dist)
    rotate([0, 0, 180/teeth_driven])  // offset half tooth for meshing
        difference() {
            spur_gear(mod=mod, teeth=teeth_driven, thickness=thickness, pressure_angle=20);
            cyl(h=thickness+1, d=bore, center=true);
        }
