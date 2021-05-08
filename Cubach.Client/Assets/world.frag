#version 330 core

uniform sampler2D colorTexture;
uniform vec3 light;

in vec3 frag_position;
in vec3 frag_normal;
in vec2 frag_texCoord;

layout (location = 0) out vec4 out_color;

void main(void) {
  vec3 ambient = vec3(0.5);
  vec3 diffuse = max(0, dot(light, frag_normal)) * vec3(0.5);
  out_color = vec4(ambient + diffuse, 1) * texture(colorTexture, frag_texCoord);
}
