#version 330 core

uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec3 in_normal;
layout (location = 2) in vec2 in_texCoord;

out vec3 frag_position;
out vec3 frag_normal;
out vec2 frag_texCoord;
out vec4 frag_eyeSpacePosition;

void main(void) {
  mat4 modelViewMatrix = viewMatrix * modelMatrix;
  mat4 modelViewProjectionMatrix = projectionMatrix * viewMatrix * modelMatrix;

  frag_position = in_position;
  frag_normal = in_normal;
  frag_texCoord = in_texCoord;
  frag_eyeSpacePosition = modelViewMatrix * vec4(in_position, 1);

  gl_Position = modelViewProjectionMatrix * vec4(in_position, 1);
}
