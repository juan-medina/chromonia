// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

shader_type canvas_item;
render_mode blend_premul_alpha;

// Applied to the group container holding all the fuzzy blobs.
// It looks at all the overlapping blurry circles and "snaps" their blur into a sharp, crisp edge.
// Because the blurs overlap, this snapping effect makes the blobs look like drops of liquid
// that perfectly merge and stick together (commonly known as a 'metaball' effect).
// It also makes the liquid bright and adds a glowing outline around the edges.

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

void fragment() {
    vec4 c = texture(screen_texture, SCREEN_UV);

    // CanvasGroups use pre-multiplied alpha (RGB gets darker as alpha fades).
    // We un-premultiply it to recover the pure, bright HDR color.
    vec3 color = c.rgb / max(c.a, 0.001);

    // Use a very tight smoothstep for a sharp, anti-aliased liquid edge.
    float alpha = smoothstep(0.48, 0.52, c.a);

    // Calculate a precise glowing edge just inside the threshold
    float edge = smoothstep(0.45, 0.50, c.a) - smoothstep(0.50, 0.55, c.a);
    color = mix(color, color * 2.5, edge);

    // Since we use blend_premul_alpha, Godot expects us to output a pre-multiplied color.
    // We multiply our recovered HDR color by our new sharp alpha!
    COLOR = vec4(color * alpha, alpha);
}
