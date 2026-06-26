shader_type canvas_item;

void fragment() {
    float dist = distance(UV, vec2(0.5, 0.5));
    
    // A perfectly smooth, wide radial gradient. 
    // This provides the CanvasGroup shader the perfect amount of alpha gradient 
    // to stretch out and "snap" into liquid trails when blobs get close to each other.
    float alpha = smoothstep(0.5, 0.0, dist);
    
    COLOR = vec4(COLOR.rgb, COLOR.a * alpha);
}
