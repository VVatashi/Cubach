#version 330 core

uniform sampler2D colorTexture;
uniform vec3 light;

in vec3 frag_position;
in vec3 frag_normal;
in vec2 frag_texCoord;
in vec4 frag_eyeSpacePosition;

layout (location = 0) out vec4 out_color;

void main(void) {
  vec3 ambient = vec3(0.5);
  vec3 diffuse = max(0, dot(light, frag_normal)) * vec3(0.5);
  out_color = vec4(ambient + diffuse, 1) * texture(colorTexture, frag_texCoord);
  
  float distance = abs(frag_eyeSpacePosition.z / frag_eyeSpacePosition.w);

  float fogFactor = exp(-pow(0.005 * distance, 2.0));
  fogFactor = 1.0 - clamp(fogFactor, 0.0, 1.0);

  out_color = mix(out_color, vec4(0.7, 0.83, 0.99, 1), fogFactor);
}
