shader_type spatial;

uniform float wave_height = 0.2;
uniform sampler2D example_texture : source_color;
uniform vec3 head_color = vec3(1.0, 0.0, 0.0);
uniform vec3 face_color = vec3(1.0, 1.0, 1.0);

void vertex() {
	// Called for every vertex the material is visible on.
	//VERTEX.y += sin(TIME * 5.0 + VERTEX.x * 10.0) * wave_height;
}

void fragment() {
	// Called for every pixel the material is visible on.
	vec3 wave_color;
	wave_color.r = (sin(TIME * 6.0 + VERTEX.x * 10.0) + 1.0) * 0.5;
	wave_color.g = (sin(TIME * 7.0 + VERTEX.x * 10.0) + 1.0) * 0.5;
	wave_color.b = (sin(TIME * 8.0 + VERTEX.x * 10.0) + 1.0) * 0.5;
	
	vec3 texture_color = texture(example_texture, UV).rgb;
	vec3 out_color = texture_color.r * head_color + texture_color.g * wave_color;
	
	ALBEDO = out_color;
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
