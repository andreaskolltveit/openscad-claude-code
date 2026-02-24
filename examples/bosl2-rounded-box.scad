// Rounded electronics enclosure using BOSL2
// Demonstrates cuboid rounding, diff(), and screw holes

include <BOSL2/std.scad>
include <BOSL2/screws.scad>

$fn = 48;

wall = 2.5;
inner = [60, 40, 25];
outer = inner + [wall*2, wall*2, wall*2];
screw_inset = 6;

module enclosure_bottom() {
    diff()
        cuboid(outer, rounding=3, edges="Z", anchor=BOTTOM) {
            // Hollow inside
            tag("remove")
                up(wall)
                    cuboid(inner, anchor=BOTTOM);

            // Screw posts at corners
            for (xm = [-1, 1], ym = [-1, 1])
                tag("keep")
                    up(wall)
                        right(xm * (inner.x/2 - screw_inset))
                            fwd(ym * (inner.y/2 - screw_inset))
                                cyl(h=inner.z - 2, d=8, anchor=BOTTOM);

            // Screw holes in posts
            for (xm = [-1, 1], ym = [-1, 1])
                tag("remove")
                    up(wall)
                        right(xm * (inner.x/2 - screw_inset))
                            fwd(ym * (inner.y/2 - screw_inset))
                                cyl(h=inner.z, d=3.4, anchor=BOTTOM);
        }
}

module enclosure_lid() {
    lip = 1.5;

    union() {
        cuboid([outer.x, outer.y, wall], rounding=3, edges="Z", anchor=BOTTOM);
        // Lip for alignment
        up(0.01)
            cuboid([inner.x - 0.4, inner.y - 0.4, lip], anchor=TOP);
    }
}

// Bottom
enclosure_bottom();

// Lid (offset for preview)
right(outer.x + 10)
    enclosure_lid();
