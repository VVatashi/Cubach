#version 330 core

uniform mat4 vp;

layout (location = 0) in vec3 in_position;

out vec3 frag_position;

void main(void) {
  frag_position = in_position;
  gl_Position = vp * vec4(in_position, 1);
}
