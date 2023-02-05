#version 330 core

uniform sampler2D texture0;
uniform vec2 pixelateResolution;
in vec2 texCoords;

out vec4 fragColour;

void main()
{
    vec3 sample = texture(texture0, floor(texCoords * pixelateResolution) / pixelateResolution).rgb;
    float value = (sample.r + sample.g + sample.b) / 3.0;
    if (value > 0.63 && value < 0.86) {value = 0.86;}
    value += 0.235;
    fragColour = vec4(0.0,0.0,0.0,value);
}