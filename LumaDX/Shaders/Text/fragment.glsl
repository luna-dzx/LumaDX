#version luma-dx

in vec2 TexCoords;

uniform sampler2D text;
uniform vec3 textColour;

void main()
{    
    vec4 sample = vec4(1.0, 1.0, 1.0, texture(text, TexCoords).r);
    lx_FragColour = vec4(textColour, 1.0) * sample;
}  