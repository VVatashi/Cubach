#version 330 core

uniform samplerCube skyboxTexture;

in vec3 frag_position;

layout (location = 0) out vec4 out_color;

void main(void) {
  out_color = texture(skyboxTexture, frag_position);
}
