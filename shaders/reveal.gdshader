shader_type canvas_item;

void fragment() {
	vec4 color = texture(TEXTURE, UV);
	// Rec. 601 luminance weights — perceptually correct greyscale
	float grey = dot(color.rgb, vec3(0.299, 0.587, 0.114)) * 0.8;
	COLOR = vec4(vec3(grey), color.a);
}
