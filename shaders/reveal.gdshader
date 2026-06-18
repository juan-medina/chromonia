shader_type canvas_item;

uniform sampler2D mask_texture : hint_default_black;
uniform float tint_strength : hint_range(0.0, 1.0) = 0.4;

const vec3 LUMINANCE_WEIGHTS = vec3(0.299, 0.587, 0.114);

uniform vec3 ZONE_FREE    = vec3(0.0, 0.0, 0.0);
uniform vec3 ZONE_CLAIMED = vec3(1.0, 1.0, 1.0);

void fragment() {
	vec4 painting = texture(TEXTURE, UV);
	vec3 mask = texture(mask_texture, UV).rgb;

	vec3 result;

	if( mask == ZONE_FREE) {
		float grey = dot(painting.rgb, LUMINANCE_WEIGHTS) * 0.8;
		result = vec3(grey);
	}else if (mask == ZONE_CLAIMED) {
		result = painting.rgb;
	}else {
		result = mix(painting.rgb, mask.rgb, tint_strength);
	}

	COLOR = vec4(result, painting.a);
}
