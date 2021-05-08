#version 330 core

uniform mat4 mvp;

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec4 in_color;

out vec4 frag_color;

void main(void) {
  frag_color = in_color;
  gl_Position = mvp * vec4(in_position, 1);
}
