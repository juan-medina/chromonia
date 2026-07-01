// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

shader_type canvas_item;

uniform float blink_frequency : hint_range(0.1, 5.0) = 5.0;
uniform vec4 blink_color : source_color = vec4(1.0, 0.0, 0.0, 1.0);

void fragment() {
    // In Godot CanvasItem shaders, COLOR contains the vertex color (modulate * self_modulate)
    // We must multiply the texture by COLOR to apply the tint set in C#
    vec4 base_color = texture(TEXTURE, UV) * COLOR;
    float alpha = mod(TIME * blink_frequency, 1.0);

    if (alpha < 0.5) {
        COLOR = vec4(mix(base_color.rgb, blink_color.rgb, blink_color.a), base_color.a);
    } else {
        COLOR = base_color;
    }
}
