// -------------------- fragment.glsl -------------------- //

#version luma-dx

in vec2 texCoords;

uniform sampler2D text;
uniform vec3 textColour;

void main()
{
    float sample = texture(text, TexCoords).x;
    lx_FragColour = vec4(textColour, sample);
}  