// Spur gear — demonstrates trigonometry and polygon/linear_extrude
// Involute tooth approximation

$fn = 64;

module_size = 2;       // gear module (tooth size)
num_teeth = 20;
pressure_angle = 20;   // degrees
gear_thickness = 8;
bore_diameter = 8;
hub_diameter = 16;
hub_height = 4;

module gear_2d(m, teeth, pa) {
    pitch_r = m * teeth / 2;
    addendum = m;
    dedendum = m * 1.25;
    outer_r = pitch_r + addendum;
    root_r = pitch_r - dedendum;
    base_r = pitch_r * cos(pa);
    tooth_angle = 360 / teeth;

    // Approximate involute tooth profile using polygon
    function involute_point(r, base_r) =
        let(angle = acos(base_r / r))
        let(roll = sqrt(r * r - base_r * base_r) / base_r)
        let(inv_angle = roll * 180 / PI - angle)
        [r * cos(inv_angle), r * sin(inv_angle)];

    difference() {
        union() {
            for (i = [0 : teeth - 1]) {
                rotate([0, 0, i * tooth_angle])
                    polygon([
                        [0, 0],
                        for (r = [root_r : 0.5 : outer_r])
                            let(a = acos(min(1, base_r / r)))
                            let(roll_a = (sqrt(r*r - base_r*base_r) / base_r) * 180 / PI - a)
                            [r * cos(roll_a + tooth_angle/4),
                             r * sin(roll_a + tooth_angle/4)],
                        for (r = [outer_r : -0.5 : root_r])
                            let(a = acos(min(1, base_r / r)))
                            let(roll_a = (sqrt(r*r - base_r*base_r) / base_r) * 180 / PI - a)
                            [r * cos(-roll_a + tooth_angle/4),
                             r * sin(-roll_a + tooth_angle/4)],
                    ]);
            }
        }
        // Root circle cleanup
        circle(r=root_r - 0.1);
    }

    // Root disk
    circle(r=root_r);
}

module gear(m, teeth, pa, thickness, bore_d, hub_d, hub_h) {
    difference() {
        union() {
            // Gear body
            linear_extrude(thickness)
                gear_2d(m, teeth, pa);

            // Hub
            cylinder(h=thickness + hub_h, d=hub_d);
        }

        // Bore hole
        translate([0, 0, -0.5])
            cylinder(h=thickness + hub_h + 1, d=bore_d);
    }
}

gear(
    m = module_size,
    teeth = num_teeth,
    pa = pressure_angle,
    thickness = gear_thickness,
    bore_d = bore_diameter,
    hub_d = hub_diameter,
    hub_h = hub_height
);
