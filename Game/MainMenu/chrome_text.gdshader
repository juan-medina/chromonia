shader_type canvas_item;

uniform float height = 160.0;
uniform sampler2D reflection_map;
varying float local_y;

void vertex() {
    local_y = VERTEX.y;
}

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    
    // Normalize Y
    float y = clamp(local_y / height, 0.0, 1.0);
    
    // Smooth metallic silver gradient (bright top, darker bottom)
    // No sharp lines or horizon cuts
    vec3 chrome = mix(vec3(1.0, 1.0, 1.0), vec3(0.25, 0.25, 0.25), y);
    
    // --- REAL-TIME BLOB REFLECTION ---
    // Simulate a 3D cylindrical surface for the reflection
    // normal_y goes from -1.0 (top edge) to 1.0 (bottom edge)
    float normal_y = (y - 0.5) * 2.0; 
    
    vec2 ref_uv = SCREEN_UV;
    
    // 1. Environment Scaling & Mirroring: 
    // Multiply by a negative value to flip the environment both horizontally and vertically,
    // while zooming it out slightly so it looks wrapped.
    ref_uv = (ref_uv - 0.5) * -1.2 + 0.5;
    
    // 2. Cylindrical Warp: Bend the reflection vertically based on the curve of the metal
    // This pushes the reflection up at the top, and down at the bottom.
    ref_uv.y += normal_y * 0.25; 
    
    // 3. Surface Imperfections: Add a static organic wave so the metal doesn't look perfectly flat
    ref_uv.x += sin(ref_uv.y * 6.0) * 0.03;
    
    // Sample the viewport texture containing the blobs
    vec4 blob_reflection = texture(reflection_map, ref_uv);
    
    // 4. Fresnel Effect: Reflections are physically brighter on the grazing edges of curved surfaces
    float fresnel = abs(normal_y); // 0.0 at center, 1.0 at the extreme top/bottom edges
    float reflection_strength = mix(0.7, 1.5, fresnel); 
    
    // Additive blend the brightly glowing blobs onto the metallic surface
    chrome += blob_reflection.rgb * blob_reflection.a * reflection_strength;
    
    // Fake bevel highlight on the edges of the font using the anti-aliasing alpha gradient
    float edge_highlight = (smoothstep(0.0, 0.5, tex.a) - smoothstep(0.5, 1.0, tex.a)) * 0.5;
    chrome += vec3(edge_highlight);
    
    // We only modify the RGB, preserving the exact alpha (anti-aliasing) of the font
    COLOR = vec4(chrome, tex.a);
}
