// Parametric hex bolt — M8 x 30mm
// Demonstrates modules, for loops, and parametric design
// (For actual threads, use BOSL2: see examples/bosl2-rounded-box.scad)

$fn = 32;

bolt_diameter = 8;
bolt_length = 30;
head_height = 5;
hex_across_flats = 13;

module hex_head(across_flats, height) {
    // Regular hexagon from 3 rotated rectangles intersected
    intersection_for(i = [0:2]) {
        rotate([0, 0, 60 * i])
            cube([across_flats, across_flats * 2, height], center=true);
    }
}

module knurled_shaft(d, length, knurl_count=12, knurl_depth=0.3) {
    // Shaft with decorative knurl pattern using for-loop
    union() {
        cylinder(h=length, d=d);

        // Knurl ridges along the shaft
        for (i = [0 : knurl_count - 1])
            rotate([0, 0, i * 360 / knurl_count])
                translate([d/2, 0, 0])
                    cylinder(h=length, r=knurl_depth, $fn=4);
    }
}

module bolt(d, length, head_h, hex_af) {
    // Hex head
    translate([0, 0, length])
        hex_head(hex_af, head_h);

    // Shaft with chamfered tip
    union() {
        knurled_shaft(d, length);

        // Chamfer at tip
        cylinder(h=1.5, d1=d - 2, d2=d);
    }
}

bolt(
    d = bolt_diameter,
    length = bolt_length,
    head_h = head_height,
    hex_af = hex_across_flats
);
