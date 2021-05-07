#version 330
in vec4 frag_color;

layout (location = 0) out vec4 out_color;

void main(void) {
  out_color = frag_color;
}
