shader_type canvas_item;

uniform vec4 color_start : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 color_end : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float flash_speed : hint_range(0.1, 10.0) = 1.0;
uniform float alpha : hint_range(0.0,1.0) = 0;

void vertex() {
    // Calculate the oscillation between color_start and color_end based on time
    float oscillation = 0.5 + 0.5 * sin(TIME * flash_speed);
    
    // Interpolate between color_start and color_end using the oscillation value
    COLOR.rgba = vec4(mix(color_start.rgb, color_end.rgb, oscillation), alpha);
}
