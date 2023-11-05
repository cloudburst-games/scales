shader_type canvas_item;

// ENABLE when using on light masks e.g. for wavy water reflection
// render_mode blend_mix,light_only;
uniform sampler2D noise1:repeat_enable;
uniform sampler2D noise2:repeat_enable;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;
uniform float speed: hint_range(0.01, 0.25) = 0.025;
uniform float magnitude1: hint_range(0.01, 0.25) = 0.025;
uniform float magnitude2: hint_range(0.01, 0.25) = 0.025;
uniform sampler2D mask;
uniform vec4 tint_color : source_color;
uniform float tint_strength: hint_range(0.01, 1.0) = 0.025;
uniform float transparency: hint_range(0.01, 1.0) = 1.0;

void fragment() {
	
	// Get the noise value by sampling the noise texture at specified UV coordinate and adjusting
	// for TIME to make it wavy. Multiply by magnitude to adjust the waviness
	float noise_val1 = texture(noise1, UV + TIME * speed).r * magnitude1;
	float noise_val2 = texture(noise2, UV + TIME * speed).r * magnitude2;
	
	// Get the mask value so we can mask the shader only to apply to specified (water) parts
	float mask_val = texture(mask, UV).a;
	
	// Start setting up our output. Subtract the noise value from each UV coordinate
	// E.g. at (0.5, 0.5) the UV might be (0.1,0.1) and we subtract e.g. 0.1
	// It displaces the image by the magnitude so we need to offset it by adding half the magnitude
	// (not sure why)
	vec4 col = texture(TEXTURE, UV - noise_val1 + magnitude1/2.0);
	col +=  (noise_val2 - magnitude2/2.0);
	// Adjust the colour by a specified tint
	col += tint_color * tint_strength;
	// Apply the mask. Where the mask alpha is 0, the output will be empty.
	col *= mask_val;
	// Make it transparent to a degree
	col.a *= transparency;
	
	COLOR = col;
}
