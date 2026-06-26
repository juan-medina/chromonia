shader_type canvas_item;

uniform float bg_min_brightness : hint_range(0.0, 1.0) = 0.1;
uniform float bg_max_brightness : hint_range(0.0, 1.0) = 0.35;
uniform sampler2D mask_texture : hint_default_black;
uniform float tint_strength : hint_range(0.0, 1.0) = 0.4;

const vec3 LUMINANCE_WEIGHTS = vec3(0.299, 0.587, 0.114);

uniform vec3 ZONE_FREE = vec3(0.0, 0.0, 0.0);
uniform vec3 ZONE_CLAIMED = vec3(1.0, 1.0, 1.0);

void fragment() {
    vec4 painting = texture(TEXTURE, UV);
    vec3 mask = texture(mask_texture, UV).rgb;

    vec3 result;

    if (mask == ZONE_FREE) {
        // Calculate the original brightness (0.0 to 1.0)
        float luma = dot(painting.rgb, LUMINANCE_WEIGHTS);

        // equilize the brightness to a range between bg_min_brightness and bg_max_brightness
        float grey = mix(bg_min_brightness, bg_max_brightness, luma);

        result = vec3(grey);
    } else if (mask == ZONE_CLAIMED) {
        result = painting.rgb;
    } else {
        result = mix(painting.rgb, mask.rgb, tint_strength);
    }

    COLOR = vec4(result, painting.a);
}
