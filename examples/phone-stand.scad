// Phone stand — adjustable viewing angle
// Fits phones up to 80mm wide, 12mm thick

$fn = 48;

phone_width = 80;
phone_thickness = 12;
stand_depth = 60;
stand_height = 80;
wall = 3;
angle = 70; // viewing angle from horizontal
lip_height = 10;

module phone_stand() {
    difference() {
        union() {
            // Base plate
            cube([phone_width + wall * 2, stand_depth, wall]);

            // Back support
            translate([0, stand_depth - wall, 0])
                rotate([90 - angle, 0, 0])
                    cube([phone_width + wall * 2, stand_height, wall]);

            // Front lip
            cube([phone_width + wall * 2, wall, lip_height]);

            // Side walls
            cube([wall, stand_depth, lip_height]);
            translate([phone_width + wall, 0, 0])
                cube([wall, stand_depth, lip_height]);
        }

        // Phone slot
        translate([wall, wall, wall])
            cube([phone_width, phone_thickness + 1, lip_height + 1]);

        // Cable hole
        translate([(phone_width + wall * 2) / 2, stand_depth - wall - 1, -0.5])
            cylinder(h=wall + 1, d=15);
    }
}

phone_stand();
