shader_type canvas_item;

uniform float bg_min_brightness : hint_range(0.0, 1.0) = 0.1;
uniform float bg_max_brightness : hint_range(0.0, 1.0) = 0.35;
uniform sampler2D mask_texture : hint_default_transparent;

const vec3 LUMINANCE_WEIGHTS = vec3(0.299, 0.587, 0.114);

void fragment() {
    vec4 painting = texture(TEXTURE, UV);
    vec4 mask = texture(mask_texture, UV);

    // Calculate the original brightness (0.0 to 1.0)
    float luma = dot(painting.rgb, LUMINANCE_WEIGHTS);

    // equilize the brightness to a range between bg_min_brightness and bg_max_brightness
    float grey = mix(bg_min_brightness, bg_max_brightness, luma);
    vec3 grayscale = vec3(grey);

    vec3 tinted = painting.rgb * mask.rgb;

    vec3 result = mix(grayscale, tinted, mask.a);

    COLOR = vec4(result, painting.a);
}
