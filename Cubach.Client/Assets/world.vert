#version 330
uniform mat4 mvp;

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec3 in_normal;
layout (location = 2) in vec2 in_texCoord;

out vec3 frag_position;
out vec3 frag_normal;
out vec2 frag_texCoord;

void main(void) {
  frag_position = in_position;
  frag_normal = in_normal;
  frag_texCoord = in_texCoord;
  gl_Position = mvp * vec4(in_position, 1);
}
