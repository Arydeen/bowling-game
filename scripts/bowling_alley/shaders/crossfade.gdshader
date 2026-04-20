shader_type canvas_item;

uniform sampler2D target_texture; // The texture we are fading to
uniform float mix_weight : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    vec4 current_color = texture(TEXTURE, UV);
    vec4 target_color = texture(target_texture, UV);
    COLOR = mix(current_color, target_color, mix_weight);
}