#version luma-dx

in vec2 TexCoords;

uniform sampler2D text;
uniform vec3 textColour;

void main()
{
    float sample = texture(text, TexCoords).r;
    lx_FragColour = vec4(textColour, sample);
}  